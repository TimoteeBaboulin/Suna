using GameNetwork;
using GameNetwork.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using static Unity.NetCode.ClientServerBootstrap;

public class GameManager : MonoBehaviour
{
    private CancellationTokenSource loadingToken;
    private ConnectionHandlerNew connectionHandler;

    private void Start()
    {
        connectionHandler = FindFirstObjectByType<ConnectionHandlerNew>();
        loadingToken = new CancellationTokenSource();

    }

    public async Task Play()
    {
        await ClientTransportHelper.StartServicesAsync();
        await connectionHandler.Connect(loadingToken.Token);
    }


#if  UNITY_EDITOR || UNITY_SERVER
    private void OnApplicationQuit()
    {
        Debug.Log("[OnApplicationQuit] Application is quitting – disconnecting and unloading worlds.");
        //LoadUtils.ResetAllCharacterComponents();

        loadingToken.Cancel();

        if (RequestedPlayType == PlayType.ClientAndServer || RequestedPlayType == PlayType.Server)
        {
            PlayerHelpers.ClearTeams();
        }
    }
#endif
#if !UNITY_EDITOR && !UNITY_SERVER
    private async void OnApplicationQuit()
    {
        await LoadUtils.QuitAsync();
    }
#endif
}
