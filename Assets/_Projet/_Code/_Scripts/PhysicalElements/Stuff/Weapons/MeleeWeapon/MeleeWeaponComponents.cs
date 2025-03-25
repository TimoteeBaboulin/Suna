using Unity.Entities;
using Unity.NetCode;

namespace MeleeWeapon
{
    [GhostComponent]
    public struct DynamicData : IComponentData
    {
        [GhostField] public float strikeTimer;
        [GhostField] public float strongStrikeTimer;
    }

    [GhostComponent]
    public struct CommonData : ISharedComponentData
    {
        [GhostField] public float damage;
        [GhostField] public float strongBlowDmg;
        [GhostField] public float backStabDmg;
        [GhostField] public float range;
        [GhostField] public float strikeRate;
        [GhostField] public float strongStrikeRate;
    }
}
