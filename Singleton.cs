using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    public static T Instance { get; private set; }

    [SerializeField]
    private bool _isPersistent = true;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this.GetComponent<T>();
            OnAwake();
            
            if (_isPersistent)
            {
                DontDestroyOnLoad(this.gameObject);
            }
        }
    }

    protected virtual void OnAwake()
    {

    }
}
