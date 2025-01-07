using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ClaymoreSoundData", menuName = "Scriptable Objects/ClaymoreSoundData")]
public class ClaymoreSoundData : ScriptableObject
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
