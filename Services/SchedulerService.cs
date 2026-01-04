using TournamentScheduler.App.Models;

namespace TournamentScheduler.App.Services;

public class SchedulerService
{
    public List<Match> Generate(List<Player> players, TournamentConfig config, List<Match>? existingHistory = null)
    {
        Console.WriteLine($"[Scheduler] Generate called. Players: {players.Count}, Courts: {config.Courts}, Mode: {config.Mode}");
        if (players.Count < 4) 
        {
            Console.WriteLine("[Scheduler] Not enough players (<4). Returning empty.");
            return existingHistory ?? new List<Match>();
        }

        var allPairs = GenerateAllPairs(players, config);
        var solutions = new System.Collections.Concurrent.ConcurrentBag<List<Match>>();
        int attempts = 12; 

        // Analyze History once
        var history = existingHistory ?? new List<Match>();
        int startRound = history.Any() ? history.Max(m => m.Round) + 1 : 1;
        
        // Count previous games for fairness
        var historyCounts = players.ToDictionary(p => p.Id, p => 0);
        foreach(var m in history)
        {
            if(historyCounts.ContainsKey(m.P1.Id)) historyCounts[m.P1.Id]++;
            if(historyCounts.ContainsKey(m.P2.Id)) historyCounts[m.P2.Id]++;
            if(historyCounts.ContainsKey(m.P3.Id)) historyCounts[m.P3.Id]++;
            if(historyCounts.ContainsKey(m.P4.Id)) historyCounts[m.P4.Id]++;
        }

        // Determine Rest Status
        var historyFreeAt = players.ToDictionary(p => p.Id, p => 1);
        foreach(var m in history)
        {
             // If played in round R, free at R+2
             int freeAt = m.Round + 2;
             if (historyFreeAt.ContainsKey(m.P1.Id) && freeAt > historyFreeAt[m.P1.Id]) historyFreeAt[m.P1.Id] = freeAt;
             if (historyFreeAt.ContainsKey(m.P2.Id) && freeAt > historyFreeAt[m.P2.Id]) historyFreeAt[m.P2.Id] = freeAt;
             if (historyFreeAt.ContainsKey(m.P3.Id) && freeAt > historyFreeAt[m.P3.Id]) historyFreeAt[m.P3.Id] = freeAt;
             if (historyFreeAt.ContainsKey(m.P4.Id) && freeAt > historyFreeAt[m.P4.Id]) historyFreeAt[m.P4.Id] = freeAt;
        }

        Parallel.For(0, attempts, _ =>
        {
            var pool = new List<Pair>(allPairs);
            Shuffle(pool);
            
            var matchCandidates = GenerateMatchesFromPairs(pool, config);
            
            // Pass History Context
            var newSchedule = SolveSchedule(matchCandidates, players, config, startRound, new Dictionary<string, int>(historyCounts), new Dictionary<string, int>(historyFreeAt));
            
            // Combine History + New
            var fullSchedule = new List<Match>(history);
            fullSchedule.AddRange(newSchedule);
            solutions.Add(fullSchedule);
        });

        return solutions
            .OrderByDescending(s => s.Count)
            .ThenBy(s => s.Count > 0 ? s.Max(m => m.Round) : 0)
            .FirstOrDefault() ?? new List<Match>();
    }
    
    private List<Pair> GenerateAllPairs(List<Player> players, TournamentConfig config)
    {
        var pairs = new List<Pair>();
        var men = players.Where(p => string.Equals(p.Gender, "M", StringComparison.OrdinalIgnoreCase)).ToList();
        var women = players.Where(p => string.Equals(p.Gender, "F", StringComparison.OrdinalIgnoreCase)).ToList();
        
        switch (config.GenderRule)
        {
            case "Doubles":
                // Doubles: M+M pairs AND F+F pairs (same gender teams)
                for (int i = 0; i < men.Count; i++)
                    for (int j = i + 1; j < men.Count; j++)
                        pairs.Add(new Pair(men[i], men[j]));
                for (int i = 0; i < women.Count; i++)
                    for (int j = i + 1; j < women.Count; j++)
                        pairs.Add(new Pair(women[i], women[j]));
                break;
                
            case "Singles":
                // Singles: Each player is a "pair" by themselves for 1v1 matches
                foreach (var m in men)
                    pairs.Add(new Pair(m, m));
                foreach (var w in women)
                    pairs.Add(new Pair(w, w));
                break;
                
            case "MixedDouble":
            default:
                // Mixed Doubles: M+F pairs preferred
                var usedMen = new HashSet<string>();
                var usedWomen = new HashSet<string>();
                
                // 1. Create as many M+F pairs as possible
                int minCount = Math.Min(men.Count, women.Count);
                for (int i = 0; i < minCount; i++)
                {
                    pairs.Add(new Pair(men[i], women[i]));
                }
                
                // 2. If there are leftover men or women, pair them with each other (M+M or F+F)
                if (men.Count > women.Count)
                {
                    for (int i = minCount; i < men.Count; i += 2)
                    {
                        if (i + 1 < men.Count)
                            pairs.Add(new Pair(men[i], men[i + 1]));
                        else
                            pairs.Add(new Pair(men[i], men[i])); // Single if odd (will be handled by match logic)
                    }
                }
                else if (women.Count > men.Count)
                {
                    for (int i = minCount; i < women.Count; i += 2)
                    {
                        if (i + 1 < women.Count)
                            pairs.Add(new Pair(women[i], women[i + 1]));
                        else
                            pairs.Add(new Pair(women[i], women[i])); // Single if odd
                    }
                }
                break;
        }
        
        return pairs;
    }

    private List<Match> GenerateMatchesFromPairs(List<Pair> pairs, TournamentConfig config)
    {
        // Simple greedy grouping of pairs into 4-player matches
        var candidates = new List<Match>();
        var usedIndices = new HashSet<int>();

        for (int i = 0; i < pairs.Count; i++)
        {
            if (usedIndices.Contains(i)) continue;
            var pA = pairs[i];

            for (int j = i + 1; j < pairs.Count; j++)
            {
                if (usedIndices.Contains(j)) continue;
                var pB = pairs[j];

                // Check Overlap
                if (pA.P1.Id == pB.P1.Id || pA.P1.Id == pB.P2.Id || 
                    pA.P2.Id == pB.P1.Id || pA.P2.Id == pB.P2.Id)
                {
                    continue;
                }
                
                // Gender Consistency Check for Doubles and Singles modes
                if (config.GenderRule == "Doubles" || config.GenderRule == "Singles")
                {
                    // Same-gender modes: Men only play men, women only play women
                    // Pairs must match against same-gender pairs only
                    if (!string.Equals(pA.P1.Gender, pB.P1.Gender, StringComparison.OrdinalIgnoreCase)) 
                        continue;
                }
                // MixedDouble: M+F vs M+F - no extra check needed since pairs are already M+F

                candidates.Add(new Match
                {
                    P1 = pA.P1, P2 = pA.P2,
                    P3 = pB.P1, P4 = pB.P2
                });
                usedIndices.Add(i);
                usedIndices.Add(j);
                break;
            }
        }
        return candidates;
    }

    private List<Match> SolveSchedule(List<Match> pool, List<Player> players, TournamentConfig config, int startRound, Dictionary<string, int> initialCounts, Dictionary<string, int> initialRest)
    {
        var scheduled = new List<Match>();
        var remainingPool = new List<Match>(pool);

        DateTime start = config.StartTime;
        DateTime end = config.EndTime;
        
        int interval = config.Duration + config.BreakTime;
        int totalMinutes = (int)(end - start).TotalMinutes;
        int maxRounds = totalMinutes / interval;
        
        if (maxRounds % 2 != 0) maxRounds--;

        // Initialize State from History
        var playerFreeAtRound = new Dictionary<string, int>(initialRest);
        var playerMatchCounts = new Dictionary<string, int>(initialCounts);
        var playerCourtsCovered = new Dictionary<string, HashSet<int>>(); // Track which courts each player has played on

        // Ensure keys exist for all current players (in case some are new or history missed them)
        foreach(var p in players)
        {
            if(!playerFreeAtRound.ContainsKey(p.Id)) playerFreeAtRound[p.Id] = 1;
            if(!playerMatchCounts.ContainsKey(p.Id)) playerMatchCounts[p.Id] = 0;
            if(!playerCourtsCovered.ContainsKey(p.Id)) playerCourtsCovered[p.Id] = new HashSet<int>();
        }

        for (int round = startRound; round <= maxRounds; round++)
        {
            if (remainingPool.Count == 0) break;
            
            // Calculate time for this round
            DateTime roundStartTime = start.AddMinutes((round - 1) * interval);
            string timeStr = roundStartTime.ToString("yyyy-MM-dd HH:mm");

            // Determine allowed gender structure based on mode
            string? targetGender = null;
            
            if (config.GenderRule == "Doubles" || config.GenderRule == "Singles")
            {
                // Alternating rounds: Women = Odd rounds, Men = Even rounds
                // Women get priority in odd rounds, men in even rounds
                targetGender = (round % 2 == 1) ? "F" : "M";
            }

            // Define Pools
            // 100% Court Utilization: Include ALL remaining matches, prioritize rested players via buckets
            
            var allValidTime = remainingPool.ToList(); // All matches eligible for 100% court fill

            // --- TIERED PRIORITY FILL ---
            // User Rule: "In women's rounds, play ALL women. Only give remaining courts to men."
            // Implicit Rule: Women can get ahead in game counts if needed.
            
            // 1. Calculate Fairness Thresholds
            int minM = 0;
            int minF = 0;
            var men = players.Where(p => string.Equals(p.Gender, "M", StringComparison.OrdinalIgnoreCase)).ToList();
            var women = players.Where(p => string.Equals(p.Gender, "F", StringComparison.OrdinalIgnoreCase)).ToList();

            if (men.Any()) minM = men.Min(p => playerMatchCounts[p.Id]);
            if (women.Any()) minF = women.Min(p => playerMatchCounts[p.Id]);

            // Helper to determine match fairness/priority
            // Returns: 0=Best (Target+Fair+Fresh), 1=(Target+Ahead+Fresh), 2=(NonTarget+Fair+Fresh), 3=(NonTarget+Ahead+Fresh), 4=Non-Fresh
            int GetPriority(Match m)
            {
                var ids = new[] { m.P1.Id, m.P2.Id, m.P3.Id, m.P4.Id };

                bool isTarget;
                if (config.GenderRule == "Separated" && targetGender != null)
                     isTarget = string.Equals(m.P1.Gender, targetGender, StringComparison.OrdinalIgnoreCase);
                else 
                     isTarget = true; // Mixed or no-target

                // Fairness Check
                // We define "Strict Fair" as <= Min+1.
                bool isFair = ids.All(id => 
                {
                    int c = playerMatchCounts[id];
                    int limit = string.Equals(players.First(x=>x.Id==id).Gender, "F", StringComparison.OrdinalIgnoreCase) ? minF : minM;
                    return c <= limit + 1;
                });
                
                // Freshness (Can play NOW vs Next)
                bool isFresh = ids.All(id => playerFreeAtRound[id] <= round);
                
            // Buckets: Ignore resting rule as per user request
            // We still prioritize Target gender and Fairness
            if (isTarget) return isFair ? 0 : 1;
            else return isFair ? 2 : 3;
        }

            // Bucketize
            var bucket0 = new List<Match>(); // Target + Fair
            var bucket1 = new List<Match>(); // Target + Ahead
            var bucket2 = new List<Match>(); // NonTarget + Fair
            var bucket3 = new List<Match>(); // NonTarget + Ahead

            foreach(var m in allValidTime)
            {
                int p = GetPriority(m);
                if (p == 0) bucket0.Add(m);
                else if (p == 1) bucket1.Add(m);
                else if (p == 2) bucket2.Add(m);
                else if (p == 3) bucket3.Add(m);
            }
            
            // Sort buckets by specific criteria (Fairness first - equal games for all)
            // Function to sort a list
            void SortBucket(List<Match> list) {
                list.Sort((a, b) => {
                    // Primary: Prioritize matches containing players with LOWEST game counts
                    // Use max to find the "most behind" player in each match
                    int maxA = new[] { playerMatchCounts[a.P1.Id], playerMatchCounts[a.P2.Id], playerMatchCounts[a.P3.Id], playerMatchCounts[a.P4.Id] }.Max();
                    int maxB = new[] { playerMatchCounts[b.P1.Id], playerMatchCounts[b.P2.Id], playerMatchCounts[b.P3.Id], playerMatchCounts[b.P4.Id] }.Max();
                    if (maxA != maxB) return maxA.CompareTo(maxB); // Prefer matches where most-played player has fewer games
                    
                    // Secondary: Sum of game counts (lower = more behind players)
                    int sumA = playerMatchCounts[a.P1.Id] + playerMatchCounts[a.P2.Id] + playerMatchCounts[a.P3.Id] + playerMatchCounts[a.P4.Id];
                    int sumB = playerMatchCounts[b.P1.Id] + playerMatchCounts[b.P2.Id] + playerMatchCounts[b.P3.Id] + playerMatchCounts[b.P4.Id];
                    if (sumA != sumB) return sumA.CompareTo(sumB); // Ascending count (fairness)
                    
                    // Tertiary: Skill matching (prefer matches where all players have similar skill)
                    int skillRangeA = new[] { a.P1.Skill, a.P2.Skill, a.P3.Skill, a.P4.Skill }.Max() - new[] { a.P1.Skill, a.P2.Skill, a.P3.Skill, a.P4.Skill }.Min();
                    int skillRangeB = new[] { b.P1.Skill, b.P2.Skill, b.P3.Skill, b.P4.Skill }.Max() - new[] { b.P1.Skill, b.P2.Skill, b.P3.Skill, b.P4.Skill }.Min();
                    if (skillRangeA != skillRangeB) return skillRangeA.CompareTo(skillRangeB); // Ascending (lower variance = better)
                    
                    // Quaternary: Registration time (FIFO)
                    return (a.P1.RegistrationTime.Ticks).CompareTo(b.P1.RegistrationTime.Ticks);
                });
            }
            
            SortBucket(bucket0);
            SortBucket(bucket1);
            SortBucket(bucket2);
            SortBucket(bucket3);
            
            // Fill Logic
            var roundSelection = new List<Match>();
            var roundPlayers = new HashSet<string>();

            void FillFrom(List<Match> candidates)
            {
                int needed = config.Courts - roundSelection.Count;
                if (needed <= 0) return;

                var filtered = candidates.Where(m => 
                    !roundPlayers.Contains(m.P1.Id) && !roundPlayers.Contains(m.P2.Id) &&
                    !roundPlayers.Contains(m.P3.Id) && !roundPlayers.Contains(m.P4.Id)
                ).ToList();

                var picked = PickBestSubset(filtered, needed);
                foreach(var m in picked)
                {
                    roundSelection.Add(m);
                    roundPlayers.Add(m.P1.Id); roundPlayers.Add(m.P2.Id); roundPlayers.Add(m.P3.Id); roundPlayers.Add(m.P4.Id);
                }
            }

            FillFrom(bucket0);
            FillFrom(bucket1); // This ensures we maximize Target Gender even if they are ahead
            FillFrom(bucket2);
            FillFrom(bucket3);




            if (roundSelection.Count > 0)
            {
                // Smart Court Assignment: Prioritize putting players on courts they haven't played on yet
                var assignedCourts = new HashSet<int>();
                var matchesToAssign = new List<Match>(roundSelection);

                for (int court = 1; court <= config.Courts && matchesToAssign.Any(); court++)
                {
                    // Score each match: Count how many of its 4 players have NOT played on this court (higher = better)
                    var best = matchesToAssign
                        .OrderByDescending(m => new[] { m.P1.Id, m.P2.Id, m.P3.Id, m.P4.Id }
                            .Count(id => !playerCourtsCovered[id].Contains(court)))
                        .ThenBy(m => playerMatchCounts[m.P1.Id] + playerMatchCounts[m.P2.Id] + playerMatchCounts[m.P3.Id] + playerMatchCounts[m.P4.Id]) // Secondary: Fairness
                        .First();

                    best.Round = round;
                    best.Court = court;
                    best.Time = timeStr;
                    best.ServingTeam = Random.Shared.Next(1, 3); // Randomly assign 1 or 2

                    scheduled.Add(best);
                    matchesToAssign.Remove(best);
                    assignedCourts.Add(court);

                    var ids = new[] { best.P1.Id, best.P2.Id, best.P3.Id, best.P4.Id };
                    foreach (var id in ids)
                    {
                        playerFreeAtRound[id] = round + 2;
                        playerMatchCounts[id]++;
                        playerCourtsCovered[id].Add(court); // Track court coverage
                    }

                    remainingPool.Remove(best);
                }
            }
        }

        return scheduled;
    }

    private List<Match> PickBestSubset(List<Match> matches, int maxCount)
    {
        // Increased for better court utilization stability. 200 is exhaustive enough for reasonable court sizes.
        var candidates = matches.Take(200).ToList();
        List<Match> bestSubset = new();

        void Backtrack(int index, List<Match> current, HashSet<string> usedPlayers)
        {
            if (bestSubset.Count == maxCount) return;

            if (current.Count > bestSubset.Count) 
                bestSubset = new List<Match>(current);

            if (current.Count == maxCount) return;
            if (index >= candidates.Count) return;

            var m = candidates[index];
            var pIds = new[] { m.P1.Id, m.P2.Id, m.P3.Id, m.P4.Id };

            if (!pIds.Any(usedPlayers.Contains))
            {
                // Include
                var newUsed = new HashSet<string>(usedPlayers);
                foreach(var id in pIds) newUsed.Add(id);
                
                var newCurrent = new List<Match>(current) { m };
                Backtrack(index + 1, newCurrent, newUsed);
                if (bestSubset.Count == maxCount) return;
            }

            // Pruning
            if ((candidates.Count - 1 - index) + current.Count > bestSubset.Count)
            {
                Backtrack(index + 1, current, usedPlayers);
            }
        }

        Backtrack(0, new List<Match>(), new HashSet<string>());
        return bestSubset;
    }

    private void Shuffle<T>(List<T> list)
    {
        var rng = new Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    private record Pair(Player P1, Player P2);
}
