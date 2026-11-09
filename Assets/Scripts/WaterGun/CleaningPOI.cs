using System.Collections.Generic;
using UnityEngine;

public class CleaningPOI : PointOfInterest
{
    [SerializeField] List<StainTarget> stains = new();

    int remaining;

    protected override void OnBegin()
    {
        if (stains == null || stains.Count == 0)
            stains = new List<StainTarget>(GetComponentsInChildren<StainTarget>(true));

        remaining = 0;
        foreach (var s in stains)
        {
            if (s == null) continue;
            s.ResetStain();
            remaining++;
            s.OnCleaned += HandleCleaned;
            s.SetArmed(true);
        }
        if (remaining == 0) { Debug.LogWarning($"CleaningPOI '{DisplayName}': no stains found."); Complete(); }
    }

    void HandleCleaned(StainTarget s)
    {
        s.OnCleaned -= HandleCleaned;
        if (--remaining <= 0) Complete();
    }

    void OnDestroy()
    {
        foreach (var s in stains) if (s != null) s.OnCleaned -= HandleCleaned;
    }
}