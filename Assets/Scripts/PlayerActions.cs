using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActions : MonoBehaviour
{
    [System.Serializable]
    public class Weapon
    {
        public string name = "Weapon";
        public WeaponType type = WeaponType.Default;
        public float fireRate = 4f;  
        public float range = 60f;
        public AudioClip shotSfx;

        [Header("MailLauncher only")]
        public MailProjectile projectilePrefab;   
        public float projectileSpeed = 25f;
        public float projectileLifetime = 4f;
        public Transform muzzle;                 
    }

    [SerializeField] Camera cam;
    [SerializeField] Weapon[] weapons;
    [SerializeField] LayerMask shootMask = ~0;
    [SerializeField] float interactRadius = 3f;
    [SerializeField] LayerMask interactMask = ~0;

    public int CurrentIndex { get; private set; }
    public Weapon Current => (weapons != null && weapons.Length > 0) ? weapons[CurrentIndex] : null;
    public Weapon[] Weapons => weapons;
    public event System.Action<int> OnWeaponChanged;

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

        if (mouse.leftButton.isPressed && Time.time >= nextShot && Current != null) Shoot(Current);
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
        nextShot = Time.time + 1f / Mathf.Max(0.01f, w.fireRate);
        if (w.shotSfx != null) AudioSource.PlayClipAtPoint(w.shotSfx, transform.position);

        if (w.type == WeaponType.MailLauncher && w.projectilePrefab != null)
        {
            FireProjectile(w);
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var hits = Physics.RaycastAll(ray, w.range, shootMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.transform.IsChildOf(transform)) continue; 
            var target = h.collider.GetComponentInParent<IShootable>();
            if (target != null) target.OnShot(h, w.type);
            break; 
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
}