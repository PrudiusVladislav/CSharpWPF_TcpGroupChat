
using SharedComponents;

namespace CSharpWPF_TcpChat.Client.ViewModels;


public class MainViewModel: ObservableObject
{
    private LoginViewModel? _loginVM;
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
        _loginVM = new LoginViewModel(this);
        CurrentViewModel = _loginVM;
    }
    
}