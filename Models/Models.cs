
namespace TournamentScheduler.App.Models;

public class Player
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..6];
    public string Name { get; set; } = "";
    public int Skill { get; set; } = 3;
    public string Gender { get; set; } = "M"; // M, F
    public string Color { get; set; } = "#ffffff";
    public DateTime RegistrationTime { get; set; } = DateTime.Now;
    public bool IsOut { get; set; } = false;
}

public class Match
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public Player P1 { get; set; } = new();
    public Player P2 { get; set; } = new();
    public Player P3 { get; set; } = new();
    public Player P4 { get; set; } = new();
    
    public int Round { get; set; }
    public int Court { get; set; }
    public string Time { get; set; } = "";
    
    public int? Score1 { get; set; }
    public int? Score2 { get; set; }
    
    public int ServingTeam { get; set; } = 1; // 1 = Team 1 (P1+P2) serves first, 2 = Team 2 (P3+P4)
}

public class TournamentConfig
{
    public int NumPlayers { get; set; } = 40;
    public int Courts { get; set; } = 4;
    public int Duration { get; set; } = 12;
    public int BreakTime { get; set; } = 1;
    public DateTime StartTime { get; set; } = DateTime.Today.AddHours(18); // 18:00 Today
    public DateTime EndTime { get; set; } = DateTime.Today.AddHours(21).AddMinutes(30);   // 21:30 Today
    public string Mode { get; set; } = "balanced"; // 'balanced' or 'complete'
    public string GenderRule { get; set; } = "Doubles"; // Doubles, Singles, MixedDouble
}
