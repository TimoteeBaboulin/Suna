using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public enum RangedWeaponState
{
    Idle,
    Shoot,
    Reload,
    Droped
}

[GhostComponent]
public struct RangedWeaponDynamicData : IComponentData
{
    [GhostField] public RangedWeaponState state;

    [GhostField] public float reloadTimer;
    [GhostField] public float fastReloadTimer;
    [GhostField] public float firerateTimer;
    [GhostField] public float timeSinceLastFire;

    [GhostField] public bool shotFired;

    [GhostField] public int currentAmmo;
    [GhostField] public int remainingAmmo;
    [GhostField] public int patternBulletIndex;
}

public struct RangedWeaponCommonData
{
    public float2 recoil;

    public float range;
    public float firerate;
    public bool isAutomatic;
    public uint roundsPerShot;
    public float spread;
    public float spreadAiming;
    public float coefSpray;
    public float coefSprayAiming;
    public float ergonomics;
    public float dmgFallOff;
    public float coefModifMoveSpeed;
    public float coefModifMoveSpeedAiming;
    public float reloadSpeed;
    public float fastReloadSpeed;
    public float knockbackForceOnKill;
    public float lastFireTimeMax;

    public int damage;
    public int nbMagazine;
    public int magazineCapacity;

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

    //Accessor
    public int MaxAmmo { get => nbMagazine * magazineCapacity + 1; }
}


[GhostComponent]
public struct RangedWeaponDatabaseAccess : IComponentData
{
    [GhostField] public int Value;

    public readonly ref RangedWeaponCommonData GetData(ref GameResourcesDatabase database)
    {
        return ref database.StuffDatabaseRef.Value.RangedWeaponsCommonData[Value];
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