using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "SilencerData", menuName = "Scriptable Objects/SilencerData")]
public class SilencerData : ScriptableObject
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
