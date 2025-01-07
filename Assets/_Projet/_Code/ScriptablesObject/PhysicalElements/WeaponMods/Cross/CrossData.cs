using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "CrossData", menuName = "Scriptable Objects/CrossData")]
public class CrossData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Info Data")]
    public string weaponName;
    public Image UIImage;
    public int price;
    public UsableEquipmentType type;
    public TeamSideType side;

    //[Header("Operating Data")]
}
