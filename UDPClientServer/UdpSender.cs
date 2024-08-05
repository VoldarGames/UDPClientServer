// See https://aka.ms/new-console-template for more information
using System.Net.Sockets;
using System.Net;
using System.Text;
using UDPClientServer.Crypto;

class UdpSender : IDisposable
{
    private readonly string _serverIpAddress;
    private readonly UdpClient _sender;
    private readonly IPEndPoint _groupEndPoint;
    private readonly bool _cypherMessages;
    private readonly AESSymmetricKeyProviderService _aesSymmetricKeyProviderService;
    private RSAAsymmetricKeyProviderService? _rsaAsymmetricKeyProviderService;
    private byte[]? _encryptedAesKey;
    private byte[]? _encryptedAesIV;
    private bool _serverEncryptionNegotiationFinished = false;

    public UdpSender(string ipAddress, int port, bool cypherMessages)
    {
        _serverIpAddress = ipAddress;
        _sender = new UdpClient();
        _groupEndPoint = new IPEndPoint(IPAddress.Parse(_serverIpAddress), port);
        _cypherMessages = cypherMessages;
        _aesSymmetricKeyProviderService = new AESSymmetricKeyProviderService();
        
    }
    public void Dispose()
    {
        _sender?.Close();
    }

    public async Task SendMessage(string message)
    {
        if(_cypherMessages && !_serverEncryptionNegotiationFinished) 
        {
            await NegotiateEncryption();
        }
        try
        {
            if (_cypherMessages)
            {
                byte[]? encryptedBytes = _aesSymmetricKeyProviderService.Encrypt(message);
                await _sender.SendAsync(encryptedBytes, encryptedBytes.Length, _groupEndPoint);
            }
            else
            {
                byte[] bytes = Encoding.ASCII.GetBytes(message);
                await _sender.SendAsync(bytes, bytes.Length, _groupEndPoint);
            }
            
            
            Console.WriteLine("Message sent to the broadcast address");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            Dispose();
        }
    }

    private async Task NegotiateEncryption()
    {
        Console.WriteLine("Negotiation started.");
        var serverGroupEndpoint = new IPEndPoint(IPAddress.Parse(_serverIpAddress), Constants.EncryptedCommunicationNegotiationPortServer);
        await StartNegotiation(serverGroupEndpoint);

        using var udpNegotiationListener = new UdpClient(Constants.EncryptedCommunicationNegotiationPortClient);
        await GetRSAPublicKey(udpNegotiationListener);

        await Task.Delay(5000);
        await SendAESKeyGenerationParameters(serverGroupEndpoint);
        await EndNegotiation(udpNegotiationListener);
    }

    private async Task EndNegotiation(UdpClient udpNegotiationListener)
    {
        while (!_serverEncryptionNegotiationFinished)
        {
            var endNegotiationBytes = await udpNegotiationListener.ReceiveAsync();
            var endNegotiation = Encoding.ASCII.GetString(endNegotiationBytes.Buffer, 0, endNegotiationBytes.Buffer.Length);
            _serverEncryptionNegotiationFinished = endNegotiation == Constants.EndNegotiation;
        }

        Console.WriteLine("Negotiation finished.");
    }

    private async Task SendAESKeyGenerationParameters(IPEndPoint serverGroupEndpoint)
    {
        _encryptedAesKey = _rsaAsymmetricKeyProviderService.Encrypt(_aesSymmetricKeyProviderService.AESKeyGenerationParameters.Key);
        Console.WriteLine($"Sending AES Key {Convert.ToBase64String(_encryptedAesKey)}");
        await _sender.SendAsync(_encryptedAesKey, _encryptedAesKey.Length, serverGroupEndpoint);

        _encryptedAesIV = _rsaAsymmetricKeyProviderService.Encrypt(_aesSymmetricKeyProviderService.AESKeyGenerationParameters.IV);
        Console.WriteLine($"Sending AES IV {Convert.ToBase64String(_encryptedAesIV)}");
        await _sender.SendAsync(_encryptedAesIV, _encryptedAesIV.Length, serverGroupEndpoint);
    }

    private async Task GetRSAPublicKey(UdpClient udpNegotiationListener)
    {
        var rsaPublicKeyBytes = await udpNegotiationListener.ReceiveAsync();
        var rsaPublicKey = Encoding.ASCII.GetString(rsaPublicKeyBytes.Buffer, 0, rsaPublicKeyBytes.Buffer.Length);
        Console.WriteLine($"Received public RSA key {rsaPublicKey} from server {_serverIpAddress}");
        _rsaAsymmetricKeyProviderService = new RSAAsymmetricKeyProviderService(publicKey: rsaPublicKey);
    }

    private async Task StartNegotiation(IPEndPoint serverGroupEndpoint)
    {
        var requestPublicKeyBytes = Encoding.ASCII.GetBytes(Constants.StartNegotiation);
        await _sender.SendAsync(requestPublicKeyBytes, requestPublicKeyBytes.Length, serverGroupEndpoint);
    }
}
