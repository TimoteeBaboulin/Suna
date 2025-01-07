using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "SpyCameraData", menuName = "Scriptable Objects/SpyCameraData")]
public class SpyCameraData : ScriptableObject
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
    public float visionField;
    public float visionRange;
}
