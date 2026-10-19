using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public InputSystem_Actions InputMap { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InputMap = new InputSystem_Actions();

        InputMap.Player.Enable();
    }

    void OnDestroy()
    {
        if (Instance != this) return;

        InputMap.Disable();
        InputMap.Dispose();
        Instance = null;
    }

    public void SetMenuOpen(bool open)
    {
        if (open)
        {
            InputMap.Player.Disable();
            InputMap.UI.Enable();
        }
        else
        {
            InputMap.UI.Disable();
            InputMap.Player.Enable();
        }
    }
}
