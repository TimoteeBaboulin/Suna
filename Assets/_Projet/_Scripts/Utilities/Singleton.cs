using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T instance;

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"An other same instance of {typeof(T).Name} exist! It was deleted");
            Destroy(gameObject);
            return;
        }

        instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<T>();

                if (instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(T).Name);
                    instance = singletonObject.AddComponent<T>();
                }
            }

            return instance;
        }
    }
}

public class MasterSingleton<T> : MonoBehaviour where T : Component
{
    private static T instance;

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"An other same instance of {typeof(T).Name} exist! It was deleted");
            Destroy(gameObject);
            return;
        }

        instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<T>();

                if (instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(T).Name);
                    instance = singletonObject.AddComponent<T>();
                }
            }

            return instance;
        }
    }

    public static C Get<C>() where C : Component
    {
        T instance = Instance;

        if (!Instance.TryGetComponent(out C component))
        {
            component = instance.gameObject.AddComponent<C>();

            Debug.LogWarning($"{typeof(C).Name} is not Component of {instance.name}, {typeof(C).Name} is default Add on {instance.name}");
        }

        return component;
    }
}

//#region Fields
//private static T instance;
//#endregion

//#region Properties
///// <summary>
///// Get Instance of current Object
///// Same as instance = instance ?? InitInstance(), Look notes about C# ? conditions :)
///// </summary>
///// 
//public static T Instance => instance ??= InitInstance();
//#endregion


//#region Messages
//protected virtual void Awake()
//{
//    if (instance == null)
//    {
//        instance = this as T;
//        DontDestroyOnLoad(gameObject);
//    }
//    else if (instance != this)
//    {
//        Destroy(gameObject);
//    }
//}
//#endregion

//#region PrivateMethods
//private static T InitInstance()
//{
//    instance = FindAnyObjectByType<T>();
//    if (instance == null)
//    {
//        Debug.Log("OUI");//
//        GameObject singletonObj = new GameObject(typeof(T).Name);
//        instance = singletonObj.AddComponent<T>();
//        DontDestroyOnLoad(singletonObj);
//    }
//    return instance;
//}
//#endregion
