using Unity.Entities;
using UnityEngine;

class FirstPersonCharacterModelAuthoring : MonoBehaviour
{
    public GameObject CorpoModelPrefab;
    public GameObject NatifModelPrefab;
    public Transform DeltaPosition;

    public ModelAnimatorData CorpoAnimatorData;
    public ModelAnimatorData NatifAnimatorData;
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
            CorpoAnimatorData = authoring.CorpoAnimatorData,
            NatifAnimatorData = authoring.NatifAnimatorData,
            DeltaPosition = authoring.DeltaPosition.position,
        });
    }
}
