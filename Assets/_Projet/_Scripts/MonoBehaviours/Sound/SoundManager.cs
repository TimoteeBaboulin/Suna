using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
#if !UNITY_SERVER
    public Dictionary<string, AK.Wwise.Event> bank = new();
    public AK.Wwise.RTPC volumeRTPC = null;
#endif

    //GameObject go;
    int goId = 0;
    //protected override void Awake()
    //{
    //base.Awake();
    //go = Instantiate(new GameObject($"TempSoundEmitter"));
    //DontDestroyOnLoad(go);
    //}

    public void Play(string keyGroup, string keyAction, Vector3 pos)
    {
#if !UNITY_SERVER
        string key = keyGroup + keyAction;

        if (bank.TryGetValue(key, out AK.Wwise.Event sound))
        {
            Transform[] childrens = GetComponentsInChildren<Transform>();

            goId++;
            if (goId >= childrens.Length)
            {
                goId = 1;
            }

            childrens[goId].transform.position = pos;
            sound.Post(childrens[goId].gameObject);
        }
        else
        {
            Debug.LogWarning($"Sound {key} not found in bank Dictionary");
        }
#endif
    }

    public void SetVolume(float volume)
    {
#if !UNITY_SERVER
        foreach (var transform in GetComponentsInChildren<Transform>())
        {
            volumeRTPC.SetValue(transform.gameObject, volume);
        }
#endif
    }
}
