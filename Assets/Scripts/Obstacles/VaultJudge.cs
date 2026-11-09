using UnityEngine;

public enum VaultQuality { Clean, Sloppy, Failed }


[RequireComponent(typeof(PlayerMovementEffects))]
public class VaultJudge : MonoBehaviour
{
    [Header("Clean vault window")]
    [SerializeField] float minCleanSpeed = 6f;
    [SerializeField] float maxCleanSpeed = 14f;
    [SerializeField] float maxCleanAngle = 35f;

    [Header("Sloppy")]
    [SerializeField] float sloppyMargin = 0.3f;
    [SerializeField] float sloppyStumble = 0.5f;
    [Range(0f, 1f)][SerializeField] float sloppySpeedMult = 0.6f;
    [SerializeField] float sloppyTimePenalty = 0f;

    [Header("Failed")]
    [SerializeField] float failedStumble = 1.1f;
    [Range(0f, 1f)][SerializeField] float failedSpeedMult = 0.15f;
    [SerializeField] float failedTimePenalty = 1.5f;

    PlayerMovementEffects fx;

    void Awake() => fx = GetComponent<PlayerMovementEffects>();


    public VaultQuality Judge(Vector3 moveDir, Vector3 obstacleNormal)
    {
        float speed = fx.HorizontalSpeed;

        moveDir.y = 0f; obstacleNormal.y = 0f;
        float angle = (moveDir.sqrMagnitude > 0.001f && obstacleNormal.sqrMagnitude > 0.001f)
            ? Vector3.Angle(moveDir, -obstacleNormal)
            : 0f;


        float speedError = 0f;
        if (speed < minCleanSpeed) speedError = (minCleanSpeed - speed) / minCleanSpeed;
        else if (speed > maxCleanSpeed) speedError = (speed - maxCleanSpeed) / maxCleanSpeed;

        float angleError = angle > maxCleanAngle ? (angle - maxCleanAngle) / maxCleanAngle : 0f;
        float error = Mathf.Max(speedError, angleError);

        VaultQuality quality =
            error <= 0f ? VaultQuality.Clean :
            error <= sloppyMargin ? VaultQuality.Sloppy :
            VaultQuality.Failed;

        switch (quality)
        {
            case VaultQuality.Sloppy:
                fx.ApplyStumble(sloppyStumble, sloppySpeedMult, sloppyTimePenalty);
                break;
            case VaultQuality.Failed:
                fx.ApplyStumble(failedStumble, failedSpeedMult, failedTimePenalty);
                break;
        }
        return quality;
    }
}