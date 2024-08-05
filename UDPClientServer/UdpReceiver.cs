// See https://aka.ms/new-console-template for more information
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Diagnostics;
using UDPClientServer.Crypto;
using System.Threading;

class UdpReceiver : IDisposable
{
    private readonly UdpClient _listener;
    private IPEndPoint _groupEndPoint;
    private bool _started = false;
    private RSAAsymmetricKeyProviderService? _rsaAsymmetricKeyProviderService;
    private AESSymmetricKeyProviderService? _aesSymmetricKeyProviderService;
    private bool _encryptionNegotiationInProgress = true;
    private byte[]? _aesKey;
    private byte[]? _aesIV;
    bool _rsaSent = false;

    public UdpReceiver(int port)
    {
        _listener = new UdpClient(port) { 
            DontFragment = true
        };
        _groupEndPoint = new IPEndPoint(IPAddress.Any, port);
    }

    public void Dispose()
    {
        _listener?.Close();
        _listener?.Dispose();
    }

    public void StartAsync(bool cypherCommunication, CancellationToken cancellationToken)
    {
        if (_started)
            return;

        cancellationToken.Register(Stop);

        Task.Run(() => {
            try
            {
                _started = true;
                if (cypherCommunication)
                {
                    _rsaAsymmetricKeyProviderService = new RSAAsymmetricKeyProviderService();
                    using var negotiationListener = new UdpClient(Constants.EncryptedCommunicationNegotiationPortServer);
                    var negotiationGroupEndPoint = new IPEndPoint(IPAddress.Any, Constants.EncryptedCommunicationNegotiationPortServer);
                    using var udpSender = new UdpClient();
                    Console.WriteLine("Waiting for negotiation");

                    while (_encryptionNegotiationInProgress)
                    {
                        byte[] bytes = negotiationListener.Receive(ref negotiationGroupEndPoint);
                        var messageReceived = Encoding.ASCII.GetString(bytes, 0, bytes.Length);

                        Console.WriteLine($"Negotiation message received: {messageReceived}");
                        Console.WriteLine($"Negotiation message received base64 {Convert.ToBase64String(bytes)}");

                        if (_rsaSent && _aesKey == default)
                        {
                            _aesKey = _rsaAsymmetricKeyProviderService.Decrypt(bytes);
                        }
                        else if (_rsaSent && _aesKey != default && _aesIV == default)
                        {
                            _aesIV = _rsaAsymmetricKeyProviderService.Decrypt(bytes);
                            _aesSymmetricKeyProviderService = new AESSymmetricKeyProviderService(key: _aesKey, iv: _aesIV);
                            _aesSymmetricKeyProviderService.SetDecryptor(_aesKey);
                            _encryptionNegotiationInProgress = false;
                            var endNegotiation = Encoding.ASCII.GetBytes(Constants.EndNegotiation);
                            udpSender.Send(endNegotiation, endNegotiation.Length, new IPEndPoint(negotiationGroupEndPoint.Address, Constants.EncryptedCommunicationNegotiationPortClient));
                        }

                        if (messageReceived == Constants.StartNegotiation)
                        {
                            var rsaPublicKey = Encoding.ASCII.GetBytes(_rsaAsymmetricKeyProviderService.PrivatePublicRSAKeys.PublicKey);
                            udpSender.Send(rsaPublicKey, rsaPublicKey.Length, new IPEndPoint(negotiationGroupEndPoint.Address, Constants.EncryptedCommunicationNegotiationPortClient));
                            _rsaSent = true;
                        }
                    }
                }
                while (true)
                {
                    Console.WriteLine("Waiting for broadcast");
                    byte[] bytes = _listener.Receive(ref _groupEndPoint);

                    Console.WriteLine($"Received broadcast from {_groupEndPoint} :");
                    var messageReceived = cypherCommunication && _aesKey != default && _aesIV != default
                    ? _aesSymmetricKeyProviderService!.DecryptAsString(bytes)
                    : Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    Console.WriteLine(messageReceived);

                    if (messageReceived.StartsWith("open"))
                    {
                        ExecuteCommand(messageReceived);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                _started = false;
                StartAsync(cypherCommunication, cancellationToken);
            }
        }, cancellationToken);
    }

    public void Stop()
    {
        Dispose();
    }

    private static void ExecuteCommand(string messageReceived)
    {
        var messageParts = messageReceived
                        .Split(' ')
                        .Skip(1);
        var command = messageParts.First();
        var arguments = messageParts.Skip(1).ToList();

        var processInfo = new ProcessStartInfo { FileName = command };
        foreach (var argument in arguments)
        {
            processInfo.ArgumentList.Add(argument);
        }

        var process = Process.Start(processInfo);
    }    
}