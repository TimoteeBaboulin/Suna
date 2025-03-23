using UnityEngine;

[CreateAssetMenu(fileName = "CharacterColliderData", menuName = "Scriptable Objects/CharacterColliderData")]
public class CharacterColliderData : ScriptableObject
{
    [Header("Damage multiplier")]
    public float HeadMultiplier;
    public float ArmMultiplier;
    public float ThoraxMultiplier;
    public float StomachMultiplier;
    public float LegMultiplier;
}
