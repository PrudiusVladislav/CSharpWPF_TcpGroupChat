using System.Net;
using System.Net.Sockets;
using System.Text;
using SharedComponents;

namespace CSharpConsole_TcpChat.Server;

public class Server
{
    private TcpListener listener;
    private ClientsRepository clientsRepositoryInstance;
    public Server()
    {
        listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
        clientsRepositoryInstance = ClientsRepository.Instance;
        listener.Start();
        Console.WriteLine($"[{DateTime.Now}] SERVER started");
    }

    public async Task HandleUsersAsync()
    {
        try
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                var stream = client.GetStream();
                var buffer = new byte[1024];
                var receivedBytes = await stream.ReadAsync(buffer);
                var receivedUsername = Encoding.UTF8.GetString(buffer, 0,receivedBytes);
                var acceptedUser = new ClientModel(client, receivedUsername);
                clientsRepositoryInstance.AddClient(acceptedUser);
                Console.WriteLine($"[{DateTime.Now}] user with username {acceptedUser.UserName} has connected");
                HandleOneUserAsync(acceptedUser);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] Error occurred while handling clients: {ex.Message}");
            await BroadCastMessageAsync(new MessageModel("SERVER", DateTime.Now, ex.Message));
        }
        finally
        {
            listener.Stop();
            Console.WriteLine($"[{DateTime.Now}] SERVER stopped");
        }
    }

    private async Task HandleOneUserAsync(ClientModel user)
    {
        try
        {
            while (true)
            {
                var stream = user.Client.GetStream();
                var buffer = new byte[1024];
                var receivedBytes = await stream.ReadAsync(buffer);
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0,receivedBytes);
                if (receivedMessage.Equals(MessageModel.ExitMessage))
                {
                    break;
                }
                
                Console.WriteLine($"[{DateTime.Now}] user with username {user.UserName} has sent message: {receivedMessage}");
                BroadCastMessageAsync(new MessageModel(user.UserName, DateTime.Now, receivedMessage));
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] Error occurred while handling {user.UserName}: {ex.Message}");
            //await BroadCastMessageAsync(new MessageModel("SERVER", DateTime.Now, ex.Message));
        }
        finally
        {
            await user.Client.GetStream().WriteAsync(Encoding.UTF8.GetBytes(MessageModel.ExitMessageResponse));
            clientsRepositoryInstance.RemoveClient(user);
            user.Client.Close();
            Console.WriteLine($"[{DateTime.Now}] user with username {user.UserName} has disconnected");
        }
    }

    private async Task BroadCastMessageAsync(MessageModel message)
    {
        var tasks = new List<Task>();
        foreach (var user in clientsRepositoryInstance.GetConnectedClients())
        {
            tasks.Add(Task.Run(async () =>
            {
                var stream = user.Client.GetStream();
                await stream.WriteAsync(Encoding.UTF8.GetBytes(message.ToString()));
            }));
        }
        await Task.WhenAll(tasks);
    }
}