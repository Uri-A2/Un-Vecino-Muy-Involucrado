using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;          
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f); 
    [Header("Mouse Look")]
    public float mouseSensitivity = 3f;
    public float minPitch = -40f;
    public float maxPitch = 75f;
    public bool invertY = false;

    [Header("Orbit Distance")]
    public float defaultDistance = 4.5f;
    public float minDistance = 0.8f;   
    public float zoomSpeed = 3f;
    public float minZoom = 2f;
    public float maxZoom = 8f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.25f; 
    public float collisionSmoothTime = 0.08f;

    [Header("Follow Smoothing")]
    public float positionSmoothTime = 0.05f;
    public bool rotatePlayerWithCamera = true;
    public float playerRotationSpeed = 12f;

    float _yaw;
    float _pitch;
    float _currentDistance;
    float _distanceVelocity;
    Vector3 _followVelocity;

    void Start()
    {
        _currentDistance = defaultDistance;
        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        _pitch = angles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (target == null) return;

        HandleMouseLook();
        HandleZoom();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 pivot = target.position + targetOffset;
        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);

       
        Vector3 desiredPos = pivot - rot * Vector3.forward * defaultDistance;

        float targetDist = defaultDistance;

       
        Vector3 dir = (desiredPos - pivot).normalized;
        if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit,
            defaultDistance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            targetDist = Mathf.Clamp(hit.distance, minDistance, defaultDistance);
        }

        _currentDistance = Mathf.SmoothDamp(_currentDistance, targetDist, ref _distanceVelocity, collisionSmoothTime);

        Vector3 finalPos = pivot - rot * Vector3.forward * _currentDistance;

        transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref _followVelocity, positionSmoothTime);
        transform.rotation = rot;

        if (rotatePlayerWithCamera)
            RotatePlayerTowardCameraForward();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (invertY ? 1f : -1f);

        _yaw += mouseX;
        _pitch += mouseY;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            defaultDistance = Mathf.Clamp(defaultDistance - scroll * zoomSpeed, minZoom, maxZoom);
        }
    }

    void RotatePlayerTowardCameraForward()
    {
       
        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
        target.rotation = Quaternion.Slerp(target.rotation, targetRot, playerRotationSpeed * Time.deltaTime);
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
