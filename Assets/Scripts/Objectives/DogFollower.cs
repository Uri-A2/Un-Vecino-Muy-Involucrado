using System;
using UnityEngine;

public class DogFollower : MonoBehaviour
{
    public enum DogState { Idle, Following, Lost }

    [Header("Follow")]
    [SerializeField] float followSharpness = 5f;
    [SerializeField] float leashLength = 7f;         
    [SerializeField] float runHomeSpeed = 6f;
    [SerializeField] LayerMask groundMask = ~0;

    [Header("Strain")]
    [SerializeField] float comfortSpeed = 11f;
    [SerializeField] float speedStrainRate = 0.08f;   
    [SerializeField] float comfortTurnRate = 200f;    
    [SerializeField] float turnStrainRate = 0.002f;  
    [SerializeField] float slackStrainRate = 0.5f;    
    [SerializeField] float recoverRate = 0.25f;

    [Header("Feedback")]
    [SerializeField] LineRenderer leash;
    [SerializeField] AudioClip barkSfx;

    [Header("Debug")]
    [SerializeField] bool showDebug = true;

    public DogState State { get; private set; } = DogState.Idle;
    public float Strain { get; private set; }       
    public event Action<DogFollower> OnLost;

    Transform leader;
    Vector3 localOffset;
    Vector3 leaderPrev, prevDir;
    Vector3 homePos;
    Quaternion homeRot;

    void Awake()
    {
        homePos = transform.position;
        homeRot = transform.rotation;
        if (leash != null) { leash.positionCount = 2; leash.enabled = false; }
    }

    public void Attach(Transform leaderTransform, Vector3 offsetInLeaderSpace)
    {
        leader = leaderTransform;
        localOffset = offsetInLeaderSpace;
        leaderPrev = leader.position;
        prevDir = Vector3.zero;
        Strain = 0f;
        State = DogState.Following;
        if (leash != null) leash.enabled = true;
    }

    public void Release()
    {
        State = DogState.Idle;
        if (leash != null) leash.enabled = false;
    }

    public void ResetDog()
    {
        State = DogState.Idle;
        Strain = 0f;
        transform.SetPositionAndRotation(homePos, homeRot);
        if (leash != null) leash.enabled = false;
    }

    void Update()
    {
        if (State == DogState.Following) Follow();
        else if (State == DogState.Lost) RunHome();
    }

    void Follow()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || leader == null) return;

        Vector3 delta = leader.position - leaderPrev;
        delta.y = 0f;
        leaderPrev = leader.position;
        Vector3 vel = delta / dt;
        float speed = vel.magnitude;
        float turnRate = 0f;
        if (speed > 1f)
        {
            Vector3 dir = vel / speed;
            if (prevDir != Vector3.zero) turnRate = Vector3.Angle(prevDir, dir) / dt;
            prevDir = dir;
        }
        else prevDir = Vector3.zero;

      
        Vector3 target = leader.TransformPoint(localOffset);
        Vector3 pos = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-followSharpness * dt));
        pos = Snap(pos);
        Vector3 move = pos - transform.position;
        move.y = 0f;
        if (move.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 10f * dt);
        transform.position = pos;

        // Strain.
        float dist = Vector3.Distance(transform.position, leader.position);
        if (dist >= leashLength) { Lose(); return; }

        float rate = Mathf.Max(0f, speed - comfortSpeed) * speedStrainRate
                   + Mathf.Max(0f, turnRate - comfortTurnRate) * turnStrainRate;
        if (dist > leashLength * 0.75f) rate += slackStrainRate;

        Strain = rate > 0f ? Strain + rate * dt : Strain - recoverRate * dt;
        Strain = Mathf.Clamp01(Strain);

        Color strainColor = Color.Lerp(Color.white, Color.red, Strain);
        if (leash != null)
        {
            leash.SetPosition(0, leader.position + Vector3.up);
            leash.SetPosition(1, transform.position + Vector3.up * 0.5f);
            leash.startColor = strainColor;
            leash.endColor = strainColor;
        }
        if (showDebug)
        {
            Debug.DrawLine(leader.position + Vector3.up, transform.position + Vector3.up * 0.5f, strainColor);
            if (dist > leashLength * 0.75f) Debug.DrawRay(transform.position + Vector3.up * 1.5f, Vector3.up * (0.3f + Strain), Color.red);
        }

        if (Strain >= 1f) Lose();
    }

    void Lose()
    {
        State = DogState.Lost;
        if (leash != null) leash.enabled = false;
        if (barkSfx != null) AudioSource.PlayClipAtPoint(barkSfx, transform.position);
        if (showDebug) Debug.Log($"[DogFollower] {name} lost the leash (strain {Strain:0.00}, dist {Vector3.Distance(transform.position, leader.position):0.0}m).");
        OnLost?.Invoke(this);
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebug || leader == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(leader.position, leashLength);
    }

    void RunHome()
    {
        Vector3 to = homePos - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.25f) return;
        Vector3 pos = transform.position + to.normalized * runHomeSpeed * Time.deltaTime;
        transform.position = Snap(pos);
        transform.rotation = Quaternion.LookRotation(to);
    }

    Vector3 Snap(Vector3 pos)
    {
        if (Physics.Raycast(pos + Vector3.up * 3f, Vector3.down, out var hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
            pos.y = hit.point.y;
        return pos;
    }
}