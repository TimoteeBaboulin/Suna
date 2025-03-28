using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace RangedWeapon
{
    public enum _State
    {
        Idle,
        Shoot,
        Reload,
        Droped
    }

    [GhostComponent]
    public struct DynamicData : IComponentData
    {
        [GhostField] public _State state;

        [GhostField] public float reloadTimer;
        [GhostField] public float fastReloadTimer;
        [GhostField] public float firerateTimer;

        [GhostField] public int currentAmmo;
        [GhostField] public int remainingAmmo;

        [GhostField] public int patternBulletIndex;
        [GhostField] public float timeSinceLastFire;
        [GhostField] public float lastFireTimeMax;
    }

    [GhostComponent]
    public struct CommonData : ISharedComponentData
    {
        [GhostField] public float2 recoil;

        [GhostField] public float range;
        [GhostField] public float firerate;
        [GhostField] public float spread;
        [GhostField] public float spreadAiming;
        [GhostField] public float coefSpray;
        [GhostField] public float coefSprayAiming;
        [GhostField] public float ergonomics;
        [GhostField] public float roundsPerMin;
        [GhostField] public float dmgFallOff;
        [GhostField] public float coefModifMoveSpeed;
        [GhostField] public float coefModifMoveSpeedAiming;
        [GhostField] public float reloadSpeed;
        [GhostField] public float fastReloadSpeed;
        [GhostField] public float knockbackForceOnKill;

        [GhostField] public int damage;
        [GhostField] public int nbMagazine;
        [GhostField] public int magazineCapacity;
    }
}





//public ScopeData scope;
//public HandleData handle;
//public CrossData cross;
//public SilencerData silencer;
//public MagazineData magazine;

//public float thorax;
//public float stomach;
//public float legs_Arms;
//public float head;

//public GameObject ammoType;

//public struct ModifiersComponent : IComponentData
//{
//    public Entity scope;
//    public Entity handle;
//    public Entity cross;
//    public Entity silencer;
//    public Entity magazine;
//}

//AddComponent(entity, new ModifiersComponent
//{
//    scope = d.scope != null ? GetEntity(d.scope.prefab, TransformUsageFlags.Dynamic) : default,
//    handle = d.handle != null ? GetEntity(d.handle.prefab, TransformUsageFlags.Dynamic) : default,
//    cross = d.cross != null ? GetEntity(d.cross.prefab, TransformUsageFlags.Dynamic) : default,
//    silencer = d.silencer != null ? GetEntity(d.silencer.prefab, TransformUsageFlags.Dynamic) : default,
//    magazine = d.magazine != null ? GetEntity(d.magazine.prefab, TransformUsageFlags.Dynamic) : default,
//});