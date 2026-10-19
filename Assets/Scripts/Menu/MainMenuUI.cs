using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] string gameScene = "SampleScene";

    [Header("Paneles")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject optionsPanel;
    [SerializeField] GameObject title;

    [Header("Botones")]
    [SerializeField] Button playButton;
    [SerializeField] Button optionsButton;
    [SerializeField] Button backButton;

    void Start()
    {
        playButton.onClick.AddListener(Play);
        optionsButton.onClick.AddListener(() => ShowOptions(true));
        backButton.onClick.AddListener(() => ShowOptions(false));

        ShowOptions(false);
    }

    void Play() => SceneManager.LoadScene(gameScene);

    void ShowOptions(bool show)
    {
        mainPanel.SetActive(!show);
        optionsPanel.SetActive(show);
        if (title != null) title.SetActive(!show);
    }
}
