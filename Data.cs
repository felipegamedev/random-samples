using System;
using UnityEngine;

public class UserData
{
    public UserProfile profile;
    public BalanceData balance;
}

public class UserProfile
{
    public string FirstName { get { return name.Split(' ')[0]; } }
    public string LastName { get { return name.Contains(" ") ? name.Split(' ')[1] : string.Empty; } }   // When the user logs in with Google Sign-in or Apple Sign-in, the server uses his email in the "Name" field, so there's no last name for him.
    public int sessionId;
    public int userId;
    public string name;
    public string email;
    public bool enableEmails;
}

public class GamePlayResponse
{
    public int gameId;
    public int bet;
    public int[] selectedCards;
    public int[] drawnCards;
    public int[] matchedCards;
    public int win;
}

public class BalanceData
{
    public int balance;
    public int timeBonus;
    public System.DateTime nextTimeBonusUTC;
}

public class HitData
{
    public int selected;
    public int matched;
    public int rate;
}

public class AdsData
{
    public string id;
    public string fileName;
    public byte[] source;
    public string url;
    public string contentType;

    public Texture2D ImageTexture
    {
        get
        {
            if (_imageTexture == null)
            {
                _imageTexture = new Texture2D(1, 1);
                _imageTexture.LoadImage(source);
            }

            return _imageTexture;
        }
    }

    private Texture2D _imageTexture;
}

public class LocalNotificationData
{
    public string title;
    public string text;
    public DateTime triggerTime;
    public string smallIcon;
    public string largeIcon;
    public string customData;
    public TimeSpan? repeatInterval;
    public int notificationId;
}
