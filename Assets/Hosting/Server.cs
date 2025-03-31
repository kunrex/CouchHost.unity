using System;

using System.Net;
using System.Net.Sockets;

using System.Text;

using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

using Hosting.Enums;
using Hosting.Unity;
using Hosting.Services;
using Hosting.Interfaces;

using Task = System.Threading.Tasks.Task;

namespace Hosting 
{
    public sealed class Server : Singleton<Server>, IRuntimeManagement
    {
        private bool isRunning;
        public bool IsRunning { get => isRunning; }
        
        private ServerStateEnum state;
        public ServerStateEnum State { get => state; }
        
        private TcpListener listener;
        private Thread serverThread;

        private string roomCode;
        public string RoomCode { get => roomCode; }
        
        protected override void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            isRunning = false;
            state = ServerStateEnum.Standby;
            
            StartServer();
        }

        private void StartServer()
        {
            if (isRunning)
                return;
            
            isRunning = true;
            serverThread = new Thread(ServerLoop);
            
            serverThread.IsBackground = true;
            serverThread.Start();
        }

        private async void ServerLoop()
        {
            var localIp = Extensions.GetLocalIPAddress();
            if (localIp.Item1 == null)
            {
                Logger.Instance.LogError("Couldn't start server, localIP not found...");
                return;
            }
            
            if (UnityClientManager.Instance == null)
            {
                Logger.Instance.LogWarning("No Unity Client Manager in scene... or is not assigned in inspector. Cannot accept new connections without Client Manager.");
                return;
            }
            
            listener = new TcpListener(IPAddress.Parse(localIp.Item1), 7777);
            listener.Start();
            
            roomCode = localIp.Item2;
            
            Logger.Instance.Log($"Server successfully started on  {localIp.Item1}:7777. Room Code: {roomCode}");
            UnityClientManager.Instance.StartManaging();

            while (isRunning)
            {
                var client = await listener.AcceptTcpClientAsync();

                if (await TryAcceptWebsocketHandshake(client))
                {
                    _ = Task.Run(() => HandleNewClient(client));
                }
            }
        }

        private async Task<bool> TryAcceptWebsocketHandshake(TcpClient client)
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];
            var request = Encoding.UTF8.GetString(buffer, 0, stream.Read(buffer, 0, buffer.Length));

            bool result;
            byte[] responseBytes;
            if (Extensions.IsWebSocketRequest(request, out var websocketKey))
            {
                Extensions.ComputeWebsocketAcceptResponse(websocketKey, out var websocketResponse);
                responseBytes = Encoding.UTF8.GetBytes(websocketResponse);
                result = true;
            }
            else
            {
                responseBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new { code = 400 }));
                result = false;
            }

            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            await stream.FlushAsync();
            return result;
        }

        private async Task HandleNewClient(TcpClient client)
        {
            var hash = Extensions.ComputeConnectionHash((IPEndPoint)client.Client.LocalEndPoint);
            
            if (state != ServerStateEnum.Standby)
            {
                Logger.Instance.LogWarning($"Received new connection from: [{hash}] while not on STANDBY.");
                await WaitCloseInvalidSocket(client);
                
                UnityClientManager.Instance.InvalidConnectionCall();
                return;
            }
            
            if (UnityClientManager.Instance.ReachedLimit)
            {
                Logger.Instance.LogWarning($"Received new connection from: [{hash}] while on connection limit.");
                await WaitCloseExtraSocket(client);
                
                UnityClientManager.Instance.InvalidConnectionCall();
                return;
            }
            
            await ManageNewSocket(hash, client);
        }
        
        private async Task ManageNewSocket(string hash, TcpClient client)
        {
            bool serverClose = true;
            
            await using (var stream = client.GetStream())
            {
                try
                {
                    var unityClient = UnityClientManager.instance.PushClientConnection(hash);
                
                    if (unityClient == null)
                    {
                        await WaitCloseDuplicateSocket(client);
                        return;
                    }
                
                    var buffer = new byte[1024];

                    while (true)
                    {
                        Extensions.ReadWebSocketMessage(stream, out var opCode, out buffer);

                        if (opCode == Extensions.WebsocketCloseCode)
                        {
                            serverClose = false;
                            break;
                        }
                        
                        unityClient.EnqueueControllerData(Extensions.Deserialize(0, buffer));
                    }
                }
                catch (Exception e)
                {
                    Logger.Instance.LogError($"Exception: `{e.Message}` while managing connection for client: `{hash}`");
                }
                finally
                {
                    UnityClientManager.instance.PushClientDisconnection(hash);
                    Logger.Instance.Log($"Client: `{hash}` disconnected successfully");

                    if (serverClose)
                    {
                        byte[] closeFrame = { 0b10001000, 0 };
                        
                        await stream.WriteAsync(closeFrame, 0, closeFrame.Length);
                        await stream.FlushAsync();
                    }
                }
            }
        }

        private async Task WaitCloseDuplicateSocket(TcpClient client)
        {
            await using (var stream = client.GetStream())
            {
                try
                {
                    await Extensions.SendWebSocketMessage(stream, "Internal Server Error");
                
                    if (client.Connected)
                        client.Client.Shutdown(SocketShutdown.Both);
                }
                catch (Exception e)
                {
                    Logger.Instance.LogError($"Exception: `{e.Message}` while waiting to close socket.");
                }
                finally
                {
                    client.Close();
                }
            }
        }
        
        private async Task WaitCloseInvalidSocket(TcpClient client)
        {
            await using (var stream = client.GetStream())
            {
                try
                {
                
                    await Extensions.SendWebSocketMessage(stream, "Server on maximum occupancy.");
                
                    if (client.Connected)
                        client.Client.Shutdown(SocketShutdown.Both);
                }
                catch (Exception e)
                {
                    Logger.Instance.LogError($"Exception: `{e.Message}` while waiting to close socket.");
                }
                finally
                {
                    client.Close();
                }
            }
        }

        private async Task WaitCloseExtraSocket(TcpClient client)
        {
            await using (var stream = client.GetStream())
            {
                try
                {
                    await Extensions.SendWebSocketMessage(stream, "Server on maximum occupancy.");
                
                    if (client.Connected)
                        client.Client.Shutdown(SocketShutdown.Both);
                }
                catch (Exception e)
                {
                    Logger.Instance.LogError($"Exception: `{e.Message}` while waiting to close socket.");
                }
                finally
                {
                    client.Close();
                }
            }
        }

        public void StartRuntime()
        {
            state = ServerStateEnum.Runtime;
        }

        public void StopRuntime()
        {
            state = ServerStateEnum.Standby;
        }
        
        void OnApplicationQuit()
        {
            isRunning = false;
            
            listener?.Stop();
            serverThread?.Abort();
        }
    }
}
