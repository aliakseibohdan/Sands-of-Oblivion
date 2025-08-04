using UnityEngine;

public class JournalSystem : MonoBehaviour
{
    [SerializeField] private GameObject _journalUI;
    [SerializeField] private CanvasGroup _backpackOverlay;

    private bool _isOpen;

    public void ToggleJournal()
    {
        _isOpen = !_isOpen;
        _journalUI.SetActive(_isOpen);
        _backpackOverlay.alpha = _isOpen ? 0.2f : 0;
    }
}