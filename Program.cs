using TournamentScheduler.App.Components;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use a fixed port
builder.WebHost.UseUrls("http://localhost:5000");

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
    var url = "http://localhost:5000";
    Console.WriteLine($"\n========================================");
    Console.WriteLine($"  Tournament Scheduler v1.5");
    Console.WriteLine($"  Running at: {url}");
    Console.WriteLine($"  Press Ctrl+C to stop the application");
    Console.WriteLine($"========================================\n");
    
    // Open default browser
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not open browser automatically: {ex.Message}");
        Console.WriteLine($"Please open {url} in your browser manually.");
    }
});

app.Run();
