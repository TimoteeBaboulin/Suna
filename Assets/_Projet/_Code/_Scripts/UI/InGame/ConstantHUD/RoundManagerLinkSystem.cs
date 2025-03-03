using Unity.Entities;
using UnityEngine;

partial class RoundManagerLinkSystem : SystemBase
{
    private bool _roundManagerComponentFound = false;

    protected override void OnUpdate()
    {
    }

    public bool TryGetRoundComponent(out RoundComponent roundComponent)
    {
        _roundManagerComponentFound = SystemAPI.TryGetSingleton(out roundComponent);
        return _roundManagerComponentFound;
    }

    public void UpdateRoundComponent(RoundComponent roundComponent)
    {
        Entity entity = SystemAPI.GetSingletonEntity<RoundComponent>();
        SystemAPI.SetComponent(entity, roundComponent);
    }
}
