using System.Windows;
using System.Windows.Input;
using CSharpWPF_TcpChat.Client.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedComponents;

namespace CSharpWPF_TcpChat.Client.ViewModels;

public class RegisterViewModel: ObservableObject
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
    
    public RegisterViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        
        RegisterCommand = new RelayCommand((action) =>
        {
            ExecuteRegisterCommand();
        }, o => !(string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password)));
        LogInCommand = new RelayCommand((action) =>
        {
            _mainViewModel.LoginVM ??= new LoginViewModel(_mainViewModel);
            _mainViewModel.CurrentViewModel = _mainViewModel.RegisterVM;
        }, o => true);
    }

    private async void ExecuteRegisterCommand()
    {
        await using var dbContext = _mainViewModel.ChatContextFactory.CreateDbContext();
        if (await dbContext.Clients.AnyAsync(c => c.Username.Equals(Username)))
        {
            MessageBox.Show("Client with such username already exists. Try another one",
                "Wrong register data", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        else
        {
            
        }
    }
}