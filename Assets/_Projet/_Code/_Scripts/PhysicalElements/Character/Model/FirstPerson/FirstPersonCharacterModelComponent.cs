using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FirstPersonCharacterModelPrefab : IComponentData
{
    public GameObject CorpoModelPrefab;
    public float3 DeltaPosition;
}

public class FirstPersonCharacterModelReference : ICleanupComponentData
{
    public GameObject ModelGameObject;
    public float3 DeltaPosition;
}
