using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CSharpWPF_TcpChat.Client.Infrastructure;
using Microsoft.VisualBasic;
using SharedComponents;

namespace CSharpWPF_TcpChat.Client;


public class MainViewModel: ObservableObject
{
    private Infrastructure.Client? client;
    public ObservableCollection<ClientModel> AvailableClients { get; }
    public ObservableCollection<string> ChatMessages { get; set; }
    public ICommand ConnectCommand { get; }
    public ICommand SendCommand { get; }
    public ICommand DisconnectCommand { get; }
    private string? username;
    public string? UserName
    {
        get => username;
        set
        {
            username = value;
            OnPropertyChanged();
        }
    }

    private string? enteredMessage;
    public string? EnteredMessage
    {
        get => enteredMessage;
        set
        {
            enteredMessage = value;
            OnPropertyChanged();
        }
    }
    
    public MainViewModel()
    {
        var clientRepositoryInstance = ClientsRepository.Instance;
        clientRepositoryInstance.ClientAdded += HandleClientAdded;
        clientRepositoryInstance.ClientRemoved += HandleClientRemoved;
        AvailableClients = new ObservableCollection<ClientModel>(clientRepositoryInstance.GetConnectedClients());
        ChatMessages = new ObservableCollection<string>();
        
        ConnectCommand = new RelayCommand((command) =>
        {
            ExecuteConnectCommand();
        }, o =>
        {
            if (string.IsNullOrWhiteSpace(UserName) || AvailableClients.Select(c => c.UserName.Equals(UserName)).Any()) return false;
            if (client != null)
                return !client.IsConnected;
            return true;
        });

        SendCommand = new RelayCommand((command) =>
            {
                ExecuteSendMessageCommand();
            },
            o => 
            {
                if (string.IsNullOrWhiteSpace(EnteredMessage) || client == null) return false;
                return client.IsConnected;
            }
        );

        DisconnectCommand = new RelayCommand((command) =>
        {
            ExecuteDisconnectCommand();
        },o => client is { IsConnected: true });
    }

    private async void ExecuteDisconnectCommand()
    {
        await client!.SendMessageAsync(MessageModel.ExitMessage);
    }
    
    private void HandleClientAdded(ClientModel addedClient)
    {
        AvailableClients.Add(addedClient);
        Console.WriteLine("Inside the HandleClientAdded method");
    }
    private void HandleClientRemoved(ClientModel removedClient)
    {
        AvailableClients.Remove(removedClient);
        Console.WriteLine("Inside the HandleClientRemoved method");
    }
    
    private async void ExecuteConnectCommand()
    {
        client = new Infrastructure.Client(UserName);
        client.MessageReceived += HandleMessageReceived;
        client.EventOccurred += HandleEventOccurred;
        await client.InitiateClientAsync();
    }

    private async void ExecuteSendMessageCommand()
    {
        await client!.SendMessageAsync(EnteredMessage!);
        EnteredMessage = string.Empty;
    }
    
    private void HandleMessageReceived(string message)
    {
        ChatMessages.Add(message);
    }

    private void HandleEventOccurred(string eventMessage)
    {
        MessageBox.Show(eventMessage, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
}