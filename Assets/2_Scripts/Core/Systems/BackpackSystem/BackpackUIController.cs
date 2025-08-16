using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BackpackUIController : MonoBehaviour
{
    [Header("Rendering Setup")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask backpackItemLayer;
    [SerializeField] private Vector3 defaultItemRotation = new(-15, 30, 0);
    private int backpackItemLayerIndex;

    [Header("ViewSettings")]
    [SerializeField] private float rotationDuration = 0.5f;
    [SerializeField] private float radius = 2f;
    [SerializeField] private float grayedAlpha = 0.5f;
    private float currentRotationAngle = 0f;

    [Header("Inspection Settings")]
    [SerializeField] private float inspectionTransitionDuration = 0.5f;
    [SerializeField] private Vector3 inspectionScale = Vector3.one * 3f;
    [SerializeField] private float rotationSensitivity = 3f;
    private GameObject currentInspectionItem;
    private Vector3 originalItemPosition;
    private Quaternion originalItemRotation;
    private Vector3 originalItemScale;
    private Transform originalItemParent;

    [Header("UI Settings")]
    [SerializeField] private float textFadeDuration = 0.2f;

    [Header("Button Settings")]
    [SerializeField] private float buttonHoverScale = 1.2f;
    [SerializeField] private float buttonHoverDuration = 0.2f;
    private Dictionary<Button, Vector3> buttonOriginalScales = new();
    private Button[] allButtons;

    [Header("Overlay Settings")]
    [SerializeField] private Color overlayColor = new(0.1f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Material overlayMaterial;


    [Header("References")]
    [SerializeField] private CanvasGroup backpackCanvas;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private Transform cameraContainer;
    [SerializeField] private TextMeshProUGUI itemTitle;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private Button inspectButton;
    [SerializeField] private Button leftArrow;
    [SerializeField] private Button rightArrow;
    [SerializeField] private RawImage itemRenderImage;
    [SerializeField] private RenderTexture itemRenderTexture;
    [SerializeField] private CanvasGroup inspectionOverlay;

    private Backpack backpack;
    private ItemDatabase itemDatabase;
    private Camera renderCamera;
    private readonly Dictionary<string, GameObject> spawnedItems = new();
    private List<string> currentItems = new();
    private int selectedIndex = 0;
    private bool isRotating = false;
    private float currentZoom = 2f;
    private const float minZoom = 0.5f;
    private const float maxZoom = 5f;

    private void Awake()
    {
        backpack = FindAnyObjectByType<Backpack>();
        itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");

        backpackItemLayerIndex = GetFirstLayerFromMask(backpackItemLayer);
        if (backpackItemLayerIndex == -1)
        {
            Debug.LogError($"No valid layer found in LayerMask '{backpackItemLayer.value}'. Using default layer.");
            backpackItemLayerIndex = 0;
        }

        if (cameraContainer == null)
        {
            GameObject cameraContainerGO = new("CameraContainer");
            cameraContainer = cameraContainerGO.transform;
            cameraContainer.SetParent(transform);
            cameraContainer.localPosition = Vector3.zero;
            cameraContainer.localRotation = Quaternion.identity;
        }

        if (renderCamera == null)
        {
            CreateRenderCamera();
        }

        backpackCanvas.alpha = 0;
        backpackCanvas.blocksRaycasts = false;
        inspectionOverlay.alpha = 0;
        inspectionOverlay.blocksRaycasts = false;

        leftArrow.onClick.AddListener(() => RotateItems(-1));
        rightArrow.onClick.AddListener(() => RotateItems(1));
        inspectButton.onClick.AddListener(EnterInspectionMode);

        allButtons = new Button[] { leftArrow, rightArrow, inspectButton };
        foreach (Button button in allButtons)
        {
            buttonOriginalScales[button] = button.transform.localScale;

            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entryEnter = new()
            {
                eventID = EventTriggerType.PointerEnter
            };
            entryEnter.callback.AddListener((data) => { OnButtonPointerEnter(button); });
            trigger.triggers.Add(entryEnter);

            EventTrigger.Entry entryExit = new()
            {
                eventID = EventTriggerType.PointerExit
            };
            entryExit.callback.AddListener((data) => { OnButtonPointerExit(button); });
            trigger.triggers.Add(entryExit);
        }
    }

    private void OnButtonPointerEnter(Button button)
    {
        if (!button.interactable) return;
        StopAllCoroutinesForButton(button);
        StartCoroutine(ScaleButton(button, buttonOriginalScales[button] * buttonHoverScale));
    }

    private void OnButtonPointerExit(Button button)
    {
        StopAllCoroutinesForButton(button);
        StartCoroutine(ScaleButton(button, buttonOriginalScales[button]));
    }

    private void StopAllCoroutinesForButton(Button button)
    {
        StopCoroutine("ScaleButton_" + button.GetInstanceID());
    }

    private IEnumerator ScaleButton(Button button, Vector3 targetScale)
    {
        string coroutineName = "ScaleButton_" + button.GetInstanceID();

        Vector3 startScale = button.transform.localScale;
        float elapsed = 0;

        while (elapsed < buttonHoverDuration)
        {
            float t = elapsed / buttonHoverDuration;
            button.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        button.transform.localScale = targetScale;
    }

    private void SetButtonState(Button button, bool interactable)
    {
        button.interactable = interactable;

        if (!interactable && button.transform.localScale != buttonOriginalScales[button])
        {
            StopAllCoroutinesForButton(button);
            button.transform.localScale = buttonOriginalScales[button];
        }
    }

    private int GetFirstLayerFromMask(LayerMask layerMask)
    {
        int mask = layerMask.value;
        if (mask == 0) return 0;

        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                return i;
            }
        }
        return 0;
    }

    private void Update()
    {
        if (inspectionOverlay.blocksRaycasts)
        {
            HandleInspectionInput();
            return;
        }

        if (backpackCanvas.blocksRaycasts && Input.GetMouseButtonDown(1))
        {
            ToggleBackpack();
        }

        if (backpackCanvas.blocksRaycasts)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                RotateItems(-1);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                RotateItems(1);
            }
            else if (Input.GetKeyDown(KeyCode.Return) && inspectButton.interactable)
            {
                EnterInspectionMode();
            }
        }
    }


    private void CreateRenderCamera()
    {
        GameObject cameraGO = new("BackpackRenderCamera");
        cameraGO.transform.SetParent(cameraContainer);
        cameraGO.transform.SetLocalPositionAndRotation(new Vector3(0, 0.5f, -2f), Quaternion.identity);

        renderCamera = cameraGO.AddComponent<Camera>();
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = Color.clear;

        renderCamera.orthographic = true;
        renderCamera.orthographicSize = 2f;
        renderCamera.depth = 100;
        renderCamera.cullingMask = 1 << backpackItemLayerIndex;
        renderCamera.nearClipPlane = -100f;

        if (itemRenderTexture == null)
        {
            itemRenderTexture = new RenderTexture(512, 512, 24);
            itemRenderTexture.Create();
        }
        renderCamera.targetTexture = itemRenderTexture;

        if (itemRenderImage != null)
        {
            itemRenderImage.texture = itemRenderTexture;
        }
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
        else
        {
            if (renderCamera != null) renderCamera.enabled = false;
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

        if (renderCamera != null)
        {
            renderCamera.enabled = show && currentItems.Count > 0;
        }
    }

    private void RefreshBackpackContents()
    {
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }
        spawnedItems.Clear();

        currentRotationAngle = 0f;
        itemContainer.localRotation = Quaternion.identity;

        currentItems = backpack.GetItems();
        selectedIndex = currentItems.Count > 0 ? currentItems.Count - 1 : -1;

        if (renderCamera != null)
        {
            renderCamera.enabled = currentItems.Count > 0;
        }

        if (currentItems.Count == 0)
        {
            itemTitle.text = "Empty Hands";
            itemDescription.text = "The weight of possibilities hangs light, awaiting discoveries yet unseen...";
            SetButtonState(inspectButton, false);
            SetButtonState(leftArrow, false);
            SetButtonState(rightArrow, false);
            return;
        }

        float angleStep = 360f / currentItems.Count;
        for (int i = 0; i < currentItems.Count; i++)
        {
            string itemID = currentItems[i];
            ItemDatabase.ItemConfig config = itemDatabase.GetItem(itemID);

            if (config != null && config.Prefab3D != null)
            {
                Vector3 position = CalculateCircularPosition(i, angleStep);

                Vector3 rotationEuler = config.DefaultRotation != Vector3.zero ?
                config.DefaultRotation :
                defaultItemRotation;

                Quaternion rotation = Quaternion.Euler(rotationEuler);

                GameObject item = Instantiate(config.Prefab3D, position, rotation, itemContainer);

                Vector3 lookDirection = -item.transform.localPosition.normalized;
                Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
                item.transform.localRotation = lookRotation * Quaternion.Euler(rotationEuler);

                item.transform.localScale = Vector3.one * 5f;
                item.transform.localPosition += new Vector3(0, renderCamera.transform.position.y, 0);

                SetItemAppearance(item, i == selectedIndex);
                SetLayerRecursive(item, backpackItemLayerIndex);
                spawnedItems.Add(itemID, item);
            }
        }

        UpdateSelectedItemUI();
        leftArrow.interactable = currentItems.Count > 1;
        rightArrow.interactable = currentItems.Count > 1;

        if (renderCamera != null && renderCamera.enabled)
        {
            renderCamera.Render();
        }
    }

    private Vector3 CalculateCircularPosition(int index, float angleStep)
    {
        float angle = angleStep * index * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
    }

    private void SetLayerRecursive(GameObject obj, int layerIndex)
    {
        obj.layer = layerIndex;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layerIndex);
        }
    }

    private void SetItemAppearance(GameObject item, bool isSelected)
    {
        Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            MaterialPropertyBlock propBlock = new();
            r.GetPropertyBlock(propBlock);

            propBlock.SetColor("_Color", isSelected ? Color.white : new Color(1, 1, 1, grayedAlpha));

            if (r.sharedMaterial.shader.name.Contains("Unlit"))
            {
                propBlock.SetColor("_BaseColor", isSelected ? Color.white : new Color(1, 1, 1, grayedAlpha));
            }

            r.SetPropertyBlock(propBlock);
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

            StartCoroutine(PulseItem(spawnedItems[itemID]));
        }
    }

    private IEnumerator PulseItem(GameObject item)
    {
        float duration = 0.3f;
        float elapsed = 0;
        Vector3 originalScale = item.transform.localScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1.1f, 1f, t);
            item.transform.localScale = originalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        item.transform.localScale = originalScale;
    }

    private void RotateItems(int direction)
    {
        if (isRotating || currentItems.Count < 2) return;

        StartCoroutine(AnimateRotation(direction));
    }

    private IEnumerator AnimateRotation(int direction)
    {
        isRotating = true;
        SetButtonState(leftArrow, false);
        SetButtonState(rightArrow, false);

        yield return StartCoroutine(FadeText(itemTitle, 0f));
        yield return StartCoroutine(FadeText(itemDescription, 0f));
        inspectButton.interactable = false;

        int newIndex = (selectedIndex + direction + currentItems.Count) % currentItems.Count;
        float angleStep = 360f / currentItems.Count;

        float targetRotationAngle = currentRotationAngle + (angleStep * direction);

        Quaternion startRotation = itemContainer.localRotation;
        Quaternion endRotation = Quaternion.Euler(0, targetRotationAngle, 0);

        float elapsed = 0;
        while (elapsed < rotationDuration)
        {
            float t = elapsed / rotationDuration;
            itemContainer.localRotation = Quaternion.Lerp(startRotation, endRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        itemContainer.localRotation = endRotation;
        currentRotationAngle = targetRotationAngle;

        selectedIndex = newIndex;
        foreach (var kvp in spawnedItems)
        {
            SetItemAppearance(kvp.Value, kvp.Key == currentItems[selectedIndex]);
        }

        UpdateSelectedItemUI();
        yield return StartCoroutine(FadeText(itemTitle, 1f));
        yield return StartCoroutine(FadeText(itemDescription, 1f));

        isRotating = false;
        SetButtonState(leftArrow, true);
        SetButtonState(rightArrow, true);
    }

    private IEnumerator FadeText(TextMeshProUGUI textElement, float targetAlpha)
    {
        float startAlpha = textElement.alpha;
        float elapsed = 0;

        while (elapsed < textFadeDuration)
        {
            textElement.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / textFadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        textElement.alpha = targetAlpha;

        if (targetAlpha < 0.1f)
        {
            textElement.text = "";
        }
    }

    private void EnterInspectionMode()
    {
        if (selectedIndex < 0 || selectedIndex >= currentItems.Count) return;

        string itemID = currentItems[selectedIndex];
        ItemDatabase.ItemConfig config = itemDatabase.GetItem(itemID);

        if (!config.IsInspectable || !spawnedItems.ContainsKey(itemID)) return;

        currentInspectionItem = spawnedItems[itemID];

        originalItemPosition = currentInspectionItem.transform.localPosition;
        originalItemRotation = currentInspectionItem.transform.localRotation;
        originalItemScale = currentInspectionItem.transform.localScale;
        originalItemParent = currentInspectionItem.transform.parent;

        currentInspectionItem.transform.SetParent(itemContainer, true);
        currentInspectionItem.transform.SetAsLastSibling();

        StartCoroutine(AnimateToInspection());

        inspectionOverlay.blocksRaycasts = true;
        currentInspectionItem.transform.SetAsLastSibling();
        StartCoroutine(FadeInspectionOverlay(true));
    }

    private IEnumerator AnimateToInspection()
    {
        Vector3 targetPosition = Vector3.forward * -2f;
        targetPosition.y = renderCamera.transform.position.y;
        Quaternion targetRotation = originalItemRotation;


        float elapsed = 0;
        while (elapsed < inspectionTransitionDuration)
        {
            float t = elapsed / inspectionTransitionDuration;
            currentInspectionItem.transform.SetLocalPositionAndRotation(Vector3.Lerp(
                originalItemPosition,
                targetPosition,
                t
            ), Quaternion.Lerp(
                originalItemRotation,
                targetRotation,
                t
            ));
            currentInspectionItem.transform.localScale = Vector3.Lerp(
                originalItemScale,
                inspectionScale,
                t
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        currentInspectionItem.transform.SetLocalPositionAndRotation(targetPosition, targetRotation);
        currentInspectionItem.transform.localScale = inspectionScale;
        currentInspectionItem.transform.SetAsLastSibling();

        foreach (var item in spawnedItems.Values)
        {
            if (item != currentInspectionItem)
            {
                SetItemAppearance(item, false);
            }
        }
    }

    private void ExitInspectionMode()
    {
        if (currentInspectionItem == null) return;

        StartCoroutine(AnimateFromInspection());
        StartCoroutine(FadeInspectionOverlay(false));
    }

    private IEnumerator AnimateFromInspection()
    {
        currentInspectionItem.transform.GetLocalPositionAndRotation(out Vector3 startPosition, out Quaternion startRotation);
        Vector3 startScale = currentInspectionItem.transform.localScale;

        float elapsed = 0;
        while (elapsed < inspectionTransitionDuration)
        {
            float t = elapsed / inspectionTransitionDuration;
            currentInspectionItem.transform.SetLocalPositionAndRotation(Vector3.Lerp(
                startPosition,
                originalItemPosition,
                t
            ), Quaternion.Lerp(
                startRotation,
                originalItemRotation,
                t
            ));
            currentInspectionItem.transform.localScale = Vector3.Lerp(
                startScale,
                originalItemScale,
                t
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        currentInspectionItem.transform.SetParent(originalItemParent, true);
        currentInspectionItem.transform.SetLocalPositionAndRotation(originalItemPosition, originalItemRotation);
        currentInspectionItem.transform.localScale = originalItemScale;

        currentInspectionItem.transform.SetParent(originalItemParent, true);

        foreach (var kvp in spawnedItems)
        {
            SetItemAppearance(kvp.Value, kvp.Key == currentItems[selectedIndex]);
        }

        currentInspectionItem = null;
        inspectionOverlay.blocksRaycasts = false;
    }

    private void HandleInspectionInput()
    {
        if (currentInspectionItem == null) return;

        if (Input.GetMouseButton(0))
        {
            float rotX = Input.GetAxis("Mouse X") * rotationSensitivity;
            float rotY = Input.GetAxis("Mouse Y") * rotationSensitivity;

            currentInspectionItem.transform.Rotate(Vector3.up, -rotX, Space.World);
            currentInspectionItem.transform.Rotate(Vector3.right, rotY, Space.World);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            currentZoom = Mathf.Clamp(currentZoom + scroll, minZoom, maxZoom);
            currentInspectionItem.transform.localScale = inspectionScale * currentZoom;
        }

        if (Input.GetMouseButtonDown(1))
        {
            ExitInspectionMode();
        }
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
}