using Unity.Entities;
using UnityEngine;

public class CommonCharacterAnimationUtils
{
    public static void SetAnimatorReference(GameObject gameObject, in Entity characterEntity, in EntityCommandBuffer ecb, in EntityManager entityManager)
    {
        if (gameObject.TryGetComponent(out Animator animator))
        {
            AnimationUtils.SetAnimator(animator, characterEntity, ecb, entityManager);
        }
    }

    public static void SetParameters(in Animator animator, RefRO<CommonCharacterAnimationState> states)
    {
        animator.SetBool("IsWalking", states.ValueRO.IsWalking);
    }
}
