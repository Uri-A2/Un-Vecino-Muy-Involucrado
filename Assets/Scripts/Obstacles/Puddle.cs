using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Puddle : MonoBehaviour
{
    [Header(" triggers")]
    [SerializeField] float minSpeedToSlip = 4f;
    [SerializeField] float groundedTolerance = 0.25f;

    [Header("Slip effect")]
    [SerializeField] float slipDuration = 0.9f;
    [Range(0f, 1f)][SerializeField] float minControl = 0.1f;
    [SerializeField] float timePenalty = 1.0f;
    [SerializeField] float cooldown = 1.5f;

    float nextAllowedTime;

    void Reset() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerStay(Collider other)
    {
        if (Time.time < nextAllowedTime) return;

        var fx = other.GetComponentInParent<PlayerMovementEffects>();
        if (fx == null) return;
        if (fx.IsImmuneToSlip || fx.HorizontalSpeed < minSpeedToSlip) return;

        float feetY = other.bounds.min.y;
        float surfaceY = GetComponent<Collider>().bounds.max.y;
        if (feetY > surfaceY + groundedTolerance) return;

        Vector3 dir = fx.transform.forward;
        fx.ApplySlip(dir, slipDuration, minControl, timePenalty);
        nextAllowedTime = Time.time + cooldown;
    }
}