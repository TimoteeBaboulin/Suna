using System;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Pro", menuName = "Scriptable Objects/RangedWeaponData")]
public class RangedWeaponData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Info Data")]
    public string weaponName;
    public Image UIImage;
    public int price;
    public UsableEquipmentType type;
    public TeamSideType side;

    [Header("Operating Data")]
    public float damage;
    public float range;
    public float fireRate;
    public float ergonomics;
    public float reloadSpeed;
    public float fastReloadSpeed;
    public float accuracy;
    public float dmgFallOff;
    public int nbMagazine;
    public int magazineCapacity;

    [Header("Default Modifiers")]
    public ScriptableObject scope;
    public ScriptableObject handle;
    public ScriptableObject silencer;

    [Header("Override Corps Dmg")]
    public float thorax;
    public float stomach;
    public float legs_Arms;
    public float head;

    [Header("To be defined")]
    public GameObject sprayPattern;
    public GameObject ammoType;

    //Accessor
    public int MaxAmmo { get => nbMagazine * magazineCapacity + 1; }
}
