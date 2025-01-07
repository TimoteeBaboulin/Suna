using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ProjectileData", menuName = "Scriptable Objects/ProjectileData")]
public class ProjectileData : ScriptableObject
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
    public float impactDamage;
    public float effectRange;
    public float lowThrowRange;
    public float mediumThrowRange;
    public float hightThrowRange;
    public float heightCurve;
    public float timerBeforeActive;

    [Header("To be defined")]
    public GameObject activeEffect;
}
