//using System;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Transforms;
//using UnityEngine;

//public class AttachCameraToPlayer : MonoBehaviour
//{
//    private Entity playerEntity;
//    private EntityManager entityManager;

//    // Start is called once before the first execution of Update after the MonoBehaviour is created

//    private void Start()
//    {
//        ConnectionManager.Instance.Connected += LoadPlayerInformation;
//    }

//    private void OnDestroy()
//    {
//        ConnectionManager.Instance.Connected -= LoadPlayerInformation;
//    }


//    void Update()
//    {
//        if (playerEntity != Entity.Null)
//        {
//            var localTransform = entityManager.GetComponentData<CameraAttachComponent>(playerEntity).transform;
//            transform.position = localTransform.Position;
//            transform.rotation = localTransform.Rotation;
//        }
//    }

//    void LoadPlayerInformation()
//    {
//        Cursor.lockState = CursorLockMode.Locked;
//        Cursor.visible = false;

//        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

//        if (entityManager == null)
//        {
//            Debug.LogError("Entity Manager is null");
//            return;
//        }


//        foreach (Entity entity in entityManager.GetAllEntities())
//        {
//            if (entityManager.HasComponent<CameraAttachComponent>(entity))
//            {
//                playerEntity = entity;
//                return;
//            }
//        }
//    }
//}



////public partial class PlayerDataHandler : ISystem
////{
////    PrefabsData prefabManager = SystemAPI.GetSingleton<PrefabsData>();

////    private Entity playerEntity;
////    private EntityManager entityManager;

////    public event Action<LocalTransform> isInit;
////    public void LoadPlayerInformation()
////    {
////        if (prefabManager.player != null)
////        {
////            Cursor.lockState = CursorLockMode.Locked;
////            Cursor.visible = false;

////            playerEntity = prefabManager.player;
////            return;
////        }
////    }

////    //public void OnCreate(ref SystemState state)
////    //{
////    //    state.RequireForUpdate<CameraAttachComponent>();
////    //}

////    //public void OnUpdate(ref SystemState state)
////    //{
////    //    var localTransform = entityManager.GetComponentData<CameraAttachComponent>(playerEntity).transform;
////    //    transform.position = localTransform.Position;
////    //    transform.rotation = localTransform.Rotation;
////    //}
////}

//public struct CameraData : IComponentData
//{
//    float3 position;
//}

//public partial class CameraHandler : SystemBase
//{

//    protected override void OnCreate()
//    {
//        base.OnCreate();
//    }

//    protected override void OnUpdate()
//    {
//        throw new NotImplementedException();
//    }
//}
