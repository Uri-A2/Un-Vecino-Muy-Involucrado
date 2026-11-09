using UnityEngine;

public class MailProjectile : MonoBehaviour
{
    [SerializeField] ParticleSystem impactVfx;
    [SerializeField] AudioClip impactSfx;
    [SerializeField] float gravity = 0f;      

    Vector3 velocity;
    WeaponType weaponType;
    float lifeLeft = 4f;
    Transform shooter;
    LayerMask mask;

    public void Launch(Vector3 dir, float speed, WeaponType type, float lifetime, Transform shooterTransform, LayerMask shootMask)
    {
        velocity = dir.normalized * speed;
        weaponType = type;
        lifeLeft = lifetime;
        shooter = shooterTransform;
        mask = shootMask;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        lifeLeft -= dt;
        if (lifeLeft <= 0f) { Destroy(gameObject); return; }

        if (gravity != 0f) velocity += Vector3.down * gravity * dt;

        Vector3 step = velocity * dt;
        float dist = step.magnitude;
        if (dist > 0.0001f && Sweep(step, dist)) return;

        transform.position += step;
        if (velocity.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(velocity);
    }

    bool Sweep(Vector3 step, float dist)
    {
      
        if (Physics.SphereCast(transform.position, 0.15f, step.normalized, out var hit,
                dist, mask, QueryTriggerInteraction.Collide))
        {
            if (shooter == null || !hit.transform.IsChildOf(shooter))
            {
                var target = hit.collider.GetComponentInParent<IShootable>();
                if (target != null)
                {
                    target.OnShot(hit, weaponType);
                    Impact(hit.point);
                    return true;
                }
            }
        }

 
        if (Physics.SphereCast(transform.position, 0.15f, step.normalized, out var solid,
                dist, mask, QueryTriggerInteraction.Ignore))
        {
            if (shooter == null || !solid.transform.IsChildOf(shooter))
            {
                Impact(solid.point);
                return true;
            }
        }

        return false;
    }

    void Impact(Vector3 pos)
    {
        if (impactVfx != null)
        {
            var fx = Instantiate(impactVfx, pos, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 3f);
        }
        if (impactSfx != null) AudioSource.PlayClipAtPoint(impactSfx, pos);
        Destroy(gameObject);
    }
}