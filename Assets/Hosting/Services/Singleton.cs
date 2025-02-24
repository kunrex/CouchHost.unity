using UnityEngine;

namespace Hosting.Services
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        protected static T instance;
        public static T Instance { get => instance; }

        protected abstract void Awake();
    }
}