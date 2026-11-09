using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class WaterGunJam : MonoBehaviour
{
    [Header("Jam roll")]
    [SerializeField] float secondsPerRoll = 3f;            
    [SerializeField, Range(0f, 1f)] float jamChance = 0.35f;

    [Header("Skill check")]
    [SerializeField] Key skillKey = Key.R;
    [SerializeField] float needleSpeed = 1.3f;             
    [SerializeField, Range(0.05f, 0.4f)] float zoneWidth = 0.15f;
    [SerializeField] float failCooldown = 0.4f;            
    [SerializeField] float failTimePenalty = 2f;
    [SerializeField] float failMeterPenalty = 0.05f;

    public bool Jammed { get; private set; }
    public event Action OnJammed, OnUnjammed;

    bool held;
    float sprayTime;
    float checkStart, zoneStart, lockedUntil;
    float Needle => Mathf.PingPong((Time.time - checkStart) * needleSpeed, 1f);

    static Texture2D px;

    void Awake()
    {
        if (px == null)
        {
            px = new Texture2D(1, 1);
            px.SetPixel(0, 0, Color.white);
            px.Apply();
        }
    }

   
    public void SetHeld(bool value) { held = value; }

    void Update()
    {
        if (Jammed) { UpdateSkillCheck(); return; }

        if (!held) return;
        sprayTime += Time.deltaTime;
        if (sprayTime >= secondsPerRoll)
        {
            sprayTime = 0f;
            if (UnityEngine.Random.value < jamChance) Jam();
        }
    }

    void Jam()
    {
        Jammed = true;
        NewZone();
        OnJammed?.Invoke();
    }

    void NewZone()
    {
        checkStart = Time.time;
        zoneStart = UnityEngine.Random.Range(0.3f, 1f - zoneWidth);
        lockedUntil = Time.time + 0.25f; 
    }

    void UpdateSkillCheck()
    {
        var kb = Keyboard.current;
        if (kb == null || Time.time < lockedUntil) return;
        if (!kb[skillKey].wasPressedThisFrame) return;

        float n = Needle;
        if (n >= zoneStart && n <= zoneStart + zoneWidth)
        {
            Jammed = false;
            sprayTime = 0f;
            OnUnjammed?.Invoke();
        }
        else
        {
            if (RunTimer.Instance != null) RunTimer.Instance.AddTime(-failTimePenalty);
            if (EfficiencyMeter.Instance != null) EfficiencyMeter.Instance.Penalty(failMeterPenalty);
            NewZone();
            lockedUntil = Time.time + failCooldown;
        }
    }

    void OnGUI()
    {
        if (!Jammed) return;

        float w = 360f, h = 26f;
        var bar = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.62f, w, h);

        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(bar.x - 3, bar.y - 3, w + 6, h + 6), px);

        GUI.color = new Color(0.25f, 0.25f, 0.25f);
        GUI.DrawTexture(bar, px);

        GUI.color = new Color(0.3f, 1f, 0.4f);
        GUI.DrawTexture(new Rect(bar.x + zoneStart * w, bar.y, zoneWidth * w, h), px);

        bool locked = Time.time < lockedUntil;
        GUI.color = locked ? Color.red : Color.white;
        GUI.DrawTexture(new Rect(bar.x + Needle * w - 2f, bar.y - 6f, 4f, h + 12f), px);

        GUI.color = Color.white;
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
        GUI.Label(new Rect(bar.x, bar.y - 34f, w, 28f), $"¡Pistola atascada! Presiona {skillKey}", style);
    }
}