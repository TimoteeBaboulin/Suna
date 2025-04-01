using Unity.Entities;
using UnityEngine;

public class CommonCharacterAnimationUtils
{
    public static void AddAnimatorReferenceComponent(GameObject gameObject, in Entity characterEntity, in EntityCommandBuffer ecb)
    {
        if (gameObject.TryGetComponent(out Animator animator))
        {
            ecb.AddComponent(characterEntity, new CharacterAnimatorReference
            {
                Animator = animator,
            });
        }
    }

    public static void SetParameters(in Animator animator, RefRO<CommonCharacterAnimationState> states)
    {
        animator.SetBool("IsWalking", states.ValueRO.IsWalking);
    }
}
