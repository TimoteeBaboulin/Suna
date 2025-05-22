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

    public RuntimeAnimatorController Grenade_Base;
    public RuntimeAnimatorController Grenade_Fire;
    public RuntimeAnimatorController Grenade_Flash;
    public RuntimeAnimatorController Grenade_Gas;
    public RuntimeAnimatorController Grenade_Smoke;

    public RuntimeAnimatorController KnifeNeutral;
    public RuntimeAnimatorController Harvester;
}
