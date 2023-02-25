namespace SampleApp.ServiceEngineSample
{
    public enum ServiceEngineCommand
    {
        contactsGet,
        contactSet,
        contactDelete
    }

    public static class ServiceEngineCommandExtensions
    {
        public static string ToProtocolString(this ServiceEngineCommand command)
        {
            switch (command)
            {
                case ServiceEngineCommand.contactsGet:
                    return "contacts.get";
                case ServiceEngineCommand.contactSet:
                    return "contact.set";
                case ServiceEngineCommand.contactDelete:
                    return "contact.delete";
                default:
                    return string.Empty;
            }
        }
    }
}
