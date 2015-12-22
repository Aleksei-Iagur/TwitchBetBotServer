using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using PrismataTvServer.Classes;
using PrismataTvServer.Enums;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Managers
{
    public class DatabaseManager : IDatabaseManager
    {
        private readonly string _connectionString;

        public DatabaseManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void InsertViewer(string name, int coins)
        {
            var command =
                $"INSERT INTO Viewers (Name, CreationDate, Coins) VALUES ('{name}','{DateTime.Now.ToString("yyyy-MM-dd")}',{coins})";
            using (var connection = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(command, connection))
            {
                connection.Open();
                cmd.ExecuteNonQuery();
                connection.Close();
            }
        }

        public int CreateNewUser(string userName)
        {
            if (IsUserBot(userName)) return -1;

            if (!UserExists(userName))
            {
                var command =
                    $"INSERT INTO Viewers (Name, CreationDate, Coins) VALUES ('{userName}','{DateTime.Now.ToString("yyyy-MM-dd")}',{1})";
                ExecuteNonQuery(command);
            }

            var command2 = $"SELECT ID FROM Viewers WHERE Name = '{userName}'";

            var userId = -1;
            using (var connection = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(command2, connection))
            {
                connection.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    userId = reader.GetInt32(0);
                }

                connection.Close();
            }

            return userId;            
        }

        public void UpdateViewer(User viewer)
        {
            var command =
                $"INSERT INTO Viewers (ID, Name, CreationDate, Coins, UserLevel) VALUES ({viewer.Id}, '{viewer.Name}', '{DateTime.Now.ToString("yyyy-MM-dd")}', {viewer.Coins}, {(int) viewer.UserLevel}) ON DUPLICATE KEY UPDATE Coins = VALUES(Coins);";
            ExecuteNonQuery(command);
        }

        public void ChangeUserPassword(User user)
        {
            var command = $"UPDATE Viewers SET Password = '{user.Password}' WHERE Name = '{user.Name}'";
            ExecuteNonQuery(command);
        }

        public Dictionary<int, List<UserOption>> GetAllViewersOptions()
        {
            const string command = "SELECT OptionID, ViewerID, ExpiredOn FROM ViewerOptions";

            var options = new Dictionary<int, List<UserOption>>();

            using (var connection = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(command, connection))
            {
                connection.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var userId = reader.GetInt32("ViewerID");
                    var optionId = reader.GetInt32("OptionID");
                    var expirationDate = reader.GetDateTime("ExpiredOn");
                    var expiredIn = expirationDate > DateTime.Now ? (int)expirationDate.Subtract(DateTime.Now).TotalDays : 0;

                    if (!options.ContainsKey(userId))
                    {
                        options.Add(userId, new List<UserOption>{new UserOption(userId, (ViewerOptions)optionId, 30, expiredIn)});
                    }
                    else
                    {
                        options[userId].Add(new UserOption(userId, (ViewerOptions)optionId, 30, expiredIn));
                    }
                }

                connection.Close();
            }

            return options;
        }

        public IEnumerable<GameInViewer> GetGamesOnDate(DateTime date)
        {
            var command =
                $"SELECT gb.GameID, p1.Name AS Player1, p2.Name AS Player2, gb.BetOn, g.Result, g.Date, COUNT(gb.BetAmount) AS BetsCount, SUM(gb.BetAmount) AS BetsAmount FROM Games g INNER JOIN GamesBets gb ON gb.GameID = g.ID INNER JOIN Players p1 ON g.Player1ID = p1.ID INNER JOIN Players p2 ON g.Player2ID = p2.ID WHERE g.Date LIKE '{date.Date.ToString("yyyy-MM-dd")}%' GROUP BY gb.GameID, g.Result, gb.BetOn";

            var games = new List<GameInViewer>();

            using (var connection = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(command, connection))
            {
                connection.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var gameId = reader.GetInt32("GameID");
                    var player1 = reader.GetString("Player1");
                    var player2 = reader.GetString("Player2");
                    var result = (GameResult) reader.GetInt32("Result");
                    var gameDate = reader.GetDateTime("Date");
                    var betsCount = reader.GetInt32("BetsCount");
                    var betsAmount = reader.GetInt32("BetsAmount");
                    var betsOn = reader.GetInt32("BetOn");

                    if (games.All(x => x.GameId != gameId))
                    {
                        games.Add(new GameInViewer{GameId = gameId, Player1 = player1, Player2 = player2, Result = result, Date = gameDate});
                    }

                    var game = games.Find(x => x.GameId == gameId);

                    switch (betsOn)
                    {
                        case 0:
                            game.BetsOnP1Count = betsCount;
                            game.BetsOnP1Amount = betsAmount;
                            break;
                        case 1:
                            game.BetsOnP2Count = betsCount;
                            game.BetsOnP2Amount = betsAmount;
                            break;
                        case 2:
                            game.BetsOnDrawCount = betsCount;
                            game.BetsOnDrawAmount = betsAmount;
                            break;
                    }

                    game.IsLocked = result != GameResult.Open;
                }

                connection.Close();
            }

            return games;
        }

        private bool IsUserBot(string user) => user == "Moobot" || user == "Nightbot" || user == "Jtv";

        private void ExecuteNonQuery(string command)
        {
            using (var connection = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(command, connection))
            {
                connection.Open();
                cmd.ExecuteNonQuery();
                connection.Close();
            }
        }

        public int CheckCurrency(string user)
        {
            var command = $"SELECT Coins FROM Viewers WHERE Name = '{user}'";
            var coins = -1;
            using (var connection = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(command, connection))
            {
                connection.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    coins = reader.GetInt32(0);
                }

                connection.Close();
            }

            return coins;
        }

        public void AddCurrency(string user, int amount)
        {
            var command = $"UPDATE Viewers SET Coins = Coins + {amount} WHERE Name = '{user}'";
            ExecuteNonQuery(command);
        }

        public void RemoveCurrency(string user, int amount)
        {
            var command = $"UPDATE Viewers SET Coins = Coins - {amount} WHERE Name = '{user}'";
            ExecuteNonQuery(command);   
        }

        public bool UserExists(string user)
        {
            var command = $"SELECT 1 FROM Viewers WHERE Name = '{user}'";
            var userExists = false;

            using (var connection = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(command, connection))
            {
                connection.Open();
                var reader = cmd.ExecuteReader();
                userExists = reader.Read();
                connection.Close();
            }

            return userExists;
        }
        
        public UserRoles GetUserLevel(string user)
        {
            if (!UserExists(user))
            {
                CreateNewUser(user);
                return UserRoles.GeneralUser;
            }

            var query = $"SELECT * FROM Viewers WHERE name = '{user}';";
            using (var connection = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(query, connection))
            {
                connection.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        int level;
                        if (int.TryParse(r["userlevel"].ToString(), out level))
                        {
                            return (UserRoles)level;
                        }
                        
                        return UserRoles.GeneralUser;
                    }
                    
                    return UserRoles.GeneralUser;
                }
            }
        }

        public void SetUserLevel(string user, int level)
        {
            var command = $"UPDATE Viewers SET UserLevel = {level} WHERE Name='{user}';";
            ExecuteNonQuery(command);
        }

        public IEnumerable<string> GetTop10()
        {
            const string command = @"SELECT CONCAT(name,"" ["",coins,""]"") FROM Viewers ORDER BY Coins DESC LIMIT 10;";

            using (var connection = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(command, connection))
            {
                connection.Open();
                using (var r = cmd.ExecuteReader())
                {
                    var list = new List<string>();
                    while (r.Read())
                    {
                        list.Add(r.GetString(0));
                    }

                    return list;
                }
            }
        }

        public void AddGame(string player1, string player2, GameResult gameResult, bool withBetting)
        {
            var player1Id = GetPlayerId(player1);
            var player2Id = GetPlayerId(player2);

            var command =
                $"INSERT INTO Games (Player1ID, Player2ID, Result, WithBetting) VALUES ('{player1Id}','{player2Id}',{gameResult},{withBetting});";
            ExecuteNonQuery(command);
        }

        public int GetPlayerId(string playerName)
        {
            var query = $"SELECT ID FROM Players WHERE Name = {playerName}";

            var playerId = -1;

            using (var connection = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(query, connection))
            {
                connection.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        playerId = r.GetInt32("ID");
                    }
                }
            }

            return playerId;
        }

        public void UpdateOrInsertViewers(IEnumerable<User> viewers)
        {
            foreach (var viewer in viewers)
            {
                var command =
                    $"INSERT INTO Viewers (ID, Name, CreationDate, Coins, UserLevel) VALUES ({viewer.Id}, '{viewer.Name}', '{DateTime.Now.ToString("yyyy-MM-dd")}', {viewer.Coins}, {viewer.UserLevel}) ON DUPLICATE KEY UPDATE Coins = VALUES(Coins);";
                ExecuteNonQuery(command);
            }
        }

        public IEnumerable<User> GetAllUsers()
        {
            var query = "SELECT * FROM Viewers";

            var users = new List<User>();

            using (var connection = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(query, connection))
            {
                connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User {Id = reader.GetInt32("ID"), Name = reader.GetString("Name"), Coins = reader.GetInt32("Coins"), UserLevel = (UserRoles)reader.GetByte("UserLevel"), Password = reader.GetString("Password")});
                    }
                }
            }

            return users;
        }

        public void SaveGame(Game game)
        {
            var date = game.GetDateAsString();
            var gameId = GetGameId(date);
            if (gameId == -1)
            {
                CreateGameInDb(game);
                gameId = GetGameId(date);
                game.GameId = gameId;
            }
            else
            {
                game.GameId = gameId;
                UpdateGameInDb(game);
            }

            foreach (var bet in game.Bets)
            {
                SaveBet(gameId, bet);
            }
        }

        private void UpdateGameInDb(Game game)
        {
            var cmd =
                $"UPDATE Games SET Player1ID = {game.Player1Id}, Player2ID = {game.Player2Id}, Result = {(int) game.Result}, WithBetting = {true}, Date = '{game.GetDateAsString()}' WHERE ID = {game.GameId}";
            ExecuteNonQuery(cmd);
        }

        private void SaveBet(int gameId, Bet bet)
        {
            var betExists = IsBetExists(gameId, bet.ViewerId);
            if (betExists)
            {
                UpdateBetInDb(gameId, bet);
            }
            else
            {
                CreateBetInDb(gameId, bet);
            }
        }

        private void UpdateBetInDb(int gameId, Bet bet)
        {
            var cmd =
                $"UPDATE GamesBets SET BetOn = {(int) bet.BetOn}, BetAmount = {bet.BetAmount}, IsWin = {bet.IsWin}, WinAmount = {bet.WinAmount} WHERE GameID = {gameId} AND ViewerID = {bet.ViewerId}";
            ExecuteNonQuery(cmd);
        }

        private void CreateBetInDb(int gameId, Bet bet)
        {
            var cmd =
                $"INSERT INTO GamesBets (GameID, ViewerID, BetOn, BetAmount, IsWin, WinAmount) VALUES ({gameId}, {bet.ViewerId}, {(int) bet.BetOn}, {bet.BetAmount}, {bet.IsWin}, {bet.WinAmount});";
            ExecuteNonQuery(cmd);
        }

        private bool IsBetExists(int gameId, int viewerId)
        {
            var query = $"SELECT 1 FROM GamesBets WHERE GameId = '{gameId}' AND ViewerID = '{viewerId}'";

            bool betExists;

            using (var connection = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(query, connection))
            {
                connection.Open();
                using (var r = cmd.ExecuteReader())
                {
                    betExists = r.Read();
                }
            }

            return betExists;
        }

        private void CreateGameInDb(Game game)
        {
            var command =
                $"INSERT INTO Games (Player1ID, Player2ID, Result, WithBetting, Date) VALUES ({game.Player1Id}, {game.Player2Id}, {(int) game.Result}, {true}, '{game.GetDateAsString()}')";
            ExecuteNonQuery(command);
        }

        private int GetGameId(string date)
        {
            var query = $"SELECT ID FROM Games WHERE Date = '{date}'";

            var gameId = -1;

            using (var connection = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(query, connection))
            {
                connection.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        gameId = r.GetInt32("ID");
                    }
                }
            }

            return gameId;
        }

        public List<Player> GetAllPlayers()
        {
            var query = "SELECT * FROM Players";

            var players = new List<Player>();

            using (var connection = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand(query, connection))
            {
                connection.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        players.Add(new Player { Id = reader.GetInt32("ID"), Name = reader.GetString("Name") });
                    }
                }
            }

            return players;
        }

        public void SavePlayer(Player player)
        {
            var command =
                $"INSERT IGNORE INTO Players (ID, Name, RegistrationDate) VALUES ({player.Id}, '{player.Name}', '{player.RegistrationDate.ToString("yyyy-MM-dd")}');";
            ExecuteNonQuery(command);
        }

        public void SaveOption(UserOption option)
        {
            var command =
                $"INSERT IGNORE INTO Options (ID, Name, Days) VALUES ({(int) option.Option}, '{option.Option}', {option.Days});";
            ExecuteNonQuery(command);

            command =
                $"INSERT IGNORE INTO ViewerOptions (ViewerID, OptionID, DateAdded, ExpiredOn) VALUES ({option.UserId}, {(int) option.Option}, '{DateTime.Now.ToString("yyyy-MM-dd")}', '{DateTime.Now.AddDays(option.Days).ToString("yyyy-MM-dd")}');";
            ExecuteNonQuery(command);
        }

        public void SaveBet(Bet bet)
        {
            var command =
                $"INSERT INTO GamesBets (GameID, ViewerID, BetOn, BetAmount, WinAmount, HasWon) VALUES ({bet.GameId}, {bet.ViewerId}, {(int) bet.BetOn}, {bet.BetAmount}, {bet.WinAmount}, {bet.IsWin})";
            ExecuteNonQuery(command);
        }
    }
}
