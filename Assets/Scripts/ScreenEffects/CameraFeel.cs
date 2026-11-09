using UnityEngine;

public class CameraFeel : MonoBehaviour
{
    public PlayerMotor motor;
    public Transform cameraRig;

    [Header("Speed Shake")]
    public float shakeStartSpeed = 14f;
    public float shakeMaxSpeed = 22f;
    public float speedShakeAngle = 0.5f;
    public float speedShakePosition = 0.012f;
    public float shakeFrequency = 22f;

    [Header("Landing")]
    public float minLandSpeed = 8f;
    public float maxLandSpeed = 25f;
    public float landTrauma = 0.7f;
    public float landDip = 0.15f;
    public float dipRecoverTime = 0.18f;

    [Header("Wall Jump Tilt")]
    public float wallJumpTilt = 8f;
    public float tiltHoldTime = 0.12f;
    public float tiltInTime = 0.05f;
    public float tiltOutTime = 0.3f;

    [Header("Slide")]
    public float slideDrop = 0.5f;        
    public float slideDownTime = 0.08f;
    public float slideUpTime = 0.15f;
    public float slideTilt = 3f;         

    [Header("impact shake)")]
    public float traumaDecay = 2.5f;
    public float maxTraumaAngle = 3f;

    float _trauma;
    float _tilt, _tiltTarget, _tiltVel, _tiltHoldTimer;
    float _dip, _dipVel;
    float _slideOffset, _slideOffsetVel;
    float _slideRoll, _slideRollVel;
    float _lastAirVelY;
    bool _wasGrounded;
    float _seed;

    void Awake()
    {
        _seed = Random.value * 100f;
        if (cameraRig == null && transform.parent != null)
            cameraRig = transform.parent;
    }

    void LateUpdate()
    {
        if (motor == null) return;

        if (!motor.IsGrounded)
            _lastAirVelY = motor.Velocity.y;

        if (motor.IsGrounded && !_wasGrounded)
        {
            float fall = -_lastAirVelY;
            float k = Mathf.InverseLerp(minLandSpeed, maxLandSpeed, fall);
            if (k > 0f)
            {
                AddTrauma(landTrauma * k);
                _dip = -landDip * k;
            }
        }
        _wasGrounded = motor.IsGrounded;

        Vector3 flat = motor.Velocity; flat.y = 0f;
        float speedK = Mathf.InverseLerp(shakeStartSpeed, shakeMaxSpeed, flat.magnitude);

        _trauma = Mathf.MoveTowards(_trauma, 0f, traumaDecay * Time.deltaTime);
        float traumaShake = _trauma * _trauma;

        float angleAmp = traumaShake * maxTraumaAngle + speedK * speedShakeAngle;
        float posAmp = speedK * speedShakePosition + traumaShake * 0.03f;

        float t = Time.time * shakeFrequency;
        float nx = Noise(t, 0f), ny = Noise(t, 10f), nz = Noise(t, 20f);

        if (_tiltHoldTimer > 0f)
        {
            _tiltHoldTimer -= Time.deltaTime;
            _tilt = Mathf.SmoothDamp(_tilt, _tiltTarget, ref _tiltVel, tiltInTime);
        }
        else
        {
            _tilt = Mathf.SmoothDamp(_tilt, 0f, ref _tiltVel, tiltOutTime);
        }

        _dip = Mathf.SmoothDamp(_dip, 0f, ref _dipVel, dipRecoverTime);

        float slideTarget = motor.IsSliding ? -slideDrop : 0f;
        _slideOffset = Mathf.SmoothDamp(_slideOffset, slideTarget, ref _slideOffsetVel,
            motor.IsSliding ? slideDownTime : slideUpTime);

        float rollTarget = motor.IsSliding ? slideTilt : 0f;
        _slideRoll = Mathf.SmoothDamp(_slideRoll, rollTarget, ref _slideRollVel, 0.1f);

        transform.localRotation = Quaternion.Euler(nx * angleAmp, ny * angleAmp, _tilt + _slideRoll + nz * angleAmp);
        transform.localPosition = new Vector3(nx * posAmp, _dip + _slideOffset + ny * posAmp, 0f);
    }

    float Noise(float t, float offset) =>
        Mathf.PerlinNoise(_seed + offset, t) * 2f - 1f;

    public void AddTrauma(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);
}