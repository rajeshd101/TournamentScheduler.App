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
        
        // Track partner pairings (how many times two players have been on the same team)
        var partnerCounts = new Dictionary<string, int>();
        
        // Track opponent matchups (how many times two players have faced each other)
        var opponentCounts = new Dictionary<string, int>();

        // Track which courts each player has played on
        var playerCourts = players.ToDictionary(p => p.Id, p => new HashSet<int>());

        // Track who played in the same match in the previous round to avoid consecutive repeats
        var playersInSameMatchLastRound = new Dictionary<string, HashSet<string>>();
        
        // Helper to create a consistent pair key
        string GetPairKey(string id1, string id2) => 
            string.CompareOrdinal(id1, id2) < 0 ? $"{id1}-{id2}" : $"{id2}-{id1}";
        
        int GetPartnerCount(string id1, string id2) => 
            partnerCounts.TryGetValue(GetPairKey(id1, id2), out var c) ? c : 0;
        
        int GetOpponentCount(string id1, string id2) => 
            opponentCounts.TryGetValue(GetPairKey(id1, id2), out var c) ? c : 0;
        
        void IncrementPartner(string id1, string id2)
        {
            var key = GetPairKey(id1, id2);
            partnerCounts[key] = partnerCounts.TryGetValue(key, out var c) ? c + 1 : 1;
        }
        
        void IncrementOpponent(string id1, string id2)
        {
            var key = GetPairKey(id1, id2);
            opponentCounts[key] = opponentCounts.TryGetValue(key, out var c) ? c + 1 : 1;
        }
        
        // Initialize tracking from history
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
            
            // Track partner history
            IncrementPartner(m.P1.Id, m.P2.Id); // Team 1
            IncrementPartner(m.P3.Id, m.P4.Id); // Team 2
            
            // Track opponent history
            IncrementOpponent(m.P1.Id, m.P3.Id);
            IncrementOpponent(m.P1.Id, m.P4.Id);
            IncrementOpponent(m.P2.Id, m.P3.Id);
            IncrementOpponent(m.P2.Id, m.P4.Id);

            // Track court history
            if (playerCourts.ContainsKey(m.P1.Id)) playerCourts[m.P1.Id].Add(m.Court);
            if (playerCourts.ContainsKey(m.P2.Id)) playerCourts[m.P2.Id].Add(m.Court);
            if (playerCourts.ContainsKey(m.P3.Id)) playerCourts[m.P3.Id].Add(m.Court);
            if (playerCourts.ContainsKey(m.P4.Id)) playerCourts[m.P4.Id].Add(m.Court);

            // Initialize last round pairings if this was the most recent round in history
            if (m.Round == startRound - 1)
            {
                var matchPlayers = new[] { m.P1.Id, m.P2.Id, m.P3.Id, m.P4.Id };
                foreach (var pId in matchPlayers)
                {
                    if (!playersInSameMatchLastRound.ContainsKey(pId)) playersInSameMatchLastRound[pId] = new HashSet<string>();
                    foreach (var otherId in matchPlayers)
                    {
                        if (pId != otherId) playersInSameMatchLastRound[pId].Add(otherId);
                    }
                }
            }
        }

        var scheduled = new List<Match>(history);
        var currentRoundPairings = new Dictionary<string, HashSet<string>>();

        for (int round = startRound; round <= maxRounds; round++)
        {
            var roundPlayers = new HashSet<string>();
            currentRoundPairings.Clear();
            string timeStr = start.AddMinutes((round - 1) * interval).ToString("yyyy-MM-dd HH:mm");

            // Mandatory condition: Fill 100% occupancy of the courts as long as we have minimum of 4 players per court.
            for (int court = 1; court <= config.Courts; court++)
            {
                var availablePlayers = players
                    .Where(p => !roundPlayers.Contains(p.Id))
                    .OrderBy(p => playerLastRound[p.Id] < round - 1 ? 0 : 1) // Rest functionality: prioritize those who didn't play last round
                    .ThenBy(p => playerMatchCounts[p.Id]) // Then fairness: prioritize those with fewest games
                    .ThenBy(_ => Random.Shared.Next()) // Maximize randomness for players with same priority
                    .ToList();

                if (availablePlayers.Count < 4) break; // Not enough players left for another court

                List<Player>? bestMatch = null;
                double bestScore = double.MaxValue;

                // Get candidate pool based on gender rules
                List<Player> candidatePool;
                
                if (config.GenderRule == "MixedDouble")
                {
                    // For mixed doubles, we need 2M + 2F from top candidates
                    var top12 = availablePlayers.Take(12).ToList();
                    var mAvail = top12.Where(p => string.Equals(p.Gender, "M", StringComparison.OrdinalIgnoreCase)).ToList();
                    var fAvail = top12.Where(p => string.Equals(p.Gender, "F", StringComparison.OrdinalIgnoreCase)).ToList();
                    
                    if (mAvail.Count >= 2 && fAvail.Count >= 2)
                    {
                        candidatePool = mAvail.Take(4).Concat(fAvail.Take(4)).ToList();
                    }
                    else
                    {
                        candidatePool = availablePlayers.Take(8).ToList();
                    }
                }
                else if (config.GenderRule == "Doubles" || config.GenderRule == "Singles")
                {
                    var mAvail = availablePlayers.Where(p => string.Equals(p.Gender, "M", StringComparison.OrdinalIgnoreCase)).ToList();
                    var fAvail = availablePlayers.Where(p => string.Equals(p.Gender, "F", StringComparison.OrdinalIgnoreCase)).ToList();

                    if (mAvail.Count >= 4) candidatePool = mAvail.Take(8).ToList();
                    else if (fAvail.Count >= 4) candidatePool = fAvail.Take(8).ToList();
                    else candidatePool = availablePlayers.Take(8).ToList();
                }
                else
                {
                    candidatePool = availablePlayers.Take(8).ToList();
                }

                // Generate all valid 4-player combinations and score them
                for (int i = 0; i < candidatePool.Count; i++)
                {
                    for (int j = i + 1; j < candidatePool.Count; j++)
                    {
                        for (int k = j + 1; k < candidatePool.Count; k++)
                        {
                            for (int l = k + 1; l < candidatePool.Count; l++)
                            {
                                var p = new[] { candidatePool[i], candidatePool[j], candidatePool[k], candidatePool[l] };
                                
                                // Check gender rules
                                if (config.GenderRule == "MixedDouble")
                                {
                                    int mCount = p.Count(x => string.Equals(x.Gender, "M", StringComparison.OrdinalIgnoreCase));
                                    if (mCount != 2) continue; // Must be exactly 2M + 2F
                                }
                                else if (config.GenderRule == "Doubles" || config.GenderRule == "Singles")
                                {
                                    // Prefer same gender
                                    int mCount = p.Count(x => string.Equals(x.Gender, "M", StringComparison.OrdinalIgnoreCase));
                                    if (mCount != 0 && mCount != 4) continue; // All same gender preferred
                                }
                                
                                // Try all 3 possible team arrangements: (0,1 vs 2,3), (0,2 vs 1,3), (0,3 vs 1,2)
                                var arrangements = new[]
                                {
                                    (new[] { 0, 1 }, new[] { 2, 3 }),
                                    (new[] { 0, 2 }, new[] { 1, 3 }),
                                    (new[] { 0, 3 }, new[] { 1, 2 })
                                };

                                foreach (var (team1Idx, team2Idx) in arrangements)
                                {
                                    var t1 = new[] { p[team1Idx[0]], p[team1Idx[1]] };
                                    var t2 = new[] { p[team2Idx[0]], p[team2Idx[1]] };
                                    
                                    // For mixed doubles, each team should be M+F
                                    if (config.GenderRule == "MixedDouble")
                                    {
                                        int t1m = t1.Count(x => string.Equals(x.Gender, "M", StringComparison.OrdinalIgnoreCase));
                                        int t2m = t2.Count(x => string.Equals(x.Gender, "M", StringComparison.OrdinalIgnoreCase));
                                        if (t1m != 1 || t2m != 1) continue;
                                    }
                                    
                                    // Calculate partner penalty (lower = better)
                                    double partnerPenalty = GetPartnerCount(t1[0].Id, t1[1].Id) + GetPartnerCount(t2[0].Id, t2[1].Id);
                                    
                                    // Calculate opponent balance penalty (variance of opponent counts)
                                    var oppCounts = new[]
                                    {
                                        GetOpponentCount(t1[0].Id, t2[0].Id),
                                        GetOpponentCount(t1[0].Id, t2[1].Id),
                                        GetOpponentCount(t1[1].Id, t2[0].Id),
                                        GetOpponentCount(t1[1].Id, t2[1].Id)
                                    };
                                    double avgOpp = oppCounts.Average();
                                    double oppVariance = oppCounts.Sum(x => (x - avgOpp) * (x - avgOpp)) / 4.0;
                                    
                                    // Also prefer lower total opponent counts to spread matches
                                    double totalOpp = oppCounts.Sum();
                                    
                                    // Check if any of these players played together in the previous round
                                    bool playedTogetherLastRound = false;
                                    for (int m1 = 0; m1 < 4; m1++)
                                    {
                                        for (int m2 = m1 + 1; m2 < 4; m2++)
                                        {
                                            if (playersInSameMatchLastRound.TryGetValue(p[m1].Id, out var lastRoundPartners) &&
                                                lastRoundPartners.Contains(p[m2].Id))
                                            {
                                                playedTogetherLastRound = true;
                                                break;
                                            }
                                        }
                                        if (playedTogetherLastRound) break;
                                    }

                                    // Calculate court penalty (prefer courts the players haven't played on yet)
                                    double courtPenalty = 0;
                                    foreach (var player in p)
                                    {
                                        if (playerCourts[player.Id].Contains(court))
                                        {
                                            courtPenalty += 1;
                                        }
                                    }

                                    // Combined score: heavily weight partner penalty, then opponent balance
                                    // Add a massive penalty for playing together consecutively
                                    // Add a penalty for repeating a court to encourage rotation
                                    // Add a small random factor to break ties and increase variety
                                    double score = (playedTogetherLastRound ? 10000 : 0) + 
                                                   courtPenalty * 500 +
                                                   partnerPenalty * 100 + 
                                                   totalOpp * 10 + 
                                                   oppVariance + 
                                                   Random.Shared.NextDouble();
                                    
                                    if (score < bestScore)
                                    {
                                        bestScore = score;
                                        bestMatch = new List<Player> { t1[0], t1[1], t2[0], t2[1] };
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Fallback if no valid combination found
                if (bestMatch == null && availablePlayers.Count >= 4)
                {
                    bestMatch = availablePlayers.Take(4).ToList();
                }

                if (bestMatch != null && bestMatch.Count == 4)
                {
                    var match = new Match
                    {
                        Id = Guid.NewGuid().ToString("N")[..8],
                        Round = round,
                        Court = court,
                        Time = timeStr,
                        P1 = bestMatch[0], P2 = bestMatch[1],
                        P3 = bestMatch[2], P4 = bestMatch[3],
                        ServingTeam = Random.Shared.Next(1, 3)
                    };

                    scheduled.Add(match);
                    foreach (var p in bestMatch)
                    {
                        roundPlayers.Add(p.Id);
                        playerMatchCounts[p.Id]++;
                        playerLastRound[p.Id] = round;
                        
                        // Track pairings for the current round to use in the next round
                        if (!currentRoundPairings.ContainsKey(p.Id)) currentRoundPairings[p.Id] = new HashSet<string>();
                        foreach (var other in bestMatch)
                        {
                            if (p.Id != other.Id) currentRoundPairings[p.Id].Add(other.Id);
                        }
                    }
                    
                    // Update partner and opponent tracking
                    IncrementPartner(bestMatch[0].Id, bestMatch[1].Id);
                    IncrementPartner(bestMatch[2].Id, bestMatch[3].Id);
                    IncrementOpponent(bestMatch[0].Id, bestMatch[2].Id);
                    IncrementOpponent(bestMatch[0].Id, bestMatch[3].Id);
                    IncrementOpponent(bestMatch[1].Id, bestMatch[2].Id);
                    IncrementOpponent(bestMatch[1].Id, bestMatch[3].Id);

                    // Update court tracking
                    foreach (var p in bestMatch)
                    {
                        playerCourts[p.Id].Add(court);
                    }
                }
            }

            // After the round is complete, update the "last round" pairings
            playersInSameMatchLastRound = new Dictionary<string, HashSet<string>>(currentRoundPairings);
        }
        
        // Log statistics for verification
        Console.WriteLine($"[Scheduler] Generated {scheduled.Count - history.Count} new matches");
        if (partnerCounts.Any())
        {
            var maxPartner = partnerCounts.Values.Max();
            var avgPartner = partnerCounts.Values.Average();
            Console.WriteLine($"[Scheduler] Partner stats - Max repeat: {maxPartner}, Avg: {avgPartner:F2}");
        }
        if (opponentCounts.Any())
        {
            var maxOpp = opponentCounts.Values.Max();
            var minOpp = opponentCounts.Values.Min();
            var avgOpp = opponentCounts.Values.Average();
            Console.WriteLine($"[Scheduler] Opponent stats - Min: {minOpp}, Max: {maxOpp}, Avg: {avgOpp:F2}");
        }

        return scheduled;
    }
}
