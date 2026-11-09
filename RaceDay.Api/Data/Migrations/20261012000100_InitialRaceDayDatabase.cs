using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Api.Data.Migrations;

/// <inheritdoc />
public partial class InitialRaceDayDatabase : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Roles",
            columns: table => new
            {
                RoleId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Roles", x => x.RoleId));

        migrationBuilder.CreateTable(
            name: "AppUsers",
            columns: table => new
            {
                UserId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FullName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                RoleId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AppUsers", x => x.UserId);
                table.ForeignKey("FK_AppUsers_Roles_RoleId", x => x.RoleId, "Roles", "RoleId", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Events",
            columns: table => new
            {
                EventId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OrganiserId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Distance = table.Column<double>(type: "float", nullable: false),
                Type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Events", x => x.EventId);
                table.ForeignKey("FK_Events_AppUsers_OrganiserId", x => x.OrganiserId, "AppUsers", "UserId", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                CategoryId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EventId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Distance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                MinAge = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", x => x.CategoryId);
                table.ForeignKey("FK_Categories_Events_EventId", x => x.EventId, "Events", "EventId", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Enrolments",
            columns: table => new
            {
                EnrolmentId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(type: "int", nullable: false),
                EventId = table.Column<int>(type: "int", nullable: false),
                CategoryId = table.Column<int>(type: "int", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Enrolments", x => x.EnrolmentId);
                table.ForeignKey("FK_Enrolments_AppUsers_UserId", x => x.UserId, "AppUsers", "UserId", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Enrolments_Categories_CategoryId", x => x.CategoryId, "Categories", "CategoryId", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_Enrolments_Events_EventId", x => x.EventId, "Events", "EventId", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Results",
            columns: table => new
            {
                ResultId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EnrolmentId = table.Column<int>(type: "int", nullable: false),
                FinishTime = table.Column<int>(type: "int", nullable: true),
                Position = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ResultStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Results", x => x.ResultId);
                table.ForeignKey("FK_Results_Enrolments_EnrolmentId", x => x.EnrolmentId, "Enrolments", "EnrolmentId", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.InsertData(
            table: "Roles",
            columns: new[] { "RoleId", "Name" },
            values: new object[,]
            {
                { 1, "Organiser" },
                { 2, "Participant" }
            });

        migrationBuilder.CreateIndex("IX_AppUsers_Email", "AppUsers", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_AppUsers_RoleId", "AppUsers", "RoleId");
        migrationBuilder.CreateIndex("IX_AppUsers_Username", "AppUsers", "Username", unique: true);
        migrationBuilder.CreateIndex("IX_Categories_EventId", "Categories", "EventId");
        migrationBuilder.CreateIndex("IX_Enrolments_CategoryId", "Enrolments", "CategoryId");
        migrationBuilder.CreateIndex("IX_Enrolments_EventId", "Enrolments", "EventId");
        migrationBuilder.CreateIndex("IX_Enrolments_UserId_EventId", "Enrolments", new[] { "UserId", "EventId" }, unique: true);
        migrationBuilder.CreateIndex("IX_Events_OrganiserId", "Events", "OrganiserId");
        migrationBuilder.CreateIndex("IX_Results_EnrolmentId", "Results", "EnrolmentId", unique: true);
        migrationBuilder.CreateIndex("IX_Roles_Name", "Roles", "Name", unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Results");
        migrationBuilder.DropTable("Enrolments");
        migrationBuilder.DropTable("Categories");
        migrationBuilder.DropTable("Events");
        migrationBuilder.DropTable("AppUsers");
        migrationBuilder.DropTable("Roles");
    }
}
