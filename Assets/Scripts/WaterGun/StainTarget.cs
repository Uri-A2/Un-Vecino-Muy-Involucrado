using System;
using UnityEngine;

public class StainTarget : MonoBehaviour, IShootable
{
    [SerializeField] Renderer[] renderers;     
    [SerializeField] int hitsToClean = 12;
    [SerializeField] float fadeSpeed = 4f;      
    [SerializeField] float destroyDelay = 2f;   
    [SerializeField] ParticleSystem cleanVfx;
    [SerializeField] GameObject targetMarker;
    [SerializeField] AudioClip cleanSfx;

    public bool Cleaned { get; private set; }
    public float Progress => Mathf.Clamp01((float)hits / hitsToClean);
    public event Action<StainTarget> OnCleaned;

    bool armed;
    int hits;
    float alpha = 1f;        
    float targetAlpha = 1f; 
    Color[] baseColors;
    string[] colorProps;

    void Awake()
    {
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>();
        baseColors = new Color[renderers.Length];
        colorProps = new string[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var m = renderers[i].material; 
            colorProps[i] = m.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            baseColors[i] = m.GetColor(colorProps[i]);
            MakeTransparent(m);
        }
    }
    void Update()
    {
        if (Mathf.Approximately(alpha, targetAlpha)) return;
        alpha = Mathf.MoveTowards(alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        ApplyAlpha(alpha);
    }

    public void SetArmed(bool value)
    {
        armed = value;
        if (targetMarker != null) targetMarker.SetActive(value && !Cleaned);
    }

    public void OnShot(RaycastHit hit, WeaponType weapon)
    {
        if (!armed || Cleaned || weapon != WeaponType.WaterGun) return;
        hits++;
        targetAlpha = 1f - Progress;
        if (hits >= hitsToClean) Finish();
    }

    void ApplyAlpha(float a)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var c = baseColors[i];
            c.a *= a;
            renderers[i].material.SetColor(colorProps[i], c);
        }
    }

    void Finish()
    {
        Cleaned = true;
        if (targetMarker != null) targetMarker.SetActive(false);
        if (cleanVfx != null) cleanVfx.Play();
        if (cleanSfx != null) AudioSource.PlayClipAtPoint(cleanSfx, transform.position);
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

        OnCleaned?.Invoke(this);
        Invoke(nameof(Hide), destroyDelay);   
    }

    void Hide() => gameObject.SetActive(false);

    public void ResetStain()
    {
        CancelInvoke(nameof(Hide));
        gameObject.SetActive(true);

        Cleaned = false;
        hits = 0;
        alpha = targetAlpha = 1f;
        ApplyAlpha(1f);

        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = true;
        SetArmed(false);
    }


    static void MakeTransparent(Material m)
    {
        if (m.HasProperty("_Surface")) //URP Lit-Unlit
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else if (m.HasProperty("_Mode")) 
        {
            m.SetFloat("_Mode", 3f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHABLEND_ON");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }
}