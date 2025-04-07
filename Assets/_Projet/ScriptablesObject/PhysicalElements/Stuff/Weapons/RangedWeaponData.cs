using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Ranged Weapon Data", menuName = "Stuff Data/Ranged Weapon Data")]
public class RangedWeaponData : ScriptableObject
{
    [Header("Stuff infos")]
    public GameObject viewPrefab;
    public Texture2D UIImage;
    public StuffInventoryLocation location;
    public StuffType type;
    public TeamSideType side;
    public string entityName;
    public float deploymentSpeed;
    public float storageSpeed;
    public int price;
    public uint killGain = 300;
    public Vector3 _stuffLocalOffsetView; //temp


    [Header("Ranged Weapon Data")]

    [Tooltip("two scalars that can be used to control \nthe amplitude spray pattern functions")]
    public Vector2 recoil;

    [Tooltip("in life points")]
    public int damage;

    [Tooltip("in meter")]
    public float range;

    [Tooltip("in rounds per minute (RPM)")]
    public float firerate;

    public bool isAutomatic = false;

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

    [Tooltip("in damage per meter")] 
    public float dmgFallOff;

    [Tooltip("The coefficient of modification \nof movement speed (scalar)")] 
    public float coefModifMoveSpeed;

    [Tooltip("The coefficient of modification \nof movement speed while aiming (scalar)")] 
    public float coefModifMoveSpeedAiming;

    [Tooltip("in seconde")]
    public float reloadSpeed;

    [Tooltip("in seconde")]
    public float fastReloadSpeed;
    
    [Tooltip("Propulsion of the enemy ragdoll when it dies")]
    public float knockbackForceOnKill;

    public float lastFireTimeMax;

    public int nbMagazine;
    public int magazineCapacity;

    [Header("Default Modifiers")]
    public ScopeData scope;
    public HandleData handle;
    public CrossData cross;
    public SilencerData silencer;
    public MagazineData magazine;

    [Header("Body Damage Overrides")]
    public float thorax;
    public float stomach;
    public float legs_Arms;
    public float head;

    [Header("To be defined...")]
    //public GameObject sprayPattern;
    public GameObject ammoType;

    //Accessor
    public int MaxAmmo { get => nbMagazine * magazineCapacity + 1; }
}