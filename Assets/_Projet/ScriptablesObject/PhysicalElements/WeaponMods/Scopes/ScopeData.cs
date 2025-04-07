using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ScopeData", menuName = "Scriptable Objects/ScopeData")]
public class ScopeData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Stuff infos")]
    public string entityName;
    public Image UIImage;
    public int price;
    public StuffType type;
    public TeamSideType side;

    [Header("Health")]
    public float fov;
    public float zoomLevel;
}
