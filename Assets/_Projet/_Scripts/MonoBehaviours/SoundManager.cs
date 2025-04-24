#if !UNITY_SERVER
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Entities;
using UnityEngine.InputSystem;

public class SoundManager : Singleton<SoundManager>
{

    public Dictionary<string, AK.Wwise.Event> bank = new();

    GameObject go;

    protected override void Awake()
    {
        base.Awake();
        go = Instantiate(new GameObject($"TempSoundEmitter"));
        DontDestroyOnLoad(go);
    }

    public void Play(string keyGroup, string keyAction, Vector3 pos)
    {

        string key = keyGroup + keyAction;

        if (bank.TryGetValue(key, out AK.Wwise.Event sound))
        {
            go.transform.position = pos;
            sound.Post(go);
        }
        else
        {
            Debug.LogWarning($"Sound {key} not found in bank Dictionary");
        }
    }

    public static bool TryGetBank(out SoundManager sm)
    {
        sm = null;
        sm = Instance.bank;
        return true;
        return false;
    }
}
#endif
