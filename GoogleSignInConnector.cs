#if UNITY_ANDROID
using System;
using UnityEngine;
using Google;
using Firebase.Extensions;

public class GoogleSignInConnector
{
    public static void SetupConfiguration()
    {
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            RequestIdToken = true,
            RequestEmail = true,
            RequestProfile = true,
            ForceTokenRefresh = true,
            WebClientId = "1234567890-g10912039alkdkgaok1i293i192i31.apps.googleusercontent.com"
        };
    }

    public static void SignIn(bool p_silentlyLogin, Action<GoogleSignInUser> p_onSuccess, Action<GoogleSignIn.SignInException> p_onFailure)
    {
        if (p_silentlyLogin)
        {
            GoogleSignIn.DefaultInstance.SignInSilently().ContinueWithOnMainThread(p_task =>
            {
                if (p_task.IsCanceled || p_task.IsFaulted)
                {
                    Console.Write("### GoogleSignInConnector ERROR ### " + Newtonsoft.Json.JsonConvert.SerializeObject(p_task.Exception));
                    p_onFailure?.Invoke(ExceptionToSignInException(p_task.Exception));

                    return;
                }

                Console.Write("### GoogleSignInConnector SUCCESS ### " + Newtonsoft.Json.JsonConvert.SerializeObject(p_task.Result));
                p_onSuccess?.Invoke(p_task.Result);
            });
        }
        else
        {
            GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(p_task =>
            {
                if (p_task.IsCanceled || p_task.IsFaulted)
                {
                    Console.Write("### GoogleSignInConnector ERROR ### " + Newtonsoft.Json.JsonConvert.SerializeObject(p_task.Exception));
                    p_onFailure?.Invoke(ExceptionToSignInException(p_task.Exception));

                    return;
                }

                Console.Write("### GoogleSignInConnector SUCCESS ### " + Newtonsoft.Json.JsonConvert.SerializeObject(p_task.Result));
                p_onSuccess?.Invoke(p_task.Result);
            });
        }
    }

    public static void SignOut()
    {
        GoogleSignIn.DefaultInstance.SignOut();
    }

    public static void Disconnect()
    {
        GoogleSignIn.DefaultInstance.Disconnect();
    }

    public static GoogleSignIn.SignInException ExceptionToSignInException(Exception p_exception)
    {
        return (GoogleSignIn.SignInException)p_exception.GetBaseException();
    }
}
#endif
