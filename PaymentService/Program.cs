using Microsoft.EntityFrameworkCore;
using PaymentService;
using PaymentService.Background;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PaymentDbContext>(o => 
    o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<OrderConsumer>();
builder.Services.AddHostedService<InboxProcessor>();
builder.Services.AddHostedService<OutboxPublisher>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment API v1"));

app.MapControllers();
app.Run();