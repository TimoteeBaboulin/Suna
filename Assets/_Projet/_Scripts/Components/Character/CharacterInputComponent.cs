using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
public struct CharacterInput : IInputComponentData
{
    [GhostField] public float2 move;

    [GhostField] public float Pitch;
    [GhostField] public float Yaw;

    [GhostField] public InputEvent jump;
    [GhostField] public InputEvent reload;
    [GhostField] public InputEvent walkStarted;
    [GhostField] public InputEvent walkCanceled;
    [GhostField] public InputEvent attackStarted;
    [GhostField] public InputEvent attackCanceled;
    [GhostField] public InputEvent selectNext;
    [GhostField] public InputEvent selectPrevious;

    [GhostField] public InputEvent aimingStarted;
    [GhostField] public InputEvent aimingCanceled;

    [GhostField] public InputEvent drop;
    [GhostField] public InputEvent interact;

    public InputEvent openShop;

    [GhostField] public int stuffLocation;
}
