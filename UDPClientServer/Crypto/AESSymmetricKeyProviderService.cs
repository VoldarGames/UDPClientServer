using System.Security.Cryptography;
using System.Text;

namespace UDPClientServer.Crypto
{
    public class AESSymmetricKeyProviderService
    {
        private readonly Aes _aes;
        public AESKeyGenerationParameters AESKeyGenerationParameters { get; init; }

        public ICryptoTransform Encryptor { get; private set; }
        public ICryptoTransform? Decryptor { get; private set; }
        public AESSymmetricKeyProviderService(int keySize = 256, byte[]? key = default, byte[]? iv = default)
        {
            _aes = Aes.Create();
            _aes.KeySize = keySize;
            GenerateKey(key);
            GenerateIV(iv);
            AESKeyGenerationParameters = new AESKeyGenerationParameters(_aes.Key, _aes.IV);
            Encryptor = CreateEncryptor();
        }

        public void SetDecryptor(byte[] key)
        {
            var decryptorAESKeyGenerationParameters = AESKeyGenerationParameters with { Key = key };
            Decryptor = _aes.CreateDecryptor(decryptorAESKeyGenerationParameters.Key, decryptorAESKeyGenerationParameters.IV);
        }

        public byte[] Encrypt(string data, int length = -1)
        {
            return Encrypt(Encoding.UTF8.GetBytes(data), length);
        }

        public byte[] Encrypt(byte[] data, int length = -1)
        {
            return Encryptor.TransformFinalBlock(data, 0,
                length <= -1 || length >= data.Length
                ? data.Length
                : length);
        }

        public string DecryptAsString(byte[] data, int length = -1)
        {
            return Encoding.UTF8.GetString(DecryptAsBytes(data, length));
        }

        public byte[] DecryptAsBytes(byte[] data, int length = -1)
        {
            return Decryptor.TransformFinalBlock(data, 0,
                length <= -1 || length >= data.Length
                ? data.Length
                : length);
        }

        private ICryptoTransform CreateEncryptor()
        {
            return _aes.CreateEncryptor(AESKeyGenerationParameters.Key, AESKeyGenerationParameters.IV);
        }

        private void GenerateIV(byte[]? iv)
        {
            if (iv == default)
            {
                _aes.GenerateIV();
            }
            else
            {
                _aes.IV = iv;
            }
        }

        private void GenerateKey(byte[]? key)
        {
            if (key == default)
            {
                _aes.GenerateKey();
            }
            else
            {
                _aes.Key = key;
            }
        }
    }
}
