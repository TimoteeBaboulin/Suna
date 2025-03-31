using Unity.Entities;
using UnityEngine;

class FirstPersonCharacterModelAuthoring : MonoBehaviour
{
    public GameObject CorpoModelPrefab;
    public Transform DeltaPosition;
}

class FirstPersonCharacterModelAuthoringBaker : Baker<FirstPersonCharacterModelAuthoring>
{
    public override void Bake(FirstPersonCharacterModelAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponentObject(entity, new FirstPersonCharacterModelPrefab
        {
            CorpoModelPrefab = authoring.CorpoModelPrefab,
            DeltaPosition = authoring.DeltaPosition.position,
        });
    }
}
