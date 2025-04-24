using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

class SoundAuthoring : MonoBehaviour
{
#if !UNITY_SERVER

    public SoundGroupMapping soundMaping;
#endif
}
#if !UNITY_SERVER

class SoundAuthoringBaker : Baker<SoundAuthoring>
{
    public override void Bake(SoundAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponentObject(entity, SoundUtils.SetRegister(authoring.soundMaping.keyGroup, authoring.soundMaping.maping));

        AddComponent(entity, new SoundEmitter
        {
            keyGroup = authoring.soundMaping.keyGroup
        });
    }
}
#endif
