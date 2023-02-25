using SampleApp.ServiceEngineSample.Model;

namespace SampleApp.ServiceEngineSample.EventContainers
{
    internal class ServiceEngineContactsUpdatedEventArgs: EventArgs
    {
        public ServiceContact[] Contacts { get; set; }
    }
}
