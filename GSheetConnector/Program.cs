using GSheetConnector.Controllers;
using GSheetConnector.Interfaces;
using GSheetConnector.Models.TelegramBot;
using GSheetConnector.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appconfig.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);


// ��������� �������� �� ������������
var googleSheetsConfig = builder.Configuration.GetSection("GoogleSheets");
var botConfig = builder.Configuration.GetSection("BotConfiguration");

// ����������� GoogleSheetsService � ����������� �� ������������
builder.Services.AddSingleton<GoogleSheetsService>(provider =>
{
    var credentialsPath = googleSheetsConfig["CredentialsPath"];
    return new GoogleSheetsService(credentialsPath);
});

// ����������� TelegramBotService
builder.Services.AddSingleton<ITelegramService>(sp =>
{
    var botToken = botConfig["BotToken"];
    var googleSheetsService = sp.GetRequiredService<GoogleSheetsService>();
    return new TelegramBotService(botToken, googleSheetsService);
});

builder.Services.AddSingleton<IFileReader, FileReader>();
builder.Services.AddSingleton<IStatementParser, StatementParser>(); 
builder.Services.AddSingleton<UserSheetStore>(); 

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

#region � ����������
//// ������������� Webhook ��� ������ (� ����������...)
//app.Lifetime.ApplicationStarted.Register(async () =>
//{
//    var botService = app.Services.GetRequiredService<TelegramBotService>();
//    string webhookUrl = $"https://config["ServerConfiguration:BotToken"]/bot";
//    await botService.SetWebhookAsync(webhookUrl);
//});
#endregion

// ��� ������� �������� ����
var botService = app.Services.GetRequiredService<ITelegramService>();
botService.Start();




app.Run();