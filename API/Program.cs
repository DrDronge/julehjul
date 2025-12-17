using DotNetEnv;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text.Json.Serialization;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Allow the frontend to call the API when deployed via Docker Compose
builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(policy =>
  {
    policy.WithOrigins("https://julehjul.madpro.dk/api")
        .AllowAnyHeader()
        .AllowAnyMethod();
  });
});

var app = builder.Build();

// In-memory, process-local flag that prevents more than one successful spin per container lifetime.
// 0 = not spun yet, 1 = already spun
int hasSpun = 0;

// Store the winning label (e.g. "50%", "75%", "100%") so clients can fetch it after refresh.
string? winningLabel = null;

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.UseHttpsRedirection();

app.MapPost("/sendresult", (SendResultRequest request) =>
{
  // Allow only a single successful spin per container lifetime.
  // Interlocked.CompareExchange returns the original value; if it was 0 we set it to 1 and proceed.
  var prev = Interlocked.CompareExchange(ref hasSpun, 1, 0);
  if (prev == 1)
  {
    return Task.FromResult(Results.StatusCode(403));
  }

  // Process the received roll result
  SendMail(request.Roll);

  // Store a human-friendly winning label for clients to show after refresh
  winningLabel = request.Roll switch
  {
    RollEnum.HalfPrice => "50%",
    RollEnum.SeventyFivePercent => "75%",
    RollEnum.FullPrice => "100%",
    _ => null
  };
  return Task.FromResult(Results.Ok());
}).Accepts<SendResultRequest>("application/json");

app.MapGet("/spinresult", () =>
{
  if (winningLabel is null) return Results.Empty;
  return Results.Ok(new { label = winningLabel });
});

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
