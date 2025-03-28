using System;
using System.Threading;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class ServerConsoleIO : SystemBase
{
    private Thread _inputThread;
    private CancellationTokenSource _cancellationTokenSource;
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>();
        _cancellationTokenSource = new CancellationTokenSource();
        _inputThread = new Thread(() => ReadTerminalInput(_cancellationTokenSource.Token));
        _inputThread.Start();
    }
    protected override void OnUpdate()
    {
        
    }

    protected override void OnDestroy()
    {
        _cancellationTokenSource.Cancel();
        _inputThread.Join();
        _cancellationTokenSource.Dispose();
    }

    private void ReadTerminalInput(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    Debug.Log($"[Server Terminal]: {input}");
                    //ServerUtils.LogTerminal(ServerUtils.LogType.Info, $"[Server Terminal]: {input}");
                }
            }

            Thread.Sleep(100);
        }
        Debug.Log("[Server Terminal] Input thread exited.");
    }
}
