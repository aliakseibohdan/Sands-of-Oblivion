using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class InteractiveElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [System.Serializable]
    public class AnimationEvent
    {
        public string name;
        public AnimationClip clip;
        public bool reverseOnExit = true;
        public float playOffset = 0f;
    }

    [System.Serializable]
    public class ChildAnimationEvent : AnimationEvent
    {
        public Transform childTransform;
    }

    [Header("Interaction Configuration")]
    public string targetSubscene;
    public string itemID;
    public string dialogueKey;
    public enum InteractionType { OpenSubscene, StartDialogue, PickupItem }
    public InteractionType interactionType;

    [Header("Visual Feedback")]
    public SpriteRenderer spriteRenderer;
    [ColorUsage(true, true)] public Color glowColor = Color.yellow;
    [Range(0f, 10f)] public float glowIntensity = 2f;
    [Range(0f, 1f)] public float glowSize = 0.5f;
    [Range(0f, 1f)] public float hoverScaleFactor = 1.1f;
    [Range(0f, 1f)] public float clickScaleFactor = 0.95f;
    public bool useGlowEffect = true;
    public bool useScaleEffect = true;

    [Header("Animations")]
    public List<AnimationEvent> hoverAnimations = new();
    public List<AnimationEvent> dehoverAnimations = new();
    public List<ChildAnimationEvent> childHoverAnimations = new();
    public List<ChildAnimationEvent> childDhoverAnimations = new();

    [Header("Events")]
    public UnityEvent OnHoverStart;
    public UnityEvent OnHoverEnd;
    public UnityEvent OnClick;

    // Private state
    private Material _glowMaterial;
    private Vector3 _originalScale;
    private Dictionary<AnimationClip, Animation> _animationComponents = new();
    private Dictionary<Transform, Dictionary<AnimationClip, Animation>> _childAnimationComponents = new();
    private bool _isHovered;
    private float _currentGlowIntensity;
    private Coroutine _glowFadeCoroutine;

    private void Awake()
    {
        // Get or create sprite renderer
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // Store original scale
        _originalScale = transform.localScale;

        // Initialize glow effect if needed
        if (useGlowEffect && spriteRenderer != null)
        {
            CreateGlowMaterial();
        }

        // Initialize animations
        InitializeAnimations();
    }

    private void CreateGlowMaterial()
    {
        Shader glowShader = Shader.Find("Custom/SpriteGlow");
        if (glowShader != null)
        {
            _glowMaterial = new Material(glowShader);
            _glowMaterial.SetColor("_GlowColor", glowColor);
            _glowMaterial.SetFloat("_GlowIntensity", 0f);
            _glowMaterial.SetFloat("_GlowSize", glowSize);

            // Apply material to renderer
            spriteRenderer.material = _glowMaterial;
        }
        else
        {
            Debug.LogWarning("Custom/SpriteGlow shader not found. Glow effect disabled.");
            useGlowEffect = false;
        }
    }

    private void InitializeAnimations()
    {
        // Initialize own animations
        foreach (var animEvent in hoverAnimations)
        {
            if (animEvent.clip == null) continue;
            AddAnimationComponent(animEvent.clip);
        }

        foreach (var animEvent in dehoverAnimations)
        {
            if (animEvent.clip == null) continue;
            AddAnimationComponent(animEvent.clip);
        }

        // Initialize child animations
        _childAnimationComponents = new Dictionary<Transform, Dictionary<AnimationClip, Animation>>();
        foreach (var animEvent in childHoverAnimations)
        {
            if (animEvent.clip == null || animEvent.childTransform == null) continue;
            AddChildAnimationComponent(animEvent.childTransform, animEvent.clip);
        }

        foreach (var animEvent in childDhoverAnimations)
        {
            if (animEvent.clip == null || animEvent.childTransform == null) continue;
            AddChildAnimationComponent(animEvent.childTransform, animEvent.clip);
        }
    }

    private void AddAnimationComponent(AnimationClip clip)
    {
        if (_animationComponents.ContainsKey(clip)) return;

        var anim = gameObject.AddComponent<Animation>();
        anim.AddClip(clip, clip.name);
        anim.playAutomatically = false;
        _animationComponents[clip] = anim;
    }

    private void AddChildAnimationComponent(Transform child, AnimationClip clip)
    {
        if (child == null) return;

        if (!_childAnimationComponents.TryGetValue(child, out var animDict))
        {
            animDict = new Dictionary<AnimationClip, Animation>();
            _childAnimationComponents[child] = animDict;
        }

        if (animDict.ContainsKey(clip)) return;

        var anim = child.gameObject.GetComponent<Animation>();
        if (anim == null) anim = child.gameObject.AddComponent<Animation>();

        anim.AddClip(clip, clip.name);
        anim.playAutomatically = false;
        animDict[clip] = anim;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        StartHoverEffects();
        OnHoverStart.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        EndHoverEffects();
        OnHoverEnd.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        StartCoroutine(ClickAnimation());
        OnClick.Invoke();
        HandleInteraction();
    }

    private void StartHoverEffects()
    {
        // Scale effect
        if (useScaleEffect)
        {
            transform.localScale = _originalScale * hoverScaleFactor;
        }

        // Glow effect
        if (useGlowEffect && _glowMaterial != null)
        {
            if (_glowFadeCoroutine != null) StopCoroutine(_glowFadeCoroutine);
            _glowFadeCoroutine = StartCoroutine(FadeGlow(0f, glowIntensity, 0.3f));
        }

        // Play hover animations
        PlayAnimationEvents(hoverAnimations, false);

        // Play child hover animations
        PlayChildAnimationEvents(childHoverAnimations, false);
    }

    private void EndHoverEffects()
    {
        // Reset scale
        if (useScaleEffect)
        {
            transform.localScale = _originalScale;
        }

        // End glow effect
        if (useGlowEffect && _glowMaterial != null)
        {
            if (_glowFadeCoroutine != null) StopCoroutine(_glowFadeCoroutine);
            _glowFadeCoroutine = StartCoroutine(FadeGlow(_currentGlowIntensity, 0f, 0.5f));
        }

        // Play dehover animations
        PlayAnimationEvents(dehoverAnimations, false);

        // Play child dehover animations
        PlayChildAnimationEvents(childDhoverAnimations, false);

        // Reverse hover animations if needed
        foreach (var animEvent in hoverAnimations)
        {
            if (animEvent.clip == null || !animEvent.reverseOnExit) continue;

            if (_animationComponents.TryGetValue(animEvent.clip, out var anim) && anim.IsPlaying(animEvent.clip.name))
            {
                anim[animEvent.clip.name].speed = -1f;
                anim[animEvent.clip.name].time = anim[animEvent.clip.name].length;
                anim.Play(animEvent.clip.name);
            }
        }

        // Reverse child hover animations if needed
        foreach (var animEvent in childHoverAnimations)
        {
            if (animEvent.clip == null || animEvent.childTransform == null || !animEvent.reverseOnExit) continue;

            if (_childAnimationComponents.TryGetValue(animEvent.childTransform, out var animDict) &&
                animDict.TryGetValue(animEvent.clip, out var anim) &&
                anim.IsPlaying(animEvent.clip.name))
            {
                anim[animEvent.clip.name].speed = -1f;
                anim[animEvent.clip.name].time = anim[animEvent.clip.name].length;
                anim.Play(animEvent.clip.name);
            }
        }
    }

    private void PlayAnimationEvents(List<AnimationEvent> events, bool reverse)
    {
        foreach (var animEvent in events)
        {
            if (animEvent.clip == null) continue;

            if (_animationComponents.TryGetValue(animEvent.clip, out var anim))
            {
                anim[animEvent.clip.name].speed = reverse ? -1f : 1f;
                anim[animEvent.clip.name].time = reverse ? anim[animEvent.clip.name].length : animEvent.playOffset;
                anim.Play(animEvent.clip.name);
            }
        }
    }

    private void PlayChildAnimationEvents(List<ChildAnimationEvent> events, bool reverse)
    {
        foreach (var animEvent in events)
        {
            if (animEvent.clip == null || animEvent.childTransform == null) continue;

            if (_childAnimationComponents.TryGetValue(animEvent.childTransform, out var animDict) &&
                animDict.TryGetValue(animEvent.clip, out var anim))
            {
                anim[animEvent.clip.name].speed = reverse ? -1f : 1f;
                anim[animEvent.clip.name].time = reverse ? anim[animEvent.clip.name].length : animEvent.playOffset;
                anim.Play(animEvent.clip.name);
            }
        }
    }

    private IEnumerator ClickAnimation()
    {
        Vector3 clickScale = _originalScale * clickScaleFactor;
        transform.localScale = clickScale;

        yield return new WaitForSeconds(0.1f);

        if (_isHovered)
        {
            transform.localScale = _originalScale * hoverScaleFactor;
        }
        else
        {
            transform.localScale = _originalScale;
        }
    }

    private IEnumerator FadeGlow(float startValue, float endValue, float duration)
    {
        float elapsed = 0;

        while (elapsed < duration)
        {
            _currentGlowIntensity = Mathf.Lerp(startValue, endValue, elapsed / duration);
            _glowMaterial.SetFloat("_GlowIntensity", _currentGlowIntensity);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _currentGlowIntensity = endValue;
        _glowMaterial.SetFloat("_GlowIntensity", _currentGlowIntensity);
    }

    private void HandleInteraction()
    {
        switch (interactionType)
        {
            case InteractionType.OpenSubscene:
                if (!string.IsNullOrEmpty(targetSubscene))
                {
                    GameManager.Instance.SceneManager.SwitchSubScene(targetSubscene);
                }
                break;

            case InteractionType.StartDialogue:
                if (!string.IsNullOrEmpty(dialogueKey))
                {
                    //DialogueSystem.Instance.StartDialogue(dialogueKey);
                }
                break;

            case InteractionType.PickupItem:
                PickupItem();
                break;
        }
    }

    private void PickupItem()
    {
        if (!string.IsNullOrEmpty(itemID))
        {
            BackpackSystem.Instance.AddItem(itemID);
            gameObject.SetActive(false); // Remove from scene
            //SoundManager.Instance.PlaySFX("item_pickup");
        }
    }

    private void OnDestroy()
    {
        if (_glowMaterial != null)
        {
            Destroy(_glowMaterial);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-set sprite renderer if not set
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // Update glow material if it exists
        if (_glowMaterial != null)
        {
            _glowMaterial.SetColor("_GlowColor", glowColor);
            _glowMaterial.SetFloat("_GlowSize", glowSize);
        }
    }
#endif
}