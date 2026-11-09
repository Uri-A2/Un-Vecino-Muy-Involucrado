using UnityEngine;

public class Reticle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerActions player;
    [SerializeField] WaterGunJam waterGun;

    [Header("Shape")]
    [SerializeField] float restGap = 6f; 
    [SerializeField] float lineLength = 10f;
    [SerializeField] float thickness = 2f;
    [SerializeField] float dotSize = 2f;

    [Header("Shooting feel")]
    [SerializeField] float kickPerShot = 8f;
    [SerializeField] float maxGap = 26f;
    [SerializeField] float recoverSpeed = 12f; 

    [Header("Colors")]
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color outlineColor = new Color(0f, 0f, 0f, 0.8f);
    [SerializeField] Color jamColor = new Color(1f, 0.15f, 0.15f);

    float gap;
    static Texture2D px;
    GUIStyle jamStyle;

    void Awake()
    {
        gap = restGap;
        if (px == null)
        {
            px = new Texture2D(1, 1);
            px.SetPixel(0, 0, Color.white);
            px.Apply();
        }
        if (player == null) player = FindFirstObjectByType<PlayerActions>();
        if (waterGun == null) waterGun = FindFirstObjectByType<WaterGunJam>();
    }

    void OnEnable() { if (player != null) player.OnShot += Kick; }
    void OnDisable() { if (player != null) player.OnShot -= Kick; }

    void Kick() { gap = Mathf.Min(gap + kickPerShot, maxGap); }

    void Update()
    {
        
        gap = Mathf.Lerp(gap, restGap, 1f - Mathf.Exp(-recoverSpeed * Time.deltaTime));
    }

    bool IsJammed =>
    waterGun != null && waterGun.Jammed &&
    player != null && player.Current != null &&
    player.Current.type == WeaponType.WaterGun;

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;

        if (IsJammed) DrawJam(cx, cy);
        else DrawCrosshair(cx, cy);
    }

    void DrawCrosshair(float cx, float cy)
    {
        float t = thickness, L = lineLength, g = gap;

        DrawLines(cx, cy, g, L, t + 2f, outlineColor, 1f);
        DrawLines(cx, cy, g, L, t, normalColor, 0f);

        Draw(new Rect(cx - dotSize * 0.5f - 1f, cy - dotSize * 0.5f - 1f, dotSize + 2f, dotSize + 2f), outlineColor);
        Draw(new Rect(cx - dotSize * 0.5f, cy - dotSize * 0.5f, dotSize, dotSize), normalColor);
    }

    void DrawLines(float cx, float cy, float g, float L, float t, Color c, float extra)
    {
        float e = extra; 
        Draw(new Rect(cx - t * 0.5f, cy - g - L - e, t, L + e * 2f), c); // top
        Draw(new Rect(cx - t * 0.5f, cy + g - e, t, L + e * 2f), c); // bottom
        Draw(new Rect(cx - g - L - e, cy - t * 0.5f, L + e * 2f, t), c); // left
        Draw(new Rect(cx + g - e, cy - t * 0.5f, L + e * 2f, t), c); // right
    }

    void DrawJam(float cx, float cy)
    {
        if (jamStyle == null)
        {
            jamStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 44
            };
        }

        float pulse = 1f + Mathf.Sin(Time.time * 12f) * 0.08f;
        int size = Mathf.RoundToInt(44f * pulse);
        jamStyle.fontSize = size;

        var r = new Rect(cx - 40f, cy - 40f, 80f, 80f);

        //Outline
        GUI.color = outlineColor;
        jamStyle.normal.textColor = outlineColor;
        for (int x = -2; x <= 2; x += 2)
            for (int y = -2; y <= 2; y += 2)
                if (x != 0 || y != 0)
                    GUI.Label(new Rect(r.x + x, r.y + y, r.width, r.height), "!", jamStyle);

        ////Red exclamation
        jamStyle.normal.textColor = jamColor;
        GUI.color = Color.white;
        GUI.Label(r, "!", jamStyle);
    }

    static void Draw(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, px);
        GUI.color = Color.white;
    }
}