using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class WeaponWheelUI : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public RectTransform root;   
        public Image icon;           
        public TMP_Text label;       
        [System.NonSerialized] public CanvasGroup group;
        [System.NonSerialized] public int weaponIndex;
    }
    float displayStep;
    int targetStep;
    int lastIndex;
    [SerializeField] PlayerActions player;
    [SerializeField] Slot[] slots = new Slot[3];
    [Header("loook")]
    [SerializeField] float spacing = 90f;
    [SerializeField] float sideScale = 0.65f;
    [SerializeField] float sideAlpha = 0.45f;
    [SerializeField] float slideSpeed = 18f;

  

    void Start()
    {
        if (player == null) { enabled = false; return; }

        lastIndex = player.CurrentIndex;
        targetStep = lastIndex;
        displayStep = lastIndex;

        foreach (var s in slots)
        {
            if (s == null || s.root == null) continue;
            s.group = s.root.GetComponent<CanvasGroup>();
            if (s.group == null) s.group = s.root.gameObject.AddComponent<CanvasGroup>();
            var slot = s;
       
        }
    }

    void OnEnable() { if (player != null) player.OnWeaponChanged += OnChanged; }
    void OnDisable() { if (player != null) player.OnWeaponChanged -= OnChanged; }

    void OnChanged(int newIndex)
    {
        int n = player.Weapons.Length;
        int delta = newIndex - lastIndex;           
        if (delta > n / 2) delta -= n;
        else if (delta < -n / 2) delta += n;

        targetStep += delta;
        lastIndex = newIndex;
    }

    void Update()
    {
        var weapons = player.Weapons;
        if (weapons == null || weapons.Length == 0) return;
        int n = weapons.Length;

        displayStep = Mathf.Lerp(displayStep, targetStep, 1f - Mathf.Exp(-slideSpeed * Time.unscaledDeltaTime));
        if (Mathf.Abs(displayStep - targetStep) < 0.001f) displayStep = targetStep;

        int baseStep = Mathf.RoundToInt(displayStep);
        float frac = displayStep - baseStep;
        float half = slots.Length / 2f;

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s == null || s.root == null || s.group == null) continue;

            int j = i - slots.Length / 2;           
            int idx = Wrap(baseStep + j, n);
            s.weaponIndex = idx;
            bool allowed = n >= slots.Length || (j >= -(n - 1) / 2 && j <= n / 2);
            if (!allowed) { s.group.alpha = 0f; s.group.blocksRaycasts = false; continue; }

            var w = weapons[idx];
            if (s.label != null) s.label.text = w.name;
            if (s.icon != null)
            {
                s.icon.sprite = w.icon;
                s.icon.enabled = w.icon != null;
            }
            float p = j - frac;                     
            float dist = Mathf.Abs(p);
            float t = Mathf.Clamp01(dist);

            float alpha = Mathf.Lerp(1f, sideAlpha, t) * Mathf.Clamp01((half - dist) / 0.5f);
            s.group.alpha = alpha;
            s.group.blocksRaycasts = alpha > 0.1f;

            s.root.anchoredPosition = new Vector2(s.root.anchoredPosition.x, p * spacing);
            s.root.localScale = Vector3.one * Mathf.Lerp(1f, sideScale, t);
        }
    }

    static int Wrap(int x, int n) => ((x % n) + n) % n;
}