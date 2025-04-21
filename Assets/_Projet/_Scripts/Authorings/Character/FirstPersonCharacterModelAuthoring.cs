using Unity.Entities;
using UnityEngine;

class FirstPersonCharacterModelAuthoring : MonoBehaviour
{
    public GameObject CorpoModelPrefab;
    public GameObject NatifModelPrefab;
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
            NatifModelPrefab = authoring.NatifModelPrefab,
            DeltaPosition = authoring.DeltaPosition.position,
        });
    }
}
