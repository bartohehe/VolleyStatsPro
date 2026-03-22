using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using VolleyStatsPro.Models;

namespace VolleyStatsPro.Data
{
    public static class Database
    {
        private static string DbPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VolleyStatsPro", "volleystats.db");

        public static SqliteConnection GetConnection()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
            var conn = new SqliteConnection($"Data Source={DbPath}");
            conn.Open();
            // Bug fix: enable foreign key enforcement (required for ON DELETE CASCADE)
            using var fkCmd = conn.CreateCommand();
            fkCmd.CommandText = "PRAGMA foreign_keys = ON;";
            fkCmd.ExecuteNonQuery();
            return conn;
        }

        public static void Initialize()
        {
            // Bug fix: Microsoft.Data.Sqlite only executes the first statement in a
            // multi-statement command, so every CREATE TABLE was silently skipped.
            // Each statement must be executed separately.
            using var conn = GetConnection();

            Execute(conn, "PRAGMA journal_mode=WAL;");

            Execute(conn, @"CREATE TABLE IF NOT EXISTS Teams (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                ShortName TEXT,
                Color TEXT DEFAULT '#2196F3',
                CreatedAt TEXT DEFAULT (datetime('now'))
            );");

            Execute(conn, @"CREATE TABLE IF NOT EXISTS Players (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TeamId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                Number INTEGER NOT NULL,
                Position TEXT,
                IsActive INTEGER DEFAULT 1,
                FOREIGN KEY (TeamId) REFERENCES Teams(Id) ON DELETE CASCADE
            );");

            Execute(conn, @"CREATE TABLE IF NOT EXISTS Matches (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                HomeTeamId INTEGER NOT NULL,
                AwayTeamId INTEGER NOT NULL,
                Date TEXT,
                Location TEXT,
                Status TEXT DEFAULT 'Scheduled',
                HomeScore INTEGER NOT NULL DEFAULT 0,
                AwayScore INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (HomeTeamId) REFERENCES Teams(Id),
                FOREIGN KEY (AwayTeamId) REFERENCES Teams(Id)
            );");

            Execute(conn, @"CREATE TABLE IF NOT EXISTS Sets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MatchId INTEGER NOT NULL,
                SetNumber INTEGER NOT NULL,
                HomePoints INTEGER NOT NULL DEFAULT 0,
                AwayPoints INTEGER NOT NULL DEFAULT 0,
                IsComplete INTEGER DEFAULT 0,
                FOREIGN KEY (MatchId) REFERENCES Matches(Id) ON DELETE CASCADE
            );");

            Execute(conn, @"CREATE TABLE IF NOT EXISTS Rallies (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                MatchId INTEGER NOT NULL,
                SetId INTEGER NOT NULL,
                RallyNumber INTEGER NOT NULL,
                Timestamp TEXT,
                WinnerTeamId INTEGER,
                FOREIGN KEY (MatchId) REFERENCES Matches(Id) ON DELETE CASCADE,
                FOREIGN KEY (SetId) REFERENCES Sets(Id) ON DELETE CASCADE
            );");

            Execute(conn, @"CREATE TABLE IF NOT EXISTS Actions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RallyId INTEGER NOT NULL,
                PlayerId INTEGER NOT NULL,
                TeamId INTEGER NOT NULL,
                ActionType TEXT NOT NULL,
                Result TEXT,
                Zone INTEGER,
                TargetZone INTEGER,
                X REAL,
                Y REAL,
                TargetX REAL,
                TargetY REAL,
                Notes TEXT,
                Timestamp TEXT DEFAULT (datetime('now')),
                FOREIGN KEY (RallyId) REFERENCES Rallies(Id) ON DELETE CASCADE,
                FOREIGN KEY (PlayerId) REFERENCES Players(Id),
                FOREIGN KEY (TeamId) REFERENCES Teams(Id)
            );");
        }

        private static void Execute(SqliteConnection conn, string sql)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }

    // ─── Team Repository ─────────────────────────────────────────────────────
    public class TeamRepository
    {
        public List<Team> GetAll()
        {
            var list = new List<Team>();
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id,Name,ShortName,Color,CreatedAt FROM Teams ORDER BY Name";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(MapTeam(reader));
            return list;
        }

        public Team? GetById(int id)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id,Name,ShortName,Color,CreatedAt FROM Teams WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? MapTeam(r) : null;
        }

        public int Insert(Team t)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Teams(Name,ShortName,Color) VALUES(@n,@s,@c)";
            cmd.Parameters.AddWithValue("@n", t.Name);
            cmd.Parameters.AddWithValue("@s", t.ShortName);
            cmd.Parameters.AddWithValue("@c", t.Color);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()";
            cmd.Parameters.Clear();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void Update(Team t)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Teams SET Name=@n,ShortName=@s,Color=@c WHERE Id=@id";
            cmd.Parameters.AddWithValue("@n", t.Name);
            cmd.Parameters.AddWithValue("@s", t.ShortName);
            cmd.Parameters.AddWithValue("@c", t.Color);
            cmd.Parameters.AddWithValue("@id", t.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = Database.GetConnection();
            using var tx = conn.BeginTransaction();
            void Exec(string sql)
            {
                var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            // Delete in dependency order because Matches and Actions lack ON DELETE CASCADE to Teams/Players
            Exec("DELETE FROM Actions WHERE TeamId = @id");
            Exec("DELETE FROM Actions WHERE PlayerId IN (SELECT Id FROM Players WHERE TeamId = @id)");
            Exec("DELETE FROM Matches WHERE HomeTeamId = @id OR AwayTeamId = @id"); // cascades Sets → Rallies → Actions
            Exec("DELETE FROM Teams  WHERE Id = @id");                               // cascades Players
            tx.Commit();
        }

        private static Team MapTeam(SqliteDataReader r) => new Team
        {
            Id = r.GetInt32(0), Name = r.GetString(1),
            ShortName = r.IsDBNull(2) ? "" : r.GetString(2),
            Color = r.IsDBNull(3) ? "#2196F3" : r.GetString(3),
            CreatedAt = r.IsDBNull(4) ? DateTime.Now : DateTime.Parse(r.GetString(4))
        };
    }

    // ─── Player Repository ───────────────────────────────────────────────────
    public class PlayerRepository
    {
        public List<Player> GetByTeam(int teamId)
        {
            var list = new List<Player>();
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id,TeamId,Name,Number,Position,IsActive FROM Players WHERE TeamId=@tid ORDER BY Number";
            cmd.Parameters.AddWithValue("@tid", teamId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapPlayer(r));
            return list;
        }

        public int Insert(Player p)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Players(TeamId,Name,Number,Position,IsActive) VALUES(@t,@n,@num,@pos,@a)";
            cmd.Parameters.AddWithValue("@t", p.TeamId);
            cmd.Parameters.AddWithValue("@n", p.Name);
            cmd.Parameters.AddWithValue("@num", p.Number);
            cmd.Parameters.AddWithValue("@pos", p.Position);
            cmd.Parameters.AddWithValue("@a", p.IsActive ? 1 : 0);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()";
            cmd.Parameters.Clear();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void Update(Player p)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Players SET Name=@n,Number=@num,Position=@pos,IsActive=@a WHERE Id=@id";
            cmd.Parameters.AddWithValue("@n", p.Name);
            cmd.Parameters.AddWithValue("@num", p.Number);
            cmd.Parameters.AddWithValue("@pos", p.Position);
            cmd.Parameters.AddWithValue("@a", p.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", p.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Players WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static Player MapPlayer(SqliteDataReader r) => new Player
        {
            Id = r.GetInt32(0), TeamId = r.GetInt32(1), Name = r.GetString(2),
            Number = r.GetInt32(3), Position = r.IsDBNull(4) ? "" : r.GetString(4),
            IsActive = r.GetInt32(5) == 1
        };
    }

    // ─── Match Repository ────────────────────────────────────────────────────
    public class MatchRepository
    {
        public List<Match> GetAll()
        {
            var list = new List<Match>();
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT m.Id,m.HomeTeamId,m.AwayTeamId,m.Date,m.Location,m.Status,m.HomeScore,m.AwayScore,
                       ht.Name,at.Name
                FROM Matches m
                JOIN Teams ht ON ht.Id=m.HomeTeamId
                JOIN Teams at ON at.Id=m.AwayTeamId
                ORDER BY m.Date DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapMatch(r));
            return list;
        }

        public Match? GetById(int id)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT m.Id,m.HomeTeamId,m.AwayTeamId,m.Date,m.Location,m.Status,m.HomeScore,m.AwayScore,
                       ht.Name,at.Name
                FROM Matches m
                JOIN Teams ht ON ht.Id=m.HomeTeamId
                JOIN Teams at ON at.Id=m.AwayTeamId
                WHERE m.Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? MapMatch(r) : null;
        }

        public int Insert(Match m)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Matches(HomeTeamId,AwayTeamId,Date,Location,Status) VALUES(@h,@a,@d,@l,@s)";
            cmd.Parameters.AddWithValue("@h", m.HomeTeamId);
            cmd.Parameters.AddWithValue("@a", m.AwayTeamId);
            cmd.Parameters.AddWithValue("@d", m.Date.ToString("o"));
            cmd.Parameters.AddWithValue("@l", m.Location);
            cmd.Parameters.AddWithValue("@s", m.Status);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()";
            cmd.Parameters.Clear();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void UpdateScore(int matchId, int homeScore, int awayScore, string status)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Matches SET HomeScore=@h,AwayScore=@a,Status=@s WHERE Id=@id";
            cmd.Parameters.AddWithValue("@h", homeScore);
            cmd.Parameters.AddWithValue("@a", awayScore);
            cmd.Parameters.AddWithValue("@s", status);
            cmd.Parameters.AddWithValue("@id", matchId);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Matches WHERE Id=@id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static Match MapMatch(SqliteDataReader r) => new Match
        {
            Id = r.GetInt32(0), HomeTeamId = r.GetInt32(1), AwayTeamId = r.GetInt32(2),
            Date = r.IsDBNull(3) ? DateTime.Now : DateTime.Parse(r.GetString(3)),
            Location = r.IsDBNull(4) ? "" : r.GetString(4),
            Status = r.IsDBNull(5) ? "Scheduled" : r.GetString(5),
            // Bug fix: guard with IsDBNull even though columns have DEFAULT, as old rows may be NULL
            HomeScore = r.IsDBNull(6) ? 0 : r.GetInt32(6),
            AwayScore = r.IsDBNull(7) ? 0 : r.GetInt32(7),
            HomeTeamName = r.GetString(8), AwayTeamName = r.GetString(9)
        };
    }

    // ─── Set Repository ───────────────────────────────────────────────────────
    public class SetRepository
    {
        public List<Set> GetByMatch(int matchId)
        {
            var list = new List<Set>();
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id,MatchId,SetNumber,HomePoints,AwayPoints,IsComplete FROM Sets WHERE MatchId=@mid ORDER BY SetNumber";
            cmd.Parameters.AddWithValue("@mid", matchId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Set
            {
                Id = r.GetInt32(0), MatchId = r.GetInt32(1), SetNumber = r.GetInt32(2),
                // Bug fix: guard with IsDBNull to avoid crash on legacy NULL rows
                HomePoints = r.IsDBNull(3) ? 0 : r.GetInt32(3),
                AwayPoints = r.IsDBNull(4) ? 0 : r.GetInt32(4),
                IsComplete = r.GetInt32(5) == 1
            });
            return list;
        }

        public int Insert(Set s)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Sets(MatchId,SetNumber,HomePoints,AwayPoints,IsComplete) VALUES(@m,@n,@h,@a,@c)";
            cmd.Parameters.AddWithValue("@m", s.MatchId);
            cmd.Parameters.AddWithValue("@n", s.SetNumber);
            cmd.Parameters.AddWithValue("@h", s.HomePoints);
            cmd.Parameters.AddWithValue("@a", s.AwayPoints);
            cmd.Parameters.AddWithValue("@c", s.IsComplete ? 1 : 0);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()";
            cmd.Parameters.Clear();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void UpdatePoints(int setId, int home, int away, bool complete)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Sets SET HomePoints=@h,AwayPoints=@a,IsComplete=@c WHERE Id=@id";
            cmd.Parameters.AddWithValue("@h", home);
            cmd.Parameters.AddWithValue("@a", away);
            cmd.Parameters.AddWithValue("@c", complete ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", setId);
            cmd.ExecuteNonQuery();
        }
    }

    // ─── Rally / Action Repository ────────────────────────────────────────────
    public class RallyRepository
    {
        public int InsertRally(Rally r)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Rallies(MatchId,SetId,RallyNumber,Timestamp,WinnerTeamId) VALUES(@m,@s,@n,@t,@w)";
            cmd.Parameters.AddWithValue("@m", r.MatchId);
            cmd.Parameters.AddWithValue("@s", r.SetId);
            cmd.Parameters.AddWithValue("@n", r.RallyNumber);
            cmd.Parameters.AddWithValue("@t", r.Timestamp.ToString("o"));
            cmd.Parameters.AddWithValue("@w", r.WinnerTeamId.HasValue ? (object)r.WinnerTeamId.Value : DBNull.Value);
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()";
            cmd.Parameters.Clear();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void SetRallyWinner(int rallyId, int teamId)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Rallies SET WinnerTeamId=@t WHERE Id=@id";
            cmd.Parameters.AddWithValue("@t", teamId);
            cmd.Parameters.AddWithValue("@id", rallyId);
            cmd.ExecuteNonQuery();
        }

        // Bug fix: renamed parameter type from Action to Models.Action to resolve ambiguity
        // with System.Action delegate (both System and VolleyStatsPro.Models are imported).
        public int InsertAction(Models.Action a)
        {
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Actions(RallyId,PlayerId,TeamId,ActionType,Result,Zone,TargetZone,X,Y,TargetX,TargetY,Notes,Timestamp)
                VALUES(@r,@p,@t,@at,@res,@z,@tz,@x,@y,@tx,@ty,@n,@ts)";
            cmd.Parameters.AddWithValue("@r", a.RallyId);
            cmd.Parameters.AddWithValue("@p", a.PlayerId);
            cmd.Parameters.AddWithValue("@t", a.TeamId);
            cmd.Parameters.AddWithValue("@at", a.ActionType);
            cmd.Parameters.AddWithValue("@res", a.Result);
            cmd.Parameters.AddWithValue("@z", a.Zone.HasValue ? (object)a.Zone.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@tz", a.TargetZone.HasValue ? (object)a.TargetZone.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@x", a.X.HasValue ? (object)a.X.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@y", a.Y.HasValue ? (object)a.Y.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@tx", a.TargetX.HasValue ? (object)a.TargetX.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@ty", a.TargetY.HasValue ? (object)a.TargetY.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@n", a.Notes ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ts", a.Timestamp.ToString("o"));
            cmd.ExecuteNonQuery();
            cmd.CommandText = "SELECT last_insert_rowid()";
            cmd.Parameters.Clear();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<Models.Action> GetActionsForMatch(int matchId)
        {
            var list = new List<Models.Action>();
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT a.Id,a.RallyId,a.PlayerId,a.TeamId,a.ActionType,a.Result,a.Zone,a.TargetZone,
                       a.X,a.Y,a.TargetX,a.TargetY,a.Notes,a.Timestamp,
                       p.Name,t.Name,r.RallyNumber,s.SetNumber
                FROM Actions a
                JOIN Players p ON p.Id=a.PlayerId
                JOIN Teams t ON t.Id=a.TeamId
                JOIN Rallies r ON r.Id=a.RallyId
                JOIN Sets s ON s.Id=r.SetId
                WHERE r.MatchId=@mid
                ORDER BY a.Id";
            cmd.Parameters.AddWithValue("@mid", matchId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapAction(r));
            return list;
        }

        public List<Models.Action> GetActionsForPlayer(int playerId)
        {
            var list = new List<Models.Action>();
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT a.Id,a.RallyId,a.PlayerId,a.TeamId,a.ActionType,a.Result,a.Zone,a.TargetZone,
                       a.X,a.Y,a.TargetX,a.TargetY,a.Notes,a.Timestamp,
                       p.Name,t.Name,r.RallyNumber,s.SetNumber
                FROM Actions a
                JOIN Players p ON p.Id=a.PlayerId
                JOIN Teams t ON t.Id=a.TeamId
                JOIN Rallies r ON r.Id=a.RallyId
                JOIN Sets s ON s.Id=r.SetId
                WHERE a.PlayerId=@pid
                ORDER BY a.Id";
            cmd.Parameters.AddWithValue("@pid", playerId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapAction(r));
            return list;
        }

        public List<Models.Action> GetActionsForTeam(int teamId)
        {
            var list = new List<Models.Action>();
            using var conn = Database.GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT a.Id,a.RallyId,a.PlayerId,a.TeamId,a.ActionType,a.Result,a.Zone,a.TargetZone,
                       a.X,a.Y,a.TargetX,a.TargetY,a.Notes,a.Timestamp,
                       p.Name,t.Name,r.RallyNumber,s.SetNumber
                FROM Actions a
                JOIN Players p ON p.Id=a.PlayerId
                JOIN Teams t ON t.Id=a.TeamId
                JOIN Rallies r ON r.Id=a.RallyId
                JOIN Sets s ON s.Id=r.SetId
                WHERE a.TeamId=@tid
                ORDER BY a.Id";
            cmd.Parameters.AddWithValue("@tid", teamId);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(MapAction(r));
            return list;
        }

        private static Models.Action MapAction(SqliteDataReader r) => new Models.Action
        {
            Id=r.GetInt32(0), RallyId=r.GetInt32(1), PlayerId=r.GetInt32(2), TeamId=r.GetInt32(3),
            ActionType=r.GetString(4), Result=r.IsDBNull(5)?"":r.GetString(5),
            Zone=r.IsDBNull(6)?null:r.GetInt32(6),
            TargetZone=r.IsDBNull(7)?null:r.GetInt32(7),
            X=r.IsDBNull(8)?null:(float)r.GetDouble(8),
            Y=r.IsDBNull(9)?null:(float)r.GetDouble(9),
            TargetX=r.IsDBNull(10)?null:(float)r.GetDouble(10),
            TargetY=r.IsDBNull(11)?null:(float)r.GetDouble(11),
            Notes=r.IsDBNull(12)?null:r.GetString(12),
            Timestamp=r.IsDBNull(13)?DateTime.Now:DateTime.Parse(r.GetString(13)),
            PlayerName=r.GetString(14), TeamName=r.GetString(15),
            RallyNumber=r.GetInt32(16), SetNumber=r.GetInt32(17)
        };
    }

    // ─── Stats Service ────────────────────────────────────────────────────────
    public class StatsService
    {
        private readonly RallyRepository _rallyRepo = new();
        private readonly PlayerRepository _playerRepo = new();

        public PlayerStats GetPlayerStats(int playerId, int? matchId = null)
        {
            var actions = matchId.HasValue
                ? _rallyRepo.GetActionsForMatch(matchId.Value).FindAll(a => a.PlayerId == playerId)
                : _rallyRepo.GetActionsForPlayer(playerId);
            return ComputeStats(actions, playerId);
        }

        public PlayerStats GetTeamAggregateStats(int teamId, int? matchId = null)
        {
            var actions = matchId.HasValue
                ? _rallyRepo.GetActionsForMatch(matchId.Value).FindAll(a => a.TeamId == teamId)
                : _rallyRepo.GetActionsForTeam(teamId);
            return ComputeStats(actions, 0);
        }

        public List<PlayerStats> GetAllPlayerStats(int teamId)
        {
            var players = _playerRepo.GetByTeam(teamId);
            var result = new List<PlayerStats>();
            foreach (var p in players)
            {
                var stats = GetPlayerStats(p.Id);
                stats.PlayerName = p.Name;
                stats.Position = p.Position;
                stats.Number = p.Number;
                stats.PlayerId = p.Id;
                result.Add(stats);
            }
            return result;
        }

        public List<ZoneData> GetZoneData(int teamId, string actionType, int? matchId = null)
        {
            var actions = matchId.HasValue
                ? _rallyRepo.GetActionsForMatch(matchId.Value).FindAll(a => a.TeamId == teamId && a.ActionType == actionType)
                : _rallyRepo.GetActionsForTeam(teamId).FindAll(a => a.ActionType == actionType);

            var zones = new Dictionary<int, ZoneData>();
            for (int i = 1; i <= 9; i++) zones[i] = new ZoneData { Zone = i };
            foreach (var a in actions)
            {
                int z = a.Zone ?? 0;
                if (z < 1 || z > 9) continue;
                zones[z].Count++;
                if (IsSuccess(a.ActionType, a.Result)) zones[z].Success++;
                if (IsError(a.Result)) zones[z].Error++;
            }
            return new List<ZoneData>(zones.Values);
        }

        // Bug fix: parameter uses Models.Action to avoid ambiguity with System.Action delegate
        private static PlayerStats ComputeStats(List<Models.Action> actions, int playerId)
        {
            var s = new PlayerStats { PlayerId = playerId };
            foreach (var a in actions)
            {
                switch (a.ActionType)
                {
                    case "Serve":
                        s.ServeTotal++;
                        if (a.Result == "#" || a.Result == "Ace") s.ServeAce++;
                        if (a.Result == "=" || a.Result == "Error") s.ServeError++;
                        break;
                    case "Attack":
                        s.AttackTotal++;
                        if (a.Result == "#" || a.Result == "Kill") s.AttackKill++;
                        if (a.Result == "=" || a.Result == "Error") s.AttackError++;
                        break;
                    case "Block":
                        s.BlockTotal++;
                        if (a.Result == "#" || a.Result == "Kill") s.BlockKill++;
                        if (a.Result == "=" || a.Result == "Error") s.BlockError++;
                        break;
                    case "Reception":
                        s.ReceptionTotal++;
                        if (a.Result == "#" || a.Result == "Perfect") s.ReceptionPerfect++;
                        if (a.Result == "=" || a.Result == "Error") s.ReceptionError++;
                        break;
                    case "Dig":
                        s.DigTotal++;
                        if (a.Result == "=" || a.Result == "Error") s.DigError++;
                        break;
                    case "Set":
                        s.SetTotal++;
                        if (a.Result == "=" || a.Result == "Error") s.SetError++;
                        break;
                }
            }
            return s;
        }

        private static bool IsSuccess(string type, string result) =>
            result is "#" or "Kill" or "Ace" or "Perfect" or "Block";
        private static bool IsError(string result) =>
            result is "=" or "Error";
    }
}
