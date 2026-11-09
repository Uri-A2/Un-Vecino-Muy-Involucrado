using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public abstract class PointOfInterest : MonoBehaviour
{
    public enum State { Idle, Active, Completed }

    public static readonly List<PointOfInterest> All = new();
    public static event Action<PointOfInterest> OnAnyCompleted;
    public static int CompletedCount { get; private set; }

    [Header("POI")]
    [SerializeField] string displayName = "POI";
    [SerializeField] float activationRadius = 6f;
    [SerializeField] float parTime = 20f;      
    [SerializeField] float timeReward = 15f;   

    [Header("Feedback")]
    [SerializeField] GameObject marker;        
    [SerializeField] ParticleSystem completeVfx; 
    [SerializeField] AudioClip completeSfx;
    [SerializeField] AudioSource beacon;       

    public string DisplayName => displayName;
    public State CurrentState { get; private set; } = State.Idle;

    protected float ActiveTime => Time.time - beginTime;

    SphereCollider zone;
    float beginTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { All.Clear(); CompletedCount = 0; OnAnyCompleted = null; }

    protected virtual void Awake()
    {
        zone = GetComponent<SphereCollider>();
        zone.isTrigger = true;
        zone.radius = activationRadius;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    protected virtual void Start()
    {
        if (marker != null) marker.SetActive(true);
        if (beacon != null) { beacon.loop = true; beacon.spatialBlend = 1f; beacon.Play(); }
    }

    void OnTriggerEnter(Collider other)
    {
        if (CurrentState == State.Idle && other.CompareTag("Player")) Begin();
    }

    void Begin()
    {
        CurrentState = State.Active;
        beginTime = Time.time;
        OnBegin();
    }

    protected void Complete(float completionQuality = 1f)
    {
        if (CurrentState != State.Active) return;
        CurrentState = State.Completed;
        CompletedCount++;

        float speedQuality = Mathf.Clamp01(parTime / Mathf.Max(ActiveTime, 0.01f));
        float quality = speedQuality * Mathf.Clamp01(completionQuality);

        if (RunTimer.Instance != null) RunTimer.Instance.AddTime(timeReward * Mathf.Lerp(0.5f, 1f, quality));
        if (EfficiencyMeter.Instance != null) EfficiencyMeter.Instance.RegisterTask(quality);

        if (completeVfx != null) completeVfx.Play();
        if (completeSfx != null) AudioSource.PlayClipAtPoint(completeSfx, transform.position);
        if (marker != null) marker.SetActive(false);
        if (beacon != null) beacon.Stop();
        zone.enabled = false;

        OnCompleted();
        OnAnyCompleted?.Invoke(this);
    }

    protected void ResetPOI()
    {
        CurrentState = State.Idle;
        OnReset();
    }

    protected abstract void OnBegin();
    protected virtual void OnCompleted() { }
    protected virtual void OnReset() { }
}
