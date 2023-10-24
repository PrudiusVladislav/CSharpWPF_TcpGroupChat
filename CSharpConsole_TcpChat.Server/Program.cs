
using System.Net;
using CSharpConsole_TcpChat.Server;
using Ef_Models;

var server = new Server(IPAddress.Parse("127.0.0.1"), 5000, new ChatDbContextFactory());
await server.StartServer();