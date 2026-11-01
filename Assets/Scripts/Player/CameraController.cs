using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{

    public Transform playerTransform;

    public float sensitivity = 0.5f;

    public float minXAngle = -30f;
    public float maxXAngle = 30f;

    public float minYAngle = -360f;
    public float maxYAngle = 360f;

    public float smoothTime = 10f;

    [SerializeField] private Camera playerCamera;

    private float xRotation = 0f;

    private float yRotation = 0f;

    private InputAction lookAction;
    private float defaultFieldOfView;
    private float aimSensitivityMultiplier = 1f;
    private bool isScoped;
    private float horizontalSensitivity;
    private float verticalSensitivity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        horizontalSensitivity = PlayerPrefs.GetFloat(
            SaveMenu.HorizontalSensitivityKey,
            sensitivity);
        verticalSensitivity = PlayerPrefs.GetFloat(
            SaveMenu.VerticalSensitivityKey,
            sensitivity);

        if (playerCamera == null)
        {
            playerCamera = GetComponent<Camera>();
        }

        if (playerCamera != null)
        {
            defaultFieldOfView = playerCamera.fieldOfView;
        }
    }

    void Start()
    {

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        lookAction = InputSystem.actions.FindAction("Player/Look");
        if (lookAction != null)
        {
            lookAction.Enable();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (lookAction == null)
        {
            return;
        }

        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        float aimMultiplier = isScoped ? aimSensitivityMultiplier : 1f;
        float mouseX = lookInput.x * horizontalSensitivity * aimMultiplier;
        float mouseY = lookInput.y * verticalSensitivity * aimMultiplier;

        xRotation -= mouseY;
        yRotation += mouseX;

        xRotation = Mathf.Clamp(xRotation, minXAngle, maxXAngle);
        yRotation = Mathf.Clamp(yRotation, minYAngle, maxYAngle);

        Quaternion targetRotation = Quaternion.Euler(
            xRotation,
            yRotation,
            0f
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            smoothTime * Time.deltaTime
        );

        if (playerTransform != null)
        {
            Quaternion playerTargetRotation = Quaternion.Euler(0f, yRotation, 0f);
            playerTransform.rotation = Quaternion.Slerp(
                playerTransform.rotation,
                playerTargetRotation,
                smoothTime * Time.deltaTime
            );
        }
    }

    public void SetScopedAim(bool aiming, float zoomMultiplier, float sensitivityMultiplier)
    {
        isScoped = aiming;
        aimSensitivityMultiplier = Mathf.Clamp(sensitivityMultiplier, 0.01f, 1f);

        if (playerCamera != null)
        {
            float safeZoomMultiplier = Mathf.Max(1f, zoomMultiplier);
            playerCamera.fieldOfView = aiming
                ? defaultFieldOfView / safeZoomMultiplier
                : defaultFieldOfView;
        }
    }

    public void SetMouseSensitivity(float horizontal, float vertical)
    {
        horizontalSensitivity = Mathf.Max(0f, horizontal);
        verticalSensitivity = Mathf.Max(0f, vertical);
    }
}
