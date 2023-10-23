using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SharedUtilities;
using Ef_Models;

namespace CSharpConsole_TcpChat.Server;

public class Server
{
    private readonly Dictionary<string, ClientModel> _onlineClients = new Dictionary<string, ClientModel>();
    private readonly TcpListener listener;
    private ChatDbContextFactory _dbContextFactory;

    private record ReceivedMessageData(int ByteOption, string ReceivedMessageContent);
    private record MessageToBroadcastInfo(ChatModel? MessageOriginChatModel, List<Client> ClientsReceivers);
    
    public Server(IPAddress ipAddress, int port, ChatDbContextFactory contextFactory)
    {
        _dbContextFactory = contextFactory;
        listener = new TcpListener(ipAddress, port);
    }

    public async Task StartServer()
    {
        listener.Start();
        Console.WriteLine($"[{DateTime.Now}] SERVER started");
        await HandleUsersAsync();
    }
    
    private async Task HandleUsersAsync()
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
                var userName = MessageModel.GetMessagePart(receivedMessage, 0);

                var isClientNew = false;
                await using var dbContext = _dbContextFactory.CreateDbContext();
                var dbClientToConnect = await dbContext.Clients.FirstOrDefaultAsync(c => c.Username.Equals(userName));
                if (dbClientToConnect == null)
                {
                    var password = MessageModel.GetMessagePart(receivedMessage, 1);
                    await dbContext.Clients.AddAsync(new Client() { Username = userName, Password = password });
                    await dbContext.SaveChangesAsync();
                    
                    dbClientToConnect = await dbContext.Clients.FirstOrDefaultAsync(c => c.Username.Equals(userName));
                    isClientNew = true;
                }

                ClientModel acceptedUser;
                if (!_onlineClients.TryGetValue(userName, out var connectedOnlineClient))
                {
                    acceptedUser = new ClientModel(client, userName, dbClientToConnect!.Id);
                    _onlineClients.Add(acceptedUser.UserName, acceptedUser);
                }
                else
                {
                    acceptedUser = connectedOnlineClient;
                }
                Console.WriteLine($"[{DateTime.Now}] user with username {acceptedUser.UserName} has connected");
                
                if (isClientNew)
                {
                    var receivers = await dbContext.Clients.ToListAsync();
                    var messageToBroadCast = MessageModel.FormMessage(MessageModel.NewUserAddedMessage,
                        acceptedUser.DbClientId.ToString());
                    
                    Task.Run(async () =>
                    {
                        await BroadCastTextMessageAsync(MessageModel.SystemMessageByteOption, 
                            receivers, messageToBroadCast);
                    });
                }
                Task.Run(async () =>
                {
                    await HandleOneUserAsync(acceptedUser);
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] Error occurred while handling clients: {ex.Message}");
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
                var receivedMessageData = await ReceiveMessageAsync(user);
                var messageCommand = MessageModel.GetMessagePart(receivedMessageData.ReceivedMessageContent, 0);
                if (receivedMessageData.ByteOption == MessageModel.SystemMessageByteOption)
                {
                    if (messageCommand == MessageModel.AddGroupRequestMessage)
                    {
                        await AddGroupAsync(receivedMessageData.ReceivedMessageContent);
                        continue;
                    }
                    break;
                }
                
                Console.WriteLine($"[{DateTime.Now}] user with username {user.UserName} has sent message: {receivedMessageData.ReceivedMessageContent}");
                
                //if the message type is not systemMessage, but common message, the structure of the message is:
                //              "Name of chat it came from" | "Content of message"
                //so messageCommand in this case is the name of chat (or client if it is personal chat)
                
                await using var dbContext = _dbContextFactory.CreateDbContext();
                var receivedMessageContent = MessageModel.GetMessagePart(receivedMessageData.ReceivedMessageContent, 1);

                var messageInfo = await ConfigureMessageToBroadcastInfoAsync(dbContext, user, messageCommand);

                await SendConfiguredMessage(user, messageInfo, receivedMessageContent, dbContext);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now}] Error occurred while handling {user.UserName}: {ex.Message}");
        }
        finally
        {
            await CloseClientConnection(user);
        }
    }

    private async Task CloseClientConnection(ClientModel user)
    {
        var stream = user.Client.GetStream();
        stream.WriteByte(MessageModel.SystemMessageByteOption);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(MessageModel.ExitMessageResponse));
        _onlineClients.Remove(user.UserName);
        user.Client.Close();
        Console.WriteLine($"[{DateTime.Now}] user with username {user.UserName} has disconnected");
    }

    private async Task<ReceivedMessageData> ReceiveMessageAsync(ClientModel user)
    {
        var stream = user.Client.GetStream();
        var buffer = new byte[1024];
        var bytesOption = stream.ReadByte();
        var receivedBytes = await stream.ReadAsync(buffer);
        var receivedMessage = Encoding.UTF8.GetString(buffer, 0,receivedBytes);
        return new ReceivedMessageData(bytesOption, receivedMessage);
    }
    
    private async Task AddGroupAsync(string receivedParametersString)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var groupName = MessageModel.GetMessagePart(receivedParametersString, 1);
        var clientCreatorId = MessageModel.GetMessagePart(receivedParametersString, 2);

        var newGroup = await HandleAddGroupRequestAsync(dbContext, groupName, int.Parse(clientCreatorId));
        
        var receivers = await dbContext.Clients.ToListAsync();
        var messageToBroadCast = MessageModel.FormMessage(MessageModel.GroupAddedMessage, newGroup.Id.ToString());
        Task.Run(async () =>
        {
            await BroadCastTextMessageAsync(MessageModel.SystemMessageByteOption, receivers, messageToBroadCast);
        });
    }
    
    private async Task<Group> HandleAddGroupRequestAsync(ChatDbContext dbContext, string groupName, int clientCreatorId)
    {
        var newGroup = new Group() { GroupName = groupName };
        var clientCreator = await dbContext.Clients.FirstOrDefaultAsync(c => c.Id == clientCreatorId);
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
        await dbContext.SaveChangesAsync();
        return newGroup;
    }

    private async Task<MessageToBroadcastInfo?> ConfigureMessageToBroadcastInfoAsync(ChatDbContext dbContext, ClientModel user, string messageCommand)
    {
        if (messageCommand[0] == '@') //the received chatName is name of a client
        {
            return await ConfigurePersonalMessageInfoAsync(dbContext, user, messageCommand);
        }

        return await ConfigureGroupMessageInfoAsync(dbContext, user, messageCommand);
    }

    private async Task<MessageToBroadcastInfo?> ConfigurePersonalMessageInfoAsync(ChatDbContext dbContext, ClientModel user, string messageCommand)
    {
        var receiverClient = await dbContext.Clients.FirstOrDefaultAsync(c => c.Username == messageCommand);
        if (receiverClient == null) return null;
        
        var personalChat = await dbContext.PersonalChats
            .Include(personalChat => personalChat.FirstClient)
            .Include(personalChat => personalChat.SecondClient)
            .GetPersonalChatAsync(receiverClient.Id, user.DbClientId);
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

        var messageInfo = new MessageToBroadcastInfo(personalChat,
            new List<Client>() { personalChat.FirstClient, personalChat.SecondClient });
        return messageInfo;
    }

    private async Task<MessageToBroadcastInfo?> ConfigureGroupMessageInfoAsync(ChatDbContext dbContext, ClientModel user, string messageCommand)
    {
        var receiverGroup = await dbContext.Groups
            .Include(g => g.GroupMembers)
            .ThenInclude(clientsGroups => clientsGroups.Client)
            .FirstOrDefaultAsync(g => g.GroupName == messageCommand);
        if (receiverGroup == null) return null;
        
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
        var messageInfo = new MessageToBroadcastInfo(receiverGroup, 
            receiverGroup.GroupMembers
                .Select(member => member.Client)
                .ToList());
        return messageInfo;
    }
    
    private async Task SendConfiguredMessage(ClientModel user, MessageToBroadcastInfo? messageInfo,
        string receivedMessageContent, ChatDbContext dbContext)
    {
        if (messageInfo?.MessageOriginChatModel == null) return;
        var dbMessage = new Message()
        {
            TimeOfSending = DateTime.Now,
            SenderClientId = user.DbClientId,
            MessageContent = receivedMessageContent,
            ChatModel = messageInfo.MessageOriginChatModel
        };
        await dbContext.Messages.AddAsync(dbMessage);
        await dbContext.SaveChangesAsync();
        Task.Run(async () =>
        {
            await BroadCastTextMessageAsync(MessageModel.CommonMessageByteOption, messageInfo.ClientsReceivers,
                $"{dbMessage.Id}");
        });
    }
    
    //the receiver name can be either name of a group or a client that should receive the message
    private async Task BroadCastTextMessageAsync(byte messageType, IEnumerable<Client> receivers, string textMessage)
    {
        var tasks = new List<Task>();
        foreach (var receiverUser in receivers)
        {
            if (!_onlineClients.TryGetValue(receiverUser.Username, out var receiverClient)) continue;
            tasks.Add(Task.Run(async () =>
            {
                var stream = receiverClient.Client.GetStream();
                stream.WriteByte(messageType);
                await stream.WriteAsync(Encoding.UTF8.GetBytes(textMessage));
            }));
        }
        await Task.WhenAll(tasks);
    }
}