using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using SharedComponents;

namespace CSharpConsole_TcpChat.Server;

public class Server
{
    private readonly Dictionary<string, ClientModel> _clients = new Dictionary<string, ClientModel>();
    private readonly List<string> _messages = new List<string>();
    private readonly TcpListener listener;
    public Server()
    {
        listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
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
                
                var clientsJson = JsonConvert.SerializeObject(_clients.Keys.ToList());
                var messagesJson = JsonConvert.SerializeObject(_messages);
                var combinedJson = clientsJson + "||" + messagesJson;
                await stream.WriteAsync(Encoding.UTF8.GetBytes(combinedJson));
                
                _clients.Add(acceptedUser.UserName, acceptedUser);
                Console.WriteLine($"[{DateTime.Now}] user with username {acceptedUser.UserName} has connected");
                BroadCastTextMessageAsync(MessageModel.SystemMessageByteOption, $"{MessageModel.NewUserAddedMessage} {acceptedUser.UserName}");
                HandleOneUserAsync(acceptedUser);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] Error occurred while handling clients: {ex.Message}");
            var messageModel = new MessageModel("SERVER", DateTime.Now, ex.Message);
            _messages.Add(messageModel.ToString());
            await BroadCastMessageAsync(MessageModel.CommonMessageByteOption, messageModel);
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
                var bytesOption = await Task.Run(stream.ReadByte);
                var receivedBytes = await stream.ReadAsync(buffer);
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0,receivedBytes);
                if (bytesOption == MessageModel.SystemMessageByteOption && receivedMessage.Equals(MessageModel.ExitMessage))
                {
                    break;
                }
                
                ////////////////////////handle add requests with parameters (names) here
                
                Console.WriteLine($"[{DateTime.Now}] user with username {user.UserName} has sent message: {receivedMessage}");
                var messageModel = new MessageModel(user.UserName, DateTime.Now, receivedMessage);
                _messages.Add(messageModel.ToString());
                BroadCastMessageAsync(MessageModel.CommonMessageByteOption,messageModel);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] Error occurred while handling {user.UserName}: {ex.Message}");
            //await BroadCastMessageAsync(new MessageModel("SERVER", DateTime.Now, ex.Message));
        }
        finally
        {
            var stream = user.Client.GetStream();
            stream.WriteByte(MessageModel.SystemMessageByteOption);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(MessageModel.ExitMessageResponse));
            _clients.Remove(user.UserName);
            user.Client.Close();
            BroadCastTextMessageAsync(MessageModel.SystemMessageByteOption, $"{MessageModel.UserRemovedMessage} {user.UserName}");
            Console.WriteLine($"[{DateTime.Now}] user with username {user.UserName} has disconnected");
        }
    }

    
    private async Task BroadCastMessageAsync(byte messageType, MessageModel message)
    {
        var tasks = new List<Task>();
        foreach (var user in _clients.Values)
        {
            tasks.Add(Task.Run(async () =>
            {
                var stream = user.Client.GetStream();
                stream.WriteByte(messageType);
                await stream.WriteAsync(Encoding.UTF8.GetBytes(message.ToString()));
            }));
        }
        await Task.WhenAll(tasks);
    }
    
    private async Task BroadCastTextMessageAsync(byte messageType, string textMessage)
    {
        var tasks = new List<Task>();
        foreach (var user in _clients.Values)
        {
            tasks.Add(Task.Run(async () =>
            {
                var stream = user.Client.GetStream();
                stream.WriteByte(messageType);
                await stream.WriteAsync(Encoding.UTF8.GetBytes(textMessage));
            }));
        }
        await Task.WhenAll(tasks);
    }
}