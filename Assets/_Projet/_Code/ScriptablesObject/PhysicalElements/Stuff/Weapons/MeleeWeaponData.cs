using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "MeleeWeaponData", menuName = "Scriptable Objects/MeleeWeaponData")]
public class MeleeWeaponData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Info Data")]
    public string weaponName;
    public Image UIImage;
    public UsableEquipmentType type;
    public TeamSideType side;

    [Header("Operating Data")]
    public float damage;
    public float strongBlowDmg;
    public float backStabDmg;
    public float range;
    public float strikeRate;
}
