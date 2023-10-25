using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using CSharpWPF_TcpChat.Client.ViewModels;
using Ef_Models;

namespace CSharpWPF_TcpChat.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var container = InitializeContainer();
            
            var mainViewModel = container.Resolve<MainViewModel>();
            var mainWindow = new MainWindow { DataContext = mainViewModel };
            mainWindow.Show();
        }

        private IContainer InitializeContainer()
        {
            var builder = new ContainerBuilder();

            // Register dependency
            builder.RegisterType<MainViewModel>();
            builder.Register<IChatDbContextFactory<IChatDbContext>>(c => new ChatDbContextFactory());
            
            return builder.Build();
        }
    }
}