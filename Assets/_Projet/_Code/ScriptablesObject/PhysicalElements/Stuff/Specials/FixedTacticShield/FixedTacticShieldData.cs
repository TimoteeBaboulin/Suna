using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "FixedTacticShieldData", menuName = "Scriptable Objects/FixedTacticShieldData")]
public class FixedTacticShieldData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Stuff infos")]
    public string entityName;
    public Image UIImage;
    public int price;
    public UsableEquipmentType type;
    public TeamSideType side;

    [Header("Health")]
    public float maxHP;
}
