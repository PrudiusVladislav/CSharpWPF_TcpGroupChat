using SharedComponents;

namespace CSharpWPF_TcpChat.Client.ViewModels;

public class LoginViewModel: ObservableObject
{
    private MainViewModel _mainViewModel;
    public LoginViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }
}