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
builder.Services.AddHttpClient<ITelegramService, TelegramService>();
builder.Services.AddSingleton<TelegramController>();
builder.Services.AddHostedService<TelegramPollingService>();


var parser = new StatementParser();
var pdfText = parser.ReadPdf("C:\\Users\\mr_bi\\Desktop\\test.pdf");

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

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/swagger");
    }
    else
    {
        await next();
    }
});

app.UseHttpsRedirection();
app.MapGet("/", () => "Hello World!");
app.MapControllers();

app.Run();