using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DogWalkRandomSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Round
    {
        public Transform spawnPoint;
        public Transform destination;
    }

    [SerializeField] DogFollower[] dogs;
    [SerializeField] Vector3[] leashOffsets;        
    [SerializeField] List<Round> rounds = new();     
    [SerializeField] Vector2 intervalRange = new(10f, 20f); 
    [SerializeField] float pickupRadius = 2.5f;
    [SerializeField] float destinationRadius = 4f;
    [SerializeField] float timeToRespond = 30f;      
    [SerializeField] float timeReward = 15f;
    [SerializeField] float lostDogMeterPenalty = 0.15f;
    [SerializeField] float missedPenalty = 0.2f;
    [SerializeField] Color spawnMarkerColor = new Color(0.3f, 0.8f, 1f);
    [SerializeField] Color destinationMarkerColor = new Color(0.3f, 1f, 0.4f);

    [Header("Debug")]
    [SerializeField] bool showDebug = true;

    Transform player;
    Round current;
    Coroutine loop;

    void Awake()
    {
        foreach (var d in dogs) if (d != null) d.gameObject.SetActive(false);
    }

    void OnEnable() { loop = StartCoroutine(SpawnLoop()); }
    void OnDisable() { if (loop != null) StopCoroutine(loop); }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(intervalRange.x, intervalRange.y));
            if (rounds.Count == 0) continue;
            yield return RunRound();
        }
    }

    IEnumerator RunRound()
    {
        var p = GameObject.FindWithTag("Player");
        if (p == null) { Debug.LogWarning("DogWalkRandomSpawner: no object tagged 'Player'."); yield break; }
        player = p.transform;

        current = rounds[Random.Range(0, rounds.Count)];

     
        foreach (var d in dogs)
        {
            if (d == null) continue;
            d.transform.SetPositionAndRotation(current.spawnPoint.position, current.spawnPoint.rotation);
            d.gameObject.SetActive(true);
        }
        WorldMarkerHUD.Register(current.spawnPoint, spawnMarkerColor, "Perros");

      
        while (Flat(player.position, current.spawnPoint.position) > pickupRadius)
        {
            if (showDebug)
            {
                bool inRange = Flat(player.position, current.spawnPoint.position) <= pickupRadius;
                Debug.DrawLine(player.position, current.spawnPoint.position, inRange ? Color.green : Color.cyan);
            }
            yield return null;
        }

        WorldMarkerHUD.Unregister(current.spawnPoint);
        for (int i = 0; i < dogs.Length; i++)
        {
            dogs[i].OnLost += HandleDogLost;
            dogs[i].Attach(player, OffsetFor(i));
        }
        WorldMarkerHUD.Register(current.destination, destinationMarkerColor, "Llevar a casa");

        float t = 0f;
        while (t < timeToRespond)
        {
            float distToGoal = Flat(player.position, current.destination.position);
            if (showDebug)
            {
                Debug.DrawLine(player.position, current.destination.position, distToGoal <= destinationRadius ? Color.green : Color.yellow);
                foreach (var d in dogs)
                    if (d != null && d.State == DogFollower.DogState.Following)
                        Debug.DrawLine(player.position, d.transform.position, Color.magenta);
            }

            if (distToGoal <= destinationRadius)
            {
                int following = CountFollowing();
                if (following > 0) { Deliver(following); yield break; }
            }
            if (CountFollowing() == 0) break; 
            t += Time.deltaTime;
            yield return null;
        }

        Miss();
    }

    Vector3 OffsetFor(int i)
    {
        if (leashOffsets != null && i < leashOffsets.Length) return leashOffsets[i];
        float side = (i % 2 == 0) ? 1.3f : -1.3f;
        return new Vector3(side, 0f, -2.2f - (i / 2) * 1.5f);
    }

    int CountFollowing()
    {
        int n = 0;
        foreach (var d in dogs) if (d != null && d.State == DogFollower.DogState.Following) n++;
        return n;
    }

    void HandleDogLost(DogFollower dog)
    {
        if (EfficiencyMeter.Instance != null) EfficiencyMeter.Instance.Penalty(lostDogMeterPenalty);
    }

    void Deliver(int followingCount)
    {
        float quality = (float)followingCount / dogs.Length;
        if (RunTimer.Instance != null) RunTimer.Instance.AddTime(timeReward * Mathf.Lerp(0.5f, 1f, quality));
        if (EfficiencyMeter.Instance != null) EfficiencyMeter.Instance.RegisterTask(quality);
        EndRound();
    }

    void Miss()
    {
        if (RunTimer.Instance != null) RunTimer.Instance.AddTime(-missedPenalty * 10f);
        EndRound();
    }

    void EndRound()
    {
        WorldMarkerHUD.Unregister(current.spawnPoint);
        WorldMarkerHUD.Unregister(current.destination);
        foreach (var d in dogs)
        {
            if (d == null) continue;
            d.OnLost -= HandleDogLost;
            d.Release();
            d.ResetDog();
            d.gameObject.SetActive(false);
        }
        current = null;
    }

    float Flat(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }

    void OnDrawGizmosSelected()
    {
        if (!showDebug || rounds == null) return;
        foreach (var r in rounds)
        {
            if (r.spawnPoint != null)
            {
                Gizmos.color = spawnMarkerColor;
                Gizmos.DrawWireSphere(r.spawnPoint.position, pickupRadius);
            }
            if (r.destination != null)
            {
                Gizmos.color = destinationMarkerColor;
                Gizmos.DrawWireSphere(r.destination.position, destinationRadius);
            }
        }
    }

    void OnDestroy()
    {
        if (dogs == null) return;
        foreach (var d in dogs) if (d != null) d.OnLost -= HandleDogLost;
    }
}