using UnityEngine;
using Unity.Entities;
using Unity.Collections;

public struct StuffInfosComponent : IComponentData
{
    public UsableEquipmentType type;
    public TeamSideType side;
    public FixedString64Bytes entityName;
    public float deploymentSpeed;
    public float storageSpeed;
    public int price;
}
