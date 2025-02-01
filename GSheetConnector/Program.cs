using GSheetConnector.Controllers;
using GSheetConnector.Interfaces;
using GSheetConnector.Services;

var builder = WebApplication.CreateBuilder(args);

// Получение значений из конфигурации
var googleSheetsConfig = builder.Configuration.GetSection("GoogleSheets");

// Регистрация GoogleSheetsService с параметрами из конфигурации
builder.Services.AddScoped<GoogleSheetsService>(provider =>
{
    var credentialsPath = googleSheetsConfig["CredentialsPath"];
    var spreadsheetId = googleSheetsConfig["SpreadsheetId"];
    return new GoogleSheetsService(credentialsPath, spreadsheetId);
});

// Добавляем сервис бота
builder.Services.AddSingleton<TelegramBotService>();




var parser = new StatementParser();
var pdfText = parser.ReadPdf("C:\\Users\\mr_bi\\Desktop\\test.pdf");
var transactions = parser.ParseTransactions(pdfText);


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

//// Устанавливаем Webhook при старте (В разработке...)
//app.Lifetime.ApplicationStarted.Register(async () =>
//{
//    var botService = app.Services.GetRequiredService<TelegramBotService>();
//    string webhookUrl = $"https://config["ServerConfiguration:BotToken"]/bot";
//    await botService.SetWebhookAsync(webhookUrl);
//});

// При запуске включаем бота
var botService = app.Services.GetRequiredService<TelegramBotService>();
botService.Start();




app.Run();