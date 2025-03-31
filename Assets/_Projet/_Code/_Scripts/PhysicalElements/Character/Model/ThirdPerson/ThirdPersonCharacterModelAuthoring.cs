using Unity.Entities;
using UnityEngine;

class ThirdPersonCharacterModelAuthoring : MonoBehaviour
{
    public GameObject CorpoModelPrefab;
    public Transform DeltaPosition;

    [Header("Model Bones")]
    public string ViewBoneName;
    public string HeadBoneName;
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
}

class ThirdPersonCharacterModelAuthoringBaker : Baker<ThirdPersonCharacterModelAuthoring>
{
    public override void Bake(ThirdPersonCharacterModelAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        AddComponentObject(entity, new ThirdPersonCharacterModelPrefab
        {
            CorpoModelPrefab = authoring.CorpoModelPrefab,
            DeltaPosition = authoring.DeltaPosition.position,
        });

        AddComponent(entity, new ThirdPersonCharacterModelBonesName
        {
            ViewBoneName = authoring.ViewBoneName,
            HeadBoneName = authoring.HeadBoneName,
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
    }
}
