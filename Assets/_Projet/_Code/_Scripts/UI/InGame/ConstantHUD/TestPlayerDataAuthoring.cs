using Unity.Entities;
using UnityEngine;

public class TestPlayerDataAuthoring : MonoBehaviour
{
    [SerializeField] uint health = 100u;
    [SerializeField] uint armor = 0u;
    [SerializeField] uint ammoLeft = 30u;
    [SerializeField] uint ammoCapacity = 30u;
    [SerializeField] uint cash = 600u;

    public class Baker : Baker<TestPlayerDataAuthoring>
    {
        public override void Bake(TestPlayerDataAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new TestPlayerData
            {
                Health = authoring.health,
                Armor = authoring.armor,
                AmmoLeft = authoring.ammoLeft,
                AmmoCapacity = authoring.ammoCapacity,
                Cash = authoring.cash
            });
        }
    }
}

public struct TestPlayerData : IComponentData
{
    public uint Health;
    public uint Armor;
    public uint AmmoLeft;
    public uint AmmoCapacity;
    public uint Cash;
}