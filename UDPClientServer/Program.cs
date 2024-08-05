// See https://aka.ms/new-console-template for more information
using NatLib;

Console.WriteLine("Welcome to UDP Client/Server console program");
Console.WriteLine("Choose option:\n[1] Start server\n[2] Start client\n[3] Configure UPnP port mapping\n[Other] Exit");

var input = Console.ReadKey().KeyChar;

if (input == '1')
{
    Console.WriteLine("Server port: ");
    var port = Convert.ToInt32(Console.ReadLine());
    var server = new UdpReceiver(port);
    Console.WriteLine("Cypher communication?\n[1] Yes\n[Other] No\n:");
    var cypherCommunication = Console.ReadKey().KeyChar;
    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    CancellationToken cancellationToken = cancellationTokenSource.Token;
    server.StartAsync(cypherCommunication == '1', cancellationToken);

    Console.WriteLine("Press [Q] to stop the server");
    while (Console.ReadKey().KeyChar != 'Q')
    {
        Console.WriteLine("Press [Q] to stop the server");
        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")}Listening...");
    }
    cancellationTokenSource.Cancel();
}
else if (input == '2')
{
    Console.WriteLine("Server ip: ");
    var serverIp = Console.ReadLine();
    Console.WriteLine("Server port: ");
    var port = Convert.ToInt32(Console.ReadLine());
    Console.WriteLine("Cyphered communication?\n[1] Yes\n[Other] No\n:");
    var cypherCommunication = Console.ReadKey().KeyChar;
    var client = new UdpSender(serverIp!, port, cypherCommunication == '1');

    var message = string.Empty;
    while (message != "exit")
    {
        Console.WriteLine("Send message or type exit to quit: ");
        message = Console.ReadLine();
        await client.SendMessage(message!);
    }
}
else if(input == '3')
{
    Console.WriteLine("Server port: ");
    var port = Convert.ToInt32(Console.ReadLine());
    await NatHelper.CreateUPnPMapping(port, port, "Test UDP chat");
}