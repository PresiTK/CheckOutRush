using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] string menuScene = "MainMenu";

    [SerializeField] GameObject panel;

    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text subtitle;

    [SerializeField] string subtitleFormat = "Products: {0} / {1}";

    [SerializeField] Button playAgainButton;
    [SerializeField] Button menuButton;

    [Header("Ganar")]
    [SerializeField] string winText = "YOU WIN!";
    [SerializeField] Color winColor = new Color(1f, 0.78f, 0.2f);

    [Header("Perder")]
    [SerializeField] string loseText = "YOU LOSE";
    [SerializeField] Color loseColor = new Color(1f, 0.35f, 0.35f);

    GameManager subscribed;

    void Awake()
    {
        if (panel == null) panel = gameObject;
        playAgainButton.onClick.AddListener(PlayAgain);
        menuButton.onClick.AddListener(GoToMenu);
    }

    void Start()
    {
        panel.SetActive(false);

        subscribed = GameManager.Instance;
        if (subscribed == null)
        {
            Debug.LogWarning("No hay GameManager: la pantalla de fin de partida no saldrá.", this);
            return;
        }

        subscribed.GameEnded += Show;
    }

    void OnDestroy()
    {
        if (subscribed != null) subscribed.GameEnded -= Show;
        subscribed = null;
    }

    void Show(bool won)
    {
        title.text = won ? winText : loseText;
        title.color = won ? winColor : loseColor;

        if (subtitle != null)
        {
            int collected = 0;
            foreach (var product in subscribed.Recipe)
                if (subscribed.IsCollected(product)) collected++;
            subtitle.text = string.Format(subtitleFormat, collected, subscribed.Recipe.Count);
        }

        panel.SetActive(true);
        playAgainButton.Select();
    }

    void PlayAgain()
    {
        TutorialUI.SkipNextTime = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoToMenu() => SceneManager.LoadScene(menuScene);
}
