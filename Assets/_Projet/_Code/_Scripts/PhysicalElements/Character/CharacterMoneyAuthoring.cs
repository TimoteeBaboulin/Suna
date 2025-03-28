using UnityEngine;
using Unity.Entities;

public sealed class CharacterMoneyAuthoring : MonoBehaviour
{
    public uint money = 0;
    public uint maxMoney = 9000;

    public class Baker : Baker<CharacterMoneyAuthoring>
    {
        public override void Bake(CharacterMoneyAuthoring cm)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new CharacterMoney
            {
                money = cm.money,
                maxMoney = cm.maxMoney
            });
        }
    }
}
