using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CSharpWPF_TcpChat.Client.Infrastructure;
using SharedComponents;

namespace CSharpWPF_TcpChat.Client.ViewModels;

public class ChatViewModel: ObservableObject
{
    private Infrastructure.Client? _client;

    public ObservableCollection<string> AvailableClientsNames { get; set; }
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
    
    public ChatViewModel(SharedComponents.EF_Models.Client dbClient)
    {
        AvailableClientsNames = new ObservableCollection<string>();
        ChatMessages = new ObservableCollection<string>();

        
        ConnectCommand = new RelayCommand((command) =>
        {
            ExecuteConnectCommand();
        }, o =>
        {
            if (string.IsNullOrWhiteSpace(UserName) || AvailableClientsNames.Contains(UserName)) return false;
            if (_client != null)
                return !_client.IsConnected;
            return true;
        });

        SendCommand = new RelayCommand((command) =>
            {
                ExecuteSendMessageCommand();
            },
            o => 
            {
                if (string.IsNullOrWhiteSpace(EnteredMessage) || _client == null) return false;
                return _client.IsConnected;
            }
        );

        DisconnectCommand = new RelayCommand((command) =>
        {
            ExecuteDisconnectCommand();
        },o => _client is { IsConnected: true });
    }

    private async void ExecuteDisconnectCommand()
    {
        await _client!.SendMessageAsync(MessageModel.SystemMessageByteOption,MessageModel.ExitMessage);
    }
    
    private void HandleClientAdded(string addedUsername)
    {
        AvailableClientsNames.Add(addedUsername);
    }
    private void HandleClientRemoved(string removedUsername)
    {
        AvailableClientsNames.Remove(removedUsername);
    }
    
    private async void ExecuteConnectCommand()
    {
        _client = new Infrastructure.Client(UserName);
        _client.MessageReceived += HandleMessageReceived;
        _client.EventOccurred += HandleEventOccurred;
        _client.NewUserAdded += HandleClientAdded;
        _client.UserRemoved += HandleClientRemoved;
        _client.UsersListReceived += HandleUsersListReceived;
        _client.MessagesListReceived += HandleMessagesListReceived;
        
        
        await _client.InitiateClientAsync();
    }

    private async void ExecuteSendMessageCommand()
    {
        await _client!.SendMessageAsync(MessageModel.CommonMessageByteOption, EnteredMessage!);
        EnteredMessage = string.Empty;
    }
    
    private void HandleMessageReceived(string message)
    {
        ChatMessages.Add(message);
    }

    private void HandleUsersListReceived(List<string> usernames)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AvailableClientsNames.Clear();
            foreach (var name in usernames)
            {
                AvailableClientsNames.Add(name);
            }
        });
    }
    
    private void HandleMessagesListReceived(List<string> messages)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ChatMessages.Clear();
            foreach (var msg in messages)
            {
                ChatMessages.Add(msg);
            }
        });
    }
    
    private void HandleEventOccurred(string eventMessage)
    {
        MessageBox.Show(eventMessage, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}