using Unity.Entities;
using UnityEngine;

public class CharacterAnimationAuthoring : MonoBehaviour
{
    public GameObject CharacterGameObject;
    public Transform DeltaPosition;
    public string HeadBoneName;
    public string ViewBoneName;
    public string ArmLeftBoneName0;
    public string ArmLeftBoneName1;
    public string ArmLeftBoneName2;
    public string ArmRightBoneName0;
    public string ArmRightBoneName1;
    public string ArmRightBoneName2;
    public string ThoraxBoneName;
    public string StomachBoneName0;
    public string StomachBoneName1;
    public string LegLeftBoneName0;
    public string LegLeftBoneName1;
    public string LegLeftBoneName2;
    public string LegRightBoneName0;
    public string LegRightBoneName1;
    public string LegRightBoneName2;

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
                ArmLeftBoneName0 = authoring.ArmLeftBoneName0,
                ArmLeftBoneName1 = authoring.ArmLeftBoneName1,
                ArmLeftBoneName2 = authoring.ArmLeftBoneName2,
                ArmRightBoneName0 = authoring.ArmRightBoneName0,
                ArmRightBoneName1 = authoring.ArmRightBoneName1,
                ArmRightBoneName2 = authoring.ArmRightBoneName2,
                ThoraxBoneName = authoring.ThoraxBoneName,
                StomachBoneName0 = authoring.StomachBoneName0,
                StomachBoneName1 = authoring.StomachBoneName1,
                LegLeftBoneName0 = authoring.LegLeftBoneName0,
                LegLeftBoneName1 = authoring.LegLeftBoneName1,
                LegLeftBoneName2 = authoring.LegLeftBoneName2,
                LegRightBoneName0 = authoring.LegRightBoneName0,
                LegRightBoneName1 = authoring.LegRightBoneName1,
                LegRightBoneName2 = authoring.LegRightBoneName2,
            });
            AddComponent(entity, new CharacterAnimationState
            {
                IsWalking = false,
            });
        }
    }
}
