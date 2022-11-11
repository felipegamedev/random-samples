using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Popup : MonoBehaviour
{
    public class Events
    {
        public const string ON_SHOW_COMPLETED = "Popup.ON_SHOW_COMPLETED";
        public const string ON_HIDE_COMPLETED = "Popup.ON_HIDE_COMPLETED";
        public const string ON_BUTTON1_CLICKED = "Popup.ON_BUTTON1_CLICKED";
        public const string ON_BUTTON2_CLICKED = "Popup.ON_BUTTON2_CLICKED";
    }

    protected RectTransform containerRectTransform;
    protected Transform buttonsParentTransform;

    [SerializeField] private Color _backgroundColor;
    [SerializeField] private Image _backgroundImage;
    private Tweener _backgroundImageTweener;
    private Tweener __containerTweener;
    private Transform _headerParentTransform;
    private Transform _bodyParentTransform;
    private Transform _footerParentTransform;

    public virtual void Initialize()
    {
        SetBackgroundAndContainerReferences();
        SetContentBaseReferences();
        Reset(false);
    }

    public virtual void Show(string p_headerText, string p_bodyText, string p_footerText, string p_buttonText, Action p_onButtonClicked, Action p_onComplete)
    {
        Reset();
        SetHeaderText(p_headerText);
        SetBodyText(p_bodyText);
        SetFooterText(p_footerText);
        SetButton1(p_buttonText, p_onButtonClicked);
        ShowPopup(p_onComplete);
    }

    public virtual void Show(string p_headerText, string p_bodyText, string p_footerText, string p_button1Text, Action p_onButton1Clicked, string p_button2Text, Action p_onButton2Clicked, Action p_onComplete)
    {
        Reset();
        SetHeaderText(p_headerText);
        SetBodyText(p_bodyText);
        SetFooterText(p_footerText);
        SetButton1(p_button1Text, p_onButton1Clicked);
        SetButton2(p_button2Text, p_onButton2Clicked);
        ShowPopup(p_onComplete);
    }

    public virtual void Hide(Action p_onComplete)
    {
        HidePopup(p_onComplete);
    }

    protected virtual void ShowPopup(Action p_onComplete)
    {
        if (_backgroundImage != null)
        {
            if (_backgroundImageTweener != null)
            {
                _backgroundImageTweener.Kill();
            }

            _backgroundImageTweener = _backgroundImage.DOColor(_backgroundColor, 0.35f).SetEase(Ease.OutCubic).OnComplete(() =>
            {
                _backgroundImageTweener = null;
            });
        }

        if (__containerTweener != null)
        {
            __containerTweener.Kill();
        }

        __containerTweener = containerRectTransform.DOAnchorPosY(0f, 0.45f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            __containerTweener = null;
            p_onComplete?.Invoke();
            EventsManager.Instance.TriggerEvent(Popup.Events.ON_SHOW_COMPLETED);
        });
    }

    public virtual void HidePopup(Action p_onComplete)
    {
        if (_backgroundImage != null)
        {
            if (_backgroundImageTweener != null)
            {
                _backgroundImageTweener.Kill();
            }

            _backgroundImageTweener = _backgroundImage.DOColor(Color.clear, 0.25f).SetEase(Ease.InCubic).OnComplete(() =>
            {
                _backgroundImageTweener = null;
            });
        }

        if (__containerTweener != null)
        {
            __containerTweener.Kill();
        }

        __containerTweener = containerRectTransform.DOAnchorPosY(Screen.height, 0.35f).SetEase(Ease.InBack).OnComplete(() =>
        {
            __containerTweener = null;
            this.gameObject.SetActive(false);
            p_onComplete?.Invoke();
            EventsManager.Instance.TriggerEvent(Popup.Events.ON_HIDE_COMPLETED);
        });
    }

    protected virtual void Reset(bool p_enable = true)
    {
        this.gameObject.SetActive(p_enable);
        ResetBackgroundAndContainer();
        ToggleHeaderEnabled(false);
        ToggleBodyEnabled(false);
        ToggleFooterEnabled(false);
        ToggleButtonsEnabled(false);
    }

    protected virtual void ResetBackgroundAndContainer()
    {
        if (_backgroundImage != null)
        {
            _backgroundImage.color = Color.clear;
        }

        containerRectTransform.anchoredPosition = Screen.height * Vector2.down;
    }

    protected virtual void ToggleHeaderEnabled(bool p_enabled)
    {
        for (int i = 0; i < _headerParentTransform.childCount; i++)
        {
            _headerParentTransform.GetChild(i).gameObject.SetActive(false);
        }
    }

    protected virtual void ToggleBodyEnabled(bool p_enabled)
    {
        for (int i = 0; i < _bodyParentTransform.childCount; i++)
        {
            _bodyParentTransform.GetChild(i).gameObject.SetActive(false);
        }
    }

    protected virtual void ToggleFooterEnabled(bool p_enable)
    {
        for (int i = 0; i < _footerParentTransform.childCount; i++)
        {
            _footerParentTransform.GetChild(i).gameObject.SetActive(false);
        }
    }

    protected virtual void ToggleButtonsEnabled(bool p_enabled)
    {
        for (int i = 0; i < buttonsParentTransform.childCount; i++)
        {
            buttonsParentTransform.GetChild(i).gameObject.SetActive(false);
        }
    }

    protected virtual void SetHeaderText(string p_headerText)
    {
        TextMeshProUGUI __headerText = _headerParentTransform.Find("HeaderText").GetComponent<TextMeshProUGUI>();

        if (string.IsNullOrEmpty(p_headerText))
        {
            __headerText.gameObject.SetActive(false);
        }
        else
        {
            __headerText.gameObject.SetActive(true);
            __headerText.text = p_headerText;
        }
    }

    protected virtual void SetBodyText(string p_bodyText)
    {
        TextMeshProUGUI __bodyText = _bodyParentTransform.Find("BodyText").GetComponent<TextMeshProUGUI>();

        if (string.IsNullOrEmpty(p_bodyText))
        {
            __bodyText.gameObject.SetActive(false);
        }
        else
        {
            __bodyText.gameObject.SetActive(true);
            __bodyText.text = p_bodyText;
        }
    }

    protected virtual void SetFooterText(string p_footerText)
    {
        TextMeshProUGUI __footerText = _footerParentTransform.Find("FooterText").GetComponent<TextMeshProUGUI>();

        if (string.IsNullOrEmpty(p_footerText))
        {
            __footerText.gameObject.SetActive(false);
        }
        else
        {
            __footerText.gameObject.SetActive(true);
            __footerText.text = p_footerText;
        }
    }

    protected virtual void SetButton1(string p_button1Text, Action p_onButton1Clicked)
    {
        Button __button = buttonsParentTransform.Find("Button1").GetComponent<Button>();

        __button.gameObject.SetActive(true);
        __button.transform.GetComponentInChildren<TextMeshProUGUI>().text = p_button1Text;
        __button.onClick.RemoveAllListeners();
        __button.onClick.AddListener(() =>
        {
            p_onButton1Clicked?.Invoke();
            EventsManager.Instance.TriggerEvent(Popup.Events.ON_BUTTON1_CLICKED);
        });
    }

    protected virtual void SetButton2(string p_button2Text, Action p_onButton2Clicked)
    {
        Button __button = buttonsParentTransform.Find("Button2").GetComponent<Button>();

        __button.gameObject.SetActive(true);
        __button.transform.GetComponentInChildren<TextMeshProUGUI>().text = p_button2Text;
        __button.onClick.RemoveAllListeners();
        __button.onClick.AddListener(() =>
        {
            p_onButton2Clicked?.Invoke();
            EventsManager.Instance.TriggerEvent(Popup.Events.ON_BUTTON2_CLICKED);
        });
    }

    protected void SetBackgroundAndContainerReferences()
    {
        containerRectTransform = this.transform.Find("Container").GetComponent<RectTransform>();
        buttonsParentTransform = containerRectTransform.Find("Buttons");
    }

    private void SetContentBaseReferences()
    {
        _headerParentTransform = containerRectTransform.Find("Header");
        _bodyParentTransform = containerRectTransform.Find("Body");
        _footerParentTransform = containerRectTransform.Find("Footer");
    }
}
