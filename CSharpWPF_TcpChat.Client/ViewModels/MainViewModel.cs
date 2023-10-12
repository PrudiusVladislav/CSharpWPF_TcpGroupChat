
using SharedUtilities;
using Ef_Models;

namespace CSharpWPF_TcpChat.Client.ViewModels;


public class MainViewModel: ObservableObject
{
    public LoginViewModel? LoginVM { get; set; }
    public RegisterViewModel?  RegisterVM{ get; set; }
    public ChatViewModel? ChatVM { get; set; }
    public ChatDbContextFactory ChatContextFactory { get;}
    
    private ObservableObject? _currentViewModel;
    public ObservableObject? CurrentViewModel
    {
        get => _currentViewModel;
        set
        {
            _currentViewModel = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel()
    {
        ChatContextFactory = new ChatDbContextFactory();
        LoginVM = new LoginViewModel(this);
        CurrentViewModel = LoginVM;
    }
    
}