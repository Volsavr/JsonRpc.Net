using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using WebSocket4Net;

namespace JsonRpc.Net
{
    /// <summary>
    /// Provides a means of calling JSON-RPC endpoints over a WebSocket connection.
    /// </summary>
    public class JsonRpcClient
    {
        #region Fields
        /// <summary>
        /// Used to keep track of the current request ID.
        /// </summary>
        private static int _requestId = 0;

        /// <summary>
        /// Used to keep track of server responses.
        /// </summary>
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<dynamic>> _responses
            = new ConcurrentDictionary<string, TaskCompletionSource<dynamic>>();

        private readonly WebSocket _webSocket;
        #endregion

        #region Constants
        private const byte PayloadVersion = 0;
        #endregion

        #region Properties
        /// <summary>
        /// Allows the maximum request ID value to be configured.
        /// </summary>
        public int MaximumRequestId { get; set; } = int.MaxValue;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="webSocket">The WebSocket channel to use</param>
        public JsonRpcClient(WebSocket webSocket)
        {
            _webSocket = webSocket;
            _webSocket.DataReceived += _webSocket_DataReceived;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates a JSON-RPC request for the specified method and parameters.
        /// </summary>
        /// <param name="method">The method name</param>
        /// <param name="parameters">The list of parameters to pass to the method</param>
        /// <returns><see cref="JsonRequest"/></returns>
        public JsonRpcRequest CreateRequest(String method, object parameters)
        {
            // Get the next available Request ID.
            int nextRequestId = Interlocked.Increment(ref _requestId);

            if (nextRequestId > MaximumRequestId)
            {
                // Reset the Request ID to 0 and start again.
                Interlocked.Exchange(ref _requestId, 0);

                nextRequestId = Interlocked.Increment(ref _requestId);
            }

            // Create and return the Request object.
            var request = new JsonRpcRequest(method, parameters, nextRequestId);

            return request;
        }

        /// <summary>
        /// Sends the specified request to the WebSocket server and gets the response.
        /// </summary>
        /// <typeparam name="TResult">The type of the expected result object</typeparam>
        /// <param name="request">The JSON-RPC request to send</param>
        /// <param name="timeout">The timeout (in milliseconds) for the request</param>
        /// <returns>The response result</returns>
        public TResult SendRequest<TResult>(JsonRpcRequest request, int timeout = 30000)
        {
            var tcs = new TaskCompletionSource<dynamic>();
            var requestId = request.Id;

            try
            {
                //prepare json
                var json = JsonConvert.SerializeObject(request);

                // Add the Request details to the Responses dictionary so that we have   
                // an entry to match up against whenever the response is received.
                _responses.TryAdd(Convert.ToString(requestId), tcs);

                //prepare binary data
                var payload = MessagePackSerializer.ConvertFromJson(json);
                var data = new byte[payload.Length + 1];
                payload.CopyTo(data, 1);
                data[0] = PayloadVersion;

                // Send the request to the server
                Debug.WriteLine($"Sending request: {json}");
                _webSocket.Send(data, 0, data.Length);
                Debug.WriteLine($"Request sent: {request.Id}");

                var task = tcs.Task;

                // Wait here until either the response has been received,
                // or we have reached the timeout limit.
                Task.WaitAll(new Task[] { task }, timeout);

                if (task.IsCompleted)
                {
                    // Throw an Exception if there was an error.
                    if (task.Result == null)
                        throw new Exception("result is empty");

                    if (task.Result.error != null)
                        throw new JsonRpcException((int)task.Result.error.code, (string)task.Result.error.message);

                    return JsonConvert.DeserializeObject<TResult>(task.Result.result.ToString());
                }
                else // Timeout response.
                {
                    Debug.WriteLine($"Client timeout of {timeout} milliseconds has expired, throwing TimeoutException");
                    throw new TimeoutException();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
            finally
            {
                // Remove the request/response entry in the 'finally' block to avoid leaking memory.
                _responses.TryRemove(Convert.ToString(requestId), out tcs);
            }
        }

        #endregion

        #region EventHandlers
        /// <summary>
        /// Processes data messages received over the WebSocket connection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _webSocket_DataReceived(object sender, WebSocket4Net.DataReceivedEventArgs e)
        {
            //Note: use https://kawanet.github.io/msgpack-lite/ to analyze data
            System.Diagnostics.Debug.WriteLine("WebSocket_DataReceived: " + String.Join(", ", e.Data));

            if (e.Data.Length == 0)
                return;

            byte version = e.Data[0];

            if (version != PayloadVersion)
                return;

            var payload = e.Data.Skip(1).ToArray();

            try
            {
                var json = MessagePackSerializer.ConvertToJson(payload);

                Debug.WriteLine($" {nameof(_webSocket_DataReceived)} message: {json}");

                dynamic data = JObject.Parse(json);

                if (data.error != null)
                {
                    OnErrorReceived(data);
                    return;
                }

                if (data.result != null)
                {
                    OnResultReceived(data);
                    return;
                }

                if (data.method != null)
                {
                    OnEventReceived(data);
                    return;
                }

                throw new Exception("Unknown command");

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void OnResultReceived(dynamic data)
        {
            // Set the response result.
            if (_responses.TryGetValue(Convert.ToString(data.id), out TaskCompletionSource<dynamic> tcs))
            {
                tcs.TrySetResult(data);
            }
            else
            {
                Debug.WriteLine($"Unexpected response received. ID: {data.id}");
            }
        }

        private void OnErrorReceived(dynamic data)
        {
            // Log the error details.
            Debug.WriteLine($"Error Message: {data.error.message}");
            Debug.WriteLine($"Error Code: {data.error.code}");
            Debug.WriteLine($"Error Data: {data.error.data}");


            // Set the response result.
            if (_responses.TryGetValue(Convert.ToString(data.id), out TaskCompletionSource<dynamic> tcs))
            {
                tcs.TrySetResult(data);
            }
            else
            {
                Debug.WriteLine($"Unexpected response received. ID: {data.id}");
            }
        }

        private void OnEventReceived(dynamic data)
        {
            EventReceived?.Invoke(this,
                new JsonRpcEventReceivedEventArgs()
                {
                    EventName = data.method,
                    Data = data.@params
                });
        }
        #endregion

        #region Events
        public event EventHandler<JsonRpcEventReceivedEventArgs> EventReceived;
        #endregion
    }
}