using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class CharacterStuffsAuthoring : MonoBehaviour
{
    public RangedWeaponData mainWeapon;
    public RangedWeaponData secondaryWeapon;
    public MeleeWeaponData melee;
    //public HarvesterData harvester;
    public List<ProjectileData> projectiles;

    public class Baker : Baker<CharacterStuffsAuthoring>
    {
        public override void Bake(CharacterStuffsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CharacterStuffsPrefabComponent
            {
                mainWeapon = authoring.mainWeapon != null ? GetEntity(authoring.mainWeapon.prefab, TransformUsageFlags.Dynamic) : default,
                secondaryWeapon = authoring.secondaryWeapon != null ? GetEntity(authoring.secondaryWeapon.prefab, TransformUsageFlags.Dynamic) : default,
                melee = authoring.melee != null ? GetEntity(authoring.melee.prefab, TransformUsageFlags.Dynamic) : default
                //harvester = authoring.harvester != null ? GetEntity(authoring.harvester.prefab, TransformUsageFlags.Dynamic) : default
            });

            var buffer = AddBuffer<ProjectileBuffer>(entity);
            foreach (var projectile in authoring.projectiles)
            {
                if (projectile != null)
                    buffer.Add(new ProjectileBuffer { Value = GetEntity(projectile.prefab, TransformUsageFlags.Dynamic) });
            }

            AddComponent(entity, new CharacterStuffsComponent());
            AddComponent(entity, new CharacterCurrentStuffComponent());
        }
    }
}

//Entities.ForEach(() =>
//{

//}).Run();