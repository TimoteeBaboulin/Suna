using TMPro;
using UnityEngine;

public class NetcodeDisplayDebug : MonoBehaviour
{

    [SerializeField] ConnectionHandlerNew connectionHandler;
    [SerializeField] TMP_Text debugText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        debugText.text = $"IP {connectionHandler.IP}, Port {connectionHandler.Port}";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
