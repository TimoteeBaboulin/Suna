using Unity.Entities;
using UnityEngine;

class ThirdPersonCharacterModelAuthoring : MonoBehaviour
{
    public GameObject CorpoModelPrefab;
    public GameObject NatifModelPrefab;
    public Transform DeltaPosition;

    public CharacterColliderBones CorpoColliderBones;
    public CharacterColliderBones NatifColliderBones;

    public ModelAnimatorData CorpoAnimatorData;
    public ModelAnimatorData NatifAnimatorData;
}

class ThirdPersonCharacterModelAuthoringBaker : Baker<ThirdPersonCharacterModelAuthoring>
{
    public override void Bake(ThirdPersonCharacterModelAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponentObject(entity, new ThirdPersonCharacterModelPrefab
        {
            CorpoModelPrefab = authoring.CorpoModelPrefab,
            NatifModelPrefab = authoring.NatifModelPrefab,
            CorpoColliderBones = authoring.CorpoColliderBones,
            NatifColliderBones = authoring.NatifColliderBones,
            CorpoAnimatorData = authoring.CorpoAnimatorData,
            NatifAnimatorData = authoring.NatifAnimatorData,
            DeltaPosition = authoring.DeltaPosition.position,
        });
    }
}
