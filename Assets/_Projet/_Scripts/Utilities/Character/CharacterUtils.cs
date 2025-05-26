using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Collections;

public class CharacterUtils
{
    public static bool IsCharacterMoving(EntityManager manager, Entity entity)
    {
        if (manager.HasComponent<PhysicsVelocity>(entity))
        {
            return math.lengthsq(manager.GetComponentData<PhysicsVelocity>(entity).Linear) > 0;
        }

        return false;
    }

    public static bool IsCharacterGrounded(EntityManager manager, Entity entity)
    {
        if (manager.HasComponent<CharacterComponent>(entity))
        {
            return manager.GetComponentData<CharacterComponent>(entity).isGrounded;
        }

        return false;
    }
}
