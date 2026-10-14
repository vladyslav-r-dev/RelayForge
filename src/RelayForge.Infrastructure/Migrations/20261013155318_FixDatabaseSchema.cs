using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RelayForge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Endpoints",
                table: "Endpoints");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Deliveries",
                table: "Deliveries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WebhookEvents",
                table: "WebhookEvents");

            migrationBuilder.RenameTable(
                name: "Endpoints",
                newName: "endpoints");

            migrationBuilder.RenameTable(
                name: "Deliveries",
                newName: "deliveries");

            migrationBuilder.RenameTable(
                name: "WebhookEvents",
                newName: "webhook_events");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "endpoints",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "webhook_events",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_endpoints",
                table: "endpoints",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_deliveries",
                table: "deliveries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_webhook_events",
                table: "webhook_events",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_deliveries_EndpointId",
                table: "deliveries",
                column: "EndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_deliveries_EventId",
                table: "deliveries",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_deliveries_endpoints_EndpointId",
                table: "deliveries",
                column: "EndpointId",
                principalTable: "endpoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_deliveries_webhook_events_EventId",
                table: "deliveries",
                column: "EventId",
                principalTable: "webhook_events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deliveries_endpoints_EndpointId",
                table: "deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_deliveries_webhook_events_EventId",
                table: "deliveries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_endpoints",
                table: "endpoints");

            migrationBuilder.DropPrimaryKey(
                name: "PK_deliveries",
                table: "deliveries");

            migrationBuilder.DropIndex(
                name: "IX_deliveries_EndpointId",
                table: "deliveries");

            migrationBuilder.DropIndex(
                name: "IX_deliveries_EventId",
                table: "deliveries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_webhook_events",
                table: "webhook_events");

            migrationBuilder.RenameTable(
                name: "endpoints",
                newName: "Endpoints");

            migrationBuilder.RenameTable(
                name: "deliveries",
                newName: "Deliveries");

            migrationBuilder.RenameTable(
                name: "webhook_events",
                newName: "WebhookEvents");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Endpoints",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "WebhookEvents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Endpoints",
                table: "Endpoints",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Deliveries",
                table: "Deliveries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WebhookEvents",
                table: "WebhookEvents",
                column: "Id");
        }
    }
}
