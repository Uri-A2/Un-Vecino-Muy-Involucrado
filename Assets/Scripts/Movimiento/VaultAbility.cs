using System.Collections;
using UnityEngine;


[RequireComponent(typeof(PlayerMotor))]
public class VaultAbility : MonoBehaviour
{
    [Header("Detection")]
    public LayerMask vaultableMask = ~0;
    public float forwardCheckDistance = 1.0f;
    public float minObstacleHeight = 0.4f;
    public float maxObstacleHeight = 1.6f;
    public float ledgeCheckHeightAbove = 2.2f;
    public float landingCheckRadius = 0.35f;

    [Header("Motion")]
    public float vaultDuration = 0.35f;
    public AnimationCurve vaultHeightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float minMoveSpeedToTrigger = 2f;

    // [OBSTACLES]
    [Header("Vault qual")]
    public float sloppyDurationMultiplier = 1.25f;
    public float failedDurationMultiplier = 1.8f;
    public bool cleanVaultKeepsMomentum = true;

    PlayerMotor _motor;
    VaultJudge _judge; 
    bool _isVaulting;

    void Awake()
    {
        _motor = GetComponent<PlayerMotor>();
        _judge = GetComponent<VaultJudge>(); 
    }

    void Update()
    {
        if (_isVaulting) return;

        Vector3 horizVel = new Vector3(_motor.Velocity.x, 0f, _motor.Velocity.z);
        bool movingForwardEnough = Vector3.Dot(horizVel, transform.forward) > minMoveSpeedToTrigger;

        if (!Input.GetButtonDown("Jump")) return;
        if (!movingForwardEnough) return;

        if (TryFindVaultTarget(out Vector3 landPoint, out float obstacleTopY, out Vector3 wallNormal))
        {
         
            VaultQuality quality = _judge != null
                ? _judge.Judge(horizVel, wallNormal)
                : VaultQuality.Clean;

            StartCoroutine(DoVault(landPoint, obstacleTopY, quality, horizVel.magnitude));
        }
    }

    bool TryFindVaultTarget(out Vector3 landPoint, out float obstacleTopY, out Vector3 wallNormal)
    {
        landPoint = default;
        obstacleTopY = 0f;
        wallNormal = default;

        Vector3 feet = transform.position + Vector3.up * 0.05f;
        Vector3 forward = transform.forward;


        if (!Physics.Raycast(feet + Vector3.up * minObstacleHeight, forward, out RaycastHit frontHit,
            forwardCheckDistance, vaultableMask, QueryTriggerInteraction.Ignore))
            return false;


        Vector3 topProbeOrigin = frontHit.point + forward * 0.1f + Vector3.up * ledgeCheckHeightAbove;
        if (!Physics.Raycast(topProbeOrigin, Vector3.down, out RaycastHit topHit,
            ledgeCheckHeightAbove + 0.5f, vaultableMask, QueryTriggerInteraction.Ignore))
            return false;

        float heightAboveFeet = topHit.point.y - transform.position.y;
        if (heightAboveFeet < minObstacleHeight || heightAboveFeet > maxObstacleHeight)
            return false;


        if (Vector3.Dot(topHit.normal, Vector3.up) < 0.7f)
            return false;

        Vector3 landProbe = topHit.point + forward * (landingCheckRadius + 0.4f) + Vector3.up * 1f;
        if (Physics.CheckSphere(landProbe, landingCheckRadius, vaultableMask, QueryTriggerInteraction.Ignore))
            return false;

        if (!Physics.Raycast(landProbe, Vector3.down, out RaycastHit landGround, 2f,
            vaultableMask | _motor.gameObject.layer, QueryTriggerInteraction.Ignore))
        {
            landPoint = topHit.point + forward * (landingCheckRadius + 0.4f);
        }
        else
        {
            landPoint = landGround.point;
        }

        obstacleTopY = topHit.point.y;
        wallNormal = frontHit.normal; 
        return true;
    }

    IEnumerator DoVault(Vector3 landPoint, float obstacleTopY, VaultQuality quality, float entrySpeed)
    {
        _isVaulting = true;
        _motor.Controller.enabled = false;

        float duration = vaultDuration * quality switch
        {
            VaultQuality.Sloppy => sloppyDurationMultiplier,
            VaultQuality.Failed => failedDurationMultiplier,
            _ => 1f
        };

        Vector3 startPos = transform.position;

        Vector3 peakPos = new Vector3(
            Mathf.Lerp(startPos.x, landPoint.x, 0.5f),
            obstacleTopY + 0.15f,
            Mathf.Lerp(startPos.z, landPoint.z, 0.5f));

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float eased = vaultHeightCurve.Evaluate(Mathf.Clamp01(t));


            Vector3 pos = eased < 0.5f
                ? Vector3.Lerp(startPos, peakPos, eased * 2f)
                : Vector3.Lerp(peakPos, landPoint, (eased - 0.5f) * 2f);

            transform.position = pos;
            yield return null;
        }

        transform.position = landPoint;
        _motor.Controller.enabled = true;

        float exitSpeed = Mathf.Max(_motor.walkSpeed, 4f);
        if (quality == VaultQuality.Clean && cleanVaultKeepsMomentum)
            exitSpeed = Mathf.Clamp(entrySpeed, exitSpeed, _motor.runSpeed);

        _motor.SetExternalVelocity(transform.forward * exitSpeed);

        _isVaulting = false;
    }
}