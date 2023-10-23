using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CSharpWPF_TcpChat.Client.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedUtilities;

namespace CSharpWPF_TcpChat.Client.ViewModels;

public class LoginViewModel: ObservableObject
{
    private readonly MainViewModel _mainViewModel;
    private string _username;
    private string _password;
    
    public ICommand LogInCommand { get; }
    public ICommand RegisterCommand { get; }
    
    public bool TestProperty { get; set; }
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
        RegisterCommand = new RelayCommand(ExecuteRegisterCommand, o => true);
    }

    private async void ExecuteLogInCommand()
    {
        await using var dbContext = _mainViewModel.ChatContextFactory.CreateDbContext();
        var client = await dbContext.Clients.FirstOrDefaultAsync(c => c.Username.Equals($"@{Username}"));
        if (client == null || !client.Password.Equals(Password))
            MessageBox.Show("Client with such username does not exist or wrong password has been entered",
                "Wrong log in data", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        else
        {
            ChatViewModel chatViewModel = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                chatViewModel = new ChatViewModel();
                _mainViewModel.CurrentViewModel = chatViewModel;
            });
            if (chatViewModel == null) return;
            await chatViewModel.StartChat(_mainViewModel, client);
        }
    }

    private void ExecuteRegisterCommand(object? param)
    {
        _mainViewModel.RegisterVM ??= new RegisterViewModel(_mainViewModel);
        _mainViewModel.CurrentViewModel = _mainViewModel.RegisterVM;
    }
}