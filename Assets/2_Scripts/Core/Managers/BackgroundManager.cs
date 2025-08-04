using UnityEngine;
using System.Collections;

public class BackgroundManager : MonoBehaviour
{
    public enum ScaleMode
    {
        StretchToFill,
        PreserveAspect,
        PixelPerfect
    }

    public static BackgroundManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private SpriteRenderer _backgroundRenderer;

    [Header("Scaling Configuration")]
    [SerializeField] private ScaleMode _scaleMode = ScaleMode.PreserveAspect;
    [SerializeField] private bool _autoUpdateOnResize = true;
    [SerializeField] private float _transitionDuration = 1f;

    private Sprite _currentBackground;
    private Coroutine _transitionCoroutine;
    private Camera _mainCamera;
    private Vector2Int _lastScreenSize;
    private float _pixelsPerUnit = 100f; // Default PPU

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _mainCamera = Camera.main;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private void Start()
    {
        if (_autoUpdateOnResize)
        {
            StartCoroutine(ScreenResizeMonitor());
        }
    }

    private IEnumerator ScreenResizeMonitor()
    {
        while (true)
        {
            if (Screen.width != _lastScreenSize.x || Screen.height != _lastScreenSize.y)
            {
                _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
                UpdateBackgroundScale();
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void SetBackground(Sprite background, bool immediate = false)
    {
        if (_backgroundRenderer == null) return;

        // Handle null background (black screen)
        if (background == null)
        {
            _backgroundRenderer.sprite = null;
            _backgroundRenderer.color = Color.black;
            return;
        }

        if (background == _currentBackground) return;

        _currentBackground = background;
        _pixelsPerUnit = background.pixelsPerUnit;

        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);

        _transitionCoroutine = StartCoroutine(
            immediate ?
            ImmediateBackgroundChange() :
            TransitionBackground()
        );
    }

    private IEnumerator ImmediateBackgroundChange()
    {
        _backgroundRenderer.sprite = _currentBackground;
        UpdateBackgroundScale();
        _backgroundRenderer.color = Color.white;
        yield break;
    }

    private IEnumerator TransitionBackground()
    {
        // Fade out current background
        float elapsed = 0;
        Color startColor = _backgroundRenderer.color;

        while (elapsed < _transitionDuration)
        {
            _backgroundRenderer.color = Color.Lerp(
                startColor,
                Color.clear,
                elapsed / _transitionDuration
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Swap sprite and update scale
        _backgroundRenderer.sprite = _currentBackground;
        UpdateBackgroundScale();

        // Fade in new background
        elapsed = 0;
        while (elapsed < _transitionDuration)
        {
            _backgroundRenderer.color = Color.Lerp(
                Color.clear,
                Color.white,
                elapsed / _transitionDuration
            );
            elapsed += Time.deltaTime;
            yield return null;
        }

        _backgroundRenderer.color = Color.white;
    }

    [ContextMenu("Update Background Scale")]
    public void UpdateBackgroundScale()
    {
        if (_backgroundRenderer == null || _backgroundRenderer.sprite == null)
            return;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null || !_mainCamera.orthographic)
            return;

        switch (_scaleMode)
        {
            case ScaleMode.StretchToFill:
                ApplyStretchToFill();
                break;
            case ScaleMode.PreserveAspect:
                ApplyPreserveAspect();
                break;
            case ScaleMode.PixelPerfect:
                ApplyPixelPerfect();
                break;
        }
    }

    private void ApplyStretchToFill()
    {
        float cameraHeight = _mainCamera.orthographicSize * 2;
        float cameraWidth = cameraHeight * _mainCamera.aspect;

        float spriteWidth = _currentBackground.bounds.size.x;
        float spriteHeight = _currentBackground.bounds.size.y;

        float scaleX = cameraWidth / spriteWidth;
        float scaleY = cameraHeight / spriteHeight;

        _backgroundRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1);
    }

    private void ApplyPreserveAspect()
    {
        float cameraHeight = _mainCamera.orthographicSize * 2;
        float cameraWidth = cameraHeight * _mainCamera.aspect;

        float spriteWidth = _currentBackground.bounds.size.x;
        float spriteHeight = _currentBackground.bounds.size.y;

        float scaleX = cameraWidth / spriteWidth;
        float scaleY = cameraHeight / spriteHeight;

        float scale = Mathf.Max(scaleX, scaleY);
        _backgroundRenderer.transform.localScale = new Vector3(scale, scale, 1);
    }

    private void ApplyPixelPerfect()
    {
        // Calculate pixel-perfect scale based on sprite PPU
        float targetHeight = _mainCamera.orthographicSize * 2 * _pixelsPerUnit;
        float pixelsPerUnitScale = Mathf.Round(targetHeight / Screen.height * _pixelsPerUnit);

        // Calculate scale to fit screen
        float cameraHeight = _mainCamera.orthographicSize * 2;
        float spriteHeight = _currentBackground.bounds.size.y;
        float baseScale = cameraHeight / spriteHeight;

        // Apply pixel-perfect adjustment
        float scale = Mathf.Round(baseScale * pixelsPerUnitScale) / pixelsPerUnitScale;
        _backgroundRenderer.transform.localScale = new Vector3(scale, scale, 1);

        // Adjust position for pixel alignment
        Vector3 position = _backgroundRenderer.transform.position;
        position.x = Mathf.Round(position.x * _pixelsPerUnit) / _pixelsPerUnit;
        position.y = Mathf.Round(position.y * _pixelsPerUnit) / _pixelsPerUnit;
        _backgroundRenderer.transform.position = position;
    }

    // Editor visualization
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_mainCamera == null || !_mainCamera.orthographic) return;

        // Draw camera bounds
        Gizmos.color = Color.green;
        float height = _mainCamera.orthographicSize * 2;
        float width = height * _mainCamera.aspect;
        Vector3 cameraCenter = _mainCamera.transform.position;
        cameraCenter.z = 0;

        Gizmos.DrawWireCube(cameraCenter, new Vector3(width, height, 0));

        // Draw background bounds
        if (_backgroundRenderer != null && _backgroundRenderer.sprite != null)
        {
            Gizmos.color = Color.blue;
            Vector3 size = _backgroundRenderer.bounds.size;
            Gizmos.DrawWireCube(_backgroundRenderer.transform.position, size);
        }
    }
#endif
}