namespace Hosting.Interfaces
{
    public interface IRuntimeManagement
    {
        public bool IsRunning { get;  }
        
        public void StartRuntime();
        public void StopRuntime();
    }
}