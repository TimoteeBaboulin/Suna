using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class SoundList : MonoBehaviour
{
#if !UNITY_SERVER

    public bool autoCreateEntityEmitter;
    public string keyGroup;
    public List<SoundMapping> soundList = new List<SoundMapping>();

    private EntityManager entityManager;

    private void Awake()
    {
        var bank = SoundManager.Instance.bank;
        foreach (var pair in soundList)
        {
            if (!bank.ContainsKey(keyGroup + pair.keyAction))
                bank.Add(keyGroup + pair.keyAction, pair.sound);
        }
        //soundList.Clear();
    }

    void Start()
    {
        if (autoCreateEntityEmitter)
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            EntityArchetype archetype = entityManager.CreateArchetype(
                typeof(SoundEmitter)
            );

            Entity entity = entityManager.CreateEntity(archetype);

            entityManager.SetComponentData(entity, new SoundEmitter
            {
                keyGroup = keyGroup
            });
        }
    }
#endif
}
