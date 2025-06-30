using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using push_notif.Services;
using push_notif.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<List<Meeting>>();
builder.Services.AddControllers();
builder.Services.AddSingleton<PushNotificationService>();
builder.Services.AddHostedService<NotificationScheduler>();


var app = builder.Build();

app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();


app.Run();