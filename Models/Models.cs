using System;
using System.Collections.Generic;

namespace VolleyStatsPro.Models
{
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string Color { get; set; } = "#2196F3";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<Player> Players { get; set; } = new();
        public override string ToString() => Name;
    }

    public class Player
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string Name { get; set; } = "";
        public int Number { get; set; }
        public string Position { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public override string ToString() => $"#{Number} {Name}  [{Position}]";
    }

    public class Match
    {
        public int Id { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Location { get; set; } = "";
        public string Status { get; set; } = "Scheduled";
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public string HomeTeamName { get; set; } = "";
        public string AwayTeamName { get; set; } = "";
    }

    public class Set
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public int SetNumber { get; set; }
        public int HomePoints { get; set; }
        public int AwayPoints { get; set; }
        public bool IsComplete { get; set; }
    }

    public class Rally
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public int SetId { get; set; }
        public int RallyNumber { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int? WinnerTeamId { get; set; }
    }

    public class Action
    {
        public int Id { get; set; }
        public int RallyId { get; set; }
        public int PlayerId { get; set; }
        public int TeamId { get; set; }
        public string ActionType { get; set; } = "";
        public string Result { get; set; } = "";
        public int? Zone { get; set; }
        public int? TargetZone { get; set; }
        public float? X { get; set; }
        public float? Y { get; set; }
        public float? TargetX { get; set; }
        public float? TargetY { get; set; }
        public string? Notes { get; set; }
        public string PlayerName { get; set; } = "";
        public string TeamName { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int SetNumber { get; set; }
        public int RallyNumber { get; set; }
    }

    public class PlayerStats
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = "";
        public string Position { get; set; } = "";
        public int Number { get; set; }
        public int ServeTotal { get; set; }
        public int ServeAce { get; set; }
        public int ServeError { get; set; }
        public double ServeEff => ServeTotal == 0 ? 0 : (ServeAce - ServeError) / (double)ServeTotal;
        public int AttackTotal { get; set; }
        public int AttackKill { get; set; }
        public int AttackError { get; set; }
        public double AttackEff => AttackTotal == 0 ? 0 : (AttackKill - AttackError) / (double)AttackTotal;
        public int BlockTotal { get; set; }
        public int BlockKill { get; set; }
        public int BlockError { get; set; }
        public int ReceptionTotal { get; set; }
        public int ReceptionPerfect { get; set; }
        public int ReceptionError { get; set; }
        public double ReceptionEff => ReceptionTotal == 0 ? 0 : (ReceptionPerfect - ReceptionError) / (double)ReceptionTotal;
        public int DigTotal { get; set; }
        public int DigError { get; set; }
        public int SetTotal { get; set; }
        public int SetError { get; set; }
    }

    public class TeamStats
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = "";
        public int MatchesPlayed { get; set; }
        public int MatchesWon { get; set; }
        public int SetsWon { get; set; }
        public int SetsLost { get; set; }
        public double WinRate => MatchesPlayed == 0 ? 0 : MatchesWon / (double)MatchesPlayed * 100;
        public PlayerStats Aggregate { get; set; } = new();
    }

    public class ZoneData
    {
        public int Zone { get; set; }
        public int Count { get; set; }
        public int Success { get; set; }
        public int Error { get; set; }
    }
}
