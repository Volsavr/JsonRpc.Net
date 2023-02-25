namespace SampleApp.ServiceEngineSample
{
    public enum ServiceEngineEvent
    {
        contactsUpdated,
        contactsDeleted,
        unknown
    }

    public static class ServiceEngineEventExtensions
    {
        public static string ToProtocolString(this ServiceEngineEvent pbxEvent)
        {
            switch (pbxEvent)
            {
                case ServiceEngineEvent.contactsUpdated:
                    return "contacts.updated";
                case ServiceEngineEvent.contactsDeleted:
                    return "contacts.deleted";
                default:
                    return string.Empty;
            }
        }

        public static ServiceEngineEvent ToServiceEngineEvent(this string pbxEvent)
        {
            var values = (ServiceEngineEvent[]) Enum.GetValues(typeof(ServiceEngineEvent));

            foreach(var v in values)
            {
                if (v.ToProtocolString() == pbxEvent)
                    return v;
            }

            return ServiceEngineEvent.unknown;
        }
    }
}
