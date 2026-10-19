using UnityEngine;
using UnityEngine.UI;

public class TutorialUI : MonoBehaviour
{
    public static bool IsShowing { get; private set; }
    public static bool SkipNextTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        IsShowing = false;
        SkipNextTime = false;
    }

    [SerializeField] GameObject panel;
    [SerializeField] GameObject[] pages;
    [SerializeField] Button skipButton;
    [SerializeField] Button playButton;

    int page;

    void Awake()
    {
        skipButton.onClick.AddListener(() => ShowPage(page + 1));
        playButton.onClick.AddListener(Close);
    }

    void Start()
    {
        if (SkipNextTime || pages.Length == 0)
        {
            SkipNextTime = false;
            panel.SetActive(false);
            return;
        }
        Open();
    }

    void OnDestroy()
    {
        if (!IsShowing) return;
        IsShowing = false;
        AudioListener.pause = false;
    }

    void Open()
    {
        IsShowing = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        if (InputManager.Instance != null) InputManager.Instance.SetMenuOpen(true);
        panel.SetActive(true);
        ShowPage(0);
    }

    void ShowPage(int index)
    {
        page = Mathf.Clamp(index, 0, pages.Length - 1);
        for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == page);

        bool last = page == pages.Length - 1;
        skipButton.gameObject.SetActive(!last);
        playButton.gameObject.SetActive(last);
        (last ? playButton : skipButton).Select();
    }

    void Close()
    {
        IsShowing = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (InputManager.Instance != null) InputManager.Instance.SetMenuOpen(false);
        panel.SetActive(false);
    }
}
