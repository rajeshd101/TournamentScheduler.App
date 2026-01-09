using TournamentScheduler.App.Components;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use a fixed port
builder.WebHost.UseUrls("http://localhost:5219");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<TournamentScheduler.App.Services.SchedulerService>();
builder.Services.AddScoped<TournamentScheduler.App.Services.TournamentState>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Auto-launch browser after server starts
app.Lifetime.ApplicationStarted.Register(() =>
{
    var url = "http://localhost:5219";
    Console.WriteLine($"\n========================================");
    Console.WriteLine($"  TourneyPro v1.5");
    Console.WriteLine($"  Running at: {url}");
    Console.WriteLine($"  Press Ctrl+C to stop the application");
    Console.WriteLine($"========================================\n");
});

app.Run();
