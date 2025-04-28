using System;

using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[Serializable]
public class SoundMapping
{
    public string keyAction;
#if !UNITY_SERVER
    public AK.Wwise.Event sound;
#endif
}

[Serializable]
public class SoundGroupMapping
{
    public string keyGroup;
    public List<SoundMapping> maping = new();
}
public static class SoundUtils
{
    public static void SetMappingList(string keyGroup, in List<SoundMapping> soundList, List<SoundGroupMapping> soundGroupMapping)
    {
        List<SoundMapping> soundMaping = new List<SoundMapping>();
        foreach (var sound in soundList)
        {
            soundMaping.Add(new SoundMapping
            {
                keyAction = sound.keyAction,
#if !UNITY_SERVER
                sound = sound.sound
#endif
            });
        }

        soundGroupMapping.Add(new SoundGroupMapping
        {
            keyGroup = keyGroup,
            maping = soundMaping
        });
    }
    public static SoundRegister SetRegister(string keyGroup, List<SoundMapping> soundList)
    {

        SoundRegister soundRegister = new SoundRegister();
#if !UNITY_SERVER
        Dictionary<string, AK.Wwise.Event> bank = soundRegister.bank;
        foreach (var pair in soundList)
        {
            if (!bank.ContainsKey(keyGroup + pair.keyAction))
                bank.Add(keyGroup + pair.keyAction, pair.sound);
        }
#endif
        return soundRegister;
    }

    public static SoundRegister SetGroupRegister(List<SoundGroupMapping> soundGroupList)
    {
        SoundRegister soundRegister = new SoundRegister();

#if !UNITY_SERVER

        Dictionary<string, AK.Wwise.Event> bank = soundRegister.bank;
        foreach (var soundList in soundGroupList)
        {
            foreach (var pair in soundList.maping)
            {
                if (!bank.ContainsKey(soundList.keyGroup + pair.keyAction))
                    bank.Add(soundList.keyGroup + pair.keyAction, pair.sound);
            }
        }
#endif
        return soundRegister;
    }

    //The entity holding the soundBuffer must have SoundAuthoring attached
    public static void PlayWithSoundQueue(DynamicBuffer<SoundQueue> soundQueue, in SoundEmitter emitter, FixedString32Bytes keyAction, float3 pos)
    {
        soundQueue.Add(new SoundQueue()
        {
            keyGroup = emitter.keyGroup,
            keyAction = keyAction,
            pos = pos
        });

    }

    //The entity holding the soundBuffer must have SoundAuthoring attached
    public static void PlayAtEmitterWithSoundQueue(ref SystemState state, DynamicBuffer<SoundQueue> soundQueue, Entity entity, FixedString32Bytes keyAction)
    {
        if (state.EntityManager.HasComponent<SoundEmitter>(entity))
        {

            SoundEmitter emitter = state.EntityManager.GetComponentData<SoundEmitter>(entity);

            if (state.EntityManager.HasComponent<LocalToWorld>(entity))
            {
                LocalToWorld transform = state.EntityManager.GetComponentData<LocalToWorld>(entity);
                PlayWithSoundQueue(soundQueue, emitter, keyAction, transform.Position);

            }
        }
    }

    public static void PlayWithRPC(FixedString32Bytes keyGroup, FixedString32Bytes keyAction, float3 pos)
    {
        SoundRpc soundRpc = new SoundRpc()
        {
            keyGroup = keyGroup,
            keyAction = keyAction,
            pos = pos
        };
        RpcUtils.SendServerToClientRpc(ref soundRpc);
    }

    public static void PlayWithRPC(ref SoundEmitter emitterRW, FixedString32Bytes keyAction, float3 pos, float cooldown = 0f, float dt = 0f)
    {
        emitterRW.timer -= dt;
        if (emitterRW.timer <= 0f)
        {
            emitterRW.timer = cooldown;
            PlayWithRPC(emitterRW.keyGroup, keyAction, pos);
        }
    }

    public static void PlayAtEmitterWithRPC(ref SystemState state, FixedString32Bytes keyAction, Entity entity)
    {
        if (state.EntityManager.HasComponent<SoundEmitter>(entity))
        {
            SoundEmitter emitter = state.EntityManager.GetComponentData<SoundEmitter>(entity);

            if (state.EntityManager.HasComponent<LocalToWorld>(entity))
            {
                LocalToWorld transform = state.EntityManager.GetComponentData<LocalToWorld>(entity);
                PlayWithRPC(emitter.keyGroup, keyAction, transform.Position);
            }
        }

    }
}
