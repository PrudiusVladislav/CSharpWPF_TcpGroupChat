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
    public ObservableCollection<string> ChatNames { get; set; }
    public ObservableCollection<MessageModel> ChatMessages { get; set; }
    
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
        Console.WriteLine("ChatViewModel constructor enter");
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
            if (createGroupWindow.DataContext is CreateGroupViewModel createGroupViewModel)
            {
                createGroupViewModel.ChatVM = this;
                createGroupWindow.ShowDialog();
            }
        }, o => true);
    }

    public async Task StartChat(MainViewModel mainVM, Ef_Models.Client dbClient)
    {
        Console.WriteLine("StartChat constructor enter");
        MainVM = mainVM;
        _dbClient = dbClient;
        await ConnectAsync();
        Console.WriteLine("StartChat after await ConnectAsync ");
        await LoadChats();
        Console.WriteLine("StartChat after await LoadChats(); ");
        ChatMessages = new ObservableCollection<MessageModel>();
        Console.WriteLine("StartChat after ChatMessages = new ObservableCollection<MessageModel>(); ");
    }
    
    private async void ExecuteDisconnectCommand()
    {
        await _client!.SendMessageAsync(MessageModel.SystemMessageByteOption,MessageModel.ExitMessage);
        WindowDialogResult = true; // close the app window on disconnect 
    }
    
    private async Task ConnectAsync()
    {
        _client = new Infrastructure.Client(_dbClient.Username);
        _client.MessageReceived += HandleMessageReceived;
        _client.EventOccurred += HandleEventOccurred;
        _client.NewUserAdded += HandleClientAdded;
        Console.WriteLine("ChatVM.ConnectAsync after creating client");
        await _client.InitiateClientAsync(_dbClient);
        Console.WriteLine("ChatVM.ConnectAsync after await _client.InitiateClientAsync(_dbClient);");
    }

    private async Task LoadChats()
    {
        Console.WriteLine("ChatVM.LoadChats enter");
        await using var dbContext = MainVM.ChatContextFactory.CreateDbContext();
        var resultChatNames = await dbContext.Groups.Select(g => g.GroupName).ToListAsync();
        resultChatNames.AddRange(await dbContext.Clients.Select(c => c.Username).ToListAsync());
        Application.Current.Dispatcher.Invoke(() =>
        {
            ChatNames = new ObservableCollection<string>(resultChatNames);
        });
        Console.WriteLine($"ChatVM.LoadChats end ChatNames Length: {ChatNames.Count}");
    }
    
    private async void ExecuteSendMessageCommand()
    {
        await _client!.SendMessageAsync(MessageModel.CommonMessageByteOption, $"{SelectedChatName}{MessageModel.MessageSeparator}{EnteredMessage}");
        Application.Current.Dispatcher.Invoke(() =>
        {
            EnteredMessage = string.Empty;
        });
    }
    
    private async void HandleMessageReceived(int messageId)
    {
        await using var dbContext = MainVM.ChatContextFactory.CreateDbContext();
        var message = await dbContext.Messages.Include(m => m.SenderClient).FirstOrDefaultAsync(m => m.Id == messageId);
        //checking if the group/personal chat the message came from is the selected one right now, if so, add the message to the ChatMessages,
        //otherwise do nothing, since it will be downloaded with other messages the next time the chat is selected
        switch (message)
        {
            case { ChatModel: Group group }:
            {
                var messageOriginGroup = await dbContext.Groups.FirstOrDefaultAsync(g => g.Id == group.Id);
                if (messageOriginGroup != null && messageOriginGroup.GroupName == SelectedChatName)
                {
                    ChatMessages.Add(new MessageModel(message.SenderClient.Username, message.TimeOfSending, message.MessageContent));
                }

                break;
            }
            case { ChatModel: PersonalChat personalChat }:
            {
                var messageOriginPersonalChat = await dbContext.PersonalChats
                    .Include(pc => pc.FirstClient).Include(pc => pc.SecondClient).FirstOrDefaultAsync(c => c.Id == personalChat.Id);
                if (messageOriginPersonalChat != null &&
                    (messageOriginPersonalChat.FirstClient.Username == SelectedChatName ||
                     messageOriginPersonalChat.SecondClient.Username == SelectedChatName))
                {
                    ChatMessages.Add(new MessageModel(message.SenderClient.Username, message.TimeOfSending, message.MessageContent));
                }
                
                break;
            }
        }
    }
    
    private async void HandleClientAdded(int clientId)
    {
        await using var dbContext = MainVM.ChatContextFactory.CreateDbContext();
        var clientToAdd = await dbContext.Clients.FirstOrDefaultAsync(c => c.Id == clientId);
        if (clientToAdd != null)
            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatNames.Add(clientToAdd.Username);
            });
    }
    
    private void HandleEventOccurred(string eventMessage)
    {
        Console.WriteLine($"Client: event occurred: {eventMessage}");
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(eventMessage, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    public async Task AddGroupAsync(string groupName)
    {
        await _client!.SendMessageAsync(MessageModel.SystemMessageByteOption,
            $"{MessageModel.AddGroupRequestMessage}{MessageModel.MessageSeparator}{groupName}{_dbClient.Id}");
    }

    private async void SelectedChatNameChanged()
    {
        await using var dbContext = MainVM.ChatContextFactory.CreateDbContext();
        if (SelectedChatName![0] == '@')//that means that the selected name is name of a client
        {
            MembersNumber = 1;
            var personalChat = await dbContext.PersonalChats.Include(personalChat => personalChat.Messages)
                .ThenInclude(message => message.SenderClient)
                .FirstOrDefaultAsync(chat =>
                    (chat.FirstClient.Username == SelectedChatName && chat.SecondClient.Username == _dbClient.Username) ||
                    (chat.FirstClient.Username == _dbClient.Username && chat.SecondClient.Username == SelectedChatName));
            if (personalChat != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ChatMessages = new ObservableCollection<MessageModel>(personalChat.Messages.Select(message =>
                            new MessageModel(message.SenderClient.Username, message.TimeOfSending,
                                message.MessageContent))
                        .ToList());
                });
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatMessages = new ObservableCollection<MessageModel>();
            });
            return;
        }

        //this executes if the selected chat name is name of a group
        var group = await dbContext.Groups.Include(g => g.Messages)
            .ThenInclude(message => message.SenderClient).Include(group => group.GroupMembers).FirstOrDefaultAsync(g => g.GroupName == SelectedChatName);
        if (group != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatMessages = new ObservableCollection<MessageModel>(group.Messages.Select(message =>
                                    new MessageModel(message.SenderClient.Username, message.TimeOfSending, message.MessageContent))
                                .ToList());
                MembersNumber = group.GroupMembers.Count;
            });
            return;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            MembersNumber = 0;
            ChatMessages = new ObservableCollection<MessageModel>();
        });
    }
}