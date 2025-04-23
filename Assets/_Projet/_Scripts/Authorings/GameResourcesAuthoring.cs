using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

public class GameResourcesAuthoring : MonoBehaviour
{
    public GameObject rangedWeaponEntityPrefab;
    public GameObject meleeWeaponEntityPrefab;
    public GameObject grenadesEntityPrefab;
    public GameObject harvesterEntityPrefab;

    public List<RangedWeaponData> rangedWeaponList;
    public List<MeleeWeaponData> meleeWeaponList;
    public List<GrenadeData> grenadesList;
    public HarvesterData harvester;

    public class Baker : Baker<GameResourcesAuthoring>
    {
        public override void Bake(GameResourcesAuthoring authoring)
        {
            List<GameObject> stuffViewPrefabList = new();

            Entity entity = GetEntity(TransformUsageFlags.None);

            var builder = new BlobBuilder(Allocator.Temp);
            ref StuffDatabase stuffCollection = ref builder.ConstructRoot<StuffDatabase>();
            DynamicBuffer<StuffEntityPrefabsBuffer> prefabs = AddBuffer<StuffEntityPrefabsBuffer>(entity);

            Entity rangedWeaponEntity = GetEntity(authoring.rangedWeaponEntityPrefab, TransformUsageFlags.Dynamic);
            Entity meleeWeaponEntity = GetEntity(authoring.meleeWeaponEntityPrefab, TransformUsageFlags.Dynamic);
            Entity grenadeEntity = GetEntity(authoring.grenadesEntityPrefab, TransformUsageFlags.Dynamic);
            Entity harvesterEntity = GetEntity(authoring.harvesterEntityPrefab, TransformUsageFlags.Dynamic);
            List<GameObject> viewPrefabs = new();

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            BlobBuilderArray<StuffCommonData> stuffs = builder.Allocate(ref stuffCollection.StuffCommonData, authoring.rangedWeaponList.Count + authoring.meleeWeaponList.Count + authoring.grenadesList.Count + 1);

            int i = 0;

            for (; i < authoring.rangedWeaponList.Count; i++)
            {
                var rangedWeaponSO = authoring.rangedWeaponList[i];

                builder.AllocateString(ref stuffs[i].Name, rangedWeaponSO.entityName); //TODO : Refactoriser tout ça

                viewPrefabs.Add(rangedWeaponSO.viewPrefab);

                prefabs.Add(new StuffEntityPrefabsBuffer
                {
                    dropedEntityPrefab = GetEntity(rangedWeaponSO.dropedEntityPrefab, TransformUsageFlags.Dynamic),
                    inHandEntityPrefab = rangedWeaponEntity
                });

                stuffs[i].slot = rangedWeaponSO.location;
                stuffs[i].type = rangedWeaponSO.type;
                stuffs[i].side = rangedWeaponSO.side;
                stuffs[i].deploymentSpeed = rangedWeaponSO.deploymentSpeed;
                stuffs[i].storageSpeed = rangedWeaponSO.storageSpeed;
                stuffs[i].price = rangedWeaponSO.price;
                stuffs[i]._stuffLocalOffsetView = rangedWeaponSO._stuffLocalOffsetView;
                stuffs[i].killGain = rangedWeaponSO.killGain;
                stuffs[i].canADS = rangedWeaponSO.canADS;
                stuffs[i].ADSFOV = rangedWeaponSO.ADSFOV;

                stuffs[i].dataID = i;
            }

            for (; i < authoring.rangedWeaponList.Count + authoring.meleeWeaponList.Count; i++)
            {
                var meleeWeaponSO = authoring.meleeWeaponList[i - authoring.rangedWeaponList.Count];

                builder.AllocateString(ref stuffs[i].Name, meleeWeaponSO.entityName);

                viewPrefabs.Add(meleeWeaponSO.viewPrefab);

                prefabs.Add(new StuffEntityPrefabsBuffer 
                { 
                    dropedEntityPrefab = GetEntity(meleeWeaponSO.dropedEntityPrefab, TransformUsageFlags.Dynamic),
                    inHandEntityPrefab = meleeWeaponEntity
                });

                stuffs[i].slot = meleeWeaponSO.location;
                stuffs[i].type = meleeWeaponSO.type;
                stuffs[i].side = meleeWeaponSO.side;
                stuffs[i].deploymentSpeed = meleeWeaponSO.deploymentSpeed;
                stuffs[i].storageSpeed = meleeWeaponSO.storageSpeed;
                stuffs[i].price = meleeWeaponSO.price;
                stuffs[i]._stuffLocalOffsetView = meleeWeaponSO._stuffLocalOffsetView;
                stuffs[i].killGain = meleeWeaponSO.killGain;
                stuffs[i].canADS = false;
                stuffs[i].ADSFOV = 0;

                stuffs[i].dataID = i - authoring.rangedWeaponList.Count;
            }

            for(; i < authoring.rangedWeaponList.Count + authoring.meleeWeaponList.Count + authoring.grenadesList.Count; i++)
            {
                var grenadeSO = authoring.grenadesList[i - authoring.rangedWeaponList.Count - authoring.meleeWeaponList.Count];

                builder.AllocateString(ref stuffs[i].Name, grenadeSO.entityName);

                viewPrefabs.Add(grenadeSO.viewPrefab);

                prefabs.Add(new StuffEntityPrefabsBuffer
                {
                    dropedEntityPrefab = GetEntity(grenadeSO.dropedEntityPrefab, TransformUsageFlags.Dynamic),
                    inHandEntityPrefab = grenadeEntityEntity
                });

                stuffs[i].viewPrefab = grenadeSO.viewPrefab;
                stuffs[i].location = grenadeSO.location;
                stuffs[i].type = grenadeSO.type;
                stuffs[i].side = grenadeSO.side;
                //stuffs[i].deploymentSpeed = 0;
                //stuffs[i].storageSpeed = 0;
                stuffs[i].price = grenadeSO.price;
                stuffs[i]._stuffLocalOffsetView = grenadeSO._stuffLocalOffsetView;
                stuffs[i].killGain = grenadeSO.killGain;
                //stuffs[i].canADS = false;
                //stuffs[i].ADSFOV = 0;
                stuffs[i].dataID = i - authoring.rangedWeaponList.Count - authoring.meleeWeaponList.Count;
            }

            int id = authoring.rangedWeaponList.Count + authoring.meleeWeaponList.Count + authoring.grenadesList.Count;
            var harvesterSO = authoring.harvester;

            builder.AllocateString(ref stuffs[id].Name, harvesterSO.entityName);

            viewPrefabs.Add(harvesterSO.viewPrefab);

            prefabs.Add(new StuffEntityPrefabsBuffer 
            { 
                dropedEntityPrefab = GetEntity(harvesterSO.dropedEntityPrefab, TransformUsageFlags.Dynamic),
                inHandEntityPrefab = harvesterEntity
            });

            stuffs[id].slot = harvesterSO.location;
            stuffs[id].type = harvesterSO.type;
            stuffs[id].side = harvesterSO.side;
            stuffs[id].deploymentSpeed = harvesterSO.deploymentSpeed;
            stuffs[id].storageSpeed = harvesterSO.storageSpeed;
            stuffs[id].price = harvesterSO.price;
            stuffs[id]._stuffLocalOffsetView = harvesterSO._stuffLocalOffsetView;
            stuffs[id].killGain = harvesterSO.killGain;
            stuffs[id].canADS = false;
            stuffs[id].ADSFOV = 0;

            stuffs[id].dataID = 0;

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            BlobBuilderArray<RangedWeaponCommonData> rangedWeapons = builder.Allocate(ref stuffCollection.RangedWeaponsCommonData, authoring.rangedWeaponList.Count);
            for (i = 0; i < authoring.rangedWeaponList.Count; i++)
            {
                var rangedWeaponSO = authoring.rangedWeaponList[i];

                rangedWeapons[i].recoil = rangedWeaponSO.recoil;
                rangedWeapons[i].range = rangedWeaponSO.range;
                rangedWeapons[i].firerate = rangedWeaponSO.firerate;
                rangedWeapons[i].spread = rangedWeaponSO.spread;
                rangedWeapons[i].spreadAiming = rangedWeaponSO.spreadAiming;
                rangedWeapons[i].coefSpray = rangedWeaponSO.coefSpray;
                rangedWeapons[i].coefSprayAiming = rangedWeaponSO.coefSprayAiming;
                rangedWeapons[i].ergonomics = rangedWeaponSO.ergonomics;
                rangedWeapons[i].isAutomatic = rangedWeaponSO.isAutomatic;
                rangedWeapons[i].roundsPerShot = rangedWeaponSO.roundsPerShot;
                rangedWeapons[i].dmgFallOff = rangedWeaponSO.dmgFallOff;
                rangedWeapons[i].coefModifMoveSpeed = rangedWeaponSO.coefModifMoveSpeed;
                rangedWeapons[i].coefModifMoveSpeedAiming = rangedWeaponSO.coefModifMoveSpeedAiming;
                rangedWeapons[i].reloadSpeed = rangedWeaponSO.reloadSpeed;
                rangedWeapons[i].fastReloadSpeed = rangedWeaponSO.fastReloadSpeed;
                rangedWeapons[i].fastReloadSpeed = rangedWeaponSO.fastReloadSpeed;
                rangedWeapons[i].knockbackForceOnKill = rangedWeaponSO.knockbackForceOnKill;
                rangedWeapons[i].damage = rangedWeaponSO.damage;
                rangedWeapons[i].nbMagazine = rangedWeaponSO.nbMagazine;
                rangedWeapons[i].magazineCapacity = rangedWeaponSO.magazineCapacity;
                rangedWeapons[i].lastFireTimeMax = rangedWeaponSO.lastFireTimeMax;
                rangedWeapons[i].scope = new ScopeComponent
                {
                    ScopeFOV = rangedWeaponSO.scope != null ? rangedWeaponSO.scope.fov : 0
                };
            }

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            BlobBuilderArray<MeleeWeaponCommonData> meleeWeapons = builder.Allocate(ref stuffCollection.MeleeWeaponsCommonData, authoring.meleeWeaponList.Count);
            for (i = 0; i < authoring.meleeWeaponList.Count; i++)
            {
                var meleeWeaponSO = authoring.meleeWeaponList[i];

                meleeWeapons[i].damage = meleeWeaponSO.damage;
                meleeWeapons[i].strongBlowDmg = meleeWeaponSO.strongBlowDmg;
                meleeWeapons[i].backStabDmg = meleeWeaponSO.backStabDmg;
                meleeWeapons[i].range = meleeWeaponSO.range;
                meleeWeapons[i].strikeRate = meleeWeaponSO.strikeRate;
                meleeWeapons[i].strongStrikeRate = meleeWeaponSO.strongStrikeRate;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            ///

            BlobBuilderArray<GrenadeCommonData> grenades = builder.Allocate(ref stuffCollection.GrenadesCommonData, authoring.grenadesList.Count);

            for (i = 0; i < authoring.grenadesList.Count; i++)
            {
                var grenadeSO = authoring.grenadesList[i];
                grenades[i].cookingTime = grenadeSO.cookingTime;
                grenades[i].impactRadius = grenadeSO.impactRadius;
                grenades[i].triggerType = grenadeSO.triggerType;
                grenades[i].timerTriggerDelay = grenadeSO.timerTriggerDelay;
                grenades[i].maxImpactAngle = grenadeSO.maxImpactAngle;
                grenades[i].stillTriggerDelay = grenadeSO.stillTriggerDelay;
                grenades[i].bounceTriggerCount = grenadeSO.bounceTriggerCount;
                grenades[i].proximityTriggerDistance = grenadeSO.proximityTriggerDistance;
            }

            var blobRef = builder.CreateBlobAssetReference<StuffDatabase>(Allocator.Persistent);
            builder.Dispose();

            AddBlobAsset(ref blobRef, out _);

            AddComponent(entity, new GameResourcesDatabase { StuffDatabaseRef = blobRef });
            AddComponentObject(entity, new GameResourcesViewPrefabs { List = viewPrefabs });

            AddBuffer<GameResourcesInstantiateStuffQueue>(entity);
            AddBuffer<EquipStuffQueue>(entity);
            AddBuffer<UnequipStuffQueue>(entity);
        }
    }
}

