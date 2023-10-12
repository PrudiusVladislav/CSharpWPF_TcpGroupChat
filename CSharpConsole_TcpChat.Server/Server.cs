using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedComponents;
using SharedComponents.EF_Models;

namespace CSharpConsole_TcpChat.Server;

public class Server
{
    private readonly Dictionary<string, ClientModel> _onlineClients = new Dictionary<string, ClientModel>();
    private readonly TcpListener listener;
    private ChatDbContextFactory _dbContextFactory;
    public Server()
    {
        _dbContextFactory = new ChatDbContextFactory();
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
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0,receivedBytes);
                var userName =
                    receivedMessage[..receivedMessage.IndexOf(MessageModel.MessageSeparator, StringComparison.Ordinal)];

                bool isClientNew = false;
                await using var dbContext = _dbContextFactory.CreateDbContext();
                var dbClientToConnect = await dbContext.Clients.FirstOrDefaultAsync(c => c.Username.Equals(userName));
                if (dbClientToConnect == null)
                {
                    var password =
                        receivedMessage[
                            (receivedMessage.IndexOf(MessageModel.MessageSeparator, StringComparison.Ordinal) + 1)..];
                    await dbContext.Clients.AddAsync(new Client() { Username = userName, Password = password });
                    await dbContext.SaveChangesAsync();
                    dbClientToConnect = await dbContext.Clients.FirstOrDefaultAsync(c => c.Username.Equals(userName));
                    isClientNew = true;
                }
                
                var acceptedUser = new ClientModel(client, userName, dbClientToConnect!.Id);
                _onlineClients.Add(acceptedUser.UserName, acceptedUser);
                Console.WriteLine($"[{DateTime.Now}] user with username {acceptedUser.UserName} has connected");
                if(isClientNew)
                    BroadCastTextMessageAsync(MessageModel.SystemMessageByteOption, $"{MessageModel.NewUserAddedMessage}{MessageModel.MessageSeparator}{acceptedUser.DbClientId}");
                HandleOneUserAsync(acceptedUser);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] Error occurred while handling clients: {ex.Message}");
            var messageModel = new MessageModel("SERVER", DateTime.Now, ex.Message);
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
                var messageCommand =
                    receivedMessage[..receivedMessage.IndexOf(MessageModel.MessageSeparator, StringComparison.Ordinal)];
                switch (messageCommand)
                {
                    case MessageModel.AddGroupRequestMessage:
                    {
                        if (bytesOption == MessageModel.SystemMessageByteOption)
                        {
                            var groupName = receivedMessage[(receivedMessage.IndexOf(MessageModel.MessageSeparator, StringComparison.Ordinal) + 1)
                                ..(receivedMessage.LastIndexOf(MessageModel.MessageSeparator, StringComparison.Ordinal))];
                            var clientCreatorId = receivedMessage[(receivedMessage.LastIndexOf(MessageModel.MessageSeparator,
                                        StringComparison.Ordinal) + 1)..];
                            
                            var newGroup = new Group() { GroupName = groupName };
                            await using (var dbContext = _dbContextFactory.CreateDbContext())
                            {
                                var clientCreator = await dbContext.Clients.FindAsync(clientCreatorId);
                                if (clientCreator != null)
                                {
                                    var clientsGroup = new ClientsGroups
                                    {
                                        Client = clientCreator,
                                        Group = newGroup,
                                    };
                                    await dbContext.ClientsGroups.AddAsync(clientsGroup);
                                }
                                await dbContext.Groups.AddAsync(newGroup);
                            }
                        }
                        break;
                    }
                }
                
                
                Console.WriteLine($"[{DateTime.Now}] user with username {user.UserName} has sent message: {receivedMessage}");
                var messageModel = new MessageModel(user.UserName, , receivedMessage);
                var messageToBroadcast = new Message() { TimeOfSending = DateTime.Now, SenderClientId = user.DbClientId,
                    MessageContent = receivedMessage, ChatModelId = }; 
                await using var context = _dbContextFactory.CreateDbContext();
                await context.Messages.AddAsync(messageToBroadcast);
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
            _onlineClients.Remove(user.UserName);
            user.Client.Close();
            Console.WriteLine($"[{DateTime.Now}] user with username {user.UserName} has disconnected");
        }
    }

    
    private async Task BroadCastMessageAsync(byte messageType, MessageModel message)
    {
        var tasks = new List<Task>();
        foreach (var user in _onlineClients.Values)
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
        foreach (var user in _onlineClients.Values)
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