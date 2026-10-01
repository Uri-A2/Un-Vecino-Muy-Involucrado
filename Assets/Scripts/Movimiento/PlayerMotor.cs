using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot;         
    public LayerMask groundMask = ~0;
    public LayerMask wallMask = ~0;

    [Header("Move Speeds")]
    public float walkSpeed = 7f;
    public float runSpeed = 11f;
    public float airControlMultiplier = 0.6f; 
    public float acceleration = 60f;
    public float airAcceleration = 25f;
    public float groundFriction = 8f;

    [Header("Jump / Gravity")]
    public float jumpHeight = 1.6f;
    public float gravity = -30f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("Ground / Wall Detection")]
    public float groundCheckDistance = 0.25f;
    public float wallCheckDistance = 0.65f;


    public CharacterController Controller { get; private set; }
    public Vector3 Velocity { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsAgainstWall { get; private set; }
    public Vector3 WallNormal { get; private set; }
    public Vector2 MoveInput { get; private set; }

    float _coyoteTimer;
    float _jumpBufferTimer;
    bool _externalVelocityActive; 

    void Awake()
    {
        Controller = GetComponent<CharacterController>();
        if (cameraPivot == null && Camera.main != null)
            cameraPivot = Camera.main.transform;
    }

    void Update()
    {
        ReadInput();
        CheckGround();
        CheckWall();
        HandleTimers();

        if (!_externalVelocityActive)
        {
            HandleHorizontalMovement();
            HandleJump();
            ApplyGravity();
        }

        Controller.Move(Velocity * Time.deltaTime);

       
        _externalVelocityActive = false;
    }

    void ReadInput()
    {
        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        IsRunning = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetButtonDown("Jump"))
            _jumpBufferTimer = jumpBufferTime;
    }

    void CheckGround()
    {
    
        Vector3 origin = transform.position + Controller.center - Vector3.up * (Controller.height * 0.5f - Controller.radius * 0.5f);
        IsGrounded = Physics.SphereCast(origin, Controller.radius * 0.9f, Vector3.down,
            out RaycastHit hit, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);

        if (IsGrounded && Velocity.y < 0f)
            _coyoteTimer = coyoteTime;
    }

    void CheckWall()
    {
       
        Vector3 origin = transform.position + Controller.center;
        Vector3[] dirs =
        {
            transform.forward,
            -transform.forward,
            transform.right,
            -transform.right
        };

        IsAgainstWall = false;
        foreach (var dir in dirs)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, wallCheckDistance, wallMask, QueryTriggerInteraction.Ignore))
            {
                
                if (Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < 0.2f)
                {
                    IsAgainstWall = true;
                    WallNormal = hit.normal;
                    break;
                }
            }
        }
    }

    void HandleTimers()
    {
        if (_coyoteTimer > 0f) _coyoteTimer -= Time.deltaTime;
        if (_jumpBufferTimer > 0f) _jumpBufferTimer -= Time.deltaTime;
    }

    void HandleHorizontalMovement()
    {
        Vector3 wishDir = (transform.right * MoveInput.x + transform.forward * MoveInput.y);
        wishDir = Vector3.ClampMagnitude(wishDir, 1f);

        float targetSpeed = IsRunning ? runSpeed : walkSpeed;
        Vector3 targetVel = wishDir * targetSpeed;

        Vector3 horizontalVel = new Vector3(Velocity.x, 0f, Velocity.z);
        float accel = IsGrounded ? acceleration : airAcceleration;

        if (wishDir.sqrMagnitude > 0.01f)
        {
            horizontalVel = Vector3.MoveTowards(horizontalVel, targetVel,
                accel * (IsGrounded ? 1f : airControlMultiplier) * Time.deltaTime);
        }
        else if (IsGrounded)
        {
            horizontalVel = Vector3.MoveTowards(horizontalVel, Vector3.zero, groundFriction * Time.deltaTime);
        }

        Velocity = new Vector3(horizontalVel.x, Velocity.y, horizontalVel.z);
    }

    void HandleJump()
    {
        bool canJump = _coyoteTimer > 0f && _jumpBufferTimer > 0f;
        if (canJump)
        {
            float jumpVel = Mathf.Sqrt(2f * jumpHeight * -gravity);
            Velocity = new Vector3(Velocity.x, jumpVel, Velocity.z);
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
        }
    }

    void ApplyGravity()
    {
        if (IsGrounded && Velocity.y < 0f)
        {
            Velocity = new Vector3(Velocity.x, -2f, Velocity.z); 
        }
        else
        {
            Velocity += Vector3.up * gravity * Time.deltaTime;
        }
    }

    public void SetExternalVelocity(Vector3 newVelocity)
    {
        Velocity = newVelocity;
        _externalVelocityActive = true;
    }

   
    public void AddImpulse(Vector3 impulse)
    {
        Velocity += impulse;
    }

    public bool ConsumeJumpBuffer()
    {
        if (_jumpBufferTimer > 0f)
        {
            _jumpBufferTimer = 0f;
            return true;
        }
        return false;
    }
}
