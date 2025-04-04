using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class GameResourcesAuthoring : MonoBehaviour
{
    public GameObject rangedWeaponEntityPrefab;
    public GameObject meleeWeaponEntityPrefab;
    public GameObject harvesterEntityPrefab;

    public List<RangedWeaponData> rangedWeaponList;
    public List<MeleeWeaponData> meleeWeaponList;
    public HarvesterData harvester;

    public class Baker : Baker<GameResourcesAuthoring>
    {
        public override void Bake(GameResourcesAuthoring authoring)
        {
            List<GameObject> stuffViewPrefabList = new();

            Entity entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new GameResourcesStuffEntityPrefabs
            {
                rangedWeaponEntityPrefab = GetEntity(authoring.rangedWeaponEntityPrefab, TransformUsageFlags.Dynamic),
                meleeWeaponEntityPrefab = GetEntity(authoring.meleeWeaponEntityPrefab, TransformUsageFlags.Dynamic),
                harvesterEntityPrefab = GetEntity(authoring.harvesterEntityPrefab, TransformUsageFlags.Dynamic)
            });

            var builder = new BlobBuilder(Allocator.Temp);
            ref StuffDatabase stuffCollection = ref builder.ConstructRoot<StuffDatabase>();

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            BlobBuilderArray<StuffCommonData> stuffs = builder.Allocate(ref stuffCollection.StuffCommonData, authoring.rangedWeaponList.Count + authoring.meleeWeaponList.Count + 1);

            for (int i = 0; i < authoring.rangedWeaponList.Count; i++)
            {
                var rangedWeaponSO = authoring.rangedWeaponList[i];

                builder.AllocateString(ref stuffs[i].Name, rangedWeaponSO.entityName); //TODO : Refactoriser tout ça

                stuffs[i].viewPrefab = rangedWeaponSO.viewPrefab;
                stuffs[i].location = rangedWeaponSO.location;
                stuffs[i].type = rangedWeaponSO.type;
                stuffs[i].side = rangedWeaponSO.side;
                stuffs[i].deploymentSpeed = rangedWeaponSO.deploymentSpeed;
                stuffs[i].storageSpeed = rangedWeaponSO.storageSpeed;
                stuffs[i].price = rangedWeaponSO.price;
                stuffs[i]._stuffLocalOffsetView = rangedWeaponSO._stuffLocalOffsetView;
                stuffs[i].killGain = rangedWeaponSO.killGain;

                stuffs[i].dataID = i;
            }

            for (int i = authoring.rangedWeaponList.Count; i < authoring.rangedWeaponList.Count + authoring.meleeWeaponList.Count; i++)
            {
                var meleeWeaponSO = authoring.meleeWeaponList[i - authoring.rangedWeaponList.Count];

                builder.AllocateString(ref stuffs[i].Name, meleeWeaponSO.entityName);

                stuffs[i].viewPrefab = meleeWeaponSO.viewPrefab;
                stuffs[i].location = meleeWeaponSO.location;
                stuffs[i].type = meleeWeaponSO.type;
                stuffs[i].side = meleeWeaponSO.side;
                stuffs[i].deploymentSpeed = meleeWeaponSO.deploymentSpeed;
                stuffs[i].storageSpeed = meleeWeaponSO.storageSpeed;
                stuffs[i].price = meleeWeaponSO.price;
                stuffs[i]._stuffLocalOffsetView = meleeWeaponSO._stuffLocalOffsetView;
                stuffs[i].killGain = meleeWeaponSO.killGain;

                stuffs[i].dataID = i - authoring.rangedWeaponList.Count;
            }

            int id = authoring.rangedWeaponList.Count + authoring.meleeWeaponList.Count;
            var harvesterSO = authoring.harvester;

            builder.AllocateString(ref stuffs[id].Name, harvesterSO.entityName);

            stuffs[id].viewPrefab = harvesterSO.viewPrefab;
            stuffs[id].location = harvesterSO.location;
            stuffs[id].type = harvesterSO.type;
            stuffs[id].side = harvesterSO.side;
            stuffs[id].deploymentSpeed = harvesterSO.deploymentSpeed;
            stuffs[id].storageSpeed = harvesterSO.storageSpeed;
            stuffs[id].price = harvesterSO.price;
            stuffs[id]._stuffLocalOffsetView = harvesterSO._stuffLocalOffsetView;
            stuffs[id].killGain = harvesterSO.killGain;

            stuffs[id].dataID = 0;

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            BlobBuilderArray<RangedWeaponCommonData> rangedWeapons = builder.Allocate(ref stuffCollection.RangedWeaponsCommonData, authoring.rangedWeaponList.Count);
            for (int i = 0; i < authoring.rangedWeaponList.Count; i++)
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
                rangedWeapons[i].roundsPerMin = rangedWeaponSO.roundsPerMin;
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
            }

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            BlobBuilderArray<MeleeWeaponCommonData> meleeWeapons = builder.Allocate(ref stuffCollection.MeleeWeaponsCommonData, authoring.meleeWeaponList.Count);
            for (int i = 0; i < authoring.meleeWeaponList.Count; i++)
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

            var blobRef = builder.CreateBlobAssetReference<StuffDatabase>(Allocator.Persistent);
            builder.Dispose();

            AddBlobAsset(ref blobRef, out _);

            AddComponent(entity, new GameResourcesDatabase { StuffDatabaseRef = blobRef });
            AddBuffer<GameResourcesInstanciateStuffQueu>(entity);
            AddBuffer<EquipStuffQueu>(entity);
            AddBuffer<UnequipStuffQueu>(entity);
        }
    }
}

