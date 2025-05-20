using GameNetwork.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class VideoPlayerSceneHandler : MonoBehaviour
{
    public string sceneToLoad;  // Name of the scene to load after video ends
    private VideoPlayer videoPlayer;

    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer component not found!");
            return;
        }

        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        Debug.LogError("in");
        LoadManager.Instance.LoadLevel(sceneToLoad);
    }
}
