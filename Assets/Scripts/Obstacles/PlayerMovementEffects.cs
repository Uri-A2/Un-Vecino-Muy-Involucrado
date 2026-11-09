using System;
using UnityEngine;
public class PlayerMovementEffects : MonoBehaviour
{
    
    public static event Action<float> OnTimePenalty;

    public event Action OnSlip;
    public event Action OnStumble;

    [Header("")]
    public bool IsImmuneToSlip;

    
    public bool IsSlipping => slipTimer > 0f;
    public bool IsStumbling => stumbleTimer > 0f;
    public bool IsDisrupted => IsSlipping || IsStumbling;
    public Vector3 SlipDirection { get; private set; }
    public float ControlMultiplier { get; private set; } = 1f;  
    public float SpeedMultiplier { get; private set; } = 1f;
    public float HorizontalSpeed { get; private set; }        

    float slipTimer, slipDuration, slipMinControl;
    float stumbleTimer, stumbleDuration, stumbleMinSpeed;
    PlayerMotor motor;

    void Awake() => motor = GetComponent<PlayerMotor>();

    void Update()
    {
        
        if (motor != null)
        {
            Vector3 v = motor.Velocity;
            HorizontalSpeed = new Vector2(v.x, v.z).magnitude;
        }

        float control = 1f;
        float speed = 1f;

        if (slipTimer > 0f)
        {
            slipTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(slipTimer / slipDuration); 
            control = Mathf.Min(control, Mathf.Lerp(slipMinControl, 1f, t));
        }

        if (stumbleTimer > 0f)
        {
            stumbleTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(stumbleTimer / stumbleDuration);
            speed = Mathf.Min(speed, Mathf.Lerp(stumbleMinSpeed, 1f, t));
        }

        ControlMultiplier = control;
        SpeedMultiplier = speed;
    }

    public void ApplySlip(Vector3 direction, float duration, float minControl = 0.1f, float timePenalty = 0f)
    {
        if (IsImmuneToSlip || IsSlipping) return;

        direction.y = 0f;
        SlipDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
        slipDuration = Mathf.Max(0.05f, duration);
        slipTimer = slipDuration;
        slipMinControl = Mathf.Clamp01(minControl);

        if (timePenalty > 0f) OnTimePenalty?.Invoke(timePenalty);
        OnSlip?.Invoke();
    }

    public void ApplyStumble(float duration, float minSpeedMultiplier = 0.3f, float timePenalty = 0f)
    {
        if (IsStumbling) return;

        stumbleDuration = Mathf.Max(0.05f, duration);
        stumbleTimer = stumbleDuration;
        stumbleMinSpeed = Mathf.Clamp01(minSpeedMultiplier);

        if (timePenalty > 0f) OnTimePenalty?.Invoke(timePenalty);
        OnStumble?.Invoke();
    }
}