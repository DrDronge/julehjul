using DotNetEnv;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

app.MapPost("/sendresult", async (SendResultRequest request) =>
{
    // Process the received roll result
    await SendMail(request.Roll);
    return Results.Ok();
});

async Task SendMail(RollEnum roll)
{

    Env.Load();
    
    var ReceiverMail = Environment.GetEnvironmentVariable("RECEIVERMAIL");
    var SenderMail = Environment.GetEnvironmentVariable("SENDEREMAIL");
    var AppPassword = Environment.GetEnvironmentVariable("APPPASSWORD");
    var SmtpServer = Environment.GetEnvironmentVariable("SMTPSERVER");
    var SmtpPort = Environment.GetEnvironmentVariable("SMTPPORT");
    
    var message = new MimeMessage();
    message.From.Add(new MailboxAddress("Christmas Tool", SenderMail));
    message.To.Add(new MailboxAddress("Me", ReceiverMail));
    message.Subject = "Snædis har rullet julehjulet!";

    message.Body = new TextPart("plain")
    {
        Text = $"Snædis har rullet: {roll}"
    };

    using var client = new SmtpClient();

    if (!int.TryParse(SmtpPort, out var port))
    {
        throw new Exception("Invalid SMTP port");
    }
    // Gmail SMTP
    client.Connect(
        SmtpServer,
        port,
        SecureSocketOptions.StartTls
    );

    client.Authenticate(
        SenderMail,
        AppPassword
    );

    client.Send(message);
    client.Disconnect(true);
}

app.Run();

record SendResultRequest(RollEnum Roll);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RollEnum
{
    HalfPrice = 1,
    SeventyFivePercent = 2,
    FullPrice = 3
}