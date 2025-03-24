using UnityEngine;

public class CharacterModelScript : MonoBehaviour
{
    public SkinnedMeshRenderer MeshRenderer;
    [HideInInspector] public Quaternion NewHeadRotation;

    [SerializeField] private Transform _headBoneTransform;

    private void LateUpdate()
    {
        _headBoneTransform.rotation = NewHeadRotation;
    }
}
