using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "MagazineData", menuName = "Scriptable Objects/MagazineData")]
public class MagazineData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Stuff infos")]
    public string entityName;
    public Image UIImage;
    public int price;
    public UsableEquipmentType type;
    public TeamSideType side;

    //[Header("Health")]
}
