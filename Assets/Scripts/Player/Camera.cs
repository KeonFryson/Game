using UnityEngine;

public class Camera : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField]private GameObject firstpersonCamera;
    [SerializeField]private GameObject thirdpersonCamera;
    private PlayerMovement playerMovement;
    private InputSystem_Actions inputActions;
    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        inputActions = new InputSystem_Actions();
        inputActions.Camera.SwitchCamera.performed += ctx => SwitchCamera();

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
        if(firstpersonCamera.activeSelf)
        {
            firstpersonCamera.SetActive(false);
            thirdpersonCamera.SetActive(true);
          playerMovement.SetCamera(thirdpersonCamera);

        }
        else
        {
            firstpersonCamera.SetActive(true);
            thirdpersonCamera.SetActive(false);
            playerMovement.SetCamera(firstpersonCamera);
        }

    }

}
