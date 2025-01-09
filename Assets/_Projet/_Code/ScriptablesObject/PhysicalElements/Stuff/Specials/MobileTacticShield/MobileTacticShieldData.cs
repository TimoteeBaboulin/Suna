using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "MobileTacticShieldData", menuName = "Scriptable Objects/MobileTacticShieldData")]
public class MobileTacticShieldData : ScriptableObject
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
