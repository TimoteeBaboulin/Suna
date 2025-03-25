using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "CrossData", menuName = "Scriptable Objects/CrossData")]
public class CrossData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Stuff infos")]
    public string entityName;
    public Image UIImage;
    public int price;
    public StuffType type;
    public TeamSideType side;

    //[Header("Health")]
}
