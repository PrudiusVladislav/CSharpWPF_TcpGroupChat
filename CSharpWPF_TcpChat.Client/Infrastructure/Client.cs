using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using SharedUtilities;

namespace CSharpWPF_TcpChat.Client.Infrastructure;

public class Client
{
    private TcpClient client;
    private Ef_Models.Client? _dbClient;
    public bool IsConnected => client.Connected;

    public event Action<int>? MessageReceived;
    public event Action<string>? EventOccurred;
    public event Action<int>? NewUserAdded;

    public Client(string userName)
    {
        client = new TcpClient();
    }

    public async Task InitiateClientAsync(Ef_Models.Client dbClient)
    {
        await Task.Run(async () =>
        {
            _dbClient = dbClient;
            await ConnectToServerAsync(_dbClient);
            Console.WriteLine("InitiateClientAsync after ConnectToServerAsync before StartReceivingAsync");
            Task.Run(async() => await StartReceivingAsync());
            Console.WriteLine("InitiateClientAsync after StartReceivingAsync  ");
        });
    }
    
    private async Task ConnectToServerAsync(Ef_Models.Client dbClient)
    {
        await client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 5000);
        var stream = client.GetStream();
        await stream.WriteAsync(Encoding.UTF8.GetBytes($"{dbClient.Username}{MessageModel.MessageSeparator}{dbClient.Password}"));
        Console.WriteLine("ConnectToServerAsync after WriteAsync ");
    }

    private async Task StartReceivingAsync()
    {
        try
        {
            Console.WriteLine("Entering the StartReceivingAsync ");
            while (true)
            {
                var stream = client.GetStream();
                var buffer = new byte[1024];
                Console.WriteLine("StartReceivingAsync before stream.ReadByte();");
                var messageOption = stream.ReadByte();
                Console.WriteLine("StartReceivingAsync before await stream.ReadAsync(buffer);");
                var receivedBytes = await stream.ReadAsync(buffer);
                Console.WriteLine("StartReceivingAsync after await stream.ReadAsync(buffer);");
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0,receivedBytes);
                Console.WriteLine("StartReceivingAsync after received message");
                
                if (messageOption == MessageModel.SystemMessageByteOption)
                {
                    if(receivedMessage.Equals(MessageModel.ExitMessageResponse))
                        break;
                    
                    var messageCommand = receivedMessage[..receivedMessage.IndexOf(MessageModel.MessageSeparator, StringComparison.Ordinal)];
                    var parameters = receivedMessage[(receivedMessage.IndexOf(MessageModel.MessageSeparator, StringComparison.Ordinal) + 1)..];
                    switch (messageCommand)
                    {
                        case MessageModel.NewUserAddedMessage:
                        {
                            var userId = int.Parse(parameters);
                            Console.WriteLine($"added user id = {userId}");
                            if(!_dbClient!.Id.Equals(userId))
                                OnNewUserAdded(userId);
                            break;
                        }
                    }
                }
                else 
                    OnMessageReceived(int.Parse(receivedMessage));
                Console.WriteLine("StartReceivingAsync after actions made on message received");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"StartReceivingAsync error: {ex}");
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
    
    private void OnMessageReceived(int messageId)
    {
        MessageReceived?.Invoke(messageId);
    }
    private void OnEventOccurred(string eventMessage)
    {
        EventOccurred?.Invoke(eventMessage);
    }
    private void OnNewUserAdded(int userId)
    {
        NewUserAdded?.Invoke(userId);
    }
    
}