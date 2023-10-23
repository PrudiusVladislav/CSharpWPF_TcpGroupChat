using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CSharpWPF_TcpChat.Client.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedUtilities;
using Ef_Models;

namespace CSharpWPF_TcpChat.Client.ViewModels;

public class ChatViewModel: ObservableObject
{
    private string? _enteredMessage;
    private string? _selectedChatName;
    private int _membersNumber;
    private volatile bool _windowDialogResult;
    private Ef_Models.Client _dbClient;
    private Infrastructure.Client? _client;
    
    public MainViewModel MainVM { get; private set; }
    public ObservableCollection<string> ChatNames { get; set; } = new ObservableCollection<string>();
    public ObservableCollection<MessageModel> ChatMessages { get; set; } = new ObservableCollection<MessageModel>();
    
    public ICommand SendCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand CreateGroupCommand { get; }

    public int MembersNumber
    {
        get => _membersNumber;
        set
        {
            _membersNumber = value;
            OnPropertyChanged();
        }
    }
    
    public string? EnteredMessage
    {
        get => _enteredMessage;
        set
        {
            _enteredMessage = value;
            OnPropertyChanged();
        }
    }
    
    public bool WindowDialogResult
    {
        get => _windowDialogResult;
        set
        {
            _windowDialogResult = value;
            OnPropertyChanged();
        }
    }
    
    public string? SelectedChatName
    {
        get => _selectedChatName;
        set
        {
            if (value != null && _selectedChatName != value)
            {
                _selectedChatName = value;
                OnPropertyChanged();
                SelectedChatNameChanged();
            }
        }
    }
    
    public ChatViewModel()
    { 
        SendCommand = new RelayCommand((command) =>
            {
                ExecuteSendMessageCommand();
            }, CanExecuteSendCommand);

        DisconnectCommand = new RelayCommand((command) =>
        {
            ExecuteDisconnectCommand();
        }, CanExecuteDisconnectCommand);
        
        CreateGroupCommand = new RelayCommand(ExecuteCreateGroupCommand, o => true);
    }

    public async Task StartChat(MainViewModel mainVM, Ef_Models.Client dbClient)
    {
        MainVM = mainVM;
        _dbClient = dbClient;
        await ConnectAsync();
        await LoadChats();
    }
    
    private async Task ConnectAsync()
    {
        _client = new Infrastructure.Client(_dbClient.Username);
        _client.MessageReceived += HandleMessageReceived;
        _client.EventOccurred += HandleEventOccurred;
        _client.NewUserAdded += HandleClientAdded;
        _client.GroupAdded += HandleGroupAdded;
        
        await _client.InitiateClientAsync(_dbClient);
    }

    private async Task LoadChats()
    {
        await using var dbContext = MainVM.ChatContextFactory.CreateDbContext();
        var resultChatNames = await dbContext.Groups
            .Select(g => g.GroupName)
            .ToListAsync();//adding group names
        var clientsNames = await dbContext.Clients
            .Where(c => c.Id != _dbClient.Id)
            .Select(c => c.Username)
            .ToListAsync();//retrieving clients names
        resultChatNames.AddRange(clientsNames);
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var name in resultChatNames)
            {
                ChatNames.Add(name);
            }
        });
    }
    
    private async void ExecuteSendMessageCommand()
    {
        var messageToSend = MessageModel.FormMessage(SelectedChatName!, EnteredMessage!);
        await _client!.SendMessageAsync(MessageModel.CommonMessageByteOption, messageToSend);
        Application.Current.Dispatcher.Invoke(() =>
        {
            EnteredMessage = string.Empty;
        });
    }

    private bool CanExecuteSendCommand(object? param)
    {
        if (string.IsNullOrWhiteSpace(EnteredMessage) || 
            string.IsNullOrWhiteSpace(SelectedChatName) || 
            _client == null) return false;
        return _client.IsConnected;
    }
    
    private async void ExecuteDisconnectCommand()
    {
        await _client!.SendMessageAsync(MessageModel.SystemMessageByteOption,MessageModel.ExitMessage);
        WindowDialogResult = true; // close the app window on disconnect 
    }

    private bool CanExecuteDisconnectCommand(object? param)
    {
        return _client is { IsConnected: true };
    }
    
    private void ExecuteCreateGroupCommand(object? param)
    {
        var createGroupWindow = new CreateGroupWindow();
        if (createGroupWindow.DataContext is not CreateGroupViewModel createGroupViewModel) return;
        createGroupViewModel.ChatVM = this;
        createGroupWindow.ShowDialog();
    }
    
    public async Task AddGroupAsync(string groupName)
    {
        var messageToServer = MessageModel.FormMessage(MessageModel.AddGroupRequestMessage, groupName, _dbClient.Id.ToString());
        await _client!.SendMessageAsync(MessageModel.SystemMessageByteOption, messageToServer);
    }
    
    private async void HandleMessageReceived(int messageId)
    {
        await using var dbContext = MainVM.ChatContextFactory.CreateDbContext();
        var message = await dbContext.Messages
            .Include(m => m.SenderClient)
            .Include(message => message.ChatModel)
            .FirstOrDefaultAsync(m => m.Id == messageId);
        
        //checking if the group/personal chat the message came from is the selected one right now, if so, add the message to the ChatMessages,
        //otherwise do nothing, since it will be downloaded with other messages the next time the chat is selected
        switch (message)
        {
            case { ChatModel: Group group }:
            {
                var messageOriginGroup = await dbContext.Groups.FirstOrDefaultAsync(g => g.Id == group.Id);
                if (messageOriginGroup != null && messageOriginGroup.GroupName == SelectedChatName)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ChatMessages.Add(new MessageModel(message.SenderClient.Username, message.TimeOfSending,
                            message.MessageContent));
                    });
                }

                break;
            }
            case { ChatModel: PersonalChat personalChat }:
            {
                var messageOriginPersonalChat = await dbContext.PersonalChats
                    .Include(pc => pc.FirstClient)
                    .Include(pc => pc.SecondClient)
                    .FirstOrDefaultAsync(c => c.Id == personalChat.Id);
                
                if (messageOriginPersonalChat != null &&
                    (messageOriginPersonalChat.FirstClient.Username == SelectedChatName ||
                     messageOriginPersonalChat.SecondClient.Username == SelectedChatName))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ChatMessages.Add(new MessageModel(message.SenderClient.Username, message.TimeOfSending,
                            message.MessageContent));
                    });
                }
                break;
            }
        }
    }
    
    private async void HandleClientAdded(int clientId)
    {
        await using var dbContext = MainVM.ChatContextFactory.CreateDbContext();
        var clientToAdd = await dbContext.Clients.FirstOrDefaultAsync(c => c.Id == clientId);
        if (clientToAdd == null) return;
        if (!clientToAdd.Username.Equals(_dbClient.Username))
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatNames.Add(clientToAdd.Username);
            });
        }
    }
    
    private void HandleEventOccurred(string eventMessage)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(eventMessage, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    private async void HandleGroupAdded(int groupId)
    {
        await using var  dbContext = MainVM.ChatContextFactory.CreateDbContext();
        var group= await dbContext.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (group != null)
        {
            Application.Current.Dispatcher.Invoke(() => { ChatNames.Add(group.GroupName); });
        }

    }

    private async void SelectedChatNameChanged()
    {
        await using var dbContext = MainVM.ChatContextFactory.CreateDbContext();
        if (SelectedChatName![0] == '@')//that means that the selected name is name of a client
        {
            MembersNumber = 1;
            var personalChat = await dbContext.PersonalChats
                .Include(personalChat => personalChat.Messages)
                .ThenInclude(message => message.SenderClient)
                .GetPersonalChatAsync(SelectedChatName, _dbClient.Username);
            ChatMessages.Clear();
            if (personalChat != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var messagesToShow = personalChat.Messages
                        .Select(message => new MessageModel(message.SenderClient.Username, 
                            message.TimeOfSending, 
                            message.MessageContent))
                        .ToList();
                    
                    foreach (var message in messagesToShow)
                    {
                        ChatMessages.Add(message);
                    }
                });
            }
            return;
        }

        //this executes if the selected chat name is name of a group
        var group = await dbContext.Groups
            .Include(g => g.Messages)
            .ThenInclude(message => message.SenderClient)
            .Include(group => group.GroupMembers)
            .FirstOrDefaultAsync(g => g.GroupName == SelectedChatName);
        if (group != null)
        {
            ChatMessages.Clear();
            Application.Current.Dispatcher.Invoke(() =>
            {
                var messagesToShow = group.Messages
                    .Select(message => new MessageModel(message.SenderClient.Username,
                        message.TimeOfSending,
                        message.MessageContent))
                    .ToList();
                
                foreach (var message in messagesToShow)
                {
                    ChatMessages.Add(message);
                }
                MembersNumber = group.GroupMembers.Count;
            });
            return;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            MembersNumber = 0;
            ChatMessages.Clear();
        });
    }
}