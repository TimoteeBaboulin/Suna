//using Unity.Collections;
//using Unity.Entities;
//using Unity.NetCode;
//using UnityEngine;
//using UnityEngine.Rendering;

//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
//[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
//partial struct RangedWeaponSystemClient : ISystem
//{
//    public void OnCreate(ref SystemState state)
//    {
//        state.RequireForUpdate<StuffGameObjectRef>();
//    }

//    public void OnUpdate(ref SystemState state)
//    {
//        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

//        //Play Sounds (Temp)
//#if !UNITY_SERVER
//        foreach (var (request, soundRpc, entity) in SystemAPI
//            .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<RangedWeaponSoundRpc>>()
//            .WithEntityAccess())
//        {
//            if (request.ValueRO.IsConsumed) continue;

//            GameObject goRef = state.EntityManager.GetComponentData<StuffGameObjectRef>(soundRpc.ValueRO.source).Value;

//            if (goRef.TryGetComponent(out RangedWeaponSound rws))
//            {
//                switch (soundRpc.ValueRO.soudToPlay)
//                {
//                    case RangedWeaponState.Idle:
//                        break;
//                    case RangedWeaponState.Shoot:
//                        rws.Shoot();
//                        break;
//                    case RangedWeaponState.Reload:
//                        Debug.Log("Son reload " + goRef);
//                        rws.Reload();
//                        break;

//                    case RangedWeaponState.Droped:
//                        break;
//                    default:
//                        break;
//                }
//            }
//            ecb.DestroyEntity(entity);
//        }
//#endif

//        ecb.Playback(state.EntityManager);
//        ecb.Dispose();
//    }
//}





