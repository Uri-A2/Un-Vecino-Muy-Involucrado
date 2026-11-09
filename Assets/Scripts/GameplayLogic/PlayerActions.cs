using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActions : MonoBehaviour
{
    [System.Serializable]
    public class Weapon
    {
        public string name = "Weapon";
        public Sprite icon;
        public WeaponType type = WeaponType.Default;
        public float fireRate = 4f;
        public float range = 60f;
        public AudioClip shotSfx;

        [Header("Visua")]
        public WaterShotVisual shotVisualPrefab;
        public float shotVisualSpeed = 60f;
        public Transform visualMuzzle;

        [Header("MailLauncher only")]
        public MailProjectile projectilePrefab;
        public float projectileSpeed = 25f;
        public float projectileLifetime = 4f;
        public Transform muzzle;
    }

    [SerializeField] Camera cam;
    [SerializeField] Weapon[] weapons;
    [SerializeField] WaterGunJam waterGun;
    [SerializeField] LayerMask shootMask = ~0;
    [SerializeField] float interactRadius = 3f;
    [SerializeField] LayerMask interactMask = ~0;

    public int CurrentIndex { get; private set; }
    public Weapon Current => (weapons != null && weapons.Length > 0) ? weapons[CurrentIndex] : null;
    public Weapon[] Weapons => weapons;
    public event System.Action<int> OnWeaponChanged;
    public event System.Action OnShot;
    float nextShot;

    void Awake() { if (cam == null) cam = Camera.main; }

    void Update()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (scroll > 0f) SwitchWeapon(1);
        else if (scroll < 0f) SwitchWeapon(-1);

        bool wantsFire = mouse.leftButton.isPressed && Current != null;
        bool isWater = Current != null && Current.type == WeaponType.WaterGun;
        bool jammed = isWater && waterGun != null && waterGun.Jammed;

        if (waterGun != null) waterGun.SetHeld(isWater && wantsFire);
        if (wantsFire && !jammed && Time.time >= nextShot) Shoot(Current);

        if (kb.eKey.wasPressedThisFrame) Interact();
        if (kb.qKey.wasPressedThisFrame && EfficiencyMeter.Instance != null) EfficiencyMeter.Instance.TryActivateNitro();
    }

    void SwitchWeapon(int dir)
    {
        if (weapons == null || weapons.Length < 2) return;
        CurrentIndex = (CurrentIndex + dir + weapons.Length) % weapons.Length;
        OnWeaponChanged?.Invoke(CurrentIndex);
    }

    void Shoot(Weapon w)
    {
        Debug.Log($"Shoot: weapon={w.name}, type={w.type}");
        nextShot = Time.time + 1f / Mathf.Max(0.01f, w.fireRate);
        OnShot?.Invoke();
        if (w.shotSfx != null) AudioSource.PlayClipAtPoint(w.shotSfx, transform.position);

        if (w.type == WeaponType.MailLauncher && w.projectilePrefab != null)
        {
            FireProjectile(w);
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 endPoint = ray.GetPoint(w.range);

        var hits = Physics.RaycastAll(ray, w.range, shootMask, QueryTriggerInteraction.Ignore);
        Debug.Log($"Hits: {hits.Length}, mask={shootMask.value}, cam={(cam != null ? cam.name : "NULL")}");

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.transform.IsChildOf(transform)) continue;
            Debug.Log($"Ray hit: {h.collider.name} (layer {LayerMask.LayerToName(h.collider.gameObject.layer)}), shootable={h.collider.GetComponentInParent<IShootable>() != null}");
            endPoint = h.point;
            var target = h.collider.GetComponentInParent<IShootable>();
            if (target != null) target.OnShot(h, w.type);
            break;
        }

        if (w.shotVisualPrefab != null)
        {
            Transform m = w.visualMuzzle != null ? w.visualMuzzle : w.muzzle;
            Vector3 origin = m != null ? m.position : transform.position + Vector3.up;
            Instantiate(w.shotVisualPrefab).Launch(origin, endPoint, w.shotVisualSpeed);
        }
    }

    void FireProjectile(Weapon w)
    {
        Vector3 origin = w.muzzle != null ? w.muzzle.position : transform.position + Vector3.up;
        Vector3 dir = cam != null ? cam.transform.forward : transform.forward;

        var proj = Instantiate(w.projectilePrefab, origin, Quaternion.LookRotation(dir));
        proj.Launch(dir, w.projectileSpeed, w.type, w.projectileLifetime, transform, shootMask);
    }

    void Interact()
    {
        var cols = Physics.OverlapSphere(transform.position, interactRadius, interactMask, QueryTriggerInteraction.Ignore);
        IInteractable best = null;
        float bestDist = float.MaxValue;
        foreach (var c in cols)
        {
            var it = c.GetComponentInParent<IInteractable>();
            if (it == null) continue;
            float d = (c.ClosestPoint(transform.position) - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = it; }
        }
        best?.Interact();
    }
    public void SetWeapon(int index)
    {
        if (weapons == null || weapons.Length == 0) return;
        index = Mathf.Clamp(index, 0, weapons.Length - 1);
        if (index == CurrentIndex) return;
        CurrentIndex = index;
        OnWeaponChanged?.Invoke(CurrentIndex);
    }

}