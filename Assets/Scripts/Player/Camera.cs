using UnityEngine;

public class Camera : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField]private GameObject firstpersonCamera;
    [SerializeField]private GameObject thirdpersonCamera;
    private InputSystem_Actions inputActions;
    void Awake()
    {
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
        }
        else
        {
            firstpersonCamera.SetActive(true);
            thirdpersonCamera.SetActive(false);
        }

    }

}
