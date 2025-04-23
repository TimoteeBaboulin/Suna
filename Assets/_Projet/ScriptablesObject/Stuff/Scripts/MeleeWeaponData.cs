using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Stuff Data", menuName = "Stuff Data/Melee Weapon Data")]
public class MeleeWeaponData : ScriptableObject
{
    [Header("Stuff infos")]
    public GameObject dropedEntityPrefab;
    public GameObject viewPrefab;
    public Image UIImage;
    public StuffSlot location;
    public StuffType type;
    public TeamSideType side;
    public string entityName;
    public float deploymentSpeed;
    public float storageSpeed;
    public int price;
    public Vector3 _stuffLocalOffsetView; //temp
    public uint killGain;

    [Header("Attack")]
    public float damage;
    public float strongBlowDmg;
    public float backStabDmg;
    public float range;
    public float strikeRate;
    public float strongStrikeRate;
}
