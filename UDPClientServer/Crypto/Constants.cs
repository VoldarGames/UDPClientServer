using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDPClientServer.Crypto
{
    public static class Constants
    {
        public const int EncryptedCommunicationNegotiationPortServer = 11001;
        public const int EncryptedCommunicationNegotiationPortClient = 11002;
        public const string StartNegotiation = nameof(StartNegotiation);
        public const string EndNegotiation = nameof(EndNegotiation);
    }
}
