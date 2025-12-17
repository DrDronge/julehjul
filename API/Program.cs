using DotNetEnv;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Allow the frontend to call the API when deployed via Docker Compose
builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(policy =>
  {
    policy.AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin();
  });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.UseHttpsRedirection();

app.MapPost("/sendresult", (SendResultRequest request) =>
{
  // Process the received roll result
  SendMail(request.Roll);
  return Task.FromResult(Results.Ok());
}).Accepts<SendResultRequest>("application/json");

void SendMail(RollEnum roll)
{

  Env.Load();

  var receiverMail = Environment.GetEnvironmentVariable("RECEIVERMAIL");
  var senderMail = Environment.GetEnvironmentVariable("SENDEREMAIL");
  var appPassword = Environment.GetEnvironmentVariable("APPPASSWORD");
  var smtpServer = Environment.GetEnvironmentVariable("SMTPSERVER");
  var smtpPort = Environment.GetEnvironmentVariable("SMTPPORT");

  var message = new MimeMessage();
  message.From.Add(new MailboxAddress("Christmas Tool", senderMail));
  message.To.Add(new MailboxAddress("Me", receiverMail));
  message.Subject = "Snædis har rullet julehjulet!";

  message.Body = new TextPart("plain")
  {
    Text = $"Snædis har rullet: {roll}"
  };

  using var client = new SmtpClient();

  if (!int.TryParse(smtpPort, out var port))
  {
    throw new Exception("Invalid SMTP port");
  }
  // Gmail SMTP
  client.Connect(
      smtpServer,
      port,
      SecureSocketOptions.StartTls
  );

  client.Authenticate(
      senderMail,
      appPassword
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
