using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CSharpWPF_TcpChat.Client.Infrastructure;
using Microsoft.VisualBasic;
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