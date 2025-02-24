using Hosting.Unity;

using UnityEngine;

public sealed class ClientManager : UnityClientManager
{
    [SerializeField] private UnityClientObject playerPrefab;
    
    protected override void OnServerStart()
    {
        Debug.Log($"Server started at {RoomCode}");
    }

    protected override void OnStartRuntime()
    {
        Debug.Log($"Runtime started for Room: {RoomCode}, no new connections will be accepted");
    }

    protected override void OnStopRuntime()
    {
        Debug.Log($"Runtime stopped for Room: {RoomCode}, connections will now be accepted");
    }

    protected override void OnClientConnect(UnityClientObject client)
    {
        Debug.Log($"Runtime started for Room: {RoomCode}, no new connections will be accepted");
    }

    protected override void OnClientDisconnect(UnityClientObject client)
    {
        Debug.Log($"Runtime started for Room: {RoomCode}, no new connections will be accepted");
    }

    protected override UnityClientObject InstantiateClientObject(string hash)
    {
        return Instantiate(playerPrefab);
    }
}
