using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using System.Threading;

using UnityEngine;

using Hosting.Enums;

using Hosting.Services;
using Hosting.Interfaces;

namespace Hosting.Unity
{
    public abstract class UnityClientManager : Singleton<UnityClientManager>, IRuntimeManagement
    {
        [SerializeField] private uint maximumClients;
        public uint MaximumClients { get => maximumClients; }
        
        protected bool isRunning;
        public bool IsRunning { get => isRunning; }

        private ConcurrentQueue<Action> actions;
        private Dictionary<string, UnityClientObject> clients;
        
        public bool ReachedLimit { get => clients.Count == maximumClients; }
        
        public bool ServerRunning { get => Server.Instance.IsRunning; }
        
        public string RoomCode { get => Server.Instance.RoomCode; }
        public bool IsOnStandby { get => Server.Instance.State == ServerStateEnum.Standby; }

        protected override void Awake()
        {
            if (instance == null)
                instance = this;
            else
            {
                Destroy(this);
                return;
            }

            actions = new ConcurrentQueue<Action>();
            
            clients = new Dictionary<string, UnityClientObject>();
            
            if (maximumClients == 0)
            {
                Debug.LogWarning("Maximum client count is 0... setting it to 2");
                maximumClients = 2;
            }
        }

        public void InvalidConnectionCall()
        {
            actions.Enqueue(OnInvalidConnection);
        }
            
        protected virtual void OnInvalidConnection() { }
        
        public void StartManaging()
        {
            actions.Enqueue(OnServerStart);
        }

        protected abstract void OnServerStart();

        public void StartRuntime()
        {
            if (isRunning)
                return;
            
            actions.Enqueue(OnStartRuntime);
            
            isRunning = true;
            Server.Instance.StartRuntime();
        }

        protected abstract void OnStartRuntime();

        public void StopRuntime()
        {
            if (!isRunning)
                return;
            
            actions.Enqueue(OnStopRuntime);
            
            isRunning = false;
            Server.Instance.StopRuntime();
        }

        protected abstract void OnStopRuntime();

        protected abstract UnityClientObject InstantiateClientObject(string hash);
        
        public UnityClientObject PushClientConnection(string hash)
        {
            if (!clients.ContainsKey(hash))
            {
                UnityClientObject client = null;
                
                DispatchWaitForUnity(() =>
                {
                    client = InstantiateClientObject(hash);
                    clients.TryAdd(hash, client);

                    OnClientConnect(client);
                    client.SetHash(hash);
                });
                
                return client;
            }
            
            Logger.Instance.LogWarning($"Trying to instantiate UNITY OBJECT for client: `{hash}` more than once");
            return null;
        }
        
        protected abstract void OnClientConnect(UnityClientObject client);
        
        public void PushClientDisconnection(string hash)
        {
            if (clients.ContainsKey(hash))
            {
                DispatchWaitForUnity(() =>
                {
                    clients.Remove(hash, out var client);
                    OnClientDisconnect(client);
                });
            }
        }
        
        protected abstract void OnClientDisconnect(UnityClientObject client);

        private void DispatchWaitForUnity(Action action)
        {
            var handle = new ManualResetEvent(false);
            
            actions.Enqueue(() =>
            {
                action?.Invoke();
                handle.Set();
            });
            
            handle.WaitOne();
        }
        
        private void Update()
        {
            if (actions.TryDequeue(out var action))
                action?.Invoke();
        }
        
        public void OnDestroy()
        {
            if (isRunning)
            {
                isRunning = false;
                Server.Instance.StopRuntime();
            }

            if (instance == this)
                instance = null;
        }
    }
}