using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

//public class AttachCameraToPlayer : MonoBehaviour
//{
//    private Entity playerEntity;
//    private EntityManager entityManager;

//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {
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

//    void LateUpdate()
//    {
//        var localTransform = entityManager.GetComponentData<CameraAttachComponent>(playerEntity).transform;
//        transform.position = localTransform.Position;
//        transform.rotation = localTransform.Rotation;
//        transform.position += Vector3.up * 0.8f;
//    }
//}

//[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class CameraAttachmentSystem : SystemBase
{
    private Camera mainCamera;
    private EntityQuery cameraAttachQuery;

    protected override void OnCreate()
    {
        base.OnCreate();

        // Find the main camera in the scene
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found!");
            return;
        }

        EntityQueryBuilder builder = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
            .WithAll<CameraAttachComponent>();

        RequireForUpdate(GetEntityQuery(builder));
    }

    protected override void OnUpdate()
    {
        if (mainCamera == null) { return; }         

        foreach (var cameraAttach in SystemAPI.Query<CameraAttachComponent>().WithAll<GhostOwnerIsLocal>())
        {
           // Debug.Log(cameraAttach.transform.Rotation);
            mainCamera.transform.position = cameraAttach.transform.Position;
            mainCamera.transform.rotation = cameraAttach.transform.Rotation;
            mainCamera.transform.position += Vector3.up * 0.8f;
        }
    }
}