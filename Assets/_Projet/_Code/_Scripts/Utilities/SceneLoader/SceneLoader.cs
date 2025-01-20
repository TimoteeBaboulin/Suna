using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneID
{
    MainMenu,
    World,
    Fight
}

public class SceneLoader : Singleton<SceneLoader>
{

    [SerializeField] GameObject sceneDebugMessagePrefab;
    [SerializeField] bool forceStartScene = false;
    [SerializeField] SceneID sceneOnStart = SceneID.MainMenu;

    TextMeshProUGUI debugMesage;
    public SceneID CurrentScene { get; private set; }

    private void Start()
    {
        if (forceStartScene) ChangeScene(sceneOnStart);
        UpdateDebugMessage();
    }

    public void ChangeScene(SceneID scene)
    {
        SceneManager.LoadScene((int)scene);
        UpdateDebugMessage();
    }

    public void ChangeSceneAsync(SceneID scene)
    {
        StartCoroutine(ChangeSceneAsyncCoroutine(scene));
    }

    private IEnumerator ChangeSceneAsyncCoroutine(SceneID scene)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync((int)scene);

        while (!asyncLoad.isDone)
        {
            Debug.Log($"Load : {asyncLoad.progress * 100}%");
            debugMesage.text = $"Load {scene} : {asyncLoad.progress * 100}%";

            yield return null;
        }

        UpdateDebugMessage();
    }

    public void LoadSceneAdditively(SceneID scene)
    {
        SceneManager.LoadScene((int)scene, LoadSceneMode.Additive);
        UpdateDebugMessage();
    }

    public void LoadSceneAdditivelyAsync(SceneID scene)
    {
        StartCoroutine(LoadSceneAdditivelyAsyncCoroutine(scene));
    }

    private IEnumerator LoadSceneAdditivelyAsyncCoroutine(SceneID scene)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync((int)scene, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
            Debug.Log($"Load : {asyncLoad.progress * 100}%");
            debugMesage.text = $"Load {scene} : {asyncLoad.progress * 100}%";
            yield return null;
        }

        UpdateDebugMessage();
    }

    public void UnloadScene(SceneID scene)
    {
        SceneManager.UnloadSceneAsync((int)scene);
        UpdateDebugMessage();
    }

    void UpdateDebugMessage()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) Debug.LogWarning("No Canvas is present in the scene to implement SceneLoaderDebugMessage");
        else if(debugMesage == null)
        {
            debugMesage = Instantiate(sceneDebugMessagePrefab, canvas.transform).GetComponent<TextMeshProUGUI>();
            debugMesage.text = $"Scene : {SceneManager.GetActiveScene().name}";
        }
        else
        {
            debugMesage.text = $"Scene : {SceneManager.GetActiveScene().name}";
        }
    }
    //static SceneLoader instance;


    //private void Awake()
    //{
    //    if (instance != null && instance != this)
    //    {
    //        Debug.LogWarning("An other same instance of SceneLoader exist! It was deleted");
    //        Destroy(gameObject);
    //        return;
    //    }

    //    instance = this;
    //    DontDestroyOnLoad(gameObject);
    //}

    //public static SceneLoader Instance
    //{
    //    get
    //    {
    //        if (instance == null)
    //        {
    //            instance = FindFirstObjectByType<SceneLoader>();

    //            if (instance == null)
    //            {
    //                GameObject singletonObject = new GameObject("SceneLoader");
    //                instance = singletonObject.AddComponent<SceneLoader>();
    //            }
    //        }

    //        return instance;
    //    }
    //}
}
