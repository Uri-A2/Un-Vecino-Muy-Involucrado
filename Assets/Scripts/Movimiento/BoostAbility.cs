using System.Collections;
using UnityEngine;


public class BoostAbility : MonoBehaviour
{
    [Header("Boost")]
    public KeyCode boostKey = KeyCode.LeftControl;
    public float boostSpeed = 22f;
    public float boostDuration = 0.18f;
    public float boostCooldown = 0.6f;
    public bool allowVerticalBoostInput = false; 
    public AnimationCurve boostSpeedCurve = AnimationCurve.Linear(0, 1, 1, 0.4f); 

    [Header("Charges (optional)")]
    public int maxCharges = 2;
    public bool refillOnGrounded = true;

    public int CurrentCharges { get; private set; }
    public bool IsBoosting { get; private set; }

    PlayerMotor _motor;
    float _cooldownTimer;

    void Awake()
    {
        _motor = GetComponent<PlayerMotor>();
        CurrentCharges = maxCharges;
    }

    void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (refillOnGrounded && _motor.IsGrounded)
            CurrentCharges = maxCharges;

        if (!IsBoosting && Input.GetKeyDown(boostKey) && _cooldownTimer <= 0f && CurrentCharges > 0)
        {
            StartCoroutine(DoBoost());
        }
    }

    Vector3 GetBoostDirection()
    {
        Vector3 wishDir = transform.right * _motor.MoveInput.x + transform.forward * _motor.MoveInput.y;
        Vector3 dir = wishDir.sqrMagnitude > 0.01f ? wishDir.normalized : transform.forward;

        if (allowVerticalBoostInput && Input.GetButton("Jump"))
            dir = (dir + Vector3.up * 0.5f).normalized;

        return dir;
    }

    IEnumerator DoBoost()
    {
        IsBoosting = true;
        CurrentCharges--;
        _cooldownTimer = boostCooldown;

        Vector3 dir = GetBoostDirection();
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / boostDuration;
            float speedMul = boostSpeedCurve.Evaluate(Mathf.Clamp01(t));
            Vector3 vel = dir * boostSpeed * speedMul;

            
            vel.y += _motor.Velocity.y * 0.2f;

            _motor.SetExternalVelocity(vel);
            yield return null;
        }

        IsBoosting = false;
    }
}
