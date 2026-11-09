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

    // [OBSTACLES]
    [Header("Obstacle effects")]
    public bool blockJumpWhileSlipping = true;
    public float slipPull = 10f;

    [Header("Slide")]
    public KeyCode slideKey = KeyCode.LeftControl;
    public float slideMinStartSpeed = 8f;     
    public float slideStartBoost = 1.2f;     
    public float slideMaxSpeed = 15f;
    public float slideDeceleration = 8f;
    public float slideEndSpeed = 4f;          
    public float slideMaxDuration = 1.2f;
    public float slideHeight = 1f;          
    public float slideCooldown = 0.3f;

    public CharacterController Controller { get; private set; }
    public Vector3 Velocity { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsAgainstWall { get; private set; }
    public Vector3 WallNormal { get; private set; }
    public Vector2 MoveInput { get; private set; }
    public bool IsSliding { get; private set; }

    float _coyoteTimer;
    float _jumpBufferTimer;
    bool _externalVelocityActive;
    PlayerMovementEffects _fx;

    //sl
    float _slideTimer, _slideCooldownTimer, _slideSpeed;
    Vector3 _slideDir;
    float _standHeight;
    Vector3 _standCenter;

    void Awake()
    {
        Controller = GetComponent<CharacterController>();
        _standHeight = Controller.height;
        _standCenter = Controller.center;
        _fx = GetComponent<PlayerMovementEffects>();
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
            HandleSlide();
            if (!IsSliding) HandleHorizontalMovement();
            HandleJump();
            ApplyGravity();
        }

        if (Controller.enabled)
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
        float control = _fx != null ? _fx.ControlMultiplier : 1f;
        float speedMult = _fx != null ? _fx.SpeedMultiplier : 1f;
        bool slipping = _fx != null && _fx.IsSlipping;

        Vector3 wishDir = (transform.right * MoveInput.x + transform.forward * MoveInput.y);
        wishDir = Vector3.ClampMagnitude(wishDir, 1f);

        float targetSpeed = (IsRunning ? runSpeed : walkSpeed) * speedMult;
        Vector3 targetVel = wishDir * targetSpeed;

        Vector3 horizontalVel = new Vector3(Velocity.x, 0f, Velocity.z);
        float accel = (IsGrounded ? acceleration : airAcceleration) * control;

        if (wishDir.sqrMagnitude > 0.01f)
        {
            horizontalVel = Vector3.MoveTowards(horizontalVel, targetVel,
                accel * (IsGrounded ? 1f : airControlMultiplier) * Time.deltaTime);
        }
        else if (IsGrounded && !slipping)
        {
            horizontalVel = Vector3.MoveTowards(horizontalVel, Vector3.zero, groundFriction * Time.deltaTime);
        }

        if (slipping)
        {
            Vector3 slipVel = _fx.SlipDirection * horizontalVel.magnitude;
            horizontalVel = Vector3.Lerp(horizontalVel, slipVel, (1f - control) * slipPull * Time.deltaTime);
        }

        if (speedMult < 1f)
            horizontalVel = Vector3.ClampMagnitude(horizontalVel, runSpeed * speedMult);

        Velocity = new Vector3(horizontalVel.x, Velocity.y, horizontalVel.z);
    }

    void HandleSlide()
    {
        if (_slideCooldownTimer > 0f) _slideCooldownTimer -= Time.deltaTime;

        if (!IsSliding)
        {
            Vector3 flat = new Vector3(Velocity.x, 0f, Velocity.z);
            if (Input.GetKeyDown(slideKey) && IsGrounded && _slideCooldownTimer <= 0f
                && flat.magnitude >= slideMinStartSpeed)
            {
                StartSlide(flat);
            }
            return;
        }

        _slideTimer -= Time.deltaTime;
        _slideSpeed = Mathf.MoveTowards(_slideSpeed, 0f, slideDeceleration * Time.deltaTime);
        Velocity = new Vector3(_slideDir.x * _slideSpeed, Velocity.y, _slideDir.z * _slideSpeed);

        bool wantsStop = _slideTimer <= 0f
                      || _slideSpeed < slideEndSpeed
                      || !Input.GetKey(slideKey)
                      || !IsGrounded;

        if (wantsStop && HasHeadroom())
            StopSlide();
    }

    void StartSlide(Vector3 flatVelocity)
    {
        IsSliding = true;
        _slideDir = flatVelocity.normalized;
        _slideSpeed = Mathf.Min(flatVelocity.magnitude * slideStartBoost, slideMaxSpeed);
        _slideTimer = slideMaxDuration;

        float feetY = _standCenter.y - _standHeight * 0.5f;
        Controller.height = slideHeight;
        Controller.center = new Vector3(_standCenter.x, feetY + slideHeight * 0.5f, _standCenter.z);
    }

    void StopSlide()
    {
        IsSliding = false;
        _slideCooldownTimer = slideCooldown;
        Controller.height = _standHeight;
        Controller.center = _standCenter;
    }

    bool HasHeadroom()
    {
        
        float r = Controller.radius * 0.9f;
        float feetY = _standCenter.y - _standHeight * 0.5f;
        Vector3 origin = transform.position + Vector3.up * (feetY + slideHeight - Controller.radius);
        float dist = _standHeight - slideHeight;
        return !Physics.SphereCast(origin, r, Vector3.up, out _, dist, groundMask, QueryTriggerInteraction.Ignore);
    }

    void HandleJump()
    {
        bool canJump = _coyoteTimer > 0f && _jumpBufferTimer > 0f;
        if (blockJumpWhileSlipping && _fx != null && _fx.IsSlipping) canJump = false;

        
        if (canJump && IsSliding && !HasHeadroom()) canJump = false;

        if (canJump)
        {
            if (IsSliding) StopSlide();   

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