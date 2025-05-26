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
        Cursor.visible = false;
        videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer component not found!");
            return;
        }
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        LoadManager.Instance.LoadLevel(sceneToLoad);
    }
}
