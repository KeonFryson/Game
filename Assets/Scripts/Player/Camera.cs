using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private Camera firstPersonCamera;
    [SerializeField] private Camera thirdPersonCamera;


    private PlayerMovement playerMovement;
    private InputSystem_Actions inputActions;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();

        inputActions = new InputSystem_Actions();
        inputActions.Camera.SwitchCamera.performed += _ => SwitchCamera();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    private void SwitchCamera()
    {
        bool firstActive = firstPersonCamera.gameObject.activeSelf;

        firstPersonCamera.gameObject.SetActive(!firstActive);
        thirdPersonCamera.gameObject.SetActive(firstActive);

        playerMovement.SetCamera(
            firstActive ? thirdPersonCamera.gameObject : firstPersonCamera.gameObject
        );
    }

}
