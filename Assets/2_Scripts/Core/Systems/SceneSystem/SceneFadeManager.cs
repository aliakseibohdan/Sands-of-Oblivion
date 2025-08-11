using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneFadeManager : MonoBehaviour
{
    public static SceneFadeManager instance;

    public float FadeDuration = 1f;
    public FadeType CurrentFadeType;

    private readonly int _fadeAmount = Shader.PropertyToID("_FadeAmount");

    private readonly int _useShutters = Shader.PropertyToID("_UseShutters");
    private readonly int _useRadialWipe = Shader.PropertyToID("_UseRadialWipe");
    private readonly int _usePlainBlack = Shader.PropertyToID("_UsePlainBlack");
    private readonly int _useGoop = Shader.PropertyToID("_UseGoop");

    private int? _lastEffect;

    private Image _image;
    private Material _material;

    public enum FadeType
    {
        Shutters,
        RadialWipe,
        PlainBlack,
        Goop
    }

    public bool IsFadingOut {  get; private set; }
    public bool IsFadingIn { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        _image = GetComponent<Image>();

        Material mat = _image.material;
        _image.material = new Material(mat);
        _material = _image.material;

        _lastEffect = _useShutters;
    }

    public void FadeOut(FadeType fadeType)
    {
        ChangeFadeEffect(fadeType);
        StartFadeOut();
    }

    public void FadeIn(FadeType fadeType)
    {
        ChangeFadeEffect(fadeType);
        StartFadeIn();
    }

    private void ChangeFadeEffect(FadeType fadeType)
    {
        if (_lastEffect.HasValue)
        {
            _material.SetFloat(_lastEffect.Value, 0f);
        }

        switch (fadeType)
        {
            case FadeType.Shutters:

                SwitchEffect(_useShutters);
                break;

            case FadeType.RadialWipe:

                SwitchEffect(_useRadialWipe);
                break;

            case FadeType.PlainBlack:

                SwitchEffect(_usePlainBlack);
                break;

            case FadeType.Goop:

                SwitchEffect(_useGoop);
                break;
        }
    }

    private void SwitchEffect(int effectToTurnOn)
    {
        _material.SetFloat(effectToTurnOn, 1f);

        _lastEffect = effectToTurnOn;
    }

    private void StartFadeOut()
    {
        IsFadingOut = true;
        _material.SetFloat(_fadeAmount, 0f);

        StartCoroutine(HandleFade(1f, 0f));
    }

    private void StartFadeIn()
    {
        IsFadingIn = true;
        _material.SetFloat(_fadeAmount, 1f);

        StartCoroutine(HandleFade(0f, 1f));
    }

    private IEnumerator HandleFade(float targetAmount, float startAmount)
    {
        float elapsedTime = 0f;
        while (elapsedTime < FadeDuration)
        {
            elapsedTime += Time.deltaTime;

            float lerpedAmount = Mathf.Lerp(startAmount, targetAmount, (elapsedTime / FadeDuration));
            _material.SetFloat(_fadeAmount, lerpedAmount);

            yield return null;
        }

        _material.SetFloat(_fadeAmount, targetAmount);
        IsFadingOut = false;
        IsFadingIn = false;
    }
}