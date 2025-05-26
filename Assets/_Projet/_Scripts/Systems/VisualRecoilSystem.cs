using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class VisualRecoilSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (visualRecoil, fpv, entity) in SystemAPI.Query<RefRW<FPVVisualRecoil>, FirstPersonCharacterModelReference>().WithEntityAccess())
        {
            fpv.ShootDelta = math.lerp(new float3(0, 0, -0.08f), float3.zero, math.saturate(visualRecoil.ValueRW.timeSinceLastShoot * 6));
        }
    }
}
