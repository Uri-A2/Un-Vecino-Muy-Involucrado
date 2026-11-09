using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScoreScreen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject panel;            
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text tasksText;
    [SerializeField] TMP_Text streakText;
    [SerializeField] TMP_Text rankText;
    [SerializeField] TMP_Text scoreText;
    [SerializeField] Button restartButton;

    [Header("Score")]
    [SerializeField] int pointsPerTask = 100;
    [SerializeField] int pointsPerStreakStep = 25;
    [SerializeField] int pointsPerRankLevel = 150;
    [SerializeField] float countUpDuration = 1.2f;

    bool subscribed;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        TrySubscribe();
    }

    void Update() { if (!subscribed) TrySubscribe(); }

    void TrySubscribe()
    {
        if (RunTimer.Instance == null) return;
        RunTimer.Instance.OnTimeUp += Show;
        subscribed = true;
    }

    void OnDestroy()
    {
        if (subscribed && RunTimer.Instance != null) RunTimer.Instance.OnTimeUp -= Show;
    }

    void Show()
    {
        var m = EfficiencyMeter.Instance;
        int tasks = m != null ? m.TasksCompleted : 0;
        int bestStreak = m != null ? m.BestStreak : 0;
        int peakRank = m != null ? m.PeakRankIndex : 0;
        string peakName = EfficiencyMeter.RankNames[peakRank];

        int score = tasks * pointsPerTask + bestStreak * pointsPerStreakStep + peakRank * pointsPerRankLevel;

        if (titleText != null) titleText.text = "¡Se acabó el tiempo!";
        if (tasksText != null) tasksText.text = $"Tareas completadas: {tasks}";
        if (streakText != null) streakText.text = $"Mejor racha: {bestStreak}";
        if (rankText != null) rankText.text = $"Mejor rango: {peakName}";
        if (scoreText != null) scoreText.text = "0";

        if (panel != null) panel.SetActive(true);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(CountUp(score));
    }

    System.Collections.IEnumerator CountUp(int target)
    {
        float t = 0f;
        while (t < countUpDuration)
        {
            t += Time.unscaledDeltaTime;   
            if (scoreText != null)
                scoreText.text = Mathf.RoundToInt(Mathf.Lerp(0, target, t / countUpDuration)).ToString();
            yield return null;
        }
        if (scoreText != null) scoreText.text = target.ToString();
    }

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
