using Unity.Entities;
using UnityEngine;

public class PlayerTemp : MonoBehaviour
{
    public float speed = 5f;
}

public struct PlayerData : IComponentData
{
    public float speed;
}

public class PlayerBaker : Baker<PlayerTemp>
{
    public override void Bake(PlayerTemp authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new PlayerData
        {
            speed = authoring.speed
        });
        AddComponent<PlayerInputData>(entity);
    }
}
