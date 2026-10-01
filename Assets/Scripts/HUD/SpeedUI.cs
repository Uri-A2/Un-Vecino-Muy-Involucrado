using TMPro;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(PlayerMotor))]
public class SpeedometerUI : MonoBehaviour
{
    public enum SpeedMode { HorizontalOnly, FullVector }

    [Header("References")]
    public TMP_Text speedText;     
    public SpeedMode mode = SpeedMode.HorizontalOnly;

    [Header("Display")]
    public bool showAcceleration = true;
    public bool showAsKmh = false;       
    public string speedFormat = "F1";
    public float smoothing = 8f;         
    [Header("BbBar Fill")]
    public Image speedBar;               
    public float speedBarMax = 25f;      

    PlayerMotor _motor;
    float _displaySpeed;
    float _lastSpeed;
    float _acceleration;

    public float CurrentSpeed { get; private set; }
    public float CurrentAcceleration => _acceleration;

    void Awake()
    {
        _motor = GetComponent<PlayerMotor>();
    }

    void Update()
    {
        Vector3 vel = _motor.Velocity;
        float rawSpeed = mode == SpeedMode.HorizontalOnly
            ? new Vector3(vel.x, 0f, vel.z).magnitude
            : vel.magnitude;

       
        _displaySpeed = Mathf.Lerp(_displaySpeed, rawSpeed, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

        _acceleration = Time.deltaTime > 0f ? (rawSpeed - _lastSpeed) / Time.deltaTime : 0f;
        _lastSpeed = rawSpeed;
        CurrentSpeed = rawSpeed;

        UpdateUI(rawSpeed);
    }

    void UpdateUI(float rawSpeed)
    {
        if (speedText != null)
        {
            float shown = showAsKmh ? _displaySpeed * 3.6f : _displaySpeed;
            string unit = showAsKmh ? "km/h" : "u/s";

            string line = $"Speed: {shown.ToString(speedFormat)} {unit}";
            if (showAcceleration)
            {
                float accelShown = showAsKmh ? _acceleration * 3.6f : _acceleration;
                string sign = accelShown >= 0f ? "+" : "";
                line += $"\nAccel: {sign}{accelShown.ToString(speedFormat)} {unit}\u00b2";
            }

            speedText.text = line;
        }

        if (speedBar != null)
        {
            speedBar.fillAmount = Mathf.Clamp01(rawSpeed / speedBarMax);
        }
    }
}