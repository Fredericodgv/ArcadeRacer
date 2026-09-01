using UnityEngine;

/// <summary>
/// Câmera de perseguição arcade em 3ª pessoa.
/// Executada em LateUpdate com amortecimento angular suave (sem trepidação)
/// e FOV dinâmico responsivo à velocidade e ao Mega Boost.
/// </summary>
public class ArcadeCameraController : MonoBehaviour
{
    [Header("Alvo")]
    [Tooltip("Transform do veículo a ser seguido")]
    [SerializeField] private Transform target;
    [Tooltip("Referência opcional ao CarController para leitura de velocidade e boost")]
    [SerializeField] private CarController carController;

    [Header("Distâncias")]
    [Tooltip("Distância horizontal atrás do carro")]
    [SerializeField] private float distance = 6f;
    [Tooltip("Altura da câmera acima do carro")]
    [SerializeField] private float height = 2.2f;
    [Tooltip("Altura do ponto focal no carro")]
    [SerializeField] private float lookAtHeight = 0.8f;

    [Header("Suavização")]
    [Tooltip("Suavização da rotação horizontal")]
    [SerializeField] private float rotationDamping = 5f;
    [Tooltip("Suavização da altura")]
    [SerializeField] private float heightDamping = 5f;

    [Header("FOV Dinâmico (Sensação de Velocidade)")]
    [Tooltip("FOV padrão com o carro parado")]
    [SerializeField] private float baseFov = 60f;
    [Tooltip("FOV ao atingir a velocidade máxima normal")]
    [SerializeField] private float maxSpeedFov = 75f;
    [Tooltip("FOV máximo durante o Mega Boost")]
    [SerializeField] private float megaBoostFov = 88f;
    [Tooltip("Velocidade de transição suave do FOV")]
    [SerializeField] private float fovDamping = 4f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (target != null && carController == null)
        {
            carController = target.GetComponent<CarController>();
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        UpdateCameraTransform();
        UpdateDynamicFov();
    }

    /// <summary>
    /// Calcula a posição e rotação da câmera atrás do veículo usando interpolação angular estável (Mathf.LerpAngle),
    /// isolando vibrações da física e mantendo a orientação alinhada suavemente com o trajeto.
    /// </summary>
    private void UpdateCameraTransform()
    {
        // Ângulo e altura desejados baseados no carro
        float wantedRotationAngle = target.eulerAngles.y;
        float wantedHeight = target.position.y + height;

        // Ângulo e altura atuais da câmera
        float currentRotationAngle = transform.eulerAngles.y;
        float currentHeight = transform.position.y;

        // Amortecimento suave de rotação (LerpAngle evita problemas no salto 0-360) e altura
        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.deltaTime);
        currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.deltaTime);

        // Converte a rotação suavizada em vetor de deslocamento
        Quaternion currentRotation = Quaternion.Euler(0f, currentRotationAngle, 0f);

        // Posiciona a câmera atrás do alvo
        Vector3 newPosition = target.position;
        newPosition -= currentRotation * Vector3.forward * distance;
        newPosition.y = currentHeight;

        transform.position = newPosition;

        // Sempre olha suavemente para o ponto de foco no carro
        transform.LookAt(target.position + Vector3.up * lookAtHeight);
    }

    /// <summary>
    /// Ajusta suavemente o Field of View (FOV) da câmera proporcionalmente à velocidade normalizada
    /// do veículo e abre o campo de visão ao máximo durante o Mega Boost para amplificar a sensação de velocidade.
    /// </summary>
    private void UpdateDynamicFov()
    {
        if (cam == null) return;

        float targetFov = baseFov;

        if (carController != null)
        {
            float speedRatio = carController.SpeedNormalized;
            targetFov = Mathf.Lerp(baseFov, maxSpeedFov, speedRatio);

            if (carController.IsBoosting)
            {
                targetFov = megaBoostFov;
            }
        }

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, fovDamping * Time.deltaTime);
    }

    /// <summary>
    /// Atribui um novo alvo para a câmera em tempo de execução e atualiza a referência do CarController.
    /// </summary>
    /// <param name="newTarget">Transform do novo veículo alvo.</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (newTarget != null)
        {
            carController = newTarget.GetComponent<CarController>();
        }
    }
}
