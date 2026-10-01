using System;
using UnityEngine;

//Timer global
public class RunTimer : MonoBehaviour
{
    public static RunTimer Instance { get; private set; }

    [SerializeField] float startTime = 120f;

    public float TimeLeft { get; private set; }
    public bool Running { get; private set; }
    public event Action OnTimeUp;

    void Awake()
    {
        Instance = this;
        TimeLeft = startTime;
        Running = true;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (!Running) return;
        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            Running = false;
            OnTimeUp?.Invoke();
        }
    }

    public void AddTime(float seconds)
    {
        if (!Running) return;
        TimeLeft = Mathf.Max(0f, TimeLeft + seconds);
    }
}
