
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

public struct SoundRpc : IRpcCommand
{
    public FixedString32Bytes keyGroup;
    public FixedString32Bytes keyAction;
    public float3 pos;
}

[GhostComponent]
public struct SoundQueue : IBufferElementData
{
    [GhostField] public FixedString32Bytes keyGroup;
    [GhostField] public FixedString32Bytes keyAction;
    [GhostField] public float3 pos;
}

public class SoundRegister : IComponentData
{
#if !UNITY_SERVER
    public Dictionary<string, AK.Wwise.Event> bank = new();
#endif

}

[GhostComponent]
public struct SoundEmitter : IComponentData
{
    [GhostField] public FixedString32Bytes keyGroup;
}

