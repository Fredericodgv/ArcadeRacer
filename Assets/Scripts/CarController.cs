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
    [Tooltip("Arraste o Rigidbody do carro aqui (ou deixe vazio para pegar automaticamente)")]
    [SerializeField] private Rigidbody rb;
    [Tooltip("Objeto visual filho para inclinar nas curvas (ex: o cubo ou modelo 3D)")]
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

    // Estado Interno
    private bool isGrounded;
    private RaycastHit groundHit;
    private float currentSpeed;
    private bool isBoosting;
    private float boostTimer;

    // Cache de Ações do Unity 6
    private InputAction accelerateAction;
    private InputAction brakeAction;
    private InputAction steerAction;
    private InputAction driftAction;
    private InputAction airTrickAction;
    private InputAction airControlAction;
    private InputAction megaBoostAction;

    // Getters Públicos para HUD / Efeitos
    public float CurrentSpeed => currentSpeed;
    public float CurrentSpeedKmh => currentSpeed * 3.6f;
    public float SpeedNormalized => Mathf.Clamp01(Mathf.Abs(currentSpeed) / (maxSpeed * (isBoosting ? boostMultiplier : 1f)));
    public bool IsGrounded => isGrounded;
    public bool IsDrifting => isGrounded && driftAction != null && driftAction.IsPressed() && Mathf.Abs(steerAction != null ? steerAction.ReadValue<float>() : 0f) > 0.1f && currentSpeed > 4f;
    public bool IsBoosting => isBoosting;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (carBodyModel == null && transform.childCount > 0) carBodyModel = transform.GetChild(0);

        CacheInputActions();
    }

    /// <summary>
    /// Localiza e armazena em cache as referências das ações do mapa 'Player'
    /// registradas globalmente no Project-Wide Input do Unity 6.
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

    private void Update()
    {
        if (megaBoostAction != null && megaBoostAction.WasPressedThisFrame())
        {
            TriggerMegaBoost();
        }

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

    /// <summary>
    /// Executa um Raycast para baixo a partir do centro do veículo para detectar
    /// se o carro está em contato com o solo e identificar a normal da superfície.
    /// </summary>
    private void CheckGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        isGrounded = Physics.Raycast(origin, -transform.up, out groundHit, groundCheckDistance + 0.5f, groundLayer, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// Processa a movimentação longitudinal do veículo (aceleração para frente e marcha ré/frenagem).
    /// Unifica a aplicação de força com atenuação progressiva contínua próxima aos limites de velocidade
    /// tanto para frente (maxSpeed) quanto para ré (maxReverseSpeed), eliminando qualquer tremor (flickering).
    /// As entradas de aceleração e freio/ré se anulam automaticamente quando pressionadas simultaneamente (netInput = accel - brake),
    /// além de aplicar frenagem ativa quando o sentido do input for oposto ao vetor de deslocamento atual.
    /// </summary>
    private void HandleLongitudinalMovement()
    {
        currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        if (!isGrounded) return;

        float accelInput = accelerateAction != null ? accelerateAction.ReadValue<float>() : 0f;
        float brakeInput = brakeAction != null ? brakeAction.ReadValue<float>() : 0f;

        // Anulação automática: calcula o input líquido (se ambos forem pressionados juntos, netInput = 0)
        float netInput = accelInput - brakeInput;
        float effectiveMaxSpeed = maxSpeed * (isBoosting ? boostMultiplier : 1f);

        // 1. Movimento Frontal (netInput positivo)
        if (netInput > 0.01f)
        {
            if (currentSpeed >= -0.5f)
            {
                // Aceleração para frente com atenuação suave perto da velocidade máxima
                float speedRatio = Mathf.Max(0f, currentSpeed) / effectiveMaxSpeed;
                float powerFactor = Mathf.Clamp01(1f - speedRatio);

                if (powerFactor > 0.001f)
                {
                    rb.AddForce(transform.forward * (netInput * motorForce * powerFactor), ForceMode.Acceleration);
                }
            }
            else
            {
                // Estava em ré: freio ativo contra o movimento para trás até parar
                rb.AddForce(transform.forward * (netInput * brakeForce), ForceMode.Acceleration);
            }
        }
        // 2. Movimento Traseiro / Frenagem (netInput negativo)
        else if (netInput < -0.01f)
        {
            float reverseStrength = -netInput; // Intensidade positiva do comando de ré/freio

            if (currentSpeed > 0.5f)
            {
                // Em movimento para frente: freio ativo contra o movimento
                rb.AddForce(-transform.forward * (reverseStrength * brakeForce), ForceMode.Acceleration);
            }
            else
            {
                // Parado ou em ré: aceleração para trás com atenuação suave perto de maxReverseSpeed (zero flickering)
                float reverseSpeedRatio = Mathf.Abs(Mathf.Min(0f, currentSpeed)) / maxReverseSpeed;
                float reversePowerFactor = Mathf.Clamp01(1f - reverseSpeedRatio);

                if (reversePowerFactor > 0.001f)
                {
                    rb.AddForce(-transform.forward * (reverseStrength * (motorForce * 0.6f) * reversePowerFactor), ForceMode.Acceleration);
                }
            }
        }

        // 3. Downforce dinâmico proporcional à velocidade para manter o carro estável
        if (Mathf.Abs(currentSpeed) > 2f)
        {
            Vector3 normal = groundHit.normal.sqrMagnitude > 0.1f ? groundHit.normal : Vector3.up;
            rb.AddForce(-normal * (downforce * Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed)), ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// Controla o esterço proporcional à velocidade do veículo (bloqueado quando parado),
    /// aplica alinhamento suave com a inclinação do terreno em uma única rotação combinada
    /// e amortece o vetor de velocidade lateral (fricção/grip vs drift) integrado ao Time.fixedDeltaTime.
    /// </summary>
    private void HandleSteeringAndGrip()
    {
        if (!isGrounded) return;

        float absSpeed = Mathf.Abs(currentSpeed);
        float steer = steerAction != null ? steerAction.ReadValue<float>() : 0f;
        bool isDrifting = driftAction != null && driftAction.IsPressed();

        // Parado: cancela esterço e qualquer deslizamento lateral residual
        if (absSpeed < 0.15f)
        {
            Vector3 stoppedSideVelocity = Vector3.Dot(rb.linearVelocity, transform.right) * transform.right;
            rb.linearVelocity -= stoppedSideVelocity * Mathf.Clamp01(20f * Time.fixedDeltaTime);
            return;
        }

        float turnFactor = Mathf.Clamp01(absSpeed / minTurnSpeed) * steerSpeedCurve.Evaluate(Mathf.Clamp01(absSpeed / maxSpeed));
        if (isDrifting && Mathf.Abs(steer) > 0.05f) turnFactor *= driftSteerMultiplier;

        float dirSign = currentSpeed >= 0f ? 1f : -1f;
        float steerAmount = steer * steerSpeed * turnFactor * dirSign * Time.fixedDeltaTime;

        Quaternion turnRotation = Quaternion.Euler(0f, steerAmount, 0f);
        Quaternion targetRot = rb.rotation * turnRotation;

        Vector3 normal = groundHit.normal.sqrMagnitude > 0.1f ? groundHit.normal : Vector3.up;
        Quaternion targetGround = Quaternion.FromToRotation(transform.up, normal) * targetRot;
        rb.MoveRotation(Quaternion.Slerp(targetRot, targetGround, groundAlignSpeed * Time.fixedDeltaTime));

        // Amortecimento lateral estável integrado ao tempo para evitar oscilações
        float gripRate = isDrifting ? (driftGrip * 25f) : (lateralGrip * 45f);
        Vector3 sideVelocity = Vector3.Dot(rb.linearVelocity, transform.right) * transform.right;
        rb.linearVelocity -= sideVelocity * Mathf.Clamp01(gripRate * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Gerencia o comportamento do carro no ar: quando 'AirTrick' está ativo, permite rotações acrobáticas
    /// de Pitch (flips) e Roll (giros em barril); quando desativado, auto-estabiliza o veículo alinhando-o com o mundo.
    /// </summary>
    private void HandleAirControl()
    {
        if (isGrounded) return;

        bool airTrick = airTrickAction != null && airTrickAction.IsPressed();
        Vector2 airInput = airControlAction != null ? airControlAction.ReadValue<Vector2>() : Vector2.zero;

        if (airTrick && airInput.sqrMagnitude > 0.05f)
        {
            Quaternion airRot = Quaternion.Euler(airInput.y * airPitchSpeed * Time.fixedDeltaTime, 0f, -airInput.x * airRollSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(rb.rotation * airRot);
        }
        else
        {
            Quaternion targetUp = Quaternion.FromToRotation(transform.up, Vector3.up) * rb.rotation;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetUp, airStabilizeSpeed * Time.fixedDeltaTime));
        }
    }

    /// <summary>
    /// Ativa o Mega Boost, aplicando um impulso instantâneo na direção frontal
    /// e habilitando o multiplicador temporário de velocidade máxima.
    /// </summary>
    public void TriggerMegaBoost()
    {
        if (isBoosting) return;
        isBoosting = true;
        boostTimer = boostDuration;
        rb.AddForce(transform.forward * boostImpulseForce, ForceMode.Impulse);
    }

    /// <summary>
    /// Atualiza o temporizador do Mega Boost e encerra o estado ao expirar a duração.
    /// </summary>
    private void UpdateBoost()
    {
        if (!isBoosting) return;
        boostTimer -= Time.deltaTime;
        if (boostTimer <= 0f) isBoosting = false;
    }

    /// <summary>
    /// Aplica uma inclinação sutil (body roll) no modelo visual filho durante curvas fechadas
    /// para feedback estético sem comprometer a raiz física do Rigidbody.
    /// </summary>
    private void UpdateBodyVisuals()
    {
        if (carBodyModel == null) return;
        float steer = steerAction != null ? steerAction.ReadValue<float>() : 0f;
        float targetRoll = isGrounded ? (-steer * bodyRollAngle) : 0f;
        carBodyModel.localRotation = Quaternion.Slerp(carBodyModel.localRotation, Quaternion.Euler(0f, 0f, targetRoll), bodyRollSpeed * Time.deltaTime);
    }
}
