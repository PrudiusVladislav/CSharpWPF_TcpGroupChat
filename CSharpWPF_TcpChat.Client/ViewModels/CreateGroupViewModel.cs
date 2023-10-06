
using System.Windows;
using System.Windows.Input;
using CSharpWPF_TcpChat.Client.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedComponents;

namespace CSharpWPF_TcpChat.Client.ViewModels;

public class CreateGroupViewModel: ObservableObject
{
    private string _groupName;
    
    public ChatViewModel? ChatVM { get; set; }
    public ICommand CreateGroupCommand { get; }
    public string GroupName
    {
        get => _groupName;
        set
        {
            _groupName = value;
            OnPropertyChanged();
        }
    }
    public CreateGroupViewModel()
    {
        CreateGroupCommand = new RelayCommand((action) =>
        {
            ExecuteCreateGroupCommand();
        }, o => !string.IsNullOrWhiteSpace(GroupName));
    }

    private async void ExecuteCreateGroupCommand()
    {
        if (ChatVM == null) return;
        
        await using var dbContext = ChatVM.MainVM.ChatContextFactory.CreateDbContext();
        if (await dbContext.Groups.AnyAsync(g => g.GroupName.Equals(GroupName)))
            MessageBox.Show("Group with such name already exists. Try another one",
                "Invalid name data", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        else
            await ChatVM.AddGroupAsync(GroupName);
    }
}