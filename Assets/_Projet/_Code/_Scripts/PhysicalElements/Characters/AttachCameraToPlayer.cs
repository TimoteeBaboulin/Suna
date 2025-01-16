using Unity.Entities;
using UnityEngine;

public class AttachCameraToPlayer : MonoBehaviour
{
    private Entity playerEntity;
    private EntityManager entityManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        if(entityManager == null)
        {
            Debug.LogError("Entity Manager is null");
            return;
        }

        foreach (Entity entity in entityManager.GetAllEntities())
        {
            if (entityManager.HasComponent<CameraAttachComponent>(entity))
            {
                playerEntity = entity;
                return;
            }
        }
    }

    void Update()
    {
        var localTransform = entityManager.GetComponentData<CameraAttachComponent>(playerEntity).transform;
        transform.position = localTransform.Position;
        transform.rotation = localTransform.Rotation;
    }
}
