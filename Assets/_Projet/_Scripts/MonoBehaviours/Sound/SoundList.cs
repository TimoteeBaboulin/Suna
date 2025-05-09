using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class SoundList : MonoBehaviour
{
    public List<SoundGroupMapping> soundGroupList = new List<SoundGroupMapping>();

    private EntityManager entityManager;

    private void Awake()
    {
#if !UNITY_SERVER
        var bank = SoundManager.Instance.bank;
        foreach (var list in soundGroupList)
        {
            foreach (var pair in list.maping)
            {
                if (!bank.ContainsKey(list.keyGroup + pair.keyAction))
                    bank.Add(list.keyGroup + pair.keyAction, pair.sound);
            }
        }
#endif
        //soundList.Clear();
    }
}
