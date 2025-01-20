using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "SpyCameraData", menuName = "Scriptable Objects/SpyCameraData")]
public class SpyCameraData : ScriptableObject
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

    [Header("CameraField")]
    public float visionField;
    public float visionRange;
}
