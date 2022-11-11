using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using CI.HttpClient;
using Firebase.Auth;
#if UNITY_ANDROID
using Google;
#endif

public class NetworkManager : Singleton<NetworkManager>
{
    public enum ENUM_AuthenticationType
    {
        NONE = -1,
        EMAIL_PASSWORD,
        GOOGLE,
        APPLE
    }

    public class Events
    {
        public const string ON_FIREBASE_INITIALIZATION_COMPLETED = "ON_FIREBASE_INITIALIZATION_COMPLETED";
        public const string ON_LOGIN_COMPLETED = "ON_LOGIN_COMPLETED";
        public const string ON_REGISTER_COMPLETED = "ON_REGISTER_COMPLETED";
        public const string ON_LOGOUT_COMPLETED = "ON_LOGOUT_COMPLETED";

        // IN GAME REQUESTS
        public const string ON_SEND_SELECTED_CARDS = "ON_SEND_SELECTED_CARDS";
        public const string ON_BUY_REQUEST_RECEIVED = "ON_BUY_REQUEST_RECEIVED";
    }

    public bool IsInitializingFirebase { get; private set; }
    public bool IsFirebaseAvailable { get; private set; }
    public bool IsUserLoggedInOnFirebase { get { return FirebaseConnector.CurrentUser != null; } }
    public bool IsUserLoggedInOnServer { get { return _userData != null; } }
    public bool IsTokenRefreshing { get; private set; }
    public UserData CurrentUserData { get { return _userData; } }
    public string CurrentUserToken { get { return _userToken == null ? string.Empty : _userToken; } }
    public DateTime CurrentNextTimeBonus { get { return _userData == null ? new DateTime() : _userData.balance.nextTimeBonusUTC; } }
    public bool IsAuthenticatedByEmailAndPassword { get { return AuthenticationType == ENUM_AuthenticationType.EMAIL_PASSWORD; } }
    public ENUM_AuthenticationType AuthenticationType { get; private set; }
    public bool IsAuthenticatedByAppleOrGoogle { get { return AuthenticationType == ENUM_AuthenticationType.APPLE || AuthenticationType == ENUM_AuthenticationType.GOOGLE; } }
    public GamePlayResponse CurrentGame
    {
        get
        {
            return GetPendingGame();
        }
    }
    public Dictionary<int, List<HitData>> TableHitData { get; private set; }

    private AppleSignInConnector _appleSignInConnector;
    private UserData _userData;
    private string _userToken;

    private const string API_URL = "https://apiurl.com/api/v1";
    private const string LOGIN_ENDPOINT = "/login";
    private const string LOGOUT_ENDPOINT = "/logout";
    private const string DELETE_USER_ACCOUNT_ENDPOINT = "/account";
    private const string UPDATE_USER_PROFILE_ENDPOINT = "/profile";
    private const string ENABLE_EMAILS_ENDPOINT = "/profile/enable-emails";
    private const string RETRIEVE_GAME_TABLE_ENDPOINT = "/game/table";
    private const string RETRIEVE_PLAYER_BALANCE_ENDPOINT = "/balance";
    private const string PLAY_A_GAME_ENDPOINT = "/game/play";
    private const string COMPLETE_THE_GAME_ENDPOINT = "/game/complete";
    private const string CLAIM_TIME_BONUS_ENDPOINT = "/bonus/claim";
    private const string REGISTER_IN_APP_PURCHASE_ENDPOINT = "/purchase";
    private const string QUICK_PICK_ENDPOINT = "/pick";
    private const string RETRIEVE_ADS_ENDPOINT = "/ads";

    public void CheckFirebaseAndFixDependencies(Action<bool> p_onComplete)
    {
        IsInitializingFirebase = true;
        FirebaseConnector.CheckAndFixDependencies(p_isAvailable =>
        {
            IsInitializingFirebase = false;
            IsFirebaseAvailable = p_isAvailable;
            AuthenticationType = (ENUM_AuthenticationType)PlayerPrefs.GetInt("AUTH_TYPE", -1);
#if UNITY_ANDROID
            GoogleSignInConnector.SetupConfiguration();
#elif UNITY_IOS
            _appleSignInConnector.Initialize();
#endif
            p_onComplete?.Invoke(p_isAvailable);
            EventsManager.Instance.TriggerEvent(NetworkManager.Events.ON_FIREBASE_INITIALIZATION_COMPLETED, p_isAvailable);
        });
    }

    public void Login(string p_email, string p_password, Action<UserData> p_onSuccess, Action<int, string> p_onFailure)
    {
        FirebaseConnector.SignInWithEmailAndPassword(p_email, p_password, p_firebaseUser =>
        {
            AuthenticationType = ENUM_AuthenticationType.EMAIL_PASSWORD;
            PlayerPrefs.SetInt("AUTH_TYPE", (int)AuthenticationType);
            RefreshTokenAndLogin(p_onSuccess, p_onFailure);
        }, p_signInException =>
        {
            IsTokenRefreshing = false;
            LogoutHandler();
            p_onFailure?.Invoke(p_signInException.ErrorCode, p_signInException.Message);
        });
    }

    public void LoginWithPlatformSpecific(bool p_isQuickLoginOrSilentlyLogin, Action<UserData> p_onSuccess, Action<int, string> p_onFailure)
    {
#if UNITY_IOS
        AppleLogin(p_isQuickLoginOrSilentlyLogin, p_onSuccess, p_onFailure);
#elif UNITY_ANDROID
        GoogleLogin(p_isQuickLoginOrSilentlyLogin, p_onSuccess, p_onFailure);
#endif
    }

#if UNITY_IOS
    public void AppleLogin(bool p_isQuickLogin, Action<UserData> p_onSuccess, Action<int, string> p_onFailure)
    {
        _appleSignInConnector.SignIn(p_isQuickLogin, AppleAuth.Enums.LoginOptions.IncludeEmail | AppleAuth.Enums.LoginOptions.IncludeFullName, p_appleUserCredential =>
        {
            FirebaseConnector.SignInWithApple(Encoding.UTF8.GetString(p_appleUserCredential.IdentityToken), _appleSignInConnector.Nounce, Encoding.UTF8.GetString(p_appleUserCredential.AuthorizationCode), p_firebaseUser =>
            {
                AuthenticationType = ENUM_AuthenticationType.APPLE;
                PlayerPrefs.SetInt("AUTH_TYPE", (int)AuthenticationType);
                RefreshTokenAndLogin(p_onSuccess, p_onFailure);
            }, p_signInException =>
            {
                IsTokenRefreshing = false;
                LogoutHandler();
                p_onFailure?.Invoke(p_signInException.ErrorCode, p_signInException.Message);
            });
        }, p_appleSignInError =>
        {
            IsTokenRefreshing = false;
            LogoutHandler();
            p_onFailure?.Invoke(p_appleSignInError.Code, p_appleSignInError.LocalizedDescription + " - " + p_appleSignInError.LocalizedFailureReason + " - " + p_appleSignInError.LocalizedRecoverySuggestion);
        });
    }
#elif UNITY_ANDROID
    public void GoogleLogin(bool p_silentlyLogin, Action<UserData> p_onSuccess, Action<int, string> p_onFailure)
    {
        GoogleSignInConnector.SignIn(p_silentlyLogin, p_googleSignInUser =>
        {
            FirebaseConnector.SignInWithGoogle(p_googleSignInUser.IdToken, null, p_firebaseUser =>
            {
                AuthenticationType = ENUM_AuthenticationType.GOOGLE;
                PlayerPrefs.SetInt("AUTH_TYPE", (int)AuthenticationType);
                RefreshTokenAndLogin(p_onSuccess, p_onFailure);
            }, p_signInException =>
            {
                IsTokenRefreshing = false;
                LogoutHandler();
                p_onFailure?.Invoke(p_signInException.ErrorCode, p_signInException.Message);
            });
        }, p_googleSignInException =>
        {
            IsTokenRefreshing = false;
            LogoutHandler();
            p_onFailure?.Invoke((int)p_googleSignInException.Status, p_googleSignInException.Message);
        });
    }
#endif

    public void RefreshTokenAndLogin(Action<UserData> p_onSuccess, Action<int, string> p_onFailure)
    {
        IsTokenRefreshing = true;
        FirebaseConnector.GetUserToken(p_userToken =>
        {
#if UNITY_ANDROID
            string __platform = "Android";
#elif UNITY_IOS
            string __platform = "IOS";
#else
            string __platform = "STANDALONE";
#endif

            _userToken = p_userToken;
            Post(API_URL + LOGIN_ENDPOINT, new Dictionary<string, object>() { { "DeviceId", SystemInfo.deviceUniqueIdentifier }, { "Platform", __platform } }, p_response =>
            {
                if (p_response.IsSuccessStatusCode)
                {
                    _userData = new UserData()
                    {
                        profile = JsonConvert.DeserializeObject<UserProfile>(p_response.ReadAsString()),
                        balance = new BalanceData()
                        {
                            balance = 0,
                            timeBonus = 0,
                            nextTimeBonusUTC = new DateTime()
                        }
                    };
                    RetrievePlayerBalance(p_balanceData =>
                    {
                        CompletePendingGames(p_balanceData =>
                        {
                            RetrieveGameTable(() =>
                            {
                                IsTokenRefreshing = false;
                                p_onSuccess?.Invoke(_userData);
                                EventsManager.Instance.TriggerEvent(NetworkManager.Events.ON_LOGIN_COMPLETED, _userData);
                            }, (p_errorCode, p_errorMessage) =>
                            {
                                IsTokenRefreshing = false;
                                LogoutHandler();
                                p_onFailure?.Invoke(p_errorCode, p_errorMessage);
                            });
                        }, (p_errorCode, p_errorMessage) =>
                        {
                            IsTokenRefreshing = false;
                            LogoutHandler();
                            p_onFailure?.Invoke(p_errorCode, p_errorMessage);
                        });
                    }, (p_errorCode, p_errorMessage) =>
                    {
                        IsTokenRefreshing = false;
                        LogoutHandler();
                        p_onFailure?.Invoke(p_errorCode, p_errorMessage);
                    });
                }
                else
                {
                    IsTokenRefreshing = false;
                    LogoutHandler();
                    p_onFailure?.Invoke((int)p_response.StatusCode, p_response.ReasonPhrase);
                }
            });
        }, p_getUserTokenException =>
        {
            IsTokenRefreshing = false;
            LogoutHandler();
            p_onFailure?.Invoke(p_getUserTokenException.ErrorCode, p_getUserTokenException.Message);
        });
    }

    public void Logout(Action p_onSuccess, Action<int, string> p_onFailure)
    {
        if (FirebaseConnector.CurrentUser == null)
        {
            p_onFailure?.Invoke((int)AuthError.UserNotFound, "User not signed in!");
        }
        else
        {
            if (_userData == null)
            {
                LogoutHandler();
                p_onSuccess?.Invoke();
                EventsManager.Instance.TriggerEvent(NetworkManager.Events.ON_LOGOUT_COMPLETED);
            }
            else
            {
                Post(API_URL + LOGOUT_ENDPOINT, new Dictionary<string, string>() { { "X-SESSION-ID", _userData.profile.sessionId.ToString() } }, new Dictionary<string, object>(), p_response =>
                {
                    if (p_response.IsSuccessStatusCode)
                    {
                        LogoutHandler();
                        p_onSuccess?.Invoke();
                        EventsManager.Instance.TriggerEvent(NetworkManager.Events.ON_LOGOUT_COMPLETED);
                    }
                    else
                    {
                        p_onFailure?.Invoke((int)p_response.StatusCode, p_response.ReasonPhrase);
                    }
                });
            }
        }
    }

    public void ForceLogout(Action p_onSuccess, Action<int, string> p_onFailure)
    {
        if (FirebaseConnector.CurrentUser == null)
        {
            LogoutHandler();
            p_onFailure?.Invoke((int)AuthError.UserNotFound, "User not signed in!");
        }
        else
        {
            if (_userData != null)
            {
                Post(API_URL + LOGOUT_ENDPOINT, new Dictionary<string, string>() { { "X-SESSION-ID", _userData.profile.sessionId.ToString() } }, new Dictionary<string, object>(), p_response =>
                {
                    if (p_response.IsSuccessStatusCode)
                    {
                        LogoutHandler();
                        p_onSuccess?.Invoke();
                        EventsManager.Instance.TriggerEvent(NetworkManager.Events.ON_LOGOUT_COMPLETED);
                    }
                    else
                    {
                        LogoutHandler();
                        p_onFailure?.Invoke((int)p_response.StatusCode, p_response.ReasonPhrase);
                    }
                });
            }
        }
    }

    public void Register(string p_name, string p_email, string p_password, Action<UserData> p_onSuccess, Action<int, string> p_onFailure)
    {
        FirebaseConnector.CreateUserWithEmailAndPassword(p_email, p_password, p_firebaseUser =>
        {
            FirebaseConnector.UpdateUserDisplayName(p_name, () =>
            {
                AuthenticationType = ENUM_AuthenticationType.EMAIL_PASSWORD;
                PlayerPrefs.SetInt("AUTH_TYPE", (int)AuthenticationType);
                RefreshTokenAndLogin(p_onSuccess, p_onFailure);
            }, p_updateUserDisplayNameException =>
            {
                IsTokenRefreshing = false;
                LogoutHandler();
                p_onFailure?.Invoke(p_updateUserDisplayNameException.ErrorCode, p_updateUserDisplayNameException.Message);
            });
        }, p_createUserException =>
        {
            IsTokenRefreshing = false;
            LogoutHandler();
            p_onFailure?.Invoke(p_createUserException.ErrorCode, p_createUserException.Message);
        });
    }

    public void ResetPassword(string p_email, Action p_onSuccess, Action<int, string> p_onFailure)
    {
        FirebaseConnector.SendPasswordResetEmail(p_email, () =>
        {
            p_onSuccess?.Invoke();
        }, p_sendPasswordResetEmailException =>
        {
            p_onFailure?.Invoke(p_sendPasswordResetEmailException.ErrorCode, p_sendPasswordResetEmailException.Message);
        });
    }

    public void UpdateUserProfile(string p_name, string p_email, Action<UserProfile> p_onSuccess, Action<int, string> p_onFailure)
    {
        if (_userData == null)
        {
            p_onFailure?.Invoke((int)AuthError.NoSignedInUser, "User not signed in!");
        }
        else
        {
            Post(API_URL + UPDATE_USER_PROFILE_ENDPOINT, new Dictionary<string, string>() { { "X-SESSION-ID", _userData.profile.sessionId.ToString() } }, new Dictionary<string, object>() { { "Name", p_name }, { "Email", p_email } }, p_response =>
            {
                if (p_response.IsSuccessStatusCode)
                {
                    _userData.profile = JsonConvert.DeserializeObject<UserProfile>(p_response.ReadAsString());
                    p_onSuccess?.Invoke(_userData.profile);
                }
                else
                {
                    p_onFailure?.Invoke((int)p_response.StatusCode, p_response.ReasonPhrase);
                }
            });
        }
    }

    public void DeleteUserAccount(Action p_onSuccess, Action<int, string> p_onFailure)
    {
        if (_userData == null)
        {
            p_onFailure?.Invoke((int)AuthError.NoSignedInUser, "User not signed in!");
        }
        else
        {
            FirebaseConnector.DeleteUser(() =>
            {
                Delete(API_URL + DELETE_USER_ACCOUNT_ENDPOINT, new Dictionary<string, string>() { { "X-SESSION-ID", _userData.profile.sessionId.ToString() } }, new Dictionary<string, object>(), p_response =>
                {
                    if (p_response.IsSuccessStatusCode)
                    {
                        p_onSuccess?.Invoke();
                        LogoutHandler();
                    }
                    else
                    {
                        p_onFailure?.Invoke((int)p_response.StatusCode, p_response.ReasonPhrase);
                    }
                });
            }, p_deleteUserException =>
            {
                p_onFailure?.Invoke(p_deleteUserException.ErrorCode, p_deleteUserException.Message);
            });
        }
    }

    public void EnableEmails(bool p_enableEmails, Action<UserProfile> p_onSuccess, Action<int, string> p_onFailure)
    {
        if (_userData == null)
        {
            p_onFailure?.Invoke((int)AuthError.NoSignedInUser, "User not signed in!");
        }
        else
        {
            Post(API_URL + ENABLE_EMAILS_ENDPOINT, new Dictionary<string, string>() { { "X-SESSION-ID", _userData.profile.sessionId.ToString() } }, new Dictionary<string, object>() { { "EnableEmails", p_enableEmails } }, p_response =>
            {
                if (p_response.IsSuccessStatusCode)
                {
                    _userData.profile = JsonConvert.DeserializeObject<UserProfile>(p_response.ReadAsString());
                    p_onSuccess?.Invoke(_userData.profile);
                }
                else
                {
                    p_onFailure?.Invoke((int)p_response.StatusCode, p_response.ReasonPhrase);
                }
            });
        }
    }

    public void RetrieveGameTable(Action p_onSuccess, Action<int, string> p_onFailure)
    {
        if (_userData == null)
        {
            p_onFailure?.Invoke((int)AuthError.NoSignedInUser, "User not signed in!");
        }
        else
        {
            Get(API_URL + RETRIEVE_GAME_TABLE_ENDPOINT, new Dictionary<string, string>() { { "X-SESSION-ID", _userData.profile.sessionId.ToString() } }, p_response =>
            {
                if (p_response.IsSuccessStatusCode)
                {
                    List<HitData> __hitDataCollection = JsonConvert.DeserializeObject<List<HitData>>(p_response.ReadAsString());

                    TableHitData = new Dictionary<int, List<HitData>>();

                    foreach (var hitData in __hitDataCollection)
                    {
                        if (!TableHitData.ContainsKey(hitData.selected))
                        {
                            TableHitData.Add(hitData.selected, new List<HitData>());
                        }

                        TableHitData[hitData.selected].Add(hitData);
                    }

                    p_onSuccess?.Invoke();
                }
                else
                {
                    p_onFailure?.Invoke((int)p_response.StatusCode, p_response.ReasonPhrase);
                }
            });
        }
    }

    public void RetrievePlayerBalance(Action<BalanceData> p_onSuccess, Action<int, string> p_onFailure)
    {
        if (_userData == null)
        {
            p_onFailure?.Invoke((int)AuthError.NoSignedInUser, "User not signed in!");
        }
        else
        {
            Get(API_URL + RETRIEVE_PLAYER_BALANCE_ENDPOINT, new Dictionary<string, string>() { { "X-SESSION-ID", _userData.profile.sessionId.ToString() } }, p_response =>
            {
                if (p_response.IsSuccessStatusCode)
                {
                    BalanceData __previousBalanceData = _userData.balance == null ? new BalanceData()
                    {
                        balance = 0,
                        timeBonus = 0,
                        nextTimeBonusUTC = new DateTime()
                    } : new BalanceData()
                    {
                        balance = _userData.balance.balance,
                        timeBonus = _userData.balance.timeBonus,
                        nextTimeBonusUTC = _userData.balance.nextTimeBonusUTC
                    };

                    _userData.balance = JsonConvert.DeserializeObject<BalanceData>(p_response.ReadAsString());
                    p_onSuccess?.Invoke(_userData.balance);
                    EventsManager.Instance.TriggerEvent(GameManager.Events.ON_BANK_VALUE_CHANGED, new Tuple<int, int>(__previousBalanceData.balance, _userData.balance.balance));
                    EventsManager.Instance.TriggerEvent(GameManager.Events.ON_TIME_BONUS_VALUE_CHANGED, new Tuple<DateTime, DateTime>(__previousBalanceData.nextTimeBonusUTC, _userData.balance.nextTimeBonusUTC));
                }
                else
                {
                    p_onFailure?.Invoke((int)p_response.StatusCode, p_response.ReasonPhrase);
                }
            });
        }
    }

    public void PlayGame(int p_bet, int[] p_selectedCards, Action<GamePlayResponse> p_onSuccess, Action<int, string> p_onFailure)
    {
        if (_userData == null)
        {
            p_onFailure?.Invoke((int)AuthError.NoSignedInUser, "User not signed in!");
        }
        else
        {
            Post(API_URL + PLAY_A_GAME_ENDPOINT, new Dictionary<string, string>() { { "X-SESSION-ID", _userData.profile.sessionId.ToString() } }, new Dictionary<string, object>() { { "Bet", p_bet }, { "Selected", p_selectedCards } }, p_response =>
            {
                if (p_response.IsSuccessStatusCode)
                {
                    string __resultString = p_response.ReadAsString();

                    PlayerPrefs.SetString("GAME_RESPONSE", __resultString);
                    p_onSuccess?.Invoke(JsonConvert.DeserializeObject<GamePlayResponse>(__resultString));
                }
                else
                {
                    p_onFailure?.Invoke((int)p_response.StatusCode, p_response.ReasonPhrase);
                }
            });
        }
    }

    public void CompleteGame(int p_gameId, Action<BalanceData> p_onSuccess, Action<int, string> p_onFailure)
    {
        if (_userData == null)
        {
            p_onFailure?.Invoke((int)AuthError.NoSignedInUser, "User not signed in!");
        }
        else
        {
            Post(API_URL + COMPLETE_THE_GAME_ENDPOINT, new Dictionary<string, string>() { { "X-SESSION-ID", _userData.profile.sessionId.ToString() } }, new Dictionary<string, object>() { { "GameId", p_gameId } }, p_response =>
            {
                if (p_response.IsSuccessStatusCode)
                {
                    int __previousBalance = _userData.balance.balance;
                    Dictionary<string, object> __dataCollection = JsonConvert.DeserializeObject<Dictionary<string, object>>(p_response.ReadAsString());

                    _userData.balance.balance = Convert.ToInt32(__dataCollection["balance"]);
                    _userData.balance.timeBonus = Convert.ToInt32(__dataCollection["timeBonus"]);
                    PlayerPrefs.DeleteKey("GAME_RESPONSE");
                    p_onSuccess?.Invoke(_userData.balance);
                    EventsManager.Instance.TriggerEvent(GameManager.Events.ON_BANK_VALUE_CHANGED, new Tuple<int, int>(__previousBalance, _userData.balance.balance));
                }
                else
                {
                    p_onFailure?.Invoke((int)p_response.StatusCode, p_response.ReasonPhrase);
                }
            });
        }
    }

    public void CompletePendingGames(Action<BalanceData> p_onSuccess, Action<int, string> p_onFailure)
    {
        if (_userData == null)
        {
            p_onFailure?.Invoke((int)AuthError.NoSignedInUser, "User not signed in!");
        }
        else
        {
            GamePlayResponse __lastGamePlayResponse = GetPendingGame();

            if (__lastGamePlayResponse == null)
            {
                p_onSuccess?.Invoke(_userData.balance);
            }
            else
            {
                CompleteGame(__lastGamePlayResponse.gameId, p_onSuccess, p_onFailure);
            }
        }
    }

    public void ClaimTimeBonus(Action<BalanceData> p_onSuccess, Action<int, string> p_onFailure)
    {
        if (_userData == null)
        {
            p_onFailure?.Invoke((int)AuthError.NoSignedInUser, "User not signed in!");
        }
        else
        {
            Post(API_URL + CLAIM_TIME_BONUS_ENDPOINT, new Dictionary<string, string>() { { "X-SESSION-ID", _userData.profile.sessionId.ToString() } }, new Dictionary<string, object>() { { "TimeBonus", _userData.balance.timeBonus } }, p_response =>
            {
                if (p_response.IsSuccessStatusCode)
                {
                    int __previousBalance = _userData.balance.balance;
                    DateTime __previousNextTimeBonusUTC = _userData.balance.nextTimeBonusUTC;
                    Dictionary<string, object> __dataCollection = JsonConvert.DeserializeObject<Dictionary<string, object>>(p_response.ReadAsString());

                    _userData.balance.balance = Convert.ToInt32(__dataCollection["balance"]);
                    _userData.balance.nextTimeBonusUTC = (DateTime)__dataCollection["nextTimeBonusUTC"];
                    p_onSuccess?.Invoke(_userData.balance);
                    EventsManager.Instance.TriggerEvent(GameManager.Events.ON_BANK_VALUE_CHANGED, new Tuple<int, int>(__previousBalance, _userData.balance.balance));
                    EventsManager.Instance.TriggerEvent(GameManager.Events.ON_TIME_BONUS_VALUE_CHANGED, new Tuple<DateTime, DateTime>(__previousNextTimeBonusUTC, _userData.balance.nextTimeBonusUTC));
                }
                else
                {
                    p_onFailure?.Invoke((int)p_response.StatusCode, p_response.ReasonPhrase);
                }
            });
        }
    }

    public void RegisterInAppPurchase(string p_productId, string p_purchaseToken, Action<BalanceData> p_onSuccess, Action<int, string> p_onFailure)
    {
        if (_userData == null)
        {
            p_onFailure?.Invoke((int)AuthError.NoSignedInUser, "User not signed in!");
        }
        else
        {
            Post(API_URL + REGISTER_IN_APP_PURCHASE_ENDPOINT, new Dictionary<string, string>() { { "X-SESSION-ID", _userData.profile.sessionId.ToString() } }, new Dictionary<string, object>() { { "ProductId", p_productId }, { "PurchaseToken", p_purchaseToken } }, p_response =>
            {
                if (p_response.IsSuccessStatusCode)
                {
                    int __previousBalance = _userData.balance.balance;
                    DateTime __previousNextTimeBonusUTC = _userData.balance.nextTimeBonusUTC;
                    Dictionary<string, object> __dataCollection = JsonConvert.DeserializeObject<Dictionary<string, object>>(p_response.ReadAsString());

                    _userData.balance.balance = Convert.ToInt32(__dataCollection["balance"]);
                    _userData.balance.nextTimeBonusUTC = (DateTime)__dataCollection["nextTimeBonusUTC"];
                    p_onSuccess?.Invoke(_userData.balance);
                    EventsManager.Instance.TriggerEvent(GameManager.Events.ON_BANK_VALUE_CHANGED, new Tuple<int, int>(__previousBalance, _userData.balance.balance));
                    EventsManager.Instance.TriggerEvent(GameManager.Events.ON_TIME_BONUS_VALUE_CHANGED, new Tuple<DateTime, DateTime>(__previousNextTimeBonusUTC, _userData.balance.nextTimeBonusUTC));
                }
                else
                {
                    p_onFailure?.Invoke((int)p_response.StatusCode, p_response.ReasonPhrase);
                }
            });
        }
    }

    public void RetrieveAds(Action<AdsData> p_onSuccess, Action<int, string> p_onFailure)
    {
        if (_userData == null)
        {
            p_onFailure?.Invoke((int)AuthError.NoSignedInUser, "User not signed in!");
        }
        else
        {
            Get(API_URL + RETRIEVE_ADS_ENDPOINT, new Dictionary<string, string>() { { "X-SESSION-ID", _userData.profile.sessionId.ToString() } }, p_response =>
            {
                if (p_response.IsSuccessStatusCode)
                {
                    p_onSuccess?.Invoke(JsonConvert.DeserializeObject<AdsData>(p_response.ReadAsString()));
                }
                else
                {
                    p_onFailure?.Invoke((int)p_response.StatusCode, p_response.ReasonPhrase);
                }
            });
        }
    }

    public void SubtractCoinsToUserBalance(int p_amountOfCoinsToSubtract)
    {
        if (_userData != null)
        {
            int __previousBalance = _userData.balance.balance;

            _userData.balance.balance -= p_amountOfCoinsToSubtract;
            EventsManager.Instance.TriggerEvent(GameManager.Events.ON_BANK_VALUE_CHANGED, new Tuple<int, int>(__previousBalance, _userData.balance.balance));
        }
    }

    protected override void OnAwake()
    {
        base.OnAwake();

        // Initialization here.
        Instance.gameObject.name = "NetworkManager";
        _appleSignInConnector = GetComponent<AppleSignInConnector>();
    }

    private void Start()
    {
        FirebaseAuth.DefaultInstance.StateChanged += OnAuthStateChangedHandler;
        FirebaseAuth.DefaultInstance.IdTokenChanged += OnAuthIdTokenChangedHandler;
        EventsManager.Instance.AddListener(NetworkManager.Events.ON_BUY_REQUEST_RECEIVED, OnBuyRequestReceivedHandler);

        //TODO: TEMPORARY WORKAROUND - this is very unsafe, needs to be removed later.
        System.Net.ServicePointManager.ServerCertificateValidationCallback += (p_o, p_certificate, p_chain, p_errors) =>
        {
            return true;
        };
    }

    private GamePlayResponse GetPendingGame()
    {
        return PlayerPrefs.HasKey("GAME_RESPONSE") ? JsonConvert.DeserializeObject<GamePlayResponse>(PlayerPrefs.GetString("GAME_RESPONSE")) : null;
    }

    private void OnAuthStateChangedHandler(object p_sender, EventArgs p_e)
    {
        FirebaseAuth __senderAuth = (FirebaseAuth)p_sender;

        Debug.LogWarning("### NetworkManager.OnAuthStateChangedHandler ### CurrentUser: " + JsonConvert.SerializeObject(__senderAuth.CurrentUser));
    }

    private void OnAuthIdTokenChangedHandler(object p_sender, EventArgs p_e)
    {
        FirebaseAuth __senderAuth = (FirebaseAuth)p_sender;

        Debug.LogWarning("### NetworkManager.OnAuthIdTokenChangedHandler ### CurrentUser: " + JsonConvert.SerializeObject(__senderAuth.CurrentUser));
    }

    private void LogoutHandler()
    {
        _userData = null;
        _userToken = string.Empty;

        switch (AuthenticationType)
        {
#if UNITY_ANDROID
            case ENUM_AuthenticationType.GOOGLE:
                GoogleSignInConnector.SignOut();
                break;
#elif UNITY_IOS
            case ENUM_AuthenticationType.APPLE:
                // _appleSignInConnector.SignOut();
                break;
#endif
        }

        AuthenticationType = ENUM_AuthenticationType.NONE;
        PlayerPrefs.DeleteKey("AUTH_TYPE");
        FirebaseConnector.ForceSignOut();
    }

    private void OnBuyRequestReceivedHandler(object p_coinsToBuy)
    {
        //SEND REQUEST TO SERVER
        // GameManager.Instance.AddFunds((int)p_coinsToBuy);    //TODO: Do this only after server response success.
    }

    #region BASE REQUESTS
    private void Get(string p_url, Action<HttpResponseMessage> p_onComplete)
    {
        HttpClient __client = new HttpClient();

        __client.Headers.Add("Authorization", "Bearer " + CurrentUserToken);
        __client.Get(new Uri(p_url), HttpCompletionOption.AllResponseContent, p_response =>
        {
            p_onComplete?.Invoke(p_response);
        });
    }

    private void Get(string p_url, Dictionary<string, string> p_headers, Action<HttpResponseMessage> p_onComplete)
    {
        HttpClient __client = new HttpClient();

        __client.Headers.Add("Authorization", "Bearer " + CurrentUserToken);

        foreach (var header in p_headers)
        {
            __client.Headers.Add(header.Key, header.Value);
        }

        __client.Get(new Uri(p_url), HttpCompletionOption.AllResponseContent, p_response =>
        {
            p_onComplete?.Invoke(p_response);
        });
    }

    private void Post(string p_url, string p_content, Action<HttpResponseMessage> p_onComplete)
    {
        HttpClient __client = new HttpClient();

        __client.Headers.Add("Authorization", "Bearer " + CurrentUserToken);
        __client.Post(new Uri(p_url), new StringContent(p_content, System.Text.Encoding.UTF8, "application/json"), HttpCompletionOption.AllResponseContent, p_response =>
        {
            p_onComplete?.Invoke(p_response);
        });
    }

    private void Post(string p_url, Dictionary<string, object> p_content, Action<HttpResponseMessage> p_onComplete)
    {
        HttpClient __client = new HttpClient();

        __client.Headers.Add("Authorization", "Bearer " + CurrentUserToken);
        __client.Post(new Uri(p_url), new StringContent(JsonConvert.SerializeObject(p_content), System.Text.Encoding.UTF8, "application/json"), HttpCompletionOption.AllResponseContent, p_response =>
        {
            p_onComplete?.Invoke(p_response);
        });
    }

    private void Post(string p_url, Dictionary<string, string> p_headers, string p_content, Action<HttpResponseMessage> p_onComplete)
    {
        HttpClient __client = new HttpClient();

        __client.Headers.Add("Authorization", "Bearer " + CurrentUserToken);

        foreach (var header in p_headers)
        {
            __client.Headers.Add(header.Key, header.Value);
        }

        __client.Post(new Uri(p_url), new StringContent(p_content, System.Text.Encoding.UTF8, "application/json"), HttpCompletionOption.AllResponseContent, p_response =>
        {
            p_onComplete?.Invoke(p_response);
        });
    }

    private void Post(string p_url, Dictionary<string, string> p_headers, Dictionary<string, object> p_content, Action<HttpResponseMessage> p_onComplete)
    {
        HttpClient __client = new HttpClient();

        __client.Headers.Add("Authorization", "Bearer " + CurrentUserToken);

        foreach (var header in p_headers)
        {
            __client.Headers.Add(header.Key, header.Value);
        }

        __client.Post(new Uri(p_url), new StringContent(JsonConvert.SerializeObject(p_content), System.Text.Encoding.UTF8, "application/json"), HttpCompletionOption.AllResponseContent, p_response =>
        {
            p_onComplete?.Invoke(p_response);
        });
    }

    private void Delete(string p_url, Dictionary<string, string> p_headers, Dictionary<string, object> p_content, Action<HttpResponseMessage> p_onComplete)
    {
        HttpClient __client = new HttpClient();

        __client.Headers.Add("Authorization", "Bearer " + CurrentUserToken);

        foreach (var header in p_headers)
        {
            __client.Headers.Add(header.Key, header.Value);
        }

        __client.Delete(new Uri(p_url), new StringContent(JsonConvert.SerializeObject(p_content), System.Text.Encoding.UTF8, "application/json"), HttpCompletionOption.AllResponseContent, p_response =>
        {
            p_onComplete?.Invoke(p_response);
        });
    }
    #endregion
}
