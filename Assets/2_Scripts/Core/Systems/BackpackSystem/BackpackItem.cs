using UnityEngine;

public class BackpackItem : MonoBehaviour
{
    [Header("Identification")]
    public string ItemID;

    [Header("Visuals")]
    public GameObject Model3D;
    public float RotationSpeed = 15f;

    [Header("Interaction")]
    public bool IsInspectable = true;
    public Vector3 InspectOffset = Vector3.zero;
    public float InspectScale = 1.5f;

    // Runtime state
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Vector3 _originalScale;
    private bool _isInspecting;

    private void Awake()
    {
        _originalPosition = transform.localPosition;
        _originalRotation = transform.localRotation;
        _originalScale = transform.localScale;
    }

    public void StartInspection()
    {
        if (!IsInspectable) return;

        _isInspecting = true;
        transform.localPosition = InspectOffset;
        transform.localScale = _originalScale * InspectScale;

        // Enable outline effect
        SetOutline(true);
    }

    public void EndInspection()
    {
        _isInspecting = false;
        transform.localPosition = _originalPosition;
        transform.localRotation = _originalRotation;
        transform.localScale = _originalScale;
        SetOutline(false);
    }

    private void Update()
    {
        if (!_isInspecting) return;

        // Rotation control
        if (Input.GetMouseButton(0))
        {
            float rotX = Input.GetAxis("Mouse X") * RotationSpeed;
            float rotY = Input.GetAxis("Mouse Y") * RotationSpeed;
            transform.Rotate(Vector3.up, -rotX, Space.World);
            transform.Rotate(Vector3.right, rotY, Space.World);
        }

        // Exit inspection
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackpackSystem.Instance.EndInspection();
        }
    }

    private void SetOutline(bool enabled)
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.material.SetFloat("_OutlineEnabled", enabled ? 1 : 0);
        }
    }
}