using UnityEngine;

public class DogWalkPOI : PointOfInterest
{
    [SerializeField] DogFollower[] dogs;
    [SerializeField] Vector3[] leashOffsets;          
    [SerializeField] Transform destination;
    [SerializeField] GameObject destinationMarker;
    [SerializeField] float destinationRadius = 4f;
    [SerializeField] float lostDogMeterPenalty = 0.15f;
    [SerializeField] float failTimePenalty = 5f;

    Transform player;

    protected override void OnBegin()
    {
        var p = GameObject.FindWithTag("Player");
        if (p == null) { Debug.LogWarning("DogWalkPO: no object tagged 'Player'."); return; }
        player = p.transform;

        for (int i = 0; i < dogs.Length; i++)
        {
            dogs[i].OnLost += HandleDogLost;
            dogs[i].Attach(player, OffsetFor(i));
        }
        if (destinationMarker != null) destinationMarker.SetActive(true);
    }

    Vector3 OffsetFor(int i)
    {
        if (leashOffsets != null && i < leashOffsets.Length) return leashOffsets[i];
        float side = (i % 2 == 0) ? 1.3f : -1.3f;
        return new Vector3(side, 0f, -2.2f - (i / 2) * 1.5f);
    }

    void Update()
    {
        if (CurrentState != State.Active || player == null || destination == null) return;

        Vector3 d = player.position - destination.position;
        d.y = 0f;
        if (d.magnitude > destinationRadius) return;

        int following = CountFollowing();
        if (following > 0) Complete((float)following / dogs.Length);
    }

    int CountFollowing()
    {
        int n = 0;
        foreach (var dog in dogs) if (dog.State == DogFollower.DogState.Following) n++;
        return n;
    }

    void HandleDogLost(DogFollower dog)
    {
        if (EfficiencyMeter.Instance != null) EfficiencyMeter.Instance.Penalty(lostDogMeterPenalty);
        if (CountFollowing() > 0) return;

   
        if (RunTimer.Instance != null) RunTimer.Instance.AddTime(-failTimePenalty);
        ResetPOI();
    }

    protected override void OnCompleted()
    {
        foreach (var dog in dogs) { dog.OnLost -= HandleDogLost; dog.Release(); }
        if (destinationMarker != null) destinationMarker.SetActive(false);
    }

    protected override void OnReset()
    {
        foreach (var dog in dogs) { dog.OnLost -= HandleDogLost; dog.ResetDog(); }
        if (destinationMarker != null) destinationMarker.SetActive(false);
    }

    void OnDestroy()
    {
        if (dogs == null) return;
        foreach (var dog in dogs) if (dog != null) dog.OnLost -= HandleDogLost;
    }
}
