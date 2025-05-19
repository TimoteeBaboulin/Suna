using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct ServerThirdPersonCharacterModelSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (modelPrefab, commonBonesName, ghostOwner, characterEntity) in SystemAPI
            .Query<ThirdPersonCharacterModelPrefab, RefRO<CommonCharacterModelBonesName>, RefRO<GhostOwner>>()
            .WithNone<ThirdPersonCharacterModelReference>()
            .WithEntityAccess())
        {
            TeamSideType teamSide;
            if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server)
            {
                teamSide = PlayerHelpers.GetPlayerInTeamOnServer(ghostOwner.ValueRO.NetworkId);
            }
            else
            {
                teamSide = PlayerHelpers.GetPlayerInTeam(ghostOwner.ValueRO.NetworkId);
            }

            if (teamSide == TeamSideType.Neutre) continue;

            GameObject modelGameObject = CommonCharacterModelUtils.InstantiateModel(modelPrefab.CorpoModelPrefab,
                modelPrefab.NatifModelPrefab, teamSide);

            if (modelPrefab == null) continue;

            CommonCharacterModelUtils.DisableModelRendering(modelGameObject);
            CommonCharacterModelUtils.AddCommonModelBonesComponent(modelGameObject.transform, commonBonesName, characterEntity, ecb);

            ThirdPersonCharacterModelUtils.AddReferenceComponent(modelGameObject, modelPrefab, characterEntity, ecb, teamSide);
            ThirdPersonCharacterModelUtils.AddModelBonesComponent(modelGameObject.transform, modelPrefab.CorpoColliderBones, 
                modelPrefab.NatifColliderBones, teamSide, characterEntity, ecb);

            Animator animator = CommonCharacterModelUtils.GetAnimator(modelGameObject);
            AnimationUtils.SetAnimator(animator, characterEntity, ecb, state.EntityManager);
        }

        foreach (var (characterTransform, modelReference, localViewRotation) in SystemAPI
            .Query<RefRO<LocalTransform>, ThirdPersonCharacterModelReference, RefRO<CharacterViewRotation>>())
        {
            float3 newPosition = characterTransform.ValueRO.Position + modelReference.DeltaPosition;
            quaternion newRotation = characterTransform.ValueRO.Rotation;
            CommonCharacterModelUtils.UpdateModelPositionAndRotation(modelReference.ModelGameObject.transform, newPosition, newRotation);
        }

        foreach (var (modelReference, characterEntity) in SystemAPI
            .Query<ThirdPersonCharacterModelReference>()
            .WithNone<LocalTransform>()
            .WithEntityAccess())
        {
            ThirdPersonCharacterModelUtils.DestroyModel(modelReference.ModelGameObject, characterEntity, ecb);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct ClientThirdPersonCharacterModelSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (modelPrefab, commonBonesName, ghostOwner, characterEntity) in SystemAPI
            .Query<ThirdPersonCharacterModelPrefab, RefRO<CommonCharacterModelBonesName>, RefRO<GhostOwner>>()
            .WithNone<ThirdPersonCharacterModelReference>()
            .WithEntityAccess())
        {
            TeamSideType teamSide;
            if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.Server)
            {
                teamSide = PlayerHelpers.GetPlayerInTeamOnServer(ghostOwner.ValueRO.NetworkId);
            }
            else
            {
                teamSide = PlayerHelpers.GetPlayerInTeam(ghostOwner.ValueRO.NetworkId);
            }

            if (teamSide == TeamSideType.Neutre) continue;

            EntityQuery query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<CharacterComponent>(), ComponentType.ReadOnly<GhostOwnerIsLocal>());
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);

            if (entities.Length == 0) continue;

            int OwnerLocalnetworkId = state.EntityManager.GetComponentData<GhostOwner>(entities[0]).NetworkId;
            TeamSideType clientLocalTeamSide = PlayerHelpers.GetPlayerInTeam(OwnerLocalnetworkId);

            GameObject modelGameObject = CommonCharacterModelUtils.InstantiateModel(modelPrefab.CorpoModelPrefab,
                modelPrefab.NatifModelPrefab, teamSide);

            GameObject actualVisualGO = modelGameObject.GetComponentInChildren<SkinnedMeshRenderer>().gameObject;

            // === Aurelien ===
            if (clientLocalTeamSide == teamSide)
            {
#if UNITY_EDITOR
                Debug.Log("Player in the same team, setting model to layer 13");
#endif
                actualVisualGO.layer = 13; // Visibility through walls is managed just by using that layer

                //Removing the enemy outline
                Material[] newMat = new Material[actualVisualGO.GetComponent<SkinnedMeshRenderer>().materials.Length - 1];
                for (int i = 0; i < newMat.Length; i++)
                {
                    newMat[i] = actualVisualGO.GetComponent<SkinnedMeshRenderer>().materials[i];
                }
                actualVisualGO.GetComponent<SkinnedMeshRenderer>().materials = newMat;
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("Player in different team, setting model to layer 14");
#endif
                actualVisualGO.layer = 14;
            }
            // === Aurelien ===

            if (modelPrefab == null) continue;

            CommonCharacterModelUtils.AddCommonModelBonesComponent(modelGameObject.transform, commonBonesName, characterEntity, ecb);

            ThirdPersonCharacterModelUtils.AddReferenceComponent(modelGameObject, modelPrefab, characterEntity, ecb, teamSide);
            ThirdPersonCharacterModelUtils.AddModelBonesComponent(modelGameObject.transform, modelPrefab.CorpoColliderBones,
                modelPrefab.NatifColliderBones, teamSide, characterEntity, ecb);
        }

        foreach (var (characterTransform, modelReference, localViewRotation, commonBonesName, commonBones, characterEntity) in SystemAPI
            .Query<RefRO<LocalTransform>, ThirdPersonCharacterModelReference, RefRO<CharacterViewRotation>, RefRO<CommonCharacterModelBonesName>,
            CommonCharacterModelBonesTransform>()
            .WithEntityAccess())
        {
            if (!state.EntityManager.IsComponentEnabled<CharacterIsEnable>(characterEntity))
            {
                CommonCharacterModelUtils.SetCommonModelBonesComponent(modelReference.ModelGameObject.transform, commonBonesName, characterEntity, ecb);

                Animator animator = CommonCharacterModelUtils.GetAnimator(modelReference.ModelGameObject);
                AnimationUtils.SetAnimator(animator, characterEntity, ecb, state.EntityManager);

                modelReference.ModelGameObject.SetActive(true);
            }
            else
            {
                if (!state.EntityManager.HasComponent<CameraIsAtached>(characterEntity))
                {
                    CommonCharacterModelUtils.SetCommonModelBonesComponent(modelReference.ModelGameObject.transform, commonBonesName, characterEntity, ecb);

                    Animator animator = CommonCharacterModelUtils.GetAnimator(modelReference.ModelGameObject);
                    AnimationUtils.SetAnimator(animator, characterEntity, ecb, state.EntityManager);

                    if (!modelReference.ModelGameObject.activeSelf)
                    {
                        modelReference.ModelGameObject.SetActive(true);
                    }
                }
                else
                {
                    if (modelReference.ModelGameObject.activeSelf)
                    {
                        modelReference.ModelGameObject.SetActive(false);
                    }
                }
            }

            float3 newPosition = characterTransform.ValueRO.Position + modelReference.DeltaPosition;
            quaternion newRotation = characterTransform.ValueRO.Rotation;
            CommonCharacterModelUtils.UpdateModelPositionAndRotation(modelReference.ModelGameObject.transform, newPosition, newRotation);
        }

        foreach (var (modelReference, entity) in SystemAPI
            .Query<ThirdPersonCharacterModelReference>()
            .WithNone<LocalTransform>()
            .WithEntityAccess())
        {
            Object.Destroy(modelReference.ModelGameObject);
            ecb.RemoveComponent<ThirdPersonCharacterModelReference>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
