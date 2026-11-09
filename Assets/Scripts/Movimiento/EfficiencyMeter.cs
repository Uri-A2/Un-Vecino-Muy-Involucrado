using System;
using UnityEngine;


public class EfficiencyMeter : MonoBehaviour
{
    public static EfficiencyMeter Instance { get; private set; }

    [Header("Meter")]
    [SerializeField] float baseGainPerTask = 0.25f;
    [SerializeField] float streakBonus = 0.25f;      
    [SerializeField] float chainWindow = 12f;        
    [SerializeField] float idleGraceSeconds = 6f;   
    [SerializeField] float decayPerSecond = 0.04f;

    [Header("Nitro")]
    [SerializeField] float nitroDuration = 6f;
    [SerializeField] float nitroSpeedMultiplier = 1.6f;

    public static readonly string[] RankNames =
        { "Mal Vecino", "Vecino Flojo", "Vecino", "Vecino Involucrado", "Vecino Muy Involucrado" };
    static readonly float[] RankThresholds = { 0f, 0.2f, 0.4f, 0.65f, 0.85f };
    public int TasksCompleted { get; private set; }
    public int BestStreak { get; private set; }
    public int PeakRankIndex { get; private set; }
    public float Value { get; private set; }         
    public int Streak { get; private set; }
    public int RankIndex { get; private set; }
    public string RankName => RankNames[RankIndex];
    public bool NitroActive { get; private set; }
    public bool NitroReady => !NitroActive && Value >= 1f;
    public float SpeedMultiplier => NitroActive ? nitroSpeedMultiplier : 1f;

    public event Action<int> OnRankChanged;
    public event Action OnNitroStarted, OnNitroEnded;

    float lastGainTime = -999f;
    float nitroLeft;

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (NitroActive)
        {
            nitroLeft -= Time.deltaTime;
            Value = Mathf.Clamp01(nitroLeft / nitroDuration);
            if (nitroLeft <= 0f)
            {
                NitroActive = false;
                Value = 0f;
                OnNitroEnded?.Invoke();
            }
        }
        else if (Value < 1f && Time.time - lastGainTime > idleGraceSeconds)
        {
            Value = Mathf.Max(0f, Value - decayPerSecond * Time.deltaTime);
        }

        if (Time.time - lastGainTime > chainWindow) Streak = 0;
        UpdateRank();
    }

   
    public void RegisterTask(float quality)
    {
        Streak = (Time.time - lastGainTime <= chainWindow) ? Streak + 1 : 0;
        lastGainTime = Time.time;
        TasksCompleted++;
        if (Streak > BestStreak) BestStreak = Streak;
        if (NitroActive) return;

        float gain = baseGainPerTask * Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(quality)) * (1f + streakBonus * Streak);
        Value = Mathf.Min(1f, Value + gain);
    }

    public void Penalty(float amount)
    {
        Streak = 0;
        if (!NitroActive) Value = Mathf.Max(0f, Value - amount);
    }

    public bool TryActivateNitro()
    {
        if (!NitroReady) return false;
        NitroActive = true;
        nitroLeft = nitroDuration;
        OnNitroStarted?.Invoke();
        return true;
    }

    void UpdateRank()
    {
        int r = 0;
        for (int i = 0; i < RankThresholds.Length; i++) if (Value >= RankThresholds[i]) r = i;
        if (r > PeakRankIndex) PeakRankIndex = r;
        { RankIndex = r; OnRankChanged?.Invoke(r); }
    }
}
