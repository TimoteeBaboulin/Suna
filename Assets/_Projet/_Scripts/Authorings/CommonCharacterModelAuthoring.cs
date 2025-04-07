using Unity.Entities;
using UnityEngine;

class CommonCharacterModelAuthoring : MonoBehaviour
{
    [Header("Common Model Bones")]
    public string WeaponSlotName;
}

class CommonCharacterModelAuthoringBaker : Baker<CommonCharacterModelAuthoring>
{
    public override void Bake(CommonCharacterModelAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new CommonCharacterModelBonesName
        {
            WeaponSlotName = authoring.WeaponSlotName,
        });
    }
}
