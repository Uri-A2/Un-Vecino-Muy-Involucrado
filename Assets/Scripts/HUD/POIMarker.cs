using UnityEngine;
using UnityEngine.UI;

public class POIMarker : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] Sprite markerIcon;         
    [SerializeField] Color markerColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] Vector2 markerSize = new Vector2(28f, 28f);
    [SerializeField] bool showLabel = true;
    [SerializeField] bool showDistance = true;
    [SerializeField] float edgePadding = 40f;    
    [SerializeField] float hideDistance = 0f;    

    [Header("World anchor")]
    [SerializeField] Vector3 worldOffset = new Vector3(0f, 2f, 0f); 

    PointOfInterest poi;
    Camera mainCam;
    Canvas canvas;
    RectTransform canvasRect;
    RectTransform markerRect;
    Image markerImage;
    Image arrowImage;
    Text label;
    Text distLabel;

    static readonly Color[] IconPixels = BuildCirclePixels();

    void Awake()
    {
        poi = GetComponent<PointOfInterest>();
        if (poi == null)
        {
            Debug.LogWarning("POIMarker needs a PointOfInterest on the same GameObject.");
            enabled = false;
            return;
        }
        mainCam = Camera.main;
        BuildUI();
    }

    void OnEnable()
    {
        PointOfInterest.OnAnyCompleted += HandleAnyCompleted;
    }

    void OnDisable()
    {
        PointOfInterest.OnAnyCompleted -= HandleAnyCompleted;
    }

    void LateUpdate()
    {
        if (poi == null || canvas == null) return;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        bool visible = poi.CurrentState != PointOfInterest.State.Completed;

        Vector3 worldPos = transform.position + worldOffset;
        float dist = Vector3.Distance(mainCam.transform.position, worldPos);
        if (hideDistance > 0f && dist < hideDistance) visible = false;

        canvas.enabled = visible;
        if (!visible) return;
        Vector3 sp = mainCam.WorldToScreenPoint(worldPos);
        bool behind = sp.z < 0f;
        Vector2 screen = new Vector2(sp.x, sp.y);
        if (behind) screen = new Vector2(Screen.width - screen.x, -screen.y); 

        bool onScreen = !behind
            && screen.x >= 0f && screen.x <= Screen.width
            && screen.y >= 0f && screen.y <= Screen.height;

        if (!onScreen)
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = screen - center;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;
            screen = ClampToRect(center, dir, edgePadding);
        }

      
        markerRect.anchoredPosition = screen - canvasRect.sizeDelta * 0.5f;
        arrowImage.enabled = !onScreen;
        if (!onScreen)
        {
            Vector2 center = canvasRect.sizeDelta * 0.5f;
            Vector2 toMarker = markerRect.anchoredPosition;
            float angle = Mathf.Atan2(toMarker.y - center.y, toMarker.x - center.x) * Mathf.Rad2Deg;
            arrowImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        markerImage.color = markerColor;
        if (distLabel != null) distLabel.text = Mathf.RoundToInt(dist) + "m";
    }

    Vector2 ClampToRect(Vector2 center, Vector2 dir, float pad)
    {
        float w = Screen.width * 0.5f - pad;
        float h = Screen.height * 0.5f - pad;
        float tx = Mathf.Abs(dir.x) < 0.001f ? float.MaxValue : w / Mathf.Abs(dir.x);
        float ty = Mathf.Abs(dir.y) < 0.001f ? float.MaxValue : h / Mathf.Abs(dir.y);
        return center + dir * Mathf.Min(tx, ty);
    }

    void HandleAnyCompleted(PointOfInterest completed)
    {
        if (completed == poi) canvas.enabled = false; 
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("POIMarker_" + (poi != null ? poi.DisplayName : name));
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<GraphicRaycaster>(); 

        canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(Screen.width, Screen.height);

        var markerGO = new GameObject("Marker");
        markerGO.transform.SetParent(canvasGO.transform, false);
        markerRect = markerGO.AddComponent<RectTransform>();
        markerRect.sizeDelta = markerSize;

        markerImage = markerGO.AddComponent<Image>();
        markerImage.sprite = markerIcon != null ? markerIcon : MakeCircleSprite();
        markerImage.color = markerColor;

      
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(markerGO.transform, false);
        var arrowRect = arrowGO.AddComponent<RectTransform>();
        arrowRect.anchoredPosition = new Vector2(0f, markerSize.y * 0.5f + 8f);
        arrowRect.sizeDelta = new Vector2(14f, 18f);
        arrowImage = arrowGO.AddComponent<Image>();
        arrowImage.sprite = MakeTriangleSprite();
        arrowImage.color = markerColor;
        arrowImage.enabled = false;

        // Labels
        if (showLabel)
        {
            label = MakeText(markerGO.transform, new Vector2(0f, -markerSize.y * 0.5f - 12f), 13, TextAnchor.UpperCenter);
            label.text = poi != null ? poi.DisplayName : name;
        }
        if (showDistance)
        {
            distLabel = MakeText(markerGO.transform, new Vector2(0f, markerSize.y * 0.5f + 26f), 11, TextAnchor.LowerCenter);
        }
    }

    Text MakeText(Transform parent, Vector2 pos, int size, TextAnchor anchor)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(200f, 20f);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.7f);
        return t;
    }

    Sprite MakeCircleSprite()
    {
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        tex.SetPixels(IconPixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
    }

    Sprite MakeTriangleSprite()
    {
        const int n = 16;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float halfWidth = Mathf.Lerp(n * 0.5f, 0.5f, (float)y / (n - 1));
                px[y * n + x] = Mathf.Abs(x - n * 0.5f + 0.5f) <= halfWidth ? Color.white : Color.clear;
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }

    static Color[] BuildCirclePixels()
    {
        const int n = 32;
        var px = new Color[n * n];
        var c = Color.white;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n * 0.5f - 0.5f, n * 0.5f - 0.5f));
                px[y * n + x] = d <= n * 0.5f - 0.5f ? c : Color.clear;
            }
        return px;
    }
}
