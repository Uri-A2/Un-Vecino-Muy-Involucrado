using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class GameHUD : MonoBehaviour
{
    [SerializeField] TMP_Text timeText, rankText, streakText, poiText;
    [SerializeField] Image nitroFill;              
    [SerializeField] GameObject nitroReadyLabel;

    void Update()
    {
        var timer = RunTimer.Instance;
        var eff = EfficiencyMeter.Instance;

        if (timer != null && timeText != null)
        {
            int s = Mathf.CeilToInt(timer.TimeLeft);
            timeText.text = $"{s / 60}:{s % 60:00}";
        }
        if (poiText != null) poiText.text = $"Tareas: {TaskStats.Completed}";

        if (eff == null) return;
        if (rankText != null) rankText.text = eff.RankName;
        if (streakText != null) streakText.text = eff.Streak > 0 ? $"x{eff.Streak + 1}" : "";
        if (nitroFill != null) nitroFill.fillAmount = eff.Value;
        if (nitroReadyLabel != null) nitroReadyLabel.SetActive(eff.NitroReady);
    }
}