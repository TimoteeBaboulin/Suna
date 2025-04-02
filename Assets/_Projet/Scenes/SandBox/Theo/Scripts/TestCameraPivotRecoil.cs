using UnityEngine;

public class TestCameraPivotRecoil : MonoBehaviour
{
    public Transform camTransform;
    public Transform pivotTransform;

    public float patternAffectedCameraXRotation = 0f;
    public float patternAffectedYRotation = 0f;
    public bool isFiring = false;

    [Range(0f, 1f)] public float lerpNoFiringPower = .85f;
    [Range(0f, 1f)] public float lerpFiringPower = .98f;

    private void Update()
    {
        patternAffectedCameraXRotation = Mathf.Lerp(0f, patternAffectedCameraXRotation, isFiring ? lerpFiringPower : lerpNoFiringPower);
        patternAffectedYRotation = Mathf.Lerp(0f, patternAffectedYRotation, isFiring ? lerpFiringPower : lerpNoFiringPower);

        patternAffectedCameraXRotation = Mathf.Abs(patternAffectedCameraXRotation) < 1e-05f ? 0f : patternAffectedCameraXRotation;
        patternAffectedYRotation = Mathf.Abs(patternAffectedYRotation) < 1e-05f ? 0f : patternAffectedYRotation;

        pivotTransform.localRotation = Quaternion.Euler(0, patternAffectedYRotation, 0);
        camTransform.localRotation = Quaternion.Euler(patternAffectedCameraXRotation, 0, 0);
    }
}
