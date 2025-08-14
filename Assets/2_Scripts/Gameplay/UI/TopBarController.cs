using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class TopBarController : MonoBehaviour
{
    [Header("References")]
    public RectTransform hiddenPanel;
    public RectTransform triggerZone;

    [Header("Settings")]
    public float slideDuration = 0.3f;
    public bool debugTrigger = true;
    public float hideDelay = 0.3f;

    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;
    private Coroutine slideCoroutine;
    private Coroutine hideCoroutine;
    private bool isPanelVisible;
    private bool isAnimating;
    private bool isCursorOverPanel;

    void Start()
    {
        // Calculate positions
        hiddenPosition = new Vector2(0, hiddenPanel.rect.height + 5);
        visiblePosition = new Vector2(0, 5);

        // Initialize panel position
        hiddenPanel.anchoredPosition = hiddenPosition;

        // Debug visualization
        if (!debugTrigger && triggerZone.TryGetComponent<UnityEngine.UI.Image>(out var image))
        {
            image.color = Color.clear;
        }
    }

    void Update()
    {
        // Only check when panel is visible and not animating
        if (isPanelVisible && !isAnimating)
        {
            // Check if cursor is over panel or any of its children
            isCursorOverPanel = IsCursorOverPanel();

            // Start delayed hide if cursor leaves panel
            if (!isCursorOverPanel && hideCoroutine == null)
            {
                hideCoroutine = StartCoroutine(HideAfterDelay());
            }
            // Cancel hide if cursor re-enters panel
            else if (isCursorOverPanel && hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }
        }
    }

    // Called by TriggerZone's EventTrigger
    public void OnTriggerZoneEnter()
    {
        if (!isPanelVisible && !isAnimating)
        {
            ShowPanel();
        }
    }

    private void ShowPanel()
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }
        slideCoroutine = StartCoroutine(SlideAnimation(true));
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);

        // Re-check if cursor is still not over panel
        if (!IsCursorOverPanel() && isPanelVisible && !isAnimating)
        {
            StartSlideAnimation(false);
        }

        hideCoroutine = null;
    }

    private void StartSlideAnimation(bool show)
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }
        slideCoroutine = StartCoroutine(SlideAnimation(show));
    }

    private IEnumerator SlideAnimation(bool show)
    {
        isAnimating = true;

        Vector2 startPos = hiddenPanel.anchoredPosition;
        Vector2 targetPos = show ? visiblePosition : hiddenPosition;
        float elapsed = 0;

        while (elapsed < slideDuration)
        {
            hiddenPanel.anchoredPosition = Vector2.Lerp(
                startPos,
                targetPos,
                elapsed / slideDuration
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        hiddenPanel.anchoredPosition = targetPos;
        isPanelVisible = show;
        isAnimating = false;
    }

    private bool IsCursorOverPanel()
    {
        // Get current mouse position
        PointerEventData eventData = new(EventSystem.current)
        {
            position = Input.mousePosition
        };

        // Get all UI elements under cursor
        List<RaycastResult> results = new();
        EventSystem.current.RaycastAll(eventData, results);

        // Check if any element is part of the panel
        foreach (RaycastResult result in results)
        {
            if (result.gameObject.transform.IsChildOf(hiddenPanel))
            {
                return true;
            }
        }
        return false;
    }

    // Visualize trigger zone in Scene view
    void OnDrawGizmosSelected()
    {
        if (triggerZone == null) return;

        Vector3[] corners = new Vector3[4];
        triggerZone.GetWorldCorners(corners);

        Gizmos.color = Color.cyan;
        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
    }
}