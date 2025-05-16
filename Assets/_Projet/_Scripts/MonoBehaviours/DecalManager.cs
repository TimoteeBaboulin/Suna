using System.Runtime.CompilerServices;
using UnityEngine;

public class DecalManager : MonoBehaviour
{
    public static DecalManager Instance => _instance;
    private static DecalManager _instance;

    [SerializeField] private DecalTimer _decalPrefab;

    private void OnEnable()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    private void OnDisable()
    {
        if ( _instance == this)
        {
            _instance = null;
        }
    }

    public void SpawnDecal(Vector3 position, Vector3 wallNormal)
    {
        Debug.Log("Spawning shit");
        DecalTimer decal = Instantiate(_decalPrefab, position, Quaternion.identity);
        decal.transform.forward = -wallNormal;
    }
}
