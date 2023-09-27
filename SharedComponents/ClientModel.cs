using System.Net.Sockets;

namespace SharedComponents;

public class ClientModel
{
    public TcpClient Client { get; set; }
    public string UserName { get; set; }

    public ClientModel(TcpClient client, string username)
    {
        Client = client;
        UserName = username;
    }
}