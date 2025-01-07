using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "FixedTacticShieldData", menuName = "Scriptable Objects/FixedTacticShieldData")]
public class FixedTacticShieldData : ScriptableObject
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
    public float maxHP;
}
