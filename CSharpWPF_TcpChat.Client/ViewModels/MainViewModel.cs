
using System;
using SharedUtilities;
using Ef_Models;

namespace CSharpWPF_TcpChat.Client.ViewModels;


public class MainViewModel: ObservableObject
{
    public LoginViewModel? LoginVM { get; set; }
    public RegisterViewModel?  RegisterVM{ get; set; }
    public ChatViewModel? ChatVM { get; set; }
    public IChatDbContextFactory<IChatDbContext> ChatContextFactory { get;}
    
    private volatile ObservableObject? _currentViewModel;
    public ObservableObject? CurrentViewModel
    {
        get => _currentViewModel;
        set
        {
            _currentViewModel = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel(IChatDbContextFactory<IChatDbContext> chatContextFactory)
    {
        ChatContextFactory = chatContextFactory;
        LoginVM = new LoginViewModel(this);
        CurrentViewModel = LoginVM;
    }
}