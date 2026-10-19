using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] string menuScene = "MainMenu";

    [SerializeField] GameObject panel;
    [SerializeField] GameObject mainCard;
    [SerializeField] GameObject optionsCard;

    [SerializeField] Button resumeButton;
    [SerializeField] Button optionsButton;
    [SerializeField] Button mainMenuButton;
    [SerializeField] Button backButton;

    public bool IsPaused { get; private set; }

    void Start() => panel.SetActive(false);

    void OnDestroy()
    {
        if (IsPaused) AudioListener.pause = false;
    }

    void Update()
    {
        var input = InputManager.Instance;
        if (input == null || TutorialUI.IsShowing) return;
        if (!input.InputMap.Player.Menu.WasPressedThisFrame() && !input.InputMap.UI.Menu.WasPressedThisFrame()) return;

        if (IsPaused) Resume();
        else if (GameManager.Instance == null || !GameManager.Instance.IsGameOver) Pause();
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        InputManager.Instance.SetMenuOpen(true);
        panel.SetActive(true);
        ShowOptions(false);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        InputManager.Instance.SetMenuOpen(false);
        panel.SetActive(false);
    }

    public void ShowOptions(bool show)
    {
        mainCard.SetActive(!show);
        optionsCard.SetActive(show);
        (show ? backButton : resumeButton).Select();
    }

    public void GoToMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(menuScene);
    }
}
