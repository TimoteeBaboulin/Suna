using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ChangeFeature : MonoBehaviour
{
    [SerializeField] UniversalRendererData data;

    private void Start()
    {
        var pm = data.rendererFeatures[1].GetType().GetField("passMaterial");
        var lol = pm.GetValue(data.rendererFeatures[1]);
        Material m = (Material)lol;
        m.SetColor("_TestColor", Color.red);
    }
}
