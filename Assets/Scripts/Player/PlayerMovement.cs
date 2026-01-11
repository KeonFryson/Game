using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
    public float walkSpeedDefault = 5f;
    public float runSpeedDefault = 10f;
    private float walkSpeed;
    private float runSpeed;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;



    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private float rotationY = 0;
    private CharacterController characterController;
    private Animator anim;
    private bool canMove = true;

    private InputSystem_Actions inputActions;
    private InputSystem_Actions.PlayerActions playerActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isRunning;

    private bool isCrouching;
    public bool isHoldingItem;
    public bool isDead;

    // New: request flag so jump triggers immediately on button down
    private bool jumpRequested;
 


    void Awake()
    {
        walkSpeed = walkSpeedDefault;
        runSpeed = runSpeedDefault;

        inputActions = new InputSystem_Actions();
        playerActions = inputActions.Player;

        playerActions.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        playerActions.Move.canceled += ctx => moveInput = Vector2.zero;
        playerActions.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        playerActions.Look.canceled += ctx => lookInput = Vector2.zero;
        playerActions.Sprint.performed += ctx => isRunning = ctx.ReadValueAsButton();
        playerActions.Sprint.canceled += ctx => isRunning = false;

      
        playerActions.Jump.performed += ctx => jumpRequested = true;
        playerActions.Jump.canceled += ctx => { jumpRequested = false; };

        playerActions.Crouch.performed += ctx => isCrouching = ctx.ReadValueAsButton();
        playerActions.Crouch.canceled += ctx => isCrouching = false;

       
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {


        characterController = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // initialize rotations from current transform so we don't snap on start
        rotationY = transform.eulerAngles.y;
        if (playerCamera != null)
            rotationX = playerCamera.transform.localEulerAngles.x;
        // convert rotationX to -180..180 range if needed
        if (rotationX > 180f) rotationX -= 360f;


    }

    void Update()
    {


        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * moveInput.y : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * moveInput.x : 0;

         
        
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        // Consume jumpRequested the first frame the character is grounded so jump happens immediately
        if (jumpRequested && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
            jumpRequested = false; // consume request
           
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }


        if (!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        if (isCrouching && canMove)
        {
            characterController.height = crouchHeight;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;
        }
        else
        {
            characterController.height = defaultHeight;
            walkSpeed = walkSpeedDefault;
            runSpeed = runSpeedDefault;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        // Update rotation values but defer applying them to LateUpdate to avoid jitter with CharacterController.Move
        if (canMove)
        {
            // Use deltaTime for smooth/framerate-independent mouse look
            rotationX += -lookInput.y * lookSpeed * Time.deltaTime;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

            rotationY += lookInput.x * lookSpeed * Time.deltaTime;
            // keep rotationY within 0-360 for numerical stability
            if (rotationY > 360f) rotationY -= 360f;
            else if (rotationY < 0f) rotationY += 360f;
        }
 
        
    }


    void LateUpdate()
    {
        if (!canMove) return;

        // Apply yaw to the player root
        transform.rotation = Quaternion.Euler(0f, rotationY, 0f);

        // Ensure camera is not null and apply pitch locally
        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }
}
