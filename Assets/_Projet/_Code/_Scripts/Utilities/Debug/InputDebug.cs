using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

public partial struct InputDebugSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (hp, entity) in SystemAPI.Query<RefRO<CurrentHealthComponent>>().WithNone<WaitForRespawnTag>().WithEntityAccess())
            {
                ecb.AddComponent<WaitForRespawnTag>(entity);
                Debug.Log("TEST");
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
