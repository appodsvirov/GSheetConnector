using GSheetConnector.Controllers;
using GSheetConnector.Interfaces;
using GSheetConnector.Services;

var builder = WebApplication.CreateBuilder(args);

// ��������� �������� �� ������������
var googleSheetsConfig = builder.Configuration.GetSection("GoogleSheets");

// ����������� GoogleSheetsService � ����������� �� ������������
builder.Services.AddScoped<GoogleSheetsService>(provider =>
{
    var credentialsPath = googleSheetsConfig["CredentialsPath"];
    var spreadsheetId = googleSheetsConfig["SpreadsheetId"];
    return new GoogleSheetsService(credentialsPath, spreadsheetId);
});

// ��������� ������ ����
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

//// ������������� Webhook ��� ������ (� ����������...)
//app.Lifetime.ApplicationStarted.Register(async () =>
//{
//    var botService = app.Services.GetRequiredService<TelegramBotService>();
//    string webhookUrl = $"https://config["ServerConfiguration:BotToken"]/bot";
//    await botService.SetWebhookAsync(webhookUrl);
//});

// ��� ������� �������� ����
var botService = app.Services.GetRequiredService<TelegramBotService>();
botService.Start();




app.Run();