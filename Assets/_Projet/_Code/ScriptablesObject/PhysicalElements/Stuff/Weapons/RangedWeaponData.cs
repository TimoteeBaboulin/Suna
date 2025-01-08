using System;
using Unity.VisualScripting;
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

    [Tooltip("two scalars that can be used to control \nthe amplitude spray pattern functions")] 
    public Vector2 recoil;

    [Tooltip("in life points")]         
    public float damage;

    [Tooltip("in minutes of angle")]    
    public float spread;

    [Tooltip("in minutes of angle")]    
    public float spreadAiming;

    [Tooltip("scalar which allows you to amplify or reduce \nthe effects of the spray pattern")] 
    public float coefSpray;

    [Tooltip("scalar which allows you to amplify or reduce \nthe effects of the spray pattern on aiming")] 
    public float coefSprayAiming;

    [Tooltip("aiming speed in milliseconds")] 
    public float ergonomics;

    [Tooltip("in rounds per minute")] 
    public float roundsPerMin;

    [Tooltip("in damage per meter")] 
    public float dmgFallOff;

    [Tooltip("The coefficient of modification \nof movement speed (scalar)")] 
    public float coefModifMoveSpeed;

    [Tooltip("The coefficient of modification \nof movement speed while aiming (scalar)")] 
    public float coefModifMoveSpeedAiming;

    [Tooltip("in meter")]
    public float range;

    [Tooltip("in seconde")]
    public float reloadSpeed;

    [Tooltip("in seconde")]
    public float fastReloadSpeed;

    public int nbMagazine;
    public int magazineCapacity;

    [Header("Default Modifiers")]
    public ScopeData scope;
    public HandleData handle;
    public CrossData cross;
    public SilencerData silencer;
    public MagazineData magazine;

    [Header("Override Corps Dmg")]
    public float thorax;
    public float stomach;
    public float legs_Arms;
    public float head;

    [Header("To be defined")]
    //public GameObject sprayPattern;
    public GameObject ammoType;

    //Accessor
    public int MaxAmmo { get => nbMagazine * magazineCapacity + 1; }
}