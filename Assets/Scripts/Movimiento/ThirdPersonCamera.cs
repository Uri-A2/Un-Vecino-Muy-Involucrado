using UnityEngine;
public class FirstPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                                  
    public Vector3 eyeOffset = new Vector3(0f, 1.6f, 0f);      

    [Header("Mouse Look")]
    public float mouseSensitivity = 3f;
    public float minPitch = -85f;
    public float maxPitch = 85f;
    public bool invertY = false;

    [Header("Follow Smoothing")]
    public float positionSmoothTime = 0f;
    public bool rotatePlayerWithCamera = true;               

    float _yaw;
    float _pitch;
    Vector3 _followVelocity;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        _yaw = target != null ? target.eulerAngles.y : angles.y;
        _pitch = angles.x > 180f ? angles.x - 360f : angles.x;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (target == null) return;
        HandleMouseLook();
    }

    void LateUpdate()
    {
        if (target == null) return;

       
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

     
        Vector3 eyePos = target.position + eyeOffset;
        transform.position = positionSmoothTime > 0f
            ? Vector3.SmoothDamp(transform.position, eyePos, ref _followVelocity, positionSmoothTime)
            : eyePos;

        if (rotatePlayerWithCamera)
            target.rotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertY ? 1f : -1f);

        _yaw += mouseX;
        _pitch += mouseY;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }
    public Vector3 GetFlatForward()
    {
        Vector3 f = transform.forward;
        f.y = 0f;
        return f.normalized;
    }

    public Vector3 GetFlatRight()
    {
        Vector3 r = transform.right;
        r.y = 0f;
        return r.normalized;
    }
}