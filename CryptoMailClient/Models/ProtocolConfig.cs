namespace CryptoMailClient.Models
{
    public struct ProtocolConfig
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public bool UseSslTsl { get; set; }

        public ProtocolConfig(string server, int port, bool useSslTsl = true)
        {
            Server = server;
            Port = port;
            UseSslTsl = useSslTsl;
        }
    }
}