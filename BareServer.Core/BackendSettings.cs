namespace BareServer.Core
{
    public class BackendSettings
    {
        public int MaxSend { get; set; } = 4096;
        public int MaxRecv { get; set; } = 4096;
    }
}