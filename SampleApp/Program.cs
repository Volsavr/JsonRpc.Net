// See https://aka.ms/new-console-template for more information

using SampleApp.ServiceEngineSample;
using System.Diagnostics;

var engine = new ServiceEngine("test_key");

engine.Connected += (obj, eventArgs) => Debug.WriteLine("Connected");
engine.Disconnected += (obj, eventArgs) => Debug.WriteLine("Connected");
engine.ContactsDeleted += (obj, eventArgs) => Debug.WriteLine("Contacts Deleted");
engine.ContactsUpdated += (obj, eventArgs) => Debug.WriteLine("Contacts Updated");

var connectionStatus = await engine.ConnectAsync();

if(connectionStatus)
{
    var contacts = await engine.GetContactsAsync();
    Debug.WriteLine("Contacts Loaded");
}
