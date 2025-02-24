using UnityEngine;

using Hosting.Unity;

namespace DefaultNamespace
{
    public sealed class Client : UnityClientObject
    {
        protected override void OnStart()
        {
            Debug.Log("Hello World!");
        }
    }
}