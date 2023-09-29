using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharedComponents;

namespace CSharpWPF_TcpChat.Client.Infrastructure;

public class Client
{
    private TcpClient client;
    public bool IsConnected => client.Connected;
    public string UserName { get; set; }
    public Guid Uid { get; }

    public event Action<string>? MessageReceived;
    public event Action<string>? EventOccurred;
    public event Action<string>? NewUserAdded;
    public event Action<string>? UserRemoved;
    public event Action<List<string>>? UsersListReceived;
    public event Action<List<string>>? MessagesListReceived;

    public Client(string userName)
    {
        UserName = userName;
        Uid = Guid.NewGuid();
        client = new TcpClient();
    }

    public async Task InitiateClientAsync()
    {
        await ConnectToServerAsync();
        StartReceivingAsync();
    }
    
    private async Task ConnectToServerAsync()
    {
        await client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 5000);
        var stream = client.GetStream();
        await stream.WriteAsync(Encoding.UTF8.GetBytes(UserName));
        
        var buffer = new byte[1024];
        var receivedBytes = await stream.ReadAsync(buffer);
        var receivedData = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
        var jsonStrings = receivedData.Split("||");
        if (jsonStrings.Length >= 2)
        {
            var clientsJson = jsonStrings[0];
            var messagesJson = jsonStrings[1];
            var clientsList = JsonConvert.DeserializeObject<List<string>>(clientsJson);
            var messagesList = JsonConvert.DeserializeObject<List<string>>(messagesJson);
            OnUsersListReceived(clientsList);
            OnMessagesListReceived(messagesList);
        }
    }

    private async Task StartReceivingAsync()
    {
        try
        {
            while (true)
            {
                var stream = client.GetStream();
                var buffer = new byte[1024];
                var messageOption = await Task.Run(stream.ReadByte);
                var receivedBytes = await stream.ReadAsync(buffer);
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0,receivedBytes);
                if (messageOption == MessageModel.SystemMessageByteOption)
                {
                    if(receivedMessage.Equals(MessageModel.ExitMessageResponse))
                        break;
                    var messageCommand = receivedMessage[..receivedMessage.LastIndexOf(' ')];
                    var username = receivedMessage[(receivedMessage.LastIndexOf(' ') + 1)..];
                    switch (messageCommand)
                    {
                        case MessageModel.NewUserAddedMessage:
                        {
                            if(!username.Equals(UserName))
                                OnNewUserAdded(username);
                            break;
                        }
                            
                        case MessageModel.UserRemovedMessage:
                            OnUserRemoved(username);
                            break;
                    }
                }
                else
                    OnMessageReceived(receivedMessage);
            }
        }
        catch (Exception ex)
        {
            OnEventOccurred(ex.Message);
        }
        finally
        {
            client.Close();
            OnEventOccurred(MessageModel.ClientDisconnectMessage);
        }
    }

    public async Task SendMessageAsync(byte messageType, string message)
    {
        try
        {
            var stream = client.GetStream();
            stream.WriteByte(messageType);
            await stream.WriteAsync(Encoding.UTF8.GetBytes(message));
        }
        catch(Exception ex)
        {
            OnEventOccurred(ex.Message);
        }
    }
    
    private void OnMessageReceived(string message)
    {
        MessageReceived?.Invoke(message);
    }
    private void OnEventOccurred(string eventMessage)
    {
        EventOccurred?.Invoke(eventMessage);
    }
    private void OnNewUserAdded(string username)
    {
        NewUserAdded?.Invoke(username);
    }
    
    private void OnUserRemoved(string username)
    {
        UserRemoved?.Invoke(username);
    }

    private void OnUsersListReceived(List<string>? usernames)
    {
        if(usernames != null)
            UsersListReceived?.Invoke(usernames);
    }
    private void OnMessagesListReceived(List<string>? messages)
    {
        if(messages != null)
            MessagesListReceived?.Invoke(messages);
    }
}