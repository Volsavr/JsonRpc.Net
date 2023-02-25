namespace SampleApp.ServiceEngineSample.EventContainers
{
    public class ServiceEngineDisconnectedEventArgs : EventArgs
    {
        public Exception Error { get; internal set; }
    }
}
