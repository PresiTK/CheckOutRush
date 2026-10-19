using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] GameObject lookPanelPrefab;

    [SerializeField] RectTransform lookPanelParent;

    GameObject lookPanel;
    TMP_Text lookText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (lookPanelParent == null) lookPanelParent = (RectTransform)transform;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ShowLookText(string message)
    {
        if (lookPanel == null)
        {
            lookPanel = Instantiate(lookPanelPrefab, lookPanelParent, false);
            lookText = lookPanel.GetComponentInChildren<TMP_Text>();
        }
        lookText.text = message;
    }

    public void HideLookText()
    {
        if (lookPanel != null) Destroy(lookPanel);
        lookPanel = null;
        lookText = null;
    }
}
