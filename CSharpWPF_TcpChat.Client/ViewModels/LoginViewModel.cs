using System.Windows;
using System.Windows.Input;
using CSharpWPF_TcpChat.Client.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedComponents;

namespace CSharpWPF_TcpChat.Client.ViewModels;

public class LoginViewModel: ObservableObject
{
    private MainViewModel _mainViewModel;
    private string _username;
    private string _password;

    public ICommand LogInCommand { get; }
    public ICommand RegisterCommand { get; }
    
    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            OnPropertyChanged();
        }
    }
    public string Password
    {
        get => _password;
        set
        {
            _password = value;
            OnPropertyChanged();
        }
    }
    
    public LoginViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        
        LogInCommand = new RelayCommand((action) =>
        {
            ExecuteLogInCommand();
        }, o => !(string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password)));
        RegisterCommand = new RelayCommand((action) =>
        {
            _mainViewModel.RegisterVM ??= new RegisterViewModel(_mainViewModel);
            _mainViewModel.CurrentViewModel = _mainViewModel.RegisterVM;
        }, o => true);
    }

    private async void ExecuteLogInCommand()
    {
        await using var dbContext = _mainViewModel.ChatContextFactory.CreateDbContext();
        var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.Username.Equals(Username));
        if (client != null && client.Password.Equals(Password))
        {
            _mainViewModel.CurrentViewModel = new ChatViewModel(client);
        }
        else
            MessageBox.Show("Client with such username does not exist or wrong password has been entered",
                "Wrong log in data", MessageBoxButton.OK, MessageBoxImage.Exclamation);

        Username = string.Empty;
        Password = string.Empty;
    }
    
}