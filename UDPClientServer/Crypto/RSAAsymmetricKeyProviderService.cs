using System.Security.Cryptography;

namespace UDPClientServer.Crypto
{
    public class RSAAsymmetricKeyProviderService
    {
        private readonly RSACryptoServiceProvider _rsa;

        public PrivatePublicRSAKeys PrivatePublicRSAKeys { get; init; }
        public RSAAsymmetricKeyProviderService(string? privateKey = default, string? publicKey = default, int dwKeySize = 2048)
        {
            _rsa = new RSACryptoServiceProvider(dwKeySize);
            PrivatePublicRSAKeys = CreatePrivatePublicKeys(privateKey, publicKey);
        }

        private PrivatePublicRSAKeys CreatePrivatePublicKeys(string? privateKey, string? publicKey)
        {
            privateKey ??= _rsa.ToXmlString(true);
            publicKey ??= _rsa.ToXmlString(false);

            return new PrivatePublicRSAKeys(privateKey, publicKey);
        }

        public byte[] Encrypt(byte[] data)
        {
            _rsa.FromXmlString(PrivatePublicRSAKeys.PublicKey);
            return _rsa.Encrypt(data, false);
        }

        public byte[] Decrypt(byte[] data)
        {
            _rsa.FromXmlString(PrivatePublicRSAKeys.PrivateKey);
            return _rsa.Decrypt(data, false);
        }
    }
}
