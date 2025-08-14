using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BackpackUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup backpackCanvas;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private TextMeshProUGUI itemTitle;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private Button inspectButton;
    [SerializeField] private Button leftArrow;
    [SerializeField] private Button rightArrow;
    [SerializeField] private Camera renderCamera;
    [SerializeField] private Transform inspectionContainer;
    [SerializeField] private CanvasGroup inspectionOverlay;

    [Header("Settings")]
    [SerializeField] private float rotationDuration = 0.5f;
    [SerializeField] private float radius = 2f;
    [SerializeField] private float grayedAlpha = 0.5f;

    private Backpack backpack;
    private ItemDatabase itemDatabase;
    private readonly Dictionary<string, GameObject> spawnedItems = new();
    private List<string> currentItems = new();
    private int selectedIndex = 0;
    private bool isRotating = false;
    private readonly Dictionary<string, Quaternion> inspectionRotations = new();
    private float currentZoom = 2f;
    private const float minZoom = 0.5f;
    private const float maxZoom = 5f;

    private void Awake()
    {
        backpack = FindAnyObjectByType<Backpack>();
        itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");

        // Initialize UI
        backpackCanvas.alpha = 0;
        backpackCanvas.blocksRaycasts = false;
        inspectionOverlay.alpha = 0;
        inspectionOverlay.blocksRaycasts = false;

        // Setup button listeners
        leftArrow.onClick.AddListener(() => RotateItems(-1));
        rightArrow.onClick.AddListener(() => RotateItems(1));
        inspectButton.onClick.AddListener(EnterInspectionMode);
    }

    public void ToggleBackpack()
    {
        bool willOpen = backpackCanvas.alpha < 0.5f;

        backpackCanvas.blocksRaycasts = willOpen;
        StopAllCoroutines();
        StartCoroutine(FadeBackpack(willOpen));

        if (willOpen)
        {
            RefreshBackpackContents();
        }
    }

    private IEnumerator FadeBackpack(bool show)
    {
        float targetAlpha = show ? 1 : 0;
        float startAlpha = backpackCanvas.alpha;
        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            backpackCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        backpackCanvas.alpha = targetAlpha;
    }

    private void RefreshBackpackContents()
    {
        // Clear existing items
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }
        spawnedItems.Clear();

        // Update UI state
        renderCamera.enabled = currentItems.Count > 0;

        // Get current items
        currentItems = backpack.GetItems();
        selectedIndex = currentItems.Count > 0 ? currentItems.Count - 1 : -1;

        // Handle empty backpack
        if (currentItems.Count == 0)
        {
            itemTitle.text = "Empty Hands";
            itemDescription.text = "The weight of possibilities hangs light, awaiting discoveries yet unseen...";
            inspectButton.interactable = false;
            leftArrow.interactable = false;
            rightArrow.interactable = false;
            return;
        }

        // Position items in circular layout
        float angleStep = 360f / currentItems.Count;
        for (int i = 0; i < currentItems.Count; i++)
        {
            string itemID = currentItems[i];
            ItemDatabase.ItemConfig config = itemDatabase.GetItem(itemID);

            if (config != null && config.Prefab3D != null)
            {
                Vector3 position = CalculateCircularPosition(i, angleStep);
                Quaternion rotation = Quaternion.Euler(0, -angleStep * i, 0);
                GameObject item = Instantiate(config.Prefab3D, position, rotation, itemContainer);

                // Set item appearance
                SetItemAppearance(item, i == selectedIndex);
                spawnedItems.Add(itemID, item);
            }
        }

        // Update UI state
        UpdateSelectedItemUI();
        leftArrow.interactable = currentItems.Count > 1;
        rightArrow.interactable = currentItems.Count > 1;
    }

    private Vector3 CalculateCircularPosition(int index, float angleStep)
    {
        float angle = angleStep * index * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
    }

    private void SetItemAppearance(GameObject item, bool isSelected)
    {
        Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            Material mat = r.material;
            Color color = mat.color;
            color.a = isSelected ? 1f : grayedAlpha;
            mat.color = color;
        }
    }

    private void UpdateSelectedItemUI()
    {
        if (selectedIndex >= 0 && selectedIndex < currentItems.Count)
        {
            string itemID = currentItems[selectedIndex];
            ItemDatabase.ItemConfig config = itemDatabase.GetItem(itemID);

            itemTitle.text = config.DisplayName;
            itemDescription.text = config.Description;
            inspectButton.interactable = config.IsInspectable;
        }
    }

    private void RotateItems(int direction)
    {
        if (isRotating || currentItems.Count < 2) return;

        StartCoroutine(AnimateRotation(direction));
    }

    private IEnumerator AnimateRotation(int direction)
    {
        isRotating = true;
        leftArrow.interactable = false;
        rightArrow.interactable = false;

        // Calculate new positions
        int newIndex = (selectedIndex + direction + currentItems.Count) % currentItems.Count;
        float angleStep = 360f / currentItems.Count;

        // Store initial positions
        Dictionary<string, Vector3> startPositions = new();
        foreach (string itemID in currentItems)
        {
            if (spawnedItems.ContainsKey(itemID))
            {
                startPositions[itemID] = spawnedItems[itemID].transform.localPosition;
            }
        }

        // Calculate target positions
        Dictionary<string, Vector3> targetPositions = new();
        for (int i = 0; i < currentItems.Count; i++)
        {
            int newPositionIndex = (i - direction + currentItems.Count) % currentItems.Count;
            targetPositions[currentItems[i]] = CalculateCircularPosition(newPositionIndex, angleStep);
        }

        // Animate movement
        float elapsed = 0;
        while (elapsed < rotationDuration)
        {
            float t = elapsed / rotationDuration;
            foreach (string itemID in currentItems)
            {
                if (spawnedItems.ContainsKey(itemID))
                {
                    Transform itemTransform = spawnedItems[itemID].transform;
                    itemTransform.localPosition = Vector3.Lerp(
                        startPositions[itemID],
                        targetPositions[itemID],
                        t
                    );
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Finalize positions
        foreach (string itemID in currentItems)
        {
            if (spawnedItems.ContainsKey(itemID))
            {
                spawnedItems[itemID].transform.localPosition = targetPositions[itemID];
            }
        }

        // Update selection
        selectedIndex = newIndex;
        foreach (var kvp in spawnedItems)
        {
            SetItemAppearance(kvp.Value, kvp.Key == currentItems[selectedIndex]);
        }

        UpdateSelectedItemUI();
        isRotating = false;
        leftArrow.interactable = true;
        rightArrow.interactable = true;
    }

    private void EnterInspectionMode()
    {
        if (selectedIndex < 0 || selectedIndex >= currentItems.Count) return;

        string itemID = currentItems[selectedIndex];
        ItemDatabase.ItemConfig config = itemDatabase.GetItem(itemID);

        if (!config.IsInspectable) return;

        // Create or position inspection item
        GameObject inspectionItem;
        if (!spawnedItems.ContainsKey(itemID + "_inspection"))
        {
            inspectionItem = Instantiate(config.Prefab3D, inspectionContainer);
            inspectionItem.name = config.ItemID + "_inspection";

            // Initialize rotation if needed
            if (!inspectionRotations.ContainsKey(itemID))
            {
                inspectionRotations[itemID] = Quaternion.identity;
            }
        }
        else
        {
            inspectionItem = spawnedItems[itemID + "_inspection"];
        }

        inspectionItem.transform.localPosition = Vector3.zero;
        inspectionItem.transform.localRotation = inspectionRotations[itemID];
        inspectionItem.transform.localScale = Vector3.one * 2f;

        // Show inspection overlay
        inspectionOverlay.blocksRaycasts = true;
        StartCoroutine(FadeInspectionOverlay(true));
    }

    private IEnumerator FadeInspectionOverlay(bool show)
    {
        float targetAlpha = show ? 1 : 0;
        float startAlpha = inspectionOverlay.alpha;
        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            inspectionOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        inspectionOverlay.alpha = targetAlpha;
    }

    private void ExitInspectionMode()
    {
        string itemID = currentItems[selectedIndex];

        // Save current rotation
        if (spawnedItems.ContainsKey(itemID + "_inspection"))
        {
            inspectionRotations[itemID] = spawnedItems[itemID + "_inspection"].transform.localRotation;
        }

        // Hide inspection overlay
        inspectionOverlay.blocksRaycasts = false;
        StartCoroutine(FadeInspectionOverlay(false));
    }

    private void Update()
    {
        HandleInspectionInput();
        HandleBackpackClosing();
    }

    private void HandleInspectionInput()
    {
        if (inspectionOverlay.blocksRaycasts)
        {
            // Rotation
            if (Input.GetMouseButton(0))
            {
                float rotX = Input.GetAxis("Mouse X") * 3f;
                float rotY = Input.GetAxis("Mouse Y") * 3f;

                string itemID = currentItems[selectedIndex];
                if (spawnedItems.ContainsKey(itemID + "_inspection"))
                {
                    Transform item = spawnedItems[itemID + "_inspection"].transform;
                    item.Rotate(Vector3.up, -rotX, Space.World);
                    item.Rotate(Vector3.right, rotY, Space.World);
                }
            }

            // Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                currentZoom = Mathf.Clamp(currentZoom - scroll, minZoom, maxZoom);
                inspectionContainer.localPosition = new Vector3(0, 0, -currentZoom);
            }

            // Exit on RMB
            if (Input.GetMouseButtonDown(1))
            {
                ExitInspectionMode();
            }
        }
    }

    private void HandleBackpackClosing()
    {
        // Close backpack with RMB when it's open but inspection is not active
        if (backpackCanvas.blocksRaycasts && !inspectionOverlay.blocksRaycasts && Input.GetMouseButtonDown(1))
        {
            ToggleBackpack();
        }
    }
}