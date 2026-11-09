using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MailboxRandomSpawner : MonoBehaviour
{
    [SerializeField] List<MailboxTarget> mailboxes = new();
    [SerializeField] Vector2 intervalRange = new(6f, 14f); 
    [SerializeField] float timeToRespond = 10f;
    [SerializeField] float timeReward = 8f;
    [SerializeField] float missedPenalty = 0.12f;        
    [SerializeField] Color markerColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] string markerLabel = "Correo";

    MailboxTarget current;
    Coroutine loop;

    void OnEnable() { loop = StartCoroutine(SpawnLoop()); }
    void OnDisable() { if (loop != null) StopCoroutine(loop); }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(intervalRange.x, intervalRange.y));
            yield return ArmRandomMailbox();
        }
    }
    IEnumerator ArmRandomMailbox()
    {
        var pool = mailboxes.FindAll(m => m != null && !m.Delivered);

        if (pool.Count == 0)
        {
            
            foreach (var m in mailboxes)
                if (m != null) m.ResetTarget();

            pool = mailboxes.FindAll(m => m != null && !m.Delivered);
            if (pool.Count == 0) yield break;
        }

        current = pool[Random.Range(0, pool.Count)];
        current.OnDelivered += HandleDelivered;
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

    void HandleDelivered(MailboxTarget m)
    {
        m.OnDelivered -= HandleDelivered;
        WorldMarkerHUD.Unregister(m.transform);
        current = null;

        if (RunTimer.Instance != null) RunTimer.Instance.AddTime(timeReward);
        if (EfficiencyMeter.Instance != null) EfficiencyMeter.Instance.RegisterTask(1f);
    }

    void HandleMissed()
    {
        current.OnDelivered -= HandleDelivered;
        current.SetArmed(false);
        WorldMarkerHUD.Unregister(current.transform);
        if (EfficiencyMeter.Instance != null) EfficiencyMeter.Instance.Penalty(missedPenalty);
        current = null;
    }
}
