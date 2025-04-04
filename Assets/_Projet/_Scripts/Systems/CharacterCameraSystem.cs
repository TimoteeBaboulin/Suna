using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine.TextCore.Text;

[UpdateAfter(typeof(SimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct ClientCharacterCameraSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CharacterCameraComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (characterTransform, characterCamera, localViewRotation, entity) in SystemAPI
            .Query<RefRO<LocalTransform>, RefRW<CharacterCameraComponent>, RefRO<CharacterLocalViewRotation>>()
            .WithAll<GhostOwnerIsLocal>()
            .WithEntityAccess())
        {
            if (characterCamera.ValueRO.CameraFollowEntity == Entity.Null)
            {
                if (!SystemAPI.TryGetSingleton(out ClientPrefabData prefabsData)) { continue; }

                characterCamera.ValueRW.CameraFollowEntity = state.EntityManager.Instantiate(prefabsData.CharacterCamera);
                ecb.AppendToBuffer(entity, new LinkedEntityGroup { Value = characterCamera.ValueRO.CameraFollowEntity });
            }

            if (state.EntityManager.IsComponentEnabled<CharacterCameraIsEnable>(entity)
                && !state.EntityManager.IsComponentEnabled<CameraFollowIsEnable>(characterCamera.ValueRO.CameraFollowEntity))
            {
                ecb.SetComponentEnabled<CameraFollowIsEnable>(characterCamera.ValueRO.CameraFollowEntity, true);
            }
            else if (!state.EntityManager.IsComponentEnabled<CharacterCameraIsEnable>(entity)
                && state.EntityManager.IsComponentEnabled<CameraFollowIsEnable>(characterCamera.ValueRO.CameraFollowEntity))
            {
                ecb.SetComponentEnabled<CameraFollowIsEnable>(characterCamera.ValueRO.CameraFollowEntity, false);
            }

            if (state.EntityManager.IsComponentEnabled<CharacterCameraIsEnable>(entity))
            {
                RefRW<LocalTransform> cameraFollowTransform = SystemAPI.GetComponentRW<LocalTransform>(characterCamera.ValueRO.CameraFollowEntity);
                cameraFollowTransform.ValueRW.Position = characterTransform.ValueRO.Position + characterCamera.ValueRO.DeltaPosition;
                cameraFollowTransform.ValueRW.Rotation = math.mul(characterTransform.ValueRO.Rotation, localViewRotation.ValueRO.ViewRotation);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
