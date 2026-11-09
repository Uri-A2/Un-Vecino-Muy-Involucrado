using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StainRandomSpawner : MonoBehaviour
{
    [SerializeField] List<StainTarget> stains = new();
    [SerializeField] Vector2 intervalRange = new(8f, 16f);
    [SerializeField] float timeToRespond = 25f;
    [SerializeField] float timeReward = 10f;
    [SerializeField] float missedPenalty = 0.12f;
    [SerializeField] Color markerColor = new Color(0.3f, 0.8f, 1f);
    [SerializeField] string markerLabel = "Limpiar";

    StainTarget current;
    Coroutine loop;

    void OnEnable() { loop = StartCoroutine(SpawnLoop()); }
    void OnDisable() { if (loop != null) StopCoroutine(loop); }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(intervalRange.x, intervalRange.y));
            yield return ArmRandomStain();
        }
    }

    IEnumerator ArmRandomStain()
    {
        var pool = stains.FindAll(s => s != null && !s.Cleaned);

        if (pool.Count == 0)
        {
            //  start a new round
            foreach (var s in stains)
                if (s != null) s.ResetStain();

            pool = stains.FindAll(s => s != null && !s.Cleaned);
            if (pool.Count == 0) yield break;
        }

        current = pool[Random.Range(0, pool.Count)];
        current.OnCleaned += HandleCleaned;
        current.SetArmed(true);
        WorldMarkerHUD.Register(current.transform, markerColor, markerLabel);

        float t = 0f;
        while (current != null && t < timeToRespond)
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (current != null) HandleMissed();
    }

    void HandleCleaned(StainTarget s)
    {
        s.OnCleaned -= HandleCleaned;
        WorldMarkerHUD.Unregister(s.transform);
        current = null;

        if (RunTimer.Instance != null) RunTimer.Instance.AddTime(timeReward);
        if (EfficiencyMeter.Instance != null) EfficiencyMeter.Instance.RegisterTask(1f);
        TaskStats.RegisterCompletion();
    }

    void HandleMissed()
    {
        current.OnCleaned -= HandleCleaned;
        current.SetArmed(false);
        WorldMarkerHUD.Unregister(current.transform);
        if (EfficiencyMeter.Instance != null) EfficiencyMeter.Instance.Penalty(missedPenalty);
        current = null;
    }
}