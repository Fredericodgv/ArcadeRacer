using UnityEngine;

/// <summary>
/// Câmera de perseguição clássica suave e sem flickering.
/// Executada em LateUpdate com interpolação angular estável.
/// </summary>
public class ArcadeCameraController : MonoBehaviour
{
    [Header("Alvo")]
    [SerializeField] private Transform target;

    [Header("Distâncias")]
    [Tooltip("Distância horizontal atrás do carro")]
    [SerializeField] private float distance = 6f;
    [Tooltip("Altura da câmera acima do carro")]
    [SerializeField] private float height = 2.2f;
    [Tooltip("Altura do ponto para onde a câmera olha no carro")]
    [SerializeField] private float lookAtHeight = 0.8f;

    [Header("Suavização")]
    [Tooltip("Suavização da rotação horizontal")]
    [SerializeField] private float rotationDamping = 5f;
    [Tooltip("Suavização da altura")]
    [SerializeField] private float heightDamping = 5f;

    private void LateUpdate()
    {
        if (target == null) return;

        // Ângulo e altura desejados baseados no carro
        float wantedRotationAngle = target.eulerAngles.y;
        float wantedHeight = target.position.y + height;

        // Ângulo e altura atuais da câmera
        float currentRotationAngle = transform.eulerAngles.y;
        float currentHeight = transform.position.y;

        // Amortecimento suave de rotação (LerpAngle evita problemas de 0-360) e altura
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

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
