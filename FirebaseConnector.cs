using System;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

public class FirebaseConnector
{
    public static FirebaseUser CurrentUser { get { return FirebaseAuth.DefaultInstance.CurrentUser; } }

    public static FirebaseException AggregateExceptionToFirebaseException(AggregateException p_aggregateException)
    {
        return (FirebaseException)p_aggregateException.GetBaseException();
    }
    
    public static void CheckAndFixDependencies(Action<bool> p_onComplete)
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(p_task =>
        {
            p_onComplete?.Invoke(p_task.Result == Firebase.DependencyStatus.Available);
        });
    }

    public static void SignInWithEmailAndPassword(string p_email, string p_password, Action<FirebaseUser> p_onSuccess, Action<FirebaseException> p_onFailure)
    {
        FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(p_email, p_password).ContinueWithOnMainThread(p_task =>
        {
            if (p_task.IsCanceled || p_task.IsFaulted)
            {
                p_onFailure?.Invoke(AggregateExceptionToFirebaseException(p_task.Exception));

                return;
            }

            p_onSuccess?.Invoke(p_task.Result);
        });
    }

#if UNITY_ANDROID
    public static void SignInWithGoogle(string p_idToken, string p_accessToken, Action<FirebaseUser> p_onSuccess, Action<FirebaseException> p_onFailure)
    {
        FirebaseAuth.DefaultInstance.SignInWithCredentialAsync(Firebase.Auth.GoogleAuthProvider.GetCredential(p_idToken, p_accessToken)).ContinueWithOnMainThread(p_task =>
        {
            if (p_task.IsCanceled || p_task.IsFaulted)
            {
                p_onFailure?.Invoke(AggregateExceptionToFirebaseException(p_task.Exception));

                return;
            }
            
            p_onSuccess?.Invoke(p_task.Result);
        });
    }
#endif

#if UNITY_IOS
    public static void SignInWithApple(string p_idToken, string p_rawNonce, string p_accessToken, Action<FirebaseUser> p_onSuccess, Action<FirebaseException> p_onFailure)
    {
        FirebaseAuth.DefaultInstance.SignInWithCredentialAsync(Firebase.Auth.OAuthProvider.GetCredential("apple.com", p_idToken, p_rawNonce, p_accessToken)).ContinueWithOnMainThread(p_task =>
        {
            if (p_task.IsCanceled || p_task.IsFaulted)
            {
                p_onFailure?.Invoke(AggregateExceptionToFirebaseException(p_task.Exception));

                return;
            }
            
            p_onSuccess?.Invoke(p_task.Result);
        });
    }
#endif

    public static void CreateUserWithEmailAndPassword(string p_email, string p_password, Action<FirebaseUser> p_onSuccess, Action<FirebaseException> p_onFailure)
    {
        FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(p_email, p_password).ContinueWithOnMainThread(p_task =>
        {
            if (p_task.IsCanceled || p_task.IsFaulted)
            {
                p_onFailure?.Invoke(AggregateExceptionToFirebaseException(p_task.Exception));

                return;
            }

            p_onSuccess?.Invoke(p_task.Result);
        });
    }

    public static void SendPasswordResetEmail(string p_email, Action p_onSuccess, Action<FirebaseException> p_onFailure)
    {
        FirebaseAuth.DefaultInstance.SendPasswordResetEmailAsync(p_email).ContinueWithOnMainThread(p_task =>
        {
            if (p_task.IsCanceled || p_task.IsFaulted)
            {
                p_onFailure?.Invoke(AggregateExceptionToFirebaseException(p_task.Exception));

                return;
            }

            p_onSuccess?.Invoke();
        });
    }

    public static void DeleteUser(Action p_onSuccess, Action<FirebaseException> p_onFailure)
    {
        if (CurrentUser != null)
        {
            CurrentUser.DeleteAsync().ContinueWithOnMainThread(p_task =>
            {
                if (p_task.IsCanceled || p_task.IsFaulted)
                {
                    p_onFailure?.Invoke(AggregateExceptionToFirebaseException(p_task.Exception));

                    return;
                }

                p_onSuccess?.Invoke();
            });
        }
        else
        {
            p_onFailure?.Invoke(new FirebaseException((int)AuthError.NoSignedInUser, "User not signed in!"));
        }
    }

    public static void ReauthenticateWithEmailAndPassword(string p_email, string p_password, Action p_onSuccess, Action<FirebaseException> p_onFailure)
    {
        if (CurrentUser != null)
        {
            CurrentUser.ReauthenticateAsync(EmailAuthProvider.GetCredential(p_email, p_password)).ContinueWithOnMainThread(p_task =>
            {
                if (p_task.IsCanceled || p_task.IsFaulted)
                {
                    p_onFailure?.Invoke(AggregateExceptionToFirebaseException(p_task.Exception));

                    return;
                }

                p_onSuccess?.Invoke();
            });
        }
        else
        {
            p_onFailure?.Invoke(new FirebaseException((int)AuthError.NoSignedInUser, "User not signed in!"));
        }
    }

    public static void ReauthenticateWithEmailAndPasswordAndRetrieveData(string p_email, string p_password, Action<FirebaseUser> p_onSuccess, Action<FirebaseException> p_onFailure)
    {
        if (CurrentUser != null)
        {
            CurrentUser.ReauthenticateAndRetrieveDataAsync(EmailAuthProvider.GetCredential(p_email, p_password)).ContinueWithOnMainThread(p_task =>
            {
                if (p_task.IsCanceled || p_task.IsFaulted)
                {
                    p_onFailure?.Invoke(AggregateExceptionToFirebaseException(p_task.Exception));

                    return;
                }

                p_onSuccess?.Invoke(p_task.Result.User);
            });
        }
        else
        {
            p_onFailure?.Invoke(new FirebaseException((int)AuthError.NoSignedInUser, "User not signed in!"));
        }
    }

    public static void SignOut(Action p_onSuccess, Action<FirebaseException> p_onFailure)
    {
        if (CurrentUser != null)
        {
            FirebaseAuth.DefaultInstance.SignOut();
            p_onSuccess?.Invoke();
        }
        else
        {
            p_onFailure?.Invoke(new FirebaseException((int)AuthError.NoSignedInUser, "User not signed in!"));
        }
    }

    public static void ForceSignOut()
    {
        FirebaseAuth.DefaultInstance.SignOut();
    }

    public static void GetUserToken(Action<string> p_onSuccess, Action<FirebaseException> p_onFailure, bool p_forceRefresh = false)
    {
        if (CurrentUser != null)
        {
            CurrentUser.TokenAsync(p_forceRefresh).ContinueWithOnMainThread(p_task =>
            {
                if (p_task.IsCanceled || p_task.IsFaulted)
                {
                    p_onFailure?.Invoke(AggregateExceptionToFirebaseException(p_task.Exception));

                    return;
                }

                p_onSuccess?.Invoke(p_task.Result);
            });
        }
        else
        {
            p_onFailure?.Invoke(new FirebaseException((int)AuthError.NoSignedInUser, "User not signed in!"));
        }
    }

    public static void UpdateUserDisplayName(string p_displayName, Action p_onSuccess, Action<FirebaseException> p_onFailure)
    {
        if (CurrentUser != null)
        {
            FirebaseAuth.DefaultInstance.CurrentUser.UpdateUserProfileAsync(new Firebase.Auth.UserProfile() { DisplayName = p_displayName }).ContinueWithOnMainThread(p_task =>
            {
                if (p_task.IsCanceled || p_task.IsFaulted)
                {
                    p_onFailure?.Invoke(AggregateExceptionToFirebaseException(p_task.Exception));

                    return;
                }

                p_onSuccess?.Invoke();
            });
        }
        else
        {
            p_onFailure?.Invoke(new FirebaseException((int)AuthError.NoSignedInUser, "User not signed in!"));
        }
    }

    public static void UpdateUserProfile(Firebase.Auth.UserProfile p_userProfile, Action p_onSuccess, Action<FirebaseException> p_onFailure)
    {
        if (CurrentUser != null)
        {
            FirebaseAuth.DefaultInstance.CurrentUser.UpdateUserProfileAsync(p_userProfile).ContinueWithOnMainThread(p_task =>
            {
                if (p_task.IsCanceled || p_task.IsFaulted)
                {
                    p_onFailure?.Invoke(AggregateExceptionToFirebaseException(p_task.Exception));

                    return;
                }

                p_onSuccess?.Invoke();
            });
        }
        else
        {
            p_onFailure?.Invoke(new FirebaseException((int)AuthError.NoSignedInUser, "User not signed in!"));
        }
    }
}
