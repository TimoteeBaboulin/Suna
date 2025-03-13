using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ProjectileData", menuName = "Scriptable Objects/ProjectileData")]
public class ProjectileData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Stuff infos")]
    public string entityName;
    public Image UIImage;
    public int price;
    public StuffType type;
    public TeamSideType side;

    [Header("Health")]
    public float impactDamage;
    public float effectRange;
    public float lowThrowRange;
    public float mediumThrowRange;
    public float hightThrowRange;
    public float heightCurve;
    public float timerBeforeActive;
    public float knockbackForceOnKill;
    public float bounceForce;
    public bool activeWhenCollid;

    [Header("To be defined...")]
    public GameObject activeEffect;
}
