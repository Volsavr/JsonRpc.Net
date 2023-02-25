namespace SampleApp.ServiceEngineSample.EventContainers
{
    public class ServiceEngineObjectsDeletedEventArgs: EventArgs
    {
        public string[] Ids { get; set; }
    }
}
