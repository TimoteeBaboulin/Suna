using UnityEngine;

[CreateAssetMenu(fileName = "ModelAnimatorData", menuName = "Scriptable Objects/ModelAnimatorData")]
public class ModelAnimatorData : ScriptableObject
{
    public AnimatorOverrideController Banduka;
    public AnimatorOverrideController Decimator;
    public AnimatorOverrideController Fakir;
    public AnimatorOverrideController Laksya;
    public AnimatorOverrideController LP17;
    public AnimatorOverrideController SKAR18;
    public AnimatorOverrideController Nelara;

    public AnimatorOverrideController Grenade_Base;
    public AnimatorOverrideController Grenade_Fire;
    public AnimatorOverrideController Grenade_Flash;
    public AnimatorOverrideController Grenade_Gas;
    public AnimatorOverrideController Grenade_Smoke;

    public RuntimeAnimatorController KnifeNeutral;
    public AnimatorOverrideController Harvester;
}
