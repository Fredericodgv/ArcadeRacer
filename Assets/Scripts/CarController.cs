using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador de física arcade para o veículo.
/// Gerencia aceleração progressiva, frenagem, aderência lateral (grip),
/// mecânica de drift, manobras aéreas (air tricks), estabilização e Mega Boost.
/// </summary>
public class CarController : MonoBehaviour
{
    [Header("Componentes")]
    [Tooltip("Rigidbody do carro (preenchido automaticamente se vazio)")]
    [SerializeField] private Rigidbody rb;
    [Tooltip("Objeto visual filho para inclinar nas curvas (ex: o modelo 3D)")]
    [SerializeField] private Transform carBodyModel;

    [Header("Motor & Velocidade")]
    [SerializeField] private float motorForce = 2200f;
    [SerializeField] private float brakeForce = 3500f;
    [SerializeField] private float maxSpeed = 50f;
    [SerializeField] private float maxReverseSpeed = 18f;

    [Header("Direção & Curvas")]
    [SerializeField] private float minTurnSpeed = 1.5f;
    [SerializeField] private float steerSpeed = 110f;
    [Range(0.5f, 1f)]
    [SerializeField] private float lateralGrip = 0.98f;
    [SerializeField] private AnimationCurve steerSpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.7f);

    [Header("Drift (Derrapagem)")]
    [Range(0.1f, 0.8f)]
    [SerializeField] private float driftGrip = 0.45f;
    [SerializeField] private float driftSteerMultiplier = 1.4f;

    [Header("Mega Boost")]
    [SerializeField] private float boostMultiplier = 1.7f;
    [SerializeField] private float boostDuration = 3.5f;
    [SerializeField] private float boostImpulseForce = 1200f;

    [Header("Aderência & Solo")]
    [SerializeField] private float downforce = 20f;
    [SerializeField] private float groundCheckDistance = 1.6f;
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float groundAlignSpeed = 8f;

    [Header("Controle Aéreo (Air Tricks)")]
    [SerializeField] private float airPitchSpeed = 170f;
    [SerializeField] private float airRollSpeed = 200f;
    [SerializeField] private float airStabilizeSpeed = 4f;

    [Header("Visual")]
    [SerializeField] private float bodyRollAngle = 6f;
    [SerializeField] private float bodyRollSpeed = 10f;

    // Estado interno
    private bool isGrounded;
    private RaycastHit groundHit;
    private float currentSpeed;
    private bool isBoosting;
    private float boostTimer;

    // Cache de ações do Input System (Unity 6 Project-Wide Actions)
    private InputAction accelerateAction;
    private InputAction brakeAction;
    private InputAction steerAction;
    private InputAction driftAction;
    private InputAction airTrickAction;
    private InputAction airControlAction;
    private InputAction megaBoostAction;

    // Getters públicos para HUD / VFX externos
    public float CurrentSpeed => currentSpeed;
    public float CurrentSpeedKmh => currentSpeed * 3.6f;
    public float SpeedNormalized => Mathf.Clamp01(Mathf.Abs(currentSpeed) / (maxSpeed * (isBoosting ? boostMultiplier : 1f)));
    public bool IsGrounded => isGrounded;
    public bool IsBoosting => isBoosting;
    public bool IsDrifting => isGrounded
                                     && driftAction != null && driftAction.IsPressed()
                                     && steerAction != null && Mathf.Abs(steerAction.ReadValue<float>()) > 0.1f
                                     && currentSpeed > 4f;

    #region Inicialização

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        // Garante que carBodyModel seja sempre um filho, nunca a raiz física
        if (carBodyModel == transform) carBodyModel = null;
        if (carBodyModel == null && transform.childCount > 0) carBodyModel = transform.GetChild(0);

        CacheInputActions();
    }

    /// <summary>
    /// Localiza e armazena em cache as ações do mapa 'Player' registradas globalmente
    /// no Project-Wide Input do Unity 6, evitando o uso de componente PlayerInput.
    /// </summary>
    private void CacheInputActions()
    {
        var actions = InputSystem.actions;
        if (actions == null) return;

        accelerateAction = actions.FindAction("Player/Accelerate");
        brakeAction = actions.FindAction("Player/Brake");
        steerAction = actions.FindAction("Player/Steer");
        driftAction = actions.FindAction("Player/Drift");
        airTrickAction = actions.FindAction("Player/AirTrick");
        airControlAction = actions.FindAction("Player/AirControl");
        megaBoostAction = actions.FindAction("Player/MegaBoost");
    }

    #endregion

    #region Loop Principal

    private void Update()
    {
        if (megaBoostAction != null && megaBoostAction.WasPressedThisFrame())
            TriggerMegaBoost();

        UpdateBoost();
        UpdateBodyVisuals();
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        HandleLongitudinalMovement();
        HandleSteeringAndGrip();
        HandleAirControl();
    }

    #endregion

    #region Física

    /// <summary>
    /// Raycast curto para baixo a partir do centro do chassi para detectar contato com o solo
    /// e capturar a normal da superfície para alinhamento de terreno.
    /// </summary>
    private void CheckGrounded()
    {
        Vector3 origin = transform.position + transform.up * 0.5f;
        isGrounded = Physics.Raycast(origin, -transform.up, out groundHit, groundCheckDistance + 0.5f, groundLayer, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// Aplica força longitudinal (frente/ré) com atenuação progressiva próxima aos limites de velocidade,
    /// eliminando flickering. Inputs de acelerar e freio se anulam quando pressionados juntos (netInput = accel - brake).
    /// Aplica também downforce dinâmico proporcional à velocidade para estabilidade no solo.
    /// </summary>
    private void HandleLongitudinalMovement()
    {
        currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        if (!isGrounded) return;

        float accelInput = accelerateAction?.ReadValue<float>() ?? 0f;
        float brakeInput = brakeAction?.ReadValue<float>() ?? 0f;
        float netInput = accelInput - brakeInput; // anulação automática ao pressionar os dois juntos
        float effectiveMaxSpeed = maxSpeed * (isBoosting ? boostMultiplier : 1f);

        if (netInput > 0.01f)
        {
            if (currentSpeed >= -0.5f)
            {
                // Frente: atenuação suave perto da velocidade máxima (sem cortes bruscos)
                float powerFactor = Mathf.Clamp01(1f - Mathf.Max(0f, currentSpeed) / effectiveMaxSpeed);
                rb.AddForce(transform.forward * (netInput * motorForce * powerFactor), ForceMode.Acceleration);
            }
            else
            {
                // Estava em ré: freio ativo
                rb.AddForce(transform.forward * (netInput * brakeForce), ForceMode.Acceleration);
            }
        }
        else if (netInput < -0.01f)
        {
            float strength = -netInput;
            if (currentSpeed > 0.5f)
            {
                // Em movimento para frente: freio ativo
                rb.AddForce(-transform.forward * (strength * brakeForce), ForceMode.Acceleration);
            }
            else
            {
                // Ré: atenuação suave perto de maxReverseSpeed (sem flickering)
                float powerFactor = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Min(0f, currentSpeed)) / maxReverseSpeed);
                rb.AddForce(-transform.forward * (strength * motorForce * 0.6f * powerFactor), ForceMode.Acceleration);
            }
        }

        // Downforce proporcional à velocidade para manter o carro aderido ao solo
        float speedAbs = Mathf.Abs(currentSpeed);
        if (speedAbs > 2f)
        {
            Vector3 normal = groundHit.normal.sqrMagnitude > 0.1f ? groundHit.normal : Vector3.up;
            rb.AddForce(-normal * (downforce * Mathf.Clamp01(speedAbs / maxSpeed)), ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// Esterço proporcional à velocidade (bloqueado quando parado), alinhamento suave com a normal
    /// do terreno e amortecimento lateral (grip vs drift) integrado ao Time.fixedDeltaTime.
    /// Toda a rotação é aplicada em uma única chamada MoveRotation para evitar micro-jitter.
    /// </summary>
    private void HandleSteeringAndGrip()
    {
        if (!isGrounded) return;

        float absSpeed = Mathf.Abs(currentSpeed);
        float steer = steerAction?.ReadValue<float>() ?? 0f;
        bool isDrifting = driftAction != null && driftAction.IsPressed();

        // Parado: apenas cancela deslizamento lateral residual
        if (absSpeed < 0.15f)
        {
            Vector3 sideVel = Vector3.Dot(rb.linearVelocity, transform.right) * transform.right;
            rb.linearVelocity -= sideVel * Mathf.Clamp01(20f * Time.fixedDeltaTime);
            return;
        }

        float curveFactor = steerSpeedCurve.Evaluate(Mathf.Clamp01(absSpeed / maxSpeed));
        float turnFactor = Mathf.Clamp01(absSpeed / minTurnSpeed) * curveFactor;
        if (isDrifting && Mathf.Abs(steer) > 0.05f) turnFactor *= driftSteerMultiplier;

        float dirSign = currentSpeed >= 0f ? 1f : -1f;
        float steerAmount = steer * steerSpeed * turnFactor * dirSign * Time.fixedDeltaTime;

        Quaternion turnRot = rb.rotation * Quaternion.Euler(0f, steerAmount, 0f);
        Vector3 normal = groundHit.normal.sqrMagnitude > 0.1f ? groundHit.normal : Vector3.up;
        Quaternion groundRot = Quaternion.FromToRotation(transform.up, normal) * turnRot;
        rb.MoveRotation(Quaternion.Slerp(turnRot, groundRot, groundAlignSpeed * Time.fixedDeltaTime));

        float gripRate = isDrifting ? (driftGrip * 25f) : (lateralGrip * 45f);
        Vector3 sideVelocity = Vector3.Dot(rb.linearVelocity, transform.right) * transform.right;
        rb.linearVelocity -= sideVelocity * Mathf.Clamp01(gripRate * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Quando 'AirTrick' (A/Espaço) está pressionado no ar, aplica Pitch (frente/trás) e Roll (giros laterais)
    /// no espaço local do carro via pós-multiplicação de Quaternion, permitindo mortais e giros de 360° contínuos.
    /// Sem o botão, auto-estabiliza suavemente o carro em posição ereta.
    /// </summary>
    private void HandleAirControl()
    {
        if (isGrounded) return;

        bool airTrick = airTrickAction != null && airTrickAction.IsPressed();
        Vector2 airInput = airControlAction?.ReadValue<Vector2>() ?? Vector2.zero;

        if (airTrick && airInput.sqrMagnitude > 0.05f)
        {
            float pitchAngle = airInput.y * airPitchSpeed * Time.fixedDeltaTime;
            float rollAngle = -airInput.x * airRollSpeed * Time.fixedDeltaTime;

            // Pós-multiplicação: aplica no espaço LOCAL do veículo (imune a Gimbal Lock do Inspector)
            Quaternion deltaRot = Quaternion.AngleAxis(pitchAngle, Vector3.right)
                                * Quaternion.AngleAxis(rollAngle, Vector3.forward);
            rb.MoveRotation(rb.rotation * deltaRot);
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            Quaternion upright = Quaternion.FromToRotation(transform.up, Vector3.up) * rb.rotation;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, upright, airStabilizeSpeed * Time.fixedDeltaTime));
        }
    }

    #endregion

    #region Boost & Visuals

    /// <summary>
    /// Ativa o Mega Boost: impulso instantâneo para frente + multiplicador de velocidade máxima por boostDuration segundos.
    /// </summary>
    public void TriggerMegaBoost()
    {
        if (isBoosting) return;
        isBoosting = true;
        boostTimer = boostDuration;
        rb.AddForce(transform.forward * boostImpulseForce, ForceMode.Impulse);
    }

    /// <summary>
    /// Atualiza o temporizador do Mega Boost, encerrando o estado ao expirar.
    /// </summary>
    private void UpdateBoost()
    {
        if (!isBoosting) return;
        boostTimer -= Time.deltaTime;
        if (boostTimer <= 0f) isBoosting = false;
    }

    /// <summary>
    /// Aplica body roll cosmético no modelo visual filho durante curvas, sem afetar a raiz física.
    /// </summary>
    private void UpdateBodyVisuals()
    {
        if (carBodyModel == null || carBodyModel == transform) return;
        float steer = steerAction?.ReadValue<float>() ?? 0f;
        float targetRoll = isGrounded ? (-steer * bodyRollAngle) : 0f;
        carBodyModel.localRotation = Quaternion.Slerp(
            carBodyModel.localRotation,
            Quaternion.Euler(0f, 0f, targetRoll),
            bodyRollSpeed * Time.deltaTime);
    }

    #endregion
}
