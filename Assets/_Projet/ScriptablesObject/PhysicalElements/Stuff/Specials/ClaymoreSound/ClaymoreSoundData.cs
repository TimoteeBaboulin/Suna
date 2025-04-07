using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ClaymoreSoundData", menuName = "Scriptable Objects/ClaymoreSoundData")]
public class ClaymoreSoundData : ScriptableObject
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
    public float maxHP;
}
