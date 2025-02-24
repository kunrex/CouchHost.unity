using System.Collections.Concurrent;

using UnityEngine;

using Hosting.Services;

namespace Hosting
{
    public sealed class Logger : Singleton<Logger>
    {
        private static ConcurrentQueue<(int, string)> messages = new ConcurrentQueue<(int, string)>();

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

        public void Log(string message)
        {
            messages.Enqueue((0, message));
        }
        
        public void LogWarning(string message)
        {
            messages.Enqueue((1, message));
        }
        
        public void LogError(string message)
        {
            messages.Enqueue((2, message));
        }

        void Update()
        {
            if (messages.TryDequeue(out var action))
            {
                switch (action.Item1)
                {
                    case 0:
                        Debug.Log(action.Item2);
                        break;
                    case 1:
                        Debug.LogWarning(action.Item2);
                        break;
                    default:
                        Debug.LogError(action.Item2);
                        break;
                }
            }
        }
    }
}