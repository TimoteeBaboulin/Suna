using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class CharacterViewUtils
{
    public static bool TryGetRotation(in Entity characterEntity, in EntityManager entityManager, out quaternion viewRotation)
    {
        if (entityManager.HasComponent<LocalTransform>(characterEntity)
            && entityManager.HasComponent<CharacterViewRotation>(characterEntity))
        {
            quaternion yawQuaterion = entityManager.GetComponentData<LocalTransform>(characterEntity).Rotation;
            quaternion pitchQuaterion = entityManager.GetComponentData<CharacterViewRotation>(characterEntity).ViewRotation;
            viewRotation = math.mul(yawQuaterion, pitchQuaterion);
            return true;
        }

        viewRotation = quaternion.identity;
        return false;
    }

    public static bool TryGetForward(in Entity characterEntity, in EntityManager entityManager, out float3 viewForward)
    {
        if (TryGetRotation(characterEntity, entityManager, out quaternion viewRotation))
        {
            viewForward = math.mul(viewRotation, math.forward());
            return true;
        }

        viewForward = float3.zero;
        return false;
    }
}
