using System.Collections.Concurrent;

using UnityEngine;

using Hosting.Structs;

namespace Hosting.Unity
{
    public abstract class UnityClientObject : MonoBehaviour
    {
        private string hash;
        public string Hash { get => hash; }
        
        private ConcurrentQueue<ControllerData> inData;

        private Vector2 joyStickA;
        protected Vector2 JoyStickA { get => joyStickA; }
        
        private Vector2 joyStickB;
        protected Vector2 JoyStickB { get => joyStickB; }

        private bool upPressed;
        protected bool UpPressed { get => upPressed; }
        
        private bool downPressed;
        protected bool DownPressed { get => downPressed; }
        
        private bool rightPressed;
        protected bool RightPressed { get => rightPressed; }
        
        private bool leftPressed;
        protected bool LeftPressed { get => leftPressed; }

        private bool aPressed;
        protected bool APressed { get => aPressed; }
        
        private bool bPressed;
        protected bool BPressed { get => bPressed; }
        
        private bool xPressed;
        protected bool XPressed { get => xPressed; }
        
        private bool yPressed;
        protected bool YPressed { get => yPressed; }

        private void Start()
        {
            hash = null;
            inData = new ConcurrentQueue<ControllerData>();

            OnStart();
        }

        protected abstract void OnStart();
        
        public void EnqueueControllerData(ControllerData data)
        {
            inData.Enqueue(data);
        }

        public void SetHash(string value)
        {
            hash ??= value;
        }

        private void LateUpdate()
        {
            if (inData.TryDequeue(out var data))
            {
                joyStickA = data.joyStickA;
                joyStickB = data.joyStickB;

                var result = ControllerData.ExtractButtonData(data.directionButtons);
                upPressed = result.Item1;
                downPressed = result.Item2;
                rightPressed = result.Item3;
                leftPressed = result.Item4;
                
                result = ControllerData.ExtractButtonData(data.letterButtons);
                aPressed = result.Item1;
                bPressed = result.Item2;
                xPressed = result.Item3;
                yPressed = result.Item4;
            }
        }
    }
}