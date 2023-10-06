using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CSharpWPF_TcpChat.Client.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedComponents;
using SharedComponents.EF_Models;

namespace CSharpWPF_TcpChat.Client.ViewModels;

public class ChatViewModel: ObservableObject
{
    private string? username;
    private string? enteredMessage;
    private ChatModel? _selectedChat;
    private SharedComponents.EF_Models.Client? _selectedClient;
    private SharedComponents.EF_Models.Client _dbClient;
    private Infrastructure.Client? _client;
    public MainViewModel MainVM { get; }
        
    public ObservableCollection<PersonalChat> PersonalChats { get; set; }
    public ObservableCollection<Group> GroupChats { get; set; }
    public ObservableCollection<SharedComponents.EF_Models.Client> Clients { get; set; }
    public ObservableCollection<Message> ChatMessages;
    public ICommand SendCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand CreateGroupCommand { get; }
    
    
    public string? UserName
    {
        get => username;
        set
        {
            username = value;
            OnPropertyChanged();
        }
    }
    public string? EnteredMessage
    {
        get => enteredMessage;
        set
        {
            enteredMessage = value;
            OnPropertyChanged();
        }
    }
    public ChatModel? SelectedChat
    {
        get => _selectedChat;
        set
        {
            if (value != null && _selectedChat != value)
            {
                _selectedChat = value;
                OnPropertyChanged();
                ChatMessages = new ObservableCollection<Message>(_selectedChat.Messages);
                SelectedClient = null;
            }
        }
    }

    public SharedComponents.EF_Models.Client? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (value != null && _selectedClient != value)
            {
                _selectedClient = value;
                OnPropertyChanged();
                using var dbContext = MainVM.ChatContextFactory.CreateDbContext();
                if(dbContext.PersonalChats.Any(c => c.FirstClient.Username.Equals(UserName) && c.SecondClient.Username.Equals(_selectedClient.Username)))
                    SelectedChat = null;
            }
        }
    }
    
    public ChatViewModel(MainViewModel mainVm, SharedComponents.EF_Models.Client dbClient)
    { 
        MainVM = mainVm;
        _dbClient = dbClient;
        ConnectAsync();
        LoadChats();
        
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
        CreateGroupCommand = new RelayCommand((action) =>
        {
            var createGroupWindow = new CreateGroupWindow();
            (createGroupWindow.DataContext as CreateGroupViewModel).ChatVM = this;
            createGroupWindow.ShowDialog();
        }, o => true);
    }

    private async void ExecuteDisconnectCommand()
    {
        await _client!.SendMessageAsync(MessageModel.SystemMessageByteOption,MessageModel.ExitMessage);
    }
    
    private async void ConnectAsync()
    {
        _client = new Infrastructure.Client(UserName);
        _client.MessageReceived += HandleMessageReceived;
        _client.EventOccurred += HandleEventOccurred;
        _client.NewUserAdded += HandleClientAdded;
        
        await _client.InitiateClientAsync(_dbClient);
    }

    private async void LoadChats()
    {
        await using var dbContext = MainVM.ChatContextFactory.CreateDbContext();
        var groupsTask = dbContext.Groups.ToListAsync();
        var personalChatsTask = dbContext.PersonalChats.ToListAsync();
        GroupChats = new ObservableCollection<Group>(await groupsTask);
        PersonalChats = new ObservableCollection<PersonalChat>(await personalChatsTask);
    }
    
    private async void ExecuteSendMessageCommand()
    {
        await _client!.SendMessageAsync(MessageModel.CommonMessageByteOption, EnteredMessage!);
        EnteredMessage = string.Empty;
    }
    
    private void HandleMessageReceived(int messageId)
    {
        using var dbContext = MainVM.ChatContextFactory.CreateDbContext();
        var message = dbContext.Messages.Find(messageId);
        if (message != null)
            ChatMessages.Add(message);
    }
    
    private void HandleClientAdded(int clientId)
    {
        using var dbContext = MainVM.ChatContextFactory.CreateDbContext();
        var clientToAdd = dbContext.Clients.Find(clientId);
        if(clientToAdd != null)
            Clients.Add(clientToAdd);
    }
    
    private void HandleEventOccurred(string eventMessage)
    {
        MessageBox.Show(eventMessage, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public async Task AddGroupAsync(string groupName)
    {
        await _client!.SendMessageAsync(MessageModel.SystemMessageByteOption,
            $"{MessageModel.AddGroupRequestMessage}{MessageModel.MessageSeparator}{groupName}{_dbClient.Username}");
    }
}