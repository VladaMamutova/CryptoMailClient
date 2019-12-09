using System;

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

        public void Set(ProtocolConfig protocolConfig)
        {
            Server = protocolConfig.Server;
            Port = protocolConfig.Port;
            UseSslTsl = protocolConfig.UseSslTsl;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ProtocolConfig))
            {
                return false;
            }

            ProtocolConfig protocolConfig = (ProtocolConfig)obj;

            return string.Compare(Server, protocolConfig.Server,
                       StringComparison.CurrentCulture) == 0 &&
                   Port == protocolConfig.Port &&
                   UseSslTsl == protocolConfig.UseSslTsl;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Server}:{Port}, {UseSslTsl}";
        }
    }
}