using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted,OwnerSendType = SendToOwnerType.SendToNonOwner)]
public struct CharacterInput :IInputComponentData
{
    [GhostField] public float2 move;
    [GhostField] public float2 look;
    [GhostField] public InputEvent jump;
    [GhostField] public InputEvent walkStarted;
    [GhostField] public InputEvent walkCanceled;
    [GhostField] public InputEvent shoot;
}
