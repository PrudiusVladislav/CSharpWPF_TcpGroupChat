using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using CSharpWPF_TcpChat.Client.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedUtilities;

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
        LogInCommand = new RelayCommand(ExecuteLogInCommand, o => true);
    }

    private async void ExecuteRegisterCommand()
    {
        const string pattern = @"[@$|\\/]"; // forbidden symbols for username

        if (Regex.IsMatch(Username, pattern))
        {
            MessageBox.Show(@"The username body must not contain the following symbols: @ $ | \ /",
                "Wrong register data", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        await using var dbContext = _mainViewModel.ChatContextFactory.CreateDbContext();
        if (await dbContext.Clients.AnyAsync(c => c.Username.Equals($"@{Username}")))
        {
            MessageBox.Show("Client with such username already exists. Try another one",
                "Wrong register data", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        else
        {
            ChatViewModel chatViewModel = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                chatViewModel = new ChatViewModel();
                _mainViewModel.CurrentViewModel = chatViewModel;
            });
            if (chatViewModel != null)
            {
                await chatViewModel.StartChat(_mainViewModel, new Ef_Models.Client()
                {
                    Username = $"@{this.Username}",
                    Password = this.Password
                });
            }
        }
    }

    private void ExecuteLogInCommand(object? param)
    {
        _mainViewModel.LoginVM ??= new LoginViewModel(_mainViewModel);
        _mainViewModel.CurrentViewModel = _mainViewModel.LoginVM;
    }
}