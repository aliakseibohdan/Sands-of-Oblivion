using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

public class BackpackSystem : MonoBehaviour
{
    public static BackpackSystem Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup _backpackCanvas;
    [SerializeField] private Transform _itemsContainer;
    [SerializeField] private TMP_Text _itemDescription;
    [SerializeField] private GameObject _navigationArrows;

    [Header("Configuration")]
    [SerializeField] private ItemDatabase _itemDatabase;
    [SerializeField] private float _rotationRadius = 2f;
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private LayerMask _itemSelectionLayer;

    private readonly List<BackpackItem> _items = new();
    private int _selectedIndex = 0;
    private bool _isOpen;
    private Camera _uiCamera;

    public bool CanAccessBackpack { get; set; } = true;
    public bool IsOpen => _isOpen;
    public bool IsInspecting => _isOpen && _items.Count > 0 &&
        _items[_selectedIndex].IsInspectable;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _uiCamera = Camera.main;
    }

    private void Update()
    {
        if (!_isOpen) return;

        HandleNavigationInput();
        UpdateItemPositions();

        // Item selection via mouse
        if (Input.GetMouseButtonDown(0) && !IsInspecting)
        {
            TrySelectItemWithMouse();
        }
    }

    public void ToggleBackpack()
    {
        if (!CanAccessBackpack) return;

        _isOpen = !_isOpen;
        _backpackCanvas.alpha = _isOpen ? 1 : 0;
        _backpackCanvas.blocksRaycasts = _isOpen;
        _navigationArrows.SetActive(_isOpen && !IsInspecting);

        if (_isOpen)
        {
            SelectItem(_selectedIndex);
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            EndInspection();
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void AddItem(string itemID)
    {
        var config = _itemDatabase.GetItem(itemID);
        if (config == null)
        {
            Debug.LogError($"Item {itemID} not found in database!");
            return;
        }

        // Create item instance
        var itemObj = Instantiate(config.Prefab3D, _itemsContainer);
        var backpackItem = itemObj.GetComponent<BackpackItem>();

        if (backpackItem == null)
        {
            backpackItem = itemObj.AddComponent<BackpackItem>();
            backpackItem.ItemID = itemID;
            backpackItem.IsInspectable = config.IsInspectable;
        }

        _items.Add(backpackItem);
        SelectItem(_items.Count - 1);
    }

    public void InspectCurrentItem()
    {
        if (_items.Count == 0 || !_items[_selectedIndex].IsInspectable) return;

        _items[_selectedIndex].StartInspection();
        _navigationArrows.SetActive(false);
    }

    public void EndInspection()
    {
        if (_items.Count > 0 && _selectedIndex < _items.Count)
        {
            _items[_selectedIndex].EndInspection();
        }
        _navigationArrows.SetActive(true);
    }

    private void SelectItem(int index)
    {
        if (_items.Count == 0) return;

        _selectedIndex = (index + _items.Count) % _items.Count;
        UpdateItemDescription();
    }

    private void UpdateItemDescription()
    {
        var config = _itemDatabase.GetItem(_items[_selectedIndex].ItemID);
        _itemDescription.text = $"<b>{config.DisplayName}</b>\n{config.Description}";
    }

    private void UpdateItemPositions()
    {
        if (_items.Count == 0) return;

        float angleStep = 360f / _items.Count;

        for (int i = 0; i < _items.Count; i++)
        {
            float angle = (i - _selectedIndex) * angleStep * Mathf.Deg2Rad;
            Vector3 targetPos = new Vector3(
                Mathf.Sin(angle) * _rotationRadius,
                0,
                Mathf.Cos(angle) * _rotationRadius
            );

            _items[i].transform.localPosition = Vector3.Lerp(
                _items[i].transform.localPosition,
                targetPos,
                Time.deltaTime * _rotationSpeed
            );

            // Scale based on selection
            float scaleFactor = (i == _selectedIndex) ? 1.2f : 0.8f;
            _items[i].transform.localScale = Vector3.one * scaleFactor;
        }
    }

    private void HandleNavigationInput()
    {
        if (IsInspecting) return;

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame ||
            Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame)
        {
            SelectItem(_selectedIndex - 1);
        }
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame ||
                 Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame)
        {
            SelectItem(_selectedIndex + 1);
        }
        else if (Keyboard.current.enterKey.wasPressedThisFrame ||
                 Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            InspectCurrentItem();
        }
    }

    private void TrySelectItemWithMouse()
    {
        Ray ray = _uiCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _itemSelectionLayer))
        {
            var item = hit.collider.GetComponentInParent<BackpackItem>();
            if (item != null)
            {
                int index = _items.IndexOf(item);
                if (index >= 0)
                {
                    SelectItem(index);

                    // Double-click to inspect
                    if (Time.time - _lastClickTime < 0.3f)
                    {
                        InspectCurrentItem();
                    }
                    _lastClickTime = Time.time;
                }
            }
        }
    }
    private float _lastClickTime;

#if UNITY_EDITOR
    [ContextMenu("Add Test Items")]
    private void AddTestItems()
    {
        AddItem("compass");
        AddItem("kerma_tablet");
        AddItem("journal");
    }
#endif
}