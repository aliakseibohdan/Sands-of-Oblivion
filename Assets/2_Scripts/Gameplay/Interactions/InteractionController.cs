using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float _interactionDistance = 3f;
    [SerializeField] private LayerMask _interactionLayer;

    [Header("References")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private InputActionReference _interactAction;

    private InteractiveElement _currentInteractive;

    private void OnEnable()
    {
        _interactAction.action.Enable();
        _interactAction.action.performed += OnInteract;
    }

    private void OnDisable()
    {
        _interactAction.action.performed -= OnInteract;
        _interactAction.action.Disable();
    }

    private void Update()
    {
        FindInteractiveElements();
    }

    private void FindInteractiveElements()
    {
        Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, _interactionDistance, _interactionLayer))
        {
            var interactive = hit.collider.GetComponent<InteractiveElement>();
            if (interactive != _currentInteractive)
            {
                _currentInteractive?.OnPointerExit(null);
                _currentInteractive = interactive;
                _currentInteractive?.OnPointerEnter(null);
            }
        }
        else if (_currentInteractive != null)
        {
            _currentInteractive.OnPointerExit(null);
            _currentInteractive = null;
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (_currentInteractive != null)
        {
            _currentInteractive.OnPointerClick(null);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_playerCamera == null) return;

        Gizmos.color = Color.cyan;
        Vector3 rayStart = _playerCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0));
        Gizmos.DrawRay(rayStart, _playerCamera.transform.forward * _interactionDistance);

        if (_currentInteractive != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(_currentInteractive.transform.position, Vector3.one * 1.2f);
        }
    }
#endif
}