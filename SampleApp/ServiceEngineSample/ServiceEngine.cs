using JsonRpc.Net;
using Newtonsoft.Json;
using SampleApp.ServiceEngineSample.EventContainers;
using SampleApp.ServiceEngineSample.Model;
using System.Diagnostics;
using WebSocket4Net;

namespace SampleApp.ServiceEngineSample
{
    internal class ServiceEngine
    {
        #region Constants
        private const string UserAgent = "myApp";
        private const string SocketUri = "wss://mapi.sample.service/auth";
        private const int CommandTimeout = 10000;
        #endregion

        #region Fields
        private static bool _isConnected;
        private WebSocket4Net.WebSocket _webSocket;
        private JsonRpcClient _jsonRpcClient;
        private readonly string _key;
        private static ManualResetEvent _connectionResetEvent = new ManualResetEvent(false);
        #endregion

        #region Constructor
        internal ServiceEngine(String key)
        {
            _key = key;
        }
        #endregion

        #region Public Methods
        public async Task<bool> ConnectAsync()
        {
            if (_isConnected)
                return true;

            //prepare header
            List<KeyValuePair<String, String>> customHeaderItems = new List<KeyValuePair<string, string>>();
            customHeaderItems.Add(new KeyValuePair<string, string>("API-KEY", _key));

            //init websocket
            _webSocket = new WebSocket4Net.WebSocket(SocketUri, "", null, customHeaderItems, UserAgent, "", WebSocketVersion.Rfc6455);
            _webSocket.EnableAutoSendPing = true;
            _webSocket.Security.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            _webSocket.Opened += WebSocket_Opened;
            _webSocket.Closed += WebSocket_Closed;
            _webSocket.Error += WebSocket_Error;

            //init JsonRpcClient
            _jsonRpcClient = new JsonRpcClient(_webSocket);
            _jsonRpcClient.EventReceived += _jsonRpcClient_EventReceived;

            //open connection
            _webSocket.Open();

            await Task.Run(() =>
            {
                _connectionResetEvent.WaitOne();
                _connectionResetEvent.Reset();
            });

            return _isConnected;
        }

        public async Task<bool> CloseAsync()
        {
            if (_webSocket != null && _isConnected)
                _webSocket.Close();

            await Task.Run(() =>
            {
                _connectionResetEvent.WaitOne();
                _connectionResetEvent.Reset();
            });

            return _isConnected;
        }

        public async Task<ServiceContact[]> GetContactsAsync()
        {
            var contacts = await Task<ServiceContact[]>.Factory.StartNew(() =>
            {
                var prm = new
                {};

                var request = _jsonRpcClient.CreateRequest(ServiceEngineCommand.contactsGet.ToProtocolString(), prm);
                return _jsonRpcClient.SendRequest<ServiceContact[]>(request, CommandTimeout);
            });

            return contacts;
        }

        public async Task<ServiceObjectUpdatedeConfirmation> UpdateContactAsync(ServiceContact contact)
        {
            var contactUpdatedConfirmation = await Task<ServiceObjectUpdatedeConfirmation>.Factory.StartNew(() =>
            {
                var request = _jsonRpcClient.CreateRequest(ServiceEngineCommand.contactSet.ToProtocolString(), contact);
                return _jsonRpcClient.SendRequest<ServiceObjectUpdatedeConfirmation>(request, CommandTimeout);
            });

            return contactUpdatedConfirmation;
        }

        public async Task<ServiceObjectsDeletedConfirmation> DeleteContactAsync(string contactId)
        {
            var contactsDeletedConfirmation = await Task<ServiceObjectsDeletedConfirmation>.Factory.StartNew(() =>
            {
                var prm = new
                {
                    id = new string[] { contactId }
                };

                var request = _jsonRpcClient.CreateRequest(ServiceEngineCommand.contactDelete.ToProtocolString(), prm);
                return _jsonRpcClient.SendRequest<ServiceObjectsDeletedConfirmation>(request, CommandTimeout);
            });

            return contactsDeletedConfirmation;
        }

        #endregion

        #region Events
        public event EventHandler<ServiceEngineObjectsDeletedEventArgs> ContactsDeleted;
        public event EventHandler<ServiceEngineContactsUpdatedEventArgs> ContactsUpdated;
        public event EventHandler<ServiceEngineDisconnectedEventArgs> Disconnected;
        public event EventHandler<EventArgs> Connected;
        #endregion

        #region Event Handlers
        private void WebSocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Debug.WriteLine(e.Exception.Message);
            _isConnected = false;
            _connectionResetEvent.Set();

            _jsonRpcClient.EventReceived -= _jsonRpcClient_EventReceived;
            //notify about disconnection
            Disconnected?.Invoke(this, new ServiceEngineDisconnectedEventArgs() { Error = e.Exception });
        }

        private void WebSocket_Closed(object sender, EventArgs e)
        {
            Debug.WriteLine(nameof(WebSocket_Closed));
            _isConnected = false;
            _connectionResetEvent.Set();
            _webSocket.Dispose();
            _jsonRpcClient.EventReceived -= _jsonRpcClient_EventReceived;

            //notify about disconnection
            Disconnected?.Invoke(this, new ServiceEngineDisconnectedEventArgs());
        }

        private void WebSocket_Opened(object sender, EventArgs e)
        {
            Debug.WriteLine(nameof(WebSocket_Opened));
            _isConnected = true;
            _connectionResetEvent.Set();

            if (Connected != null)
                Connected(this, new EventArgs());
        }

        private void _jsonRpcClient_EventReceived(object sender, JsonRpcEventReceivedEventArgs e)
        {
            switch (e.EventName.ToServiceEngineEvent())
            {
                case ServiceEngineEvent.contactsUpdated:
                    var eventData = JsonConvert.DeserializeObject<ServiceContact[]>(e.Data.ToString());
                    ContactsUpdated?.Invoke(this, new ServiceEngineContactsUpdatedEventArgs() { Contacts = eventData.data });
                    break;

                case ServiceEngineEvent.contactsDeleted:
                    var deleteEventData = JsonConvert.DeserializeObject<ServiceObjectsDeletedConfirmation>(e.Data.ToString());
                    ContactsDeleted?.Invoke(this, new ServiceEngineObjectsDeletedEventArgs() { Ids = deleteEventData.id });
                    break;

                default:
                    Debug.WriteLine($"Unknown event received: {e.EventName} with data: {e.Data}");
                    break;
            }
        }
        #endregion
    }
}
