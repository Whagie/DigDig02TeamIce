using UnityEngine;
using UnityEngine.UI;

public class SceneFadeManager : MonoBehaviour
{
    public static SceneFadeManager instance;

    [SerializeField] private Image _fadeOutImage;
    [Range(0f, 1f), SerializeField] private float _fadeOutSpeed = 2f;
    [Range(0f, 1f), SerializeField] private float _fadeInSpeed = 2f;

    [SerializeField] public Color _fadeOutStartColor;

    public bool IsFadingOut { get; private set; }
    public bool IsFadingIn { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        _fadeOutStartColor.a = 0f;
    }

    private void Update()
    {
        if (IsFadingOut)
        {
            if (_fadeOutImage.color.a < 1f)
            {
                _fadeOutStartColor.a += Time.unscaledDeltaTime * _fadeOutSpeed;
                _fadeOutImage.color = _fadeOutStartColor;
            }
            else
            {
                IsFadingOut = false;
            }
        }

        if (IsFadingIn)
        {
            if (_fadeOutImage.color.a > 0f)
            {
                _fadeOutStartColor.a -= Time.unscaledDeltaTime * _fadeInSpeed;
                _fadeOutImage.color = _fadeOutStartColor;
            }
            else
            {
                IsFadingIn = false;
            }
        }
    }

    public void StartFadeOut()
    {
        _fadeOutImage.color = _fadeOutStartColor;
        IsFadingOut = true;
    }

    public void StartFadeIn()
    {
        if (_fadeOutImage.color.a >= 1f)
        {
            _fadeOutImage.color = _fadeOutStartColor;
            IsFadingIn = true;
        }
        else
        {
            Debug.LogWarning("Scene fade-out not fully completed before fade-in! Fading out anyway...");
            _fadeOutImage.color = _fadeOutStartColor;
            IsFadingIn = true;
            IsFadingOut = false;
        }
    }
}
