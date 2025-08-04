using UnityEngine;
using UnityEngine.InputSystem;
using Game.Input;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private InputActions _inputActions;
    private InputActionMap _currentMap;

    // Public accessors
    public InputActions.GameplayActions Gameplay => _inputActions.Gameplay;
    public InputActions.DialogueActions Dialogue => _inputActions.Dialogue;
    public InputActions.BackpackActions Backpack => _inputActions.Backpack;
    public InputActions.JournalActions Journal => _inputActions.Journal;

    public InputActionMap CurrentMap { get => _currentMap; set => _currentMap = value; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _inputActions = new InputActions();
        EnableGameplayInput();
    }

    public void EnableGameplayInput()
    {
        DisableAllInput();
        _inputActions.Gameplay.Enable();
        _currentMap = _inputActions.Gameplay;
    }

    public void EnableDialogueInput()
    {
        DisableAllInput();
        _inputActions.Dialogue.Enable();
        _currentMap = _inputActions.Dialogue;
    }

    public void EnableBackpackInput()
    {
        DisableAllInput();
        _inputActions.Backpack.Enable();
        _currentMap = _inputActions.Backpack;
    }

    public void EnableJournalInput()
    {
        DisableAllInput();
        _inputActions.Journal.Enable();
        _currentMap = _inputActions.Journal;
    }

    private void DisableAllInput()
    {
        _inputActions.Gameplay.Disable();
        _inputActions.Dialogue.Disable();
        _inputActions.Backpack.Disable();
        _inputActions.Journal.Disable();
    }

    private void OnDestroy()
    {
        _inputActions?.Dispose();
    }
}