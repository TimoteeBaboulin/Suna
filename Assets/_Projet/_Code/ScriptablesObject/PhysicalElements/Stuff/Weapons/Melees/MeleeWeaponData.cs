using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "MeleeWeaponData", menuName = "Scriptable Objects/MeleeWeaponData")]
public class MeleeWeaponData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Stuff infos")]
    public string entityName;
    public Image UIImage;
    public UsableEquipmentType type;
    public TeamSideType side;
    public float deploymentSpeed;
    public float storageSpeed;

    [Header("Attack")]
    public float damage;
    public float range;

    [Header("Melee")]
    public float strongBlowDmg;
    public float backStabDmg;
    public float strikeRate;
}
