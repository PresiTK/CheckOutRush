using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TimerUI : MonoBehaviour
{
    [SerializeField, Min(0)] int warningSeconds = 10;

    [SerializeField] Color warningColor = new Color(1f, 0.3f, 0.3f);

    TMP_Text text;
    Color normalColor;
    GameTimer subscribed;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        normalColor = text.color;
    }

    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("No hay GameManager: el temporizador no se muestra.", this);
            return;
        }

        subscribed = GameManager.Instance.Timer;
        subscribed.Ticked += Show;
        Show(subscribed.IsRunning ? subscribed.Remaining : subscribed.Duration);
    }

    void OnDestroy()
    {
        if (subscribed != null) subscribed.Ticked -= Show;
        subscribed = null;
    }

    void Show(int seconds)
    {
        text.text = $"{seconds / 60:00}:{seconds % 60:00}";
        text.color = seconds <= warningSeconds ? warningColor : normalColor;
    }
}
