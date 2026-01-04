using TournamentScheduler.App.Models;

namespace TournamentScheduler.App.Services;

public class SchedulerService
{
    public List<Match> Generate(List<Player> players, TournamentConfig config, List<Match>? existingHistory = null)
    {
        Console.WriteLine($"[Scheduler] Generate called. Players: {players.Count}, Courts: {config.Courts}, GenderRule: {config.GenderRule}");
        
        if (players.Count < 4) 
        {
            return existingHistory ?? new List<Match>();
        }

        var history = existingHistory ?? new List<Match>();
        int startRound = history.Any() ? history.Max(m => m.Round) + 1 : 1;
        
        DateTime start = config.StartTime;
        int interval = config.Duration + config.BreakTime;
        int totalMinutes = (int)(config.EndTime - start).TotalMinutes;
        int maxRounds = totalMinutes / interval;
        
        if (maxRounds < startRound) return history;

        // Track match counts for fairness and last round played for resting
        var playerMatchCounts = players.ToDictionary(p => p.Id, p => 0);
        var playerLastRound = players.ToDictionary(p => p.Id, p => 0);
        
        foreach(var m in history)
        {
            void UpdateStats(string id, int r) {
                if(playerMatchCounts.ContainsKey(id)) playerMatchCounts[id]++;
                if(playerLastRound.ContainsKey(id) && r > playerLastRound[id]) playerLastRound[id] = r;
            }
            UpdateStats(m.P1.Id, m.Round);
            UpdateStats(m.P2.Id, m.Round);
            UpdateStats(m.P3.Id, m.Round);
            UpdateStats(m.P4.Id, m.Round);
        }

        var scheduled = new List<Match>(history);

        for (int round = startRound; round <= maxRounds; round++)
        {
            var roundPlayers = new HashSet<string>();
            string timeStr = start.AddMinutes((round - 1) * interval).ToString("yyyy-MM-dd HH:mm");

            // Mandatory condition: Fill 100% occupancy of the courts as long as we have minimum of 4 players per court.
            for (int court = 1; court <= config.Courts; court++)
            {
                var availablePlayers = players
                    .Where(p => !roundPlayers.Contains(p.Id))
                    .OrderBy(p => playerLastRound[p.Id] < round - 1 ? 0 : 1) // Rest functionality: prioritize those who didn't play last round
                    .ThenBy(p => playerMatchCounts[p.Id]) // Then fairness: prioritize those with fewest games
                    .ThenBy(p => p.RegistrationTime) // Then by registration time
                    .ToList();

                if (availablePlayers.Count < 4) break; // Not enough players left for another court

                List<Player> selected = new();

                if (config.GenderRule == "MixedDouble")
                {
                    // Try to get 2 Men and 2 Women for a Mixed Double match, but prioritize fairness
                    var mAvail = availablePlayers.Where(p => string.Equals(p.Gender, "M", StringComparison.OrdinalIgnoreCase)).ToList();
                    var fAvail = availablePlayers.Where(p => string.Equals(p.Gender, "F", StringComparison.OrdinalIgnoreCase)).ToList();

                    // If we have enough of both genders among the MOST BEHIND players, use them
                    // We look at the top 8 available players to see if we can form a mixed match while staying fair
                    var top8 = availablePlayers.Take(8).ToList();
                    var mTop8 = top8.Where(p => string.Equals(p.Gender, "M", StringComparison.OrdinalIgnoreCase)).ToList();
                    var fTop8 = top8.Where(p => string.Equals(p.Gender, "F", StringComparison.OrdinalIgnoreCase)).ToList();

                    if (mTop8.Count >= 2 && fTop8.Count >= 2)
                    {
                        selected.Add(mTop8[0]);
                        selected.Add(fTop8[0]);
                        selected.Add(mTop8[1]);
                        selected.Add(fTop8[1]);
                    }
                    else
                    {
                        // Fallback: Just take the most "behind" players regardless of gender to ensure 100% occupancy and fairness
                        selected = availablePlayers.Take(4).ToList();
                    }
                }
                else if (config.GenderRule == "Doubles" || config.GenderRule == "Singles")
                {
                    // Same gender preference for these modes
                    var mAvail = availablePlayers.Where(p => string.Equals(p.Gender, "M", StringComparison.OrdinalIgnoreCase)).ToList();
                    var fAvail = availablePlayers.Where(p => string.Equals(p.Gender, "F", StringComparison.OrdinalIgnoreCase)).ToList();

                    if (mAvail.Count >= 4) selected = mAvail.Take(4).ToList();
                    else if (fAvail.Count >= 4) selected = fAvail.Take(4).ToList();
                    else selected = availablePlayers.Take(4).ToList();
                }
                else
                {
                    selected = availablePlayers.Take(4).ToList();
                }

                if (selected.Count == 4)
                {
                    var match = new Match
                    {
                        Id = Guid.NewGuid().ToString("N")[..8],
                        Round = round,
                        Court = court,
                        Time = timeStr,
                        P1 = selected[0], P2 = selected[1],
                        P3 = selected[2], P4 = selected[3],
                        ServingTeam = Random.Shared.Next(1, 3)
                    };

                    scheduled.Add(match);
                    foreach (var p in selected)
                    {
                        roundPlayers.Add(p.Id);
                        playerMatchCounts[p.Id]++;
                        playerLastRound[p.Id] = round;
                    }
                }
            }
        }

        return scheduled;
    }
}
