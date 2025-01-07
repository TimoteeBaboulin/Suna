using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ScopeData", menuName = "Scriptable Objects/ScopeData")]
public class ScopeData : ScriptableObject
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
    public float fov;
    public float zoomLevel;
}
