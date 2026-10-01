using System.Collections.Generic;
using UnityEngine;

public class MailDeliveryPOI : PointOfInterest
{
    [SerializeField] List<MailboxTarget> mailboxes = new();

    int remaining;

    protected override void OnBegin()
    {
       
        if (mailboxes == null || mailboxes.Count == 0)
        {
            mailboxes = new List<MailboxTarget>(GetComponentsInChildren<MailboxTarget>(true));
            if (mailboxes.Count == 0)
                Debug.LogWarning($"MailDeliveryPOI '{DisplayName}': no mailboxes assigned or found in children.");
        }

        remaining = 0;
        foreach (var m in mailboxes)
        {
            if (m == null) continue;
            remaining++;
            m.OnDelivered += HandleDelivered;
            m.SetArmed(true);
        }
        Debug.Log($"MailDeliveryPOI '{DisplayName}': {remaining} mailboxes armed.");
        if (remaining == 0) Complete();
    }

    void HandleDelivered(MailboxTarget m)
    {
        m.OnDelivered -= HandleDelivered;
        remaining--;
        Debug.Log($"MailDeliveryPOI '{DisplayName}': mailbox delivered, {remaining} left.");
        if (remaining <= 0) Complete();
    }

    void OnDestroy()
    {
        foreach (var m in mailboxes) if (m != null) m.OnDelivered -= HandleDelivered;
    }
}