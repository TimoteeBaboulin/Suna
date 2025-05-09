using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Entities;
using UnityEngine.InputSystem;
using System.Linq;

public class SoundManager : Singleton<SoundManager>
{
#if !UNITY_SERVER
    public Dictionary<string, AK.Wwise.Event> bank = new();
    public AK.Wwise.RTPC volumeRTPC = null;

    public List<string> keys = new List<string>();
#endif

    GameObject go;



    protected override void Awake()
    {
        base.Awake();
        go = Instantiate(new GameObject($"TempSoundEmitter"));
        DontDestroyOnLoad(go);
    }

#if !UNITY_SERVER
    private void Update()
    {
        if(bank != null)
        {
            keys.Clear();
            keys = bank.Keys.ToList();
        }
        

    }
#endif

    public void Play(string keyGroup, string keyAction, Vector3 pos)
    {
        string key = keyGroup + keyAction;
#if !UNITY_SERVER
        if (bank.TryGetValue(key, out AK.Wwise.Event sound))
        {
            go.transform.position = pos;
            sound.Post(go);
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
        volumeRTPC.SetValue(go, volume);
#endif
    }
}
