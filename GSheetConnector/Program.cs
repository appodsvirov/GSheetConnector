using GSheetConnector.Controllers;
using GSheetConnector.Interfaces;
using GSheetConnector.Services;

var builder = WebApplication.CreateBuilder(args);

// Получение значений из конфигурации
var googleSheetsConfig = builder.Configuration.GetSection("GoogleSheets");
var tgConfig = builder.Configuration.GetSection("BotConfiguration");


builder.Services.AddSingleton<IFileReader, FileReader>();
builder.Services.AddSingleton<IStatementParser, StatementParser>();
builder.Services.AddSingleton<ITelegramService>(provider =>
{
    var credentialsPath = googleSheetsConfig["CredentialsPath"];
    var token = tgConfig["BotToken"];

    return new TelegramBotService(
        provider.GetService<IFileReader>(),
        provider.GetService<IStatementParser>(),
        credentialsPath, token);
});

//
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

#region В разработке
//// Устанавливаем Webhook при старте (В разработке...)
//app.Lifetime.ApplicationStarted.Register(async () =>
//{
//    var botService = app.Services.GetRequiredService<TelegramBotService>();
//    string webhookUrl = $"https://config["ServerConfiguration:BotToken"]/bot";
//    await botService.SetWebhookAsync(webhookUrl);
//});
#endregion

// При запуске включаем бота
var botService = app.Services.GetRequiredService<ITelegramService>();
botService.Start();




app.Run();