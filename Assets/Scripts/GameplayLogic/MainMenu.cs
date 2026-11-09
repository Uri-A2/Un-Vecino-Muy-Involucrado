using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject howToPlayPanel;

    [Header("Option")]
    [SerializeField] private Slider volumeSlider;

    private const string VolumeKey = "MasterVolume";

    private void Start()
    {
        Time.timeScale = 1f;

        float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        AudioListener.volume = volume;

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(volume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        ShowMain();
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void ShowMain() => ShowOnly(mainPanel);
    public void ShowOptions() => ShowOnly(optionsPanel);
    public void ShowCredits() => ShowOnly(creditsPanel);
    public void ShowHowToPlay() => ShowOnly(howToPlayPanel);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
    }
    public void ToggleHowToPlay()
    {
        if (howToPlayPanel != null && howToPlayPanel.activeSelf)
            ShowMain();
        else
            ShowHowToPlay();
    }

    private void ShowOnly(GameObject panel)
    {
        if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
        if (optionsPanel != null) optionsPanel.SetActive(panel == optionsPanel);
        if (creditsPanel != null) creditsPanel.SetActive(panel == creditsPanel);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(panel == howToPlayPanel);
    }
}