using Unity.Entities;
using UnityEngine;

public class CharacterAnimationAuthoring : MonoBehaviour
{
    public GameObject CharacterGameObject;
    public Transform DeltaPosition;
    public string HeadBoneName;
    public string ViewBoneName;
    public Vector3 StuffOffset; //temp

    public class Baker : Baker<CharacterAnimationAuthoring>
    {
        public override void Bake(CharacterAnimationAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, new CharacterGameObjectPrefab 
            {
                GameObjectPrefab = authoring.CharacterGameObject,
                DeltaPosition = authoring.DeltaPosition.position,
                HeadBoneName = authoring.HeadBoneName,
                ViewBoneName = authoring.ViewBoneName,
            });

            AddComponent(entity, new CharacterAnimationState
            {
                IsWalking = false,
            });
        }
    }
}
