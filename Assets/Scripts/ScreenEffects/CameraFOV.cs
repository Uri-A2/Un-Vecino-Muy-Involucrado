using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class SpeedEffects : MonoBehaviour
{
    [Header("References")]
    public PlayerMotor motor;
        

    [Header("Speed Range")]
    public float startSpeed = 11f;         
    public float maxSpeed = 22f;           

    [Header("FOV")]
    public float baseFov = 70f;
    public float maxExtraFov = 18f;
   

    [Header("Smoothing")]
    public float riseSpeed = 6f;          
    public float fallSpeed = 3f;           

    Camera _cam;
    float _intensity;                    

    void Awake()
    {
        _cam = GetComponent<Camera>();
        baseFov = _cam.fieldOfView;
    }

    void LateUpdate()
    {
        if (motor == null) return;

        Vector3 v = motor.Velocity;
        v.y = 0f;                         
        float speed = v.magnitude;

        float target = Mathf.InverseLerp(startSpeed, maxSpeed, speed);
        float rate = target > _intensity ? riseSpeed : fallSpeed;
        _intensity = Mathf.MoveTowards(_intensity, target, rate * Time.deltaTime);

        _cam.fieldOfView = baseFov + maxExtraFov * _intensity;

     
    }
}