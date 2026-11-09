using System;
using UnityEngine;
public class MailboxTarget : MonoBehaviour, IShootable
{
    [SerializeField] ParticleSystem deliveredVfx;
    [SerializeField] GameObject targetMarker; 
    [SerializeField] AudioClip deliveredSfx;

    public bool Delivered { get; private set; }
    public event Action<MailboxTarget> OnDelivered;

    bool armed;

    public void SetArmed(bool value)
    {
        armed = value;
        if (targetMarker != null) targetMarker.SetActive(value && !Delivered);
    }

    public void ResetTarget()
    {
        Delivered = false;
        SetArmed(false);
    }

    public void OnShot(RaycastHit hit, WeaponType weapon)
    {
        if (!armed || Delivered || weapon != WeaponType.MailLauncher) return;
        Delivered = true;
        if (targetMarker != null) targetMarker.SetActive(false);
        if (deliveredVfx != null) deliveredVfx.Play();
        if (deliveredSfx != null) AudioSource.PlayClipAtPoint(deliveredSfx, transform.position);
        OnDelivered?.Invoke(this);
    }
}
