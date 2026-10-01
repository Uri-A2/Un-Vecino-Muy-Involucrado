using System;
using UnityEngine;


public class TrashPile : MonoBehaviour, IInteractable
{
    [SerializeField] ParticleSystem cleanVfx;
    [SerializeField] GameObject targetMarker;
    [SerializeField] AudioClip cleanSfx;

    public bool Cleaned { get; private set; }
    public event Action<TrashPile> OnCleaned;

    bool armed;

    public void SetArmed(bool value)
    {
        armed = value;
        if (targetMarker != null) targetMarker.SetActive(value && !Cleaned);
    }

    public void Interact()
    {
        if (!armed || Cleaned) return;
        Cleaned = true;
        if (cleanVfx != null) cleanVfx.Play();
        if (cleanSfx != null) AudioSource.PlayClipAtPoint(cleanSfx, transform.position);
        if (targetMarker != null) targetMarker.SetActive(false);
        OnCleaned?.Invoke(this);
        // Hide visuals but keep the object alive so the VFX can finish.
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
    }
}
