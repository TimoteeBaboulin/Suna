using Unity.Entities;
using UnityEngine;

class ThirdPersonCharacterModelAuthoring : MonoBehaviour
{
    public GameObject CorpoModelPrefab;
    public GameObject NatifModelPrefab;
    public Transform DeltaPosition;

    public CharacterColliderBones CorpoColliderBones;
    public CharacterColliderBones NatifColliderBones;
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
            DeltaPosition = authoring.DeltaPosition.position,
        });
    }
}
