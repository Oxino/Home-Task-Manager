using HomeTaskManager.Application;
using HomeTaskManager.Infrastructure;
using HomeTaskManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "HomeTaskManager.Session";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<ReminderNotificationBackgroundService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<HomeTaskManagerDbContext>();
    dbContext.Database.EnsureCreated();
    if (dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
    {
        dbContext.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS TaskComments (Id TEXT NOT NULL CONSTRAINT PK_TaskComments PRIMARY KEY, TaskInstanceId TEXT NOT NULL, AuthorUserId TEXT NOT NULL, Content TEXT NOT NULL, CreatedAt TEXT NOT NULL);");
        dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_TaskComments_TaskInstanceId_CreatedAt ON TaskComments (TaskInstanceId, CreatedAt);");
        dbContext.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS UserPreferences (Id TEXT NOT NULL CONSTRAINT PK_UserPreferences PRIMARY KEY, UserId TEXT NOT NULL, NotificationsEnabled INTEGER NOT NULL, CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL);");
        dbContext.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_UserPreferences_UserId ON UserPreferences (UserId);");
        dbContext.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Reminders (Id TEXT NOT NULL CONSTRAINT PK_Reminders PRIMARY KEY, TaskTemplateId TEXT NOT NULL, TriggerType INTEGER NOT NULL, OffsetValue INTEGER NOT NULL, OffsetUnit INTEGER NOT NULL, IsEnabled INTEGER NOT NULL);");
        dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Reminders_TaskTemplateId ON Reminders (TaskTemplateId);");
        try
        {
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE Reminders ADD COLUMN CreatedAt TEXT NOT NULL DEFAULT '2000-01-01T00:00:00.0000000';");
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 1 && exception.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
        {
        }
        try
        {
            dbContext.Database.ExecuteSqlRaw("ALTER TABLE Reminders ADD COLUMN UpdatedAt TEXT NOT NULL DEFAULT '2000-01-01T00:00:00.0000000';");
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 1 && exception.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
        {
        }
        dbContext.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS DailyFlexibleExecutions (Id TEXT NOT NULL CONSTRAINT PK_DailyFlexibleExecutions PRIMARY KEY, TaskInstanceId TEXT NOT NULL, ExecutedByUserId TEXT NOT NULL, ExecutionDate TEXT NOT NULL, CreatedAt TEXT NOT NULL);");
        dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_DailyFlexibleExecutions_TaskInstanceId_ExecutionDate ON DailyFlexibleExecutions (TaskInstanceId, ExecutionDate);");
        dbContext.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Notifications (Id TEXT NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY, UserId TEXT NOT NULL, Title TEXT NOT NULL, Message TEXT NOT NULL, Link TEXT NULL, IsRead INTEGER NOT NULL, ReadAt TEXT NULL, CreatedAt TEXT NOT NULL);");
        dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Notifications_UserId_CreatedAt ON Notifications (UserId, CreatedAt);");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
