using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class Bomb : MonoBehaviour
{

}

public struct BombData : IComponentData
{
    [GhostField] public Entity owner;
}

public class BombBaker : Baker<Bomb>
{
    public override void Bake(Bomb authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new BombData
        {
            owner = Entity.Null
        });
    }
}
