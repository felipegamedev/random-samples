using System;
#if UNITY_IOS
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Interfaces;
using AppleAuth.Native;
#endif
using UnityEngine;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

public class AppleSignInConnector : MonoBehaviour
{
    public string AppleUserId { get { return PlayerPrefs.GetString("APPLE_USER_ID", string.Empty); } }
    public string Nounce { get { return GenerateSHA256NonceFromRawNonce(GenerateRandomString(32)); } }

#if UNITY_IOS
    private IAppleAuthManager _appleAuthManager;

    public void Initialize()
    {
        // Check if the current platform is supported.
        if (AppleAuthManager.IsCurrentPlatformSupported)
        {
            // Create a default JSON deserializer, to transform JSON Native responses to C# instances
            // and create an Apple Authentication manager with the deserializer.
            _appleAuthManager = new AppleAuthManager(new PayloadDeserializer());
        }
        else
        {
            _appleAuthManager = null;
        }
    }

    public void SignIn(bool p_isQuickSignIn, LoginOptions p_loginOptions, Action<IAppleIDCredential> p_onSuccess, Action<IAppleError> p_onFailure)
    {
        if (p_isQuickSignIn)
        {
            QuickSignIn(p_onSuccess, p_onFailure);
        }
        else
        {
            _appleAuthManager.LoginWithAppleId(new AppleAuthLoginArgs(p_loginOptions), p_credential =>
            {
                // If a sign in with apple succeeds, we should have obtained the credential with the user id, name, and email, save it
                PlayerPrefs.SetString("APPLE_USER_ID", p_credential.User);
                p_onSuccess?.Invoke((IAppleIDCredential)p_credential);
            }, p_error =>
            {
                p_onFailure(p_error);
            });
        }
    }

    private void Update()
    {
        // Updates the AppleAuthManager instance to execute
        // pending callbacks inside Unity's execution loop
        if (_appleAuthManager != null)
        {
            _appleAuthManager.Update();
        }
    }

    private void QuickSignIn(Action<IAppleIDCredential> p_onSuccess, Action<IAppleError> p_onFailure)
    {
        // Quick login should succeed if the credential was authorized before and not revoked
        _appleAuthManager.QuickLogin(new AppleAuthQuickLoginArgs(), p_credential =>
        {
            // If a sign in with apple succeeds, we should have obtained the credential with the user id, name, and email, save it
            PlayerPrefs.SetString("APPLE_USER_ID", p_credential.User);
            p_onSuccess?.Invoke((IAppleIDCredential)p_credential);
        }, p_error =>
        {
            p_onFailure(p_error);
        });
    }

    private void CheckCredentialStatusForUserId(string p_appleUserId, Action<CredentialState> p_onSuccess, Action<IAppleError> p_onFailure)
    {
        // If there is an apple ID available, we should check the credential state
        _appleAuthManager.GetCredentialState(p_appleUserId, p_credentialState =>
        {
            p_onSuccess?.Invoke(p_credentialState);
        }, p_error =>
        {
            p_onFailure?.Invoke(p_error);
        });
    }
#endif

    private string GenerateRandomString(int p_length)
    {
        if (p_length <= 0)
        {
            throw new Exception("Expected nonce to have positive length");
        }

        const string __charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVXYZabcdefghijklmnopqrstuvwxyz-._";
        RNGCryptoServiceProvider __cryptographicallySecureRandomNumberGenerator = new RNGCryptoServiceProvider();
        string __result = string.Empty;
        int __remainingLength = p_length;
        byte[] __randomNumberHolder = new byte[1];

        while (__remainingLength > 0)
        {
            List<int> __randomNumbers = new List<int>(16);

            for (var randomNumberCount = 0; randomNumberCount < 16; randomNumberCount++)
            {
                __cryptographicallySecureRandomNumberGenerator.GetBytes(__randomNumberHolder);
                __randomNumbers.Add(__randomNumberHolder[0]);
            }

            for (var randomNumberIndex = 0; randomNumberIndex < __randomNumbers.Count; randomNumberIndex++)
            {
                if (__remainingLength == 0)
                {
                    break;
                }

                var randomNumber = __randomNumbers[randomNumberIndex];

                if (randomNumber < __charset.Length)
                {
                    __result += __charset[randomNumber];
                    __remainingLength--;
                }
            }
        }

        return __result;
    }

    private string GenerateSHA256NonceFromRawNonce(string p_rawNonce)
    {
        SHA256Managed __sha = new SHA256Managed();
        var __utf8RawNonce = Encoding.UTF8.GetBytes(p_rawNonce);
        var __hash = __sha.ComputeHash(__utf8RawNonce);
        var __result = string.Empty;

        for (var i = 0; i < __hash.Length; i++)
        {
            __result += __hash[i].ToString("x2");
        }

        return __result;
    }
}
