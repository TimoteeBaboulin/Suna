using Unity.Entities;
using UnityEngine;

public class ThirdPersonCharacterAnimationUtils
{
    public static void AddAnimatorReferenceComponent(GameObject gameObject, in Entity characterEntity, in EntityCommandBuffer ecb)
    {
        if (gameObject.TryGetComponent(out Animator animator))
        {
            ecb.AddComponent(characterEntity, new ThirdPersonCharacterAnimatorReference
            {
                Animator = animator,
            });
        }
    }

    public static void SetParameters(in Animator animator, RefRO<ThirdPersonCharacterAnimationState> states)
    {
        animator.SetBool("IsWalking", states.ValueRO.IsWalking);
    }
}
