using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "SilencerData", menuName = "Scriptable Objects/SilencerData")]
public class SilencerData : ScriptableObject
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
