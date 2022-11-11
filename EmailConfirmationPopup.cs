using System;
using UnityEngine;
using TMPro;

public class EmailConfirmationPopup : Popup
{
    public string EmailInputFieldText { get { return _emailInputField.text; } }
    public string PasswordInputFieldText { get { return _passwordInputField.text; } }
    [SerializeField] private TipBalloon _tipBalloon;
    [SerializeField] private TMP_InputField _emailInputField;
    [SerializeField] private TMP_InputField _passwordInputField;

    public override void Initialize()
    {
        SetBackgroundAndContainerReferences();
        SetInputFieldsReferences();
        _tipBalloon.Initialize();
        Reset(false);
    }

    public void Show(string p_email, string p_button1Text, Action p_onButton1Clicked, string p_button2Text, Action p_onButton2Clicked, Action p_onComplete)
    {
        Reset();
        _emailInputField.text = p_email;
        SetButton1(p_button1Text, p_onButton1Clicked);
        SetButton2(p_button2Text, p_onButton2Clicked);
        ShowPopup(p_onComplete);
    }

    public bool IsPasswordFieldValid()
    {
        return ValidateInputFromPasswordInputField();
    }

    protected override void Reset(bool p_enable = true)
    {
        this.gameObject.SetActive(p_enable);
        ResetBackgroundAndContainer();
        ResetInputFields();
        ResetPasswordInputFieldValidation();
        ToggleButtonsEnabled(false);
    }

    private void ResetInputFields()
    {
        _emailInputField.text = string.Empty;
        _passwordInputField.text = string.Empty;
    }

    private void ResetPasswordInputFieldValidation()
    {
        _passwordInputField.transform.Find("Outline").gameObject.SetActive(false);
    }

    private bool ValidateInputFromPasswordInputField()
    {
        string __errorString = ValidatePassword(_passwordInputField.text);

        if (string.IsNullOrEmpty(__errorString))
        {
            _passwordInputField.transform.Find("Outline").gameObject.SetActive(false);

            return true;
        }
        else
        {
            RectTransform __tipBalloonRectTransform = _tipBalloon.Show(TipBalloon.ENUM_ArrowDirections.LEFT, __errorString);

            __tipBalloonRectTransform.SetParent(_passwordInputField.transform);
            __tipBalloonRectTransform.anchoredPosition = Vector2.zero;
            __tipBalloonRectTransform.anchorMin = new Vector2(1f, 0.5f);
            __tipBalloonRectTransform.anchorMax = new Vector2(1f, 0.5f);
            _passwordInputField.transform.Find("Outline").gameObject.SetActive(true);

            return false;
        }
    }

    private string ValidatePassword(string p_text)
    {
        if (string.IsNullOrEmpty(p_text))
        {
            return Assets.SimpleLocalization.LocalizationManager.Localize("Error.EmptyField").Replace("<BR>", "\n");
        }
        else if (p_text.Length < 6)
        {
            return Assets.SimpleLocalization.LocalizationManager.Localize("Error.InvalidPasswordMinCharacters").Replace("<BR>", "\n");
        }

        return string.Empty;
    }

    private void SetInputFieldsReferences()
    {
        _emailInputField = containerRectTransform.Find("Form/EmailInputField").GetComponent<TMP_InputField>();
        _emailInputField.readOnly = true;
        _passwordInputField = containerRectTransform.Find("Form/PasswordInputField").GetComponent<TMP_InputField>();
    }
}
