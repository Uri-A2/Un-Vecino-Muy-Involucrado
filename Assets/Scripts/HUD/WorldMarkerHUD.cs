using System.Collections.Generic;
using UnityEngine;

public class WorldMarkerHUD : MonoBehaviour
{
    class Entry { public Transform target; public Color color; public string label; }

    static readonly List<Entry> markers = new();
    static Texture2D dot;

    [SerializeField] Camera cam;
    [SerializeField] float markerSize = 18f;
    [SerializeField] float edgeMargin = 40f;
    [SerializeField] float maxDistance = 80f; 

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (dot == null)
        {
            dot = new Texture2D(2, 2);
            var px = new Color[4] { Color.white, Color.white, Color.white, Color.white };
            dot.SetPixels(px);
            dot.Apply();
        }
    }

    public static void Register(Transform target, Color color, string label = null)
    {
        Unregister(target);
        markers.Add(new Entry { target = target, color = color, label = label });
    }

    public static void Unregister(Transform target) => markers.RemoveAll(e => e.target == target);

    void OnGUI()
    {
        if (cam == null) { cam = Camera.main; if (cam == null) return; }
        for (int i = markers.Count - 1; i >= 0; i--)
        {
            if (markers[i].target == null) { markers.RemoveAt(i); continue; }
            DrawMarker(markers[i]);
        }
    }

    void DrawMarker(Entry e)
    {
        Vector3 vp = cam.WorldToViewportPoint(e.target.position);
        bool behind = vp.z < 0f;
        if (behind) { vp.x = 1f - vp.x; vp.y = 1f - vp.y; }

        float distance = Vector3.Distance(cam.transform.position, e.target.position);
        float alpha = Mathf.Clamp01(1f - distance / maxDistance);

        Vector2 screen = new Vector2(vp.x * Screen.width, (1f - vp.y) * Screen.height);
        Rect bounds = new Rect(edgeMargin, edgeMargin, Screen.width - edgeMargin * 2f, Screen.height - edgeMargin * 2f);
        bool offscreen = behind || !bounds.Contains(screen);

        if (offscreen)
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = (screen - center).normalized;
            screen = center + Vector2.Scale(dir, bounds.size * 0.5f);
        }
        else if (alpha <= 0.02f) return; 

        Color prevColor = GUI.color;
        Matrix4x4 prevMatrix = GUI.matrix;

        GUI.color = new Color(e.color.r, e.color.g, e.color.b, offscreen ? 1f : Mathf.Lerp(0.35f, 1f, alpha));
        float size = offscreen ? markerSize * 0.8f : markerSize;

        float angle = offscreen
            ? Mathf.Atan2(screen.x - Screen.width * 0.5f, -(screen.y - Screen.height * 0.5f)) * Mathf.Rad2Deg
            : 45f; // on-screen marker is a diamond
        GUIUtility.RotateAroundPivot(angle, screen);
        GUI.DrawTexture(new Rect(screen.x - size * 0.5f, screen.y - size * 0.5f, size, size), dot);
        GUI.matrix = prevMatrix;

        if (!offscreen && !string.IsNullOrEmpty(e.label))
            GUI.Label(new Rect(screen.x - 40f, screen.y + size * 0.6f, 80f, 20f), e.label);

        GUI.color = prevColor;
    }
}
