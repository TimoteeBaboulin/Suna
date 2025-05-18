using Unity.Entities;
using UnityEngine;

class SoundAuthoring : MonoBehaviour
{
    public SoundGroupMapping soundMaping;
}

class SoundAuthoringBaker : Baker<SoundAuthoring>
{
    public override void Bake(SoundAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponentObject(entity, new SoundRegister
        {
            bank = SoundUtils.SetBankRegister(authoring.soundMaping.keyGroup, authoring.soundMaping.maping)
        });

        AddComponent(entity, new SoundEmitter
        {
            keyGroup = authoring.soundMaping.keyGroup
        });
        
    }
}