using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;          
    [SerializeField] Button resumeButton;
    [SerializeField] Button quitToMenuButton;
    [SerializeField] string menuSceneName = "MainMenu";

    public bool IsPaused { get; private set; }

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (quitToMenuButton != null) quitToMenuButton.onClick.AddListener(QuitToMenu);
    }

    void Update()
    {
        if (!EscapePressed()) return;

        if (!IsPaused && RunTimer.Instance != null && !RunTimer.Instance.Running) return;

        if (IsPaused) Resume(); else Pause();
    }

    static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    public void Pause()
    {
        IsPaused = true;
        if (panel != null) panel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        IsPaused = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;   
        SceneManager.LoadScene(menuSceneName);
    }
}
