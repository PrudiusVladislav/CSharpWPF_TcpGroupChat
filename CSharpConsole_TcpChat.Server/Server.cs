using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedUtilities;
using Ef_Models;

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
                    BroadCastTextMessageAsync(MessageModel.SystemMessageByteOption, await dbContext.Clients.ToListAsync(),
                        $"{MessageModel.NewUserAddedMessage}{MessageModel.MessageSeparator}{acceptedUser.DbClientId}");
                HandleOneUserAsync(acceptedUser);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] Error occurred while handling clients: {ex.Message}");
            //var messageModel = new MessageModel("SERVER", DateTime.Now, ex.Message);
            //await BroadCastTextMessageAsync(MessageModel.CommonMessageByteOption, messageModel.ToString());
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
                await using var dbContext = _dbContextFactory.CreateDbContext();
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
                        break;
                    }
                }
                
                Console.WriteLine($"[{DateTime.Now}] user with username {user.UserName} has sent message: {receivedMessage}");
                
                //if the message type is not systemMessage, but common message, the structure of the message is:
                //              "Name of chat it came from" | "Content of message"
                //so messageCommand in this case is the name of chat (or client if it is personal chat)
                var receivedMessageContent = receivedMessage[(receivedMessage.IndexOf(MessageModel.MessageSeparator,
                    StringComparison.Ordinal) + 1)..];
                ChatModel messageOriginChatModel = null;
                var clientsToReceiveMessage = new List<Client>();
                if (messageCommand[0] == '@') //the received chatName is name of a client
                {
                    //gets receiver client
                    var receiverClient = await dbContext.Clients.FirstOrDefaultAsync(c => c.Username == messageCommand);
                    if (receiverClient != null)
                    {
                        var personalChat = await dbContext.PersonalChats.FirstOrDefaultAsync(pc =>
                            (pc.FirstClient.Id == receiverClient.Id && pc.SecondClientId == user.DbClientId) ||
                            (pc.SecondClient.Id == receiverClient.Id && pc.FirstClientId == user.DbClientId));
                        
                        //checks if there is no personal chat established yet, between the user and receiverClient, than creates one
                        if (personalChat == null)
                        {
                            var personalChatToAdd = new PersonalChat()
                            {
                                FirstClientId = user.DbClientId,
                                SecondClientId = receiverClient.Id
                            };
                            await dbContext.PersonalChats.AddAsync(personalChatToAdd);
                            await dbContext.SaveChangesAsync();
                            personalChat = personalChatToAdd;
                        }

                        messageOriginChatModel = personalChat;        
                        clientsToReceiveMessage.Add(receiverClient);
                    }
                }
                //means that the receiver is a group
                else
                {
                    var receiverGroup = await dbContext.Groups.Include(g => g.GroupMembers)
                        .ThenInclude(clientsGroups => clientsGroups.Client)
                        .FirstOrDefaultAsync(g => g.GroupName == messageCommand);
                    if (receiverGroup != null)
                    {
                        //checks if the client is not connected to the group yet, adds him to the members
                        if (!(receiverGroup.GroupMembers.Any(member => member.ClientId == user.DbClientId)))
                        {
                            await dbContext.ClientsGroups.AddAsync(new ClientsGroups()
                            {
                                ClientId = user.DbClientId,
                                GroupId = receiverGroup.Id
                            });
                            await dbContext.SaveChangesAsync();
                        }
                        messageOriginChatModel = receiverGroup;
                        clientsToReceiveMessage.AddRange(receiverGroup.GroupMembers.Select(member => member.Client));
                    }
                }

                if (messageOriginChatModel != null)
                {
                    var dbMessage = new Message() { TimeOfSending = DateTime.Now, SenderClientId = user.DbClientId,
                        MessageContent = receivedMessage, ChatModel = messageOriginChatModel}; 
                    await dbContext.Messages.AddAsync(dbMessage);
                    await dbContext.SaveChangesAsync();
                    BroadCastTextMessageAsync(MessageModel.CommonMessageByteOption, clientsToReceiveMessage,  $"{dbMessage.Id}");
                }
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] Error occurred while handling {user.UserName}: {ex.Message}");
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

    
    // private async Task BroadCastMessageAsync(byte messageType,string receiverName,  MessageModel message)
    // {
    //     var tasks = new List<Task>();
    //     foreach (var user in _onlineClients.Values)
    //     {
    //         tasks.Add(Task.Run(async () =>
    //         {
    //             var stream = user.Client.GetStream();
    //             stream.WriteByte(messageType);
    //             await stream.WriteAsync(Encoding.UTF8.GetBytes(message.ToString()));
    //         }));
    //     }
    //     await Task.WhenAll(tasks);
    // }
    
    
    //the receiver name can be either name of a group or a client that should receive the message
    private async Task BroadCastTextMessageAsync(byte messageType, IEnumerable<Client> receivers, string textMessage)
    {
        var tasks = new List<Task>();
        foreach (var receiverUser in receivers)
        {
            if (_onlineClients.TryGetValue(receiverUser.Username, out var receiverClient))
            {
                tasks.Add(Task.Run(async () =>
                {
                    var stream = receiverClient.Client.GetStream();
                    stream.WriteByte(messageType);
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(textMessage));
                }));
            }
        }
        await Task.WhenAll(tasks);
    }
}