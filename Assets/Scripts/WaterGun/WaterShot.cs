using UnityEngine;

public class WaterShotVisual : MonoBehaviour
{
    [SerializeField] ParticleSystem splashVfx;

    Vector3 target;
    float speed;

    public void Launch(Vector3 from, Vector3 to, float travelSpeed)
    {
        transform.position = from;
        target = to;
        speed = travelSpeed;
        Vector3 d = to - from;
        if (d.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(d);
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if ((transform.position - target).sqrMagnitude > 0.0001f) return;

        if (splashVfx != null)
        {
            var fx = Instantiate(splashVfx, target, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 2f);
        }
        Destroy(gameObject);
    }
}