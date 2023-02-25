namespace JsonRpc.Net
{
    public class JsonRpcEventReceivedEventArgs: EventArgs
    {
        public string EventName { get; internal set; }
        public dynamic Data { get; internal set; }
    }
}
