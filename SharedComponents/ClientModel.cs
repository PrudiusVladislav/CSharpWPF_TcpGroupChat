using System.Net.Sockets;

namespace SharedComponents;

public class ClientModel
{
    public TcpClient Client { get; set; }
    public string UserName { get; set; }
    public int DbClientId { get; set; }

    public ClientModel(TcpClient client, string username, int dbClientId)
    {
        Client = client;
        UserName = username;
        DbClientId = dbClientId;
    }
}