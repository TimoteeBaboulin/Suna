using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    #region Fields
    private static T instance;
    #endregion

    #region Properties
    /// <summary>
    /// Get Instance of current Object
    /// Same as instance = instance ?? InitInstance(), Look notes about C# ? conditions :)
    /// </summary>
    /// 
    public static T Instance => instance ??= InitInstance();
    #endregion


    #region Messages
    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region PrivateMethods
    private static T InitInstance()
    {
        instance = FindAnyObjectByType<T>();
        if (instance == null)
        {
            GameObject singletonObj = new GameObject(typeof(T).Name);
            instance = singletonObj.AddComponent<T>();
            DontDestroyOnLoad(singletonObj);
        }
        return instance;
    }
    #endregion
}
