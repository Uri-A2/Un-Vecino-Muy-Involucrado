using UnityEngine;


[RequireComponent(typeof(PlayerMotor))]
public class WallJumpAbility : MonoBehaviour
{
    [Header("Wall Jump")]
    public LayerMask validWallMask = ~0;
    public float wallJumpUpForce = 9f;
    public float wallJumpPushForce = 10f;
    public float minAirTimeBeforeWallJump = 0.05f; 
    public float wallJumpCooldown = 0.15f;
    public float maxWallStickTime = 0.6f;

    PlayerMotor _motor;
    float _cooldownTimer;
    float _wallStickTimer;
    float _airTimer;

    void Awake()
    {
        _motor = GetComponent<PlayerMotor>();
    }

    void Update()
    {
        if (_motor.IsGrounded)
        {
            _airTimer = 0f;
            _wallStickTimer = 0f;
        }
        else
        {
            _airTimer += Time.deltaTime;
        }

        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        bool onValidWall = _motor.IsAgainstWall && IsWallValid();

        if (onValidWall && !_motor.IsGrounded)
        {
            _wallStickTimer += Time.deltaTime;

            
            if (_wallStickTimer < maxWallStickTime)
            {
                Vector3 slide = new Vector3(0f, Mathf.Max(-2f, -3f * _wallStickTimer), 0f);
                _motor.AddImpulse(slide * Time.deltaTime);
            }

            if (Input.GetButtonDown("Jump") && _cooldownTimer <= 0f && _airTimer > minAirTimeBeforeWallJump)
            {
                DoWallJump(_motor.WallNormal);
            }
        }
    }

    bool IsWallValid()
    {
       
        Vector3 origin = transform.position + _motor.Controller.center;
        Vector3 dir = _motor.WallNormal.sqrMagnitude > 0.01f ? -_motor.WallNormal : transform.forward;

        return Physics.Raycast(origin, dir, out RaycastHit hit, _motor.wallCheckDistance + 0.1f,
            validWallMask, QueryTriggerInteraction.Ignore)
            && Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < 0.2f;
    }

    void DoWallJump(Vector3 wallNormal)
    {
        Vector3 pushDir = wallNormal.normalized;
        Vector3 newVel = pushDir * wallJumpPushForce + Vector3.up * wallJumpUpForce;

      
        Vector3 currentHoriz = new Vector3(_motor.Velocity.x, 0f, _motor.Velocity.z);
        newVel += Vector3.ClampMagnitude(currentHoriz, 3f) * 0.3f;

        _motor.SetExternalVelocity(newVel);
        _cooldownTimer = wallJumpCooldown;
        _wallStickTimer = 0f;
        _motor.ConsumeJumpBuffer();
    }
}
