
using CSharpConsole_TcpChat.Server;

var server = new Server();
await server.HandleUsersAsync();