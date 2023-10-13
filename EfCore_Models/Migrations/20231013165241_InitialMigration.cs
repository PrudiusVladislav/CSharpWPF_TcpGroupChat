using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharedComponents.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatModel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Discriminator = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FirstClientId = table.Column<int>(type: "int", nullable: true),
                    SecondClientId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatModel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatModel_Clients_FirstClientId",
                        column: x => x.FirstClientId,
                        principalTable: "Clients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChatModel_Clients_SecondClientId",
                        column: x => x.SecondClientId,
                        principalTable: "Clients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClientsGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientsGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientsGroups_ChatModel_GroupId",
                        column: x => x.GroupId,
                        principalTable: "ChatModel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientsGroups_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimeOfSending = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SenderClientId = table.Column<int>(type: "int", nullable: false),
                    ChatModelId = table.Column<int>(type: "int", nullable: false),
                    MessageContent = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_ChatModel_ChatModelId",
                        column: x => x.ChatModelId,
                        principalTable: "ChatModel",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_Clients_SenderClientId",
                        column: x => x.SenderClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatModel_FirstClientId",
                table: "ChatModel",
                column: "FirstClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatModel_GroupName",
                table: "ChatModel",
                column: "GroupName",
                unique: true,
                filter: "[GroupName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChatModel_SecondClientId",
                table: "ChatModel",
                column: "SecondClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Username",
                table: "Clients",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientsGroups_ClientId",
                table: "ClientsGroups",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientsGroups_GroupId",
                table: "ClientsGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChatModelId",
                table: "Messages",
                column: "ChatModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderClientId",
                table: "Messages",
                column: "SenderClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientsGroups");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "ChatModel");

            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
