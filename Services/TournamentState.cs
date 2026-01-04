
using TournamentScheduler.App.Models;

namespace TournamentScheduler.App.Services;

public class TournamentState
{
    public List<Player> Players { get; set; } = new();
    public List<Match> Schedule { get; set; } = new();
    public TournamentConfig Config { get; set; } = new();

    public event Action? OnChange;
    public event Action? OnSearchChanged; // Specific event for search to avoid full state re-renders if possible
    
    public string TournamentName { get; private set; } = "MyTournament";
    public string SearchTerm { get; private set; } = "";

    public void SetSearch(string term)
    {
        if (SearchTerm != term)
        {
            SearchTerm = term;
            OnSearchChanged?.Invoke();
        }
    }
    
    // Configurable Data Path (Default to "Documents/TournamentScheduler" folder)
    public string DataPath { get; set; } = System.IO.Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), 
        "TournamentScheduler");
    public bool AutoSaveEnabled { get; set; } = true;

    public TournamentState()
    {
        if (!System.IO.Directory.Exists(DataPath)) System.IO.Directory.CreateDirectory(DataPath);
        // Try load last used or default
        LoadTournament("MyTournament");
    }

    public void GenerateDemoPlayers()
    {
        Players.Clear();
        for (int i = 1; i <= 20; i++) AddPlayer($"Man {i}", "M");
        for (int i = 1; i <= 20; i++) AddPlayer($"Woman {i}", "F");
        NotifyStateChanged();
    }
    
    public string GetContrastColor(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#") || hexColor.Length != 7) return "#000000";
        // Simple luminance calculation
        int r = int.Parse(hexColor.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
        int g = int.Parse(hexColor.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
        int b = int.Parse(hexColor.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
        double luminance = (0.299 * r + 0.587 * g + 0.114 * b);
        return luminance > 128 ? "#000000" : "#ffffff";
    }

    public void AddPlayer(string name, string gender = "M", int skill = 3)
    {
        Console.WriteLine($"[State] Adding player: {name} ({gender})");
        if (Players.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return;
        
        // Ensure unique ID
        var id = Guid.NewGuid().ToString("N")[..6];
        string color = GeneratePastelColor();
        Players.Add(new Player { Id = id, Name = name, Gender = gender, Color = color, Skill = skill });
        NotifyStateChanged();
    }

    private string GeneratePastelColor()
    {
        var rng = new Random();
        int r = rng.Next(150, 255);
        int g = rng.Next(150, 255);
        int b = rng.Next(150, 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    public void RemovePlayer(string id)
    {
        var p = Players.FirstOrDefault(x => x.Id == id);
        if (p != null)
        {
            Players.Remove(p);
            NotifyStateChanged();
        }
    }

    public void RemoveAllPlayers()
    {
        Players.Clear();
        NotifyStateChanged();
    }

    public void UpdatePlayer(string id, string name, string gender)
    {
        var p = Players.FirstOrDefault(x => x.Id == id);
        if (p != null)
        {
            p.Name = name;
            p.Gender = gender;
            NotifyStateChanged();
        }
    }

    public void UpdatePlayerDetails(string id, string name, string gender, string color, DateTime registrationTime, int skill)
    {
        var p = Players.FirstOrDefault(x => x.Id == id);
        if (p != null)
        {
            p.Name = name;
            p.Gender = gender;
            p.Color = color;
            p.RegistrationTime = registrationTime;
            p.Skill = skill;
            NotifyStateChanged();
        }
    }

    public void UpdateMatchScore(string matchId, int? s1, int? s2)
    {
        var m = Schedule.FirstOrDefault(x => x.Id == matchId);
        if (m != null)
        {
            m.Score1 = s1;
            m.Score2 = s2;
            NotifyStateChanged();
        }
    }

    public void SetSchedule(List<Match> matches)
    {
        Schedule = matches;
        NotifyStateChanged();
    }
    
    // Called when a player drops out: Preservation Logic
    public void RegenerateSchedule(Services.SchedulerService scheduler)
    {
        if (!Schedule.Any()) return;

        // 1. Identify "Active History" vs "Future to Regenerate"
        // Rule: Keep matches that have Scores (Completed) OR are in the past/active round.
        // Simple heuristic: If a match has a score, it's history.
        // If a match is in a round that has ANY score, maybe keep that whole round?
        // User Request: "remaining matches should be zero... schedule changed from point where he is out"
        
        // Let's define "Point where he is out" as: The first round that has NOT started or has NO scores yet.
        // Actually, safer: Keep all matches that have scores. Keep all matches in rounds < Current Time?
        // Let's stick to: Keep all matches where Round < Current Theoretical Round?
        // Or strictly: Keep matches with scores. Remove unscheduled/future matches involving the OUT player.
        // But we need to regenerate the structure or else we leave holes.
        
        // Robust Logic:
        // Find maximum round number that has at least one scored match.
        // Let's say Round 3 is active and partially scored. We keep Round 3.
        // We regenerate Round 4+.
        
        int maxScoredRound = Schedule.Where(m => m.Score1.HasValue || m.Score2.HasValue).Select(m => m.Round).DefaultIfEmpty(0).Max();
        
        // Also consider time. If Round 3 is active but no scores yet, we probably shouldn't wipe it if people are on court.
        // But for "Out", usually implies "I'm leaving now".
        // Let's use maxScoredRound as the baseline.
        // We KEEP everything <= maxScoredRound.
        
        var history = Schedule.Where(m => m.Round <= maxScoredRound).ToList();
        
        // Active Players for Future (All currently not out)
        // NOTE: The dropout player IS ALREADY marked IsOut in state before this is called.
        var activePlayers = Players.Where(p => !p.IsOut).ToList();
        
        if (activePlayers.Count < 4) return; // Cannot schedule
        
        // 2. Regenerate
        var newFullSchedule = scheduler.Generate(activePlayers, Config, history);
        
        // 3. Update
        SetSchedule(newFullSchedule);
    }

    // --- Persistence & Export ---

    public void NewTournament(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        TournamentName = name;
        Players.Clear();
        Schedule.Clear();
        Config = new TournamentConfig(); // Reset config
        NotifyStateChanged();
    }

    public List<string> GetTournamentList()
    {
        if (!System.IO.Directory.Exists(DataPath)) return new List<string>();
        
        // Scan for Subdirectories that contain a matching JSON
        var dirs = System.IO.Directory.GetDirectories(DataPath);
        var list = new List<string>();
        foreach (var d in dirs)
        {
            var name = System.IO.Path.GetFileName(d);
            var jsonPath = System.IO.Path.Combine(d, name + ".json");
            if (System.IO.File.Exists(jsonPath))
            {
                list.Add(name);
            }
        }
        return list;
    }

    public void LoadTournament(string name)
    {
        this.TournamentName = name; // Always set name first
        
        // Path: Documents/TourneyPro/{Name}/{Name}.json
        string folder = System.IO.Path.Combine(DataPath, name);
        string filePath = System.IO.Path.Combine(folder, name + ".json");
        
        if (System.IO.File.Exists(filePath))
        {
            try 
            {
                string json = System.IO.File.ReadAllText(filePath);
                var data = System.Text.Json.JsonSerializer.Deserialize<TournamentData>(json);
                if (data != null)
                {
                    this.Players = data.Players ?? new List<Player>();
                    this.Config = data.Config ?? new TournamentConfig();
                    this.Schedule = data.Schedule ?? new List<Match>();
                    // No need to set Name again
                    NotifyStateChanged(autoSave: false); // Don't verify save on load
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
            }
        }
        // If not exists, we just start with empty state for this new name
    }

    public string ExportScheduleToCsv()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Round,Time,Court,Player1,Player2,Player3,Player4,Score1,Score2,Winner");

        foreach (var m in Schedule.OrderBy(x => x.Round).ThenBy(x => x.Court))
        {
            string winner = "";
            if (m.Score1.HasValue && m.Score2.HasValue)
            {
                if (m.Score1 > m.Score2) winner = $"{m.P1.Name}/{m.P2.Name}";
                else if (m.Score2 > m.Score1) winner = $"{m.P3.Name}/{m.P4.Name}";
                else winner = "Draw";
            }

            sb.AppendLine($"{m.Round},{m.Time},{m.Court},{m.P1.Name},{m.P2.Name},{m.P3.Name},{m.P4.Name},{m.Score1},{m.Score2},{winner}");
        }
        return sb.ToString();
    }

    public void SaveData(string folderPath) // Manual Export/Save
    {
        if (string.IsNullOrWhiteSpace(folderPath)) return;
        if (!System.IO.Directory.Exists(folderPath)) System.IO.Directory.CreateDirectory(folderPath);
        
        // Save CSV
        System.IO.File.WriteAllText(System.IO.Path.Combine(folderPath, $"{TournamentName}_Schedule.csv"), ExportScheduleToCsv());
        
        // Save JSON
        SaveInternal(System.IO.Path.Combine(folderPath, $"{TournamentName}.json"));
        
        // Update DataPath if successful manual save implies new working directory? 
        // Let's NOT implicitly change DataPath unless user intentionally sets it in UI.
        // But for "Save & Export", we usually just dump files there.
    }

    private void SaveInternal(string filePath)
    {
        try {
            var data = new TournamentData
            {
                Players = this.Players,
                Config = this.Config,
                Schedule = this.Schedule
            };
            string json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, json);
        } catch (Exception ex) {
            Console.WriteLine($"[Error Saving] {ex.Message}");
        }
    }

    public void LoadData(string filePath) // Manual Load
    {
        if (!System.IO.File.Exists(filePath)) return;
        try 
        {
            string json = System.IO.File.ReadAllText(filePath);
            var data = System.Text.Json.JsonSerializer.Deserialize<TournamentData>(json);
            if (data != null)
            {
                this.Players = data.Players ?? new List<Player>();
                this.Config = data.Config ?? new TournamentConfig();
                this.Schedule = data.Schedule ?? new List<Match>();
                NotifyStateChanged();
            }
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
    }

    public class TournamentData
    {
        public List<Player> Players { get; set; } = new();
        public TournamentConfig Config { get; set; } = new();
        public List<Match> Schedule { get; set; } = new();
    }

    public void NotifyStateChanged(bool autoSave = true) 
    {
        if (autoSave && AutoSaveEnabled)
        {
            // Auto-persist to Documents/TourneyPro/{Name}/{Name}.json
            string folder = System.IO.Path.Combine(DataPath, TournamentName);
            if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
            
            string path = System.IO.Path.Combine(folder, TournamentName + ".json");
            SaveInternal(path);
        }
        OnChange?.Invoke();
    }
}
