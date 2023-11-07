# Local network chat application using TCP protocol

This is a simple chat application for local network users using TCP connections. It allows users to send messages within private chats established between two clients, as well as communicating with many using group chats.

## Installation

1. Clone this repository.
2. Open the solution in your IDE.
3. Build and run the server and then client applications.

## Technologies Used

- .NET 7.0
- C# 11.0
- Entity Framework Core
- MSSQL Server

## Project Structure

- **Server**: Console Application
  - Listens for incoming connections using `TcpListener`.
  - Manages clients (adding new ones, updating online status).
  - Manages the messages
- **Client**: WPF Application
  - Connects to the server using `TcpClient`.
  - Always ready to receive a message
- **EfCore_Models**: Class library
  - Contains ef models, ChatDbContext and ChatDbContextFactory, as well as migration records
- **ShareUtilities**: Class library
  - Contains extension methods for ef queries
  - Helper classes like ClientModel and MessageModel, that help managing connected clients and sent messages

### Database

The application uses MSSQL Server to store all the data. You can configure the database connection adding your `appSettings.json`.

## Usage

Here are some of the basic actions a user can do within the chat app:

### Authentication

Before entering the chat, every user should login or register

- Login demo:

![Login Demo](/Documentation_Images/Login_Demo.gif)

- And a demo for registration process:

![Register Demo](/Documentation_Images/Register_Demo.gif)

### Sending messages

- **Choose chat**
  After you logged in, you can choose a _private chat_ (marked with a @) to communicate with another user directly, or a _group chat_, where all the members will be able to see your message. To choose a chat you can click on one of the chat names from the list on the left side of the window:

  ![ChooseChat Demo](/Documentation_Images/ChooseChat_Demo.gif)

- **Enter and send message**
  When you you have chosen a chat, you can enter a text message in the _input_ field at the bottom of the window, and then press the _send_ button that is right next to the input field:

  ![SendMessage Demo](/Documentation_Images/SendMessage_Demo.gif)

- **Creating a group chat**
  You can also create a _new group chat_, instead of communicating within existing ones. All the group chats within this app are _public_, and anyone can access them. But to appear as a _member_ of the group chat, you need to send a message to the chat, unless you are the creator of the group. The process of creation of a new group looks like this:

  ![AddGroup Demo](/Documentation_Images/AddGroup_Demo.gif)

  After that, the new group is going to appear in the list of chats:

  ![Image](/Documentation_Images/NewGroup_Image.png)

- **Disconnect**
  After you've done everything you needed within the chat app, you can easily disconnect, by pressing the _disconnect button_:

  ![Disconnect Demo](/Documentation_Images/Disconnect_Demo.gif)
