using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;

//----------------------------------------------------------------------------------------------------------
//
//
// To Create an Error Message, do as the following
// Entity entity = World.EntityManager.CreateEntity(typeof(ErrorMessageComponent));
// World.EntityManager.AddComponentData(entity, new ErrorMessageComponent { Message = "hehe" });
//
//
//----------------------------------------------------------------------------------------------------------

partial class ErrorWindowCallerSystem : SystemBase
{
    public class ErrorMessage : EventArgs { public List<string> Messages = new(); }
    public event EventHandler<ErrorMessage> OnErrorMessageSent;

    protected override void OnUpdate()
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);
        List<string> messages = new();

        foreach (var(errorMessageComponent, entity) in SystemAPI.Query<ErrorMessageComponent>().WithEntityAccess())
        {
            messages.Add(errorMessageComponent.Message);
            ecb.DestroyEntity(entity);
        }

        OnErrorMessageSent?.Invoke(this, new() { Messages = messages });
    }
}
