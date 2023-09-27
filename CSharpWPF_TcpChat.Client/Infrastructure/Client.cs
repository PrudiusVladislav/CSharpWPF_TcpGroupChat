using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
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
    }

    private async Task StartReceivingAsync()
    {
        try
        {
            while (true)
            {
                var stream = client.GetStream();
                var buffer = new byte[1024];
                var receivedBytes = await stream.ReadAsync(buffer);
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0,receivedBytes);
                if (receivedMessage.Equals(MessageModel.ExitMessageResponse))
                    break;
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

    public async Task SendMessageAsync(string message)
    {
        try
        {
            var stream = client.GetStream();
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
}