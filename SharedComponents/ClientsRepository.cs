using System.Collections.ObjectModel;

namespace SharedComponents;

public class ClientsRepository
{
    public static ClientsRepository Instance { get; } = new ClientsRepository();

    private readonly List<ClientModel> connectedClients = new List<ClientModel>();

    public event Action<ClientModel>? ClientAdded;
    public event Action<ClientModel>? ClientRemoved;

    public void AddClient(ClientModel client)
    {
        lock (connectedClients)
        {
            connectedClients.Add(client);
        }
        OnClientAdded(client);
    }

    public void RemoveClient(ClientModel client)
    {
        lock (connectedClients)
        {
            connectedClients.Remove(client);
        }
        OnClientRemoved(client);
    }

    public List<ClientModel> GetConnectedClients()
    {
        lock (connectedClients)
        {
            return connectedClients.ToList();
        }
    }

    private void OnClientAdded(ClientModel client)
    {
        //if(ClientAdded == null)
        //    Console.WriteLine("ClientsAdded == null");
        ClientAdded?.Invoke(client);
    }

    private void OnClientRemoved(ClientModel client)
    {
        ClientRemoved?.Invoke(client);
    }
}
