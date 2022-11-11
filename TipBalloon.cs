using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class TipBalloon : MonoBehaviour
{
    public enum ENUM_ArrowDirections
    {
        LEFT,
        RIGHT
    }

    public event Action OnClick;

    protected CanvasGroup _canvasGroup;
    protected Image _containerImage;
    protected Button _containerButton;
    protected TMP_Text _text;
    protected Tweener _alphaTweener;

    private bool _isInitialized;

    public virtual void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        this.gameObject.SetActive(false);
        _canvasGroup = this.GetComponent<CanvasGroup>();
        _containerImage = this.transform.Find("Container").GetComponent<Image>();
        _containerButton = _containerImage.GetComponent<Button>();
        _containerButton.onClick.AddListener(() =>
        {
            OnClick?.Invoke();
        });
        _text = this.transform.Find("Text").GetComponent<TMP_Text>();
        _text.transform.SetAsLastSibling(); // Make sure the text is always rendered in front of the container.
        _isInitialized = true;
    }

    public virtual RectTransform Show(ENUM_ArrowDirections p_arrowDirection, string p_text, Action p_onComplete, bool p_playAnimation = true)
    {
        this.gameObject.SetActive(true);
        Reset();
        SetContainerOrientation(p_arrowDirection);
        _text.text = p_text;

        if (p_playAnimation)
        {
            PlayShowAnimation(p_onComplete);
        }
        else
        {
            ForceShow();
        }

        return (RectTransform)this.transform;
    }

    public RectTransform Show(ENUM_ArrowDirections p_arrowDirection, string p_text)
    {
        return Show(p_arrowDirection, p_text, null);
    }

    public virtual void Hide(Action p_onComplete, bool p_playAnimation = true)
    {
        _canvasGroup.interactable = false;

        if (p_playAnimation)
        {
            PlayHideAnimation(p_onComplete);
        }
        else
        {
            ForceHide();
        }
    }

    public void Hide()
    {
        Hide(null);
    }

    public void SetText(string p_text)
    {
        _text.text = p_text;
    }

    protected virtual void Reset()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
    }

    protected virtual void PlayShowAnimation(Action p_onComplete)
    {
        if (_alphaTweener != null)
        {
            _alphaTweener.Kill();
        }

        _alphaTweener = _canvasGroup.DOFade(1f, 0.15f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            _canvasGroup.interactable = true;
            _alphaTweener = null;
            p_onComplete?.Invoke();
        });
    }

    protected virtual void ForceShow()
    {
        _canvasGroup.alpha = 1f;
    }

    protected virtual void PlayHideAnimation(Action p_onComplete)
    {
        if (_alphaTweener != null)
        {
            _alphaTweener.Kill();
        }

        _alphaTweener = _canvasGroup.DOFade(0f, 0.1f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            this.gameObject.SetActive(false);
            _alphaTweener = null;
            p_onComplete?.Invoke();
        });
    }

    protected virtual void ForceHide()
    {
        _canvasGroup.alpha = 0f;
        this.gameObject.SetActive(false);
    }

    protected virtual void SetContainerOrientation(ENUM_ArrowDirections p_arrowDirection)
    {
        RectTransform __rectTransform = (RectTransform)this.transform;
        Vector2 __pivot;
        Vector3 __containerScale = _containerImage.transform.localScale;

        switch (p_arrowDirection)
        {
            case ENUM_ArrowDirections.RIGHT:
                __pivot = new Vector2(1f, 0.5f);
                __containerScale.x = -1f;
                break;
            default:
                __pivot = new Vector2(0f, 0.5f);
                __containerScale.x = 1f;
                break;
        }

        __rectTransform.pivot = __pivot;
        _containerImage.transform.localScale = __containerScale;
    }
}
