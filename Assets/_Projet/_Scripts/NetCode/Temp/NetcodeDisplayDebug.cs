using GameNetwork.Utils;
using TMPro;
using UnityEngine;

public class NetcodeDisplayDebug : MonoBehaviour
{

    [SerializeField] ConnectionHandlerNew connectionHandler;
    [SerializeField] TMP_Text debugText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        debugText.text = $"IP {ClientTransportHelper.CurrentIP}, Port {ClientTransportHelper.CurrentPort}";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
