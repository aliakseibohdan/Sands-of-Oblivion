using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuEvents : MonoBehaviour
{
    private UIDocument _document;

    private Button _newGameButton;

    private List<Button> _menuButtons = new();

    private AudioSource _audioSource;

    private void Awake()
    {
        _document = GetComponent<UIDocument>();
        _audioSource = GetComponent<AudioSource>();

        _newGameButton = _document.rootVisualElement.Q("NewGameButton") as Button;
        _newGameButton.RegisterCallback<ClickEvent>(OnNewGameClick);

        _menuButtons = _document.rootVisualElement.Query<Button>().ToList();
        for (int i = 0; i < _menuButtons.Count; i++)
        {
            _menuButtons[i].RegisterCallback<ClickEvent>(OnAllButtonsClick);
        }
    }

    private void OnDisable()
    {
        _newGameButton.UnregisterCallback<ClickEvent>(OnNewGameClick);

        for (int i = 0; i < _menuButtons.Count; i++)
        {
            _menuButtons[i].UnregisterCallback<ClickEvent>(OnAllButtonsClick);
        }
    }

    private void OnNewGameClick(ClickEvent evt)
    {

    }

    private void OnAllButtonsClick(ClickEvent evt)
    {
        _audioSource.Play();
    }
}
