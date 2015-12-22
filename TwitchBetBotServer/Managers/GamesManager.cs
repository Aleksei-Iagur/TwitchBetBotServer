using System;
using System.Collections.Generic;
using System.Linq;
using PrismataTvServer.Classes;
using PrismataTvServer.Enums;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Managers
{
    public class GamesManager : IGamesManager
    {
        private readonly IDatabaseManager _databaseManager;
        private readonly ICurrencyManager _currencyManager;
        private readonly IUsersManager _usersManager;
        private readonly IPlayersManager _playersManager;
        private readonly List<Game> _games;
        private Game _currentGame, _previousGame;

        public GamesManager(IDatabaseManager databaseManager, ICurrencyManager currencyManager, IUsersManager usersManager, IPlayersManager playersManager)
        {
            _databaseManager = databaseManager;
            _currencyManager = currencyManager;
            _usersManager = usersManager;
            _playersManager = playersManager;
            _games = new List<Game>();
        }

        public void CreateNewGame()
        {
            var game = new Game {Bets = new List<Bet>(), Date = DateTime.Now, Result = GameResult.Open};
            _games.Add(game);
            _currentGame = _games.Last();
        }

        public void CloseGame(GameResult gameResult)
        {
            var game = _games.Last();
            SetGameResult(game, gameResult);
            GiveCoinsToWinners(game);
            SaveGame(game);
            _previousGame = _games.Last();
            _currentGame = null;
        }

        private void GiveCoinsToWinners(Game game)
        {
            game.Bets
                .Where(x => x.BetOn == game.Result)
                .ToList()
                .ForEach(bet => _currencyManager.AddCoinsToUser(bet.ViewerId, bet.WinAmount));
        }

        public void ChangeGameResult(GameResult newResult, int gameId = -1)
        {
            var game = _games.Find(x => x.GameId == gameId) ?? _previousGame;
            WithdrawCoinsFromWinners(game);
            SetGameResult(game, newResult);
            _databaseManager.SaveGame(game);
        }

        private void WithdrawCoinsFromWinners(Game game)
        {
            game.Bets
                .Where(x => x.BetOn == game.Result)
                .ToList()
                .ForEach(bet => _currencyManager.RemoveCoinsFromUser(bet.ViewerId, bet.WinAmount));
        }

        public void PlaceBet(int userId, GameResult option, int amount)
        {
            var oldBet = _currentGame.Bets.Find(x => x.ViewerId == userId);
            if (oldBet != null)
            {
                _currencyManager.AddCoinsToUser(userId, oldBet.BetAmount);
                _currentGame.Bets.Remove(oldBet);
            }

            _currencyManager.RemoveCoinsFromUser(userId, amount);
            _currentGame.Bets.Add(new Bet { BetAmount = amount, BetOn = option, ViewerId = userId });
        }

        public int GetTotalBetsForClosedGame()
        {
            return _previousGame.Bets.Sum(x => x.BetAmount);
        }

        public int GetTotalBetsForCurrentGame()
        {
            return _currentGame.Bets.Sum(x => x.BetAmount);
        }

        public List<Winner> GetWinners()
        {
            return (from user in _usersManager.GetAllUsers()
                join bet in _previousGame.Bets on user.Id equals bet.ViewerId
                where bet.BetOn == _previousGame.Result
                select new Winner {Name = user.Name, BetAmount = bet.BetAmount, WinAmount = bet.WinAmount}).ToList();
        }

        public void CancelCurrentGame()
        {
            _currentGame.Result = GameResult.Cancel;
            RefundBets(_currentGame);
        }

        private void RefundBets(Game game)
        {
            foreach (var bet in game.Bets)
            {
                _currencyManager.AddCoinsToUser(bet.ViewerId, bet.BetAmount);
            }
        }

        public int GetTotalBetsOn(GameResult gameResult)
        {
            return _currentGame.Bets.Where(x => x.BetOn == gameResult).Sum(x => x.BetAmount);
        }

        public int GetNumberOfBets(GameResult gameResult)
        {
            return _currentGame.Bets.Count(x => x.BetOn == gameResult);
        }

        public bool UserHasBet(int userId)
        {
            return _currentGame != null && _currentGame.Bets.Any(x => x.ViewerId == userId);
        }

        public int GetBetAmount(int userId)
        {
            return _currentGame?.Bets.Find(x => x.ViewerId == userId)?.BetAmount ?? 0;
        }

        public string GetBetOn(int userId)
        {
            var gameResult = _currentGame.Bets.Find(x => x.ViewerId == userId).BetOn;
            return GetOptionName(gameResult);
        }

        public string GetOptionName(int option)
        {
            return GetOptionName((GameResult) option);
        }

        public string GetOptionName(GameResult gameResult)
        {
            switch (gameResult)
            {
                case GameResult.TopPlayerWin:
                    return "Top";
                case GameResult.BottomPlayerWin:
                    return "Bottom";
                case GameResult.Draw:
                    return "Draw";
                default:
                    return "Wrong option.";
            }   
        }

        public void SetGameResult(Game game, GameResult result)
        {
            game.Result = result;
            var bank = game.Bets.Where(x => x.BetOn != result).Sum(x => x.BetAmount);
            var winnersBets = game.Bets.Where(x => x.BetOn == result).ToList();
            var winnersBetsSum = winnersBets.Sum(x => x.BetAmount);

            foreach (var winnersBet in winnersBets)
            {
                winnersBet.IsWin = true;
                winnersBet.WinAmount = (int)((double)winnersBet.BetAmount / winnersBetsSum * bank + winnersBet.BetAmount);
            }

            var losersBets = game.Bets.Where(x => x.BetOn != result);

            foreach (var losersBet in losersBets)
            {
                losersBet.IsWin = false;
                losersBet.WinAmount = 0;
            }
        }
        
        public void SaveGame(Game game)
        {
            _databaseManager.SavePlayer(_playersManager.GetPlayer(game.Player1Id));
            _databaseManager.SavePlayer(_playersManager.GetPlayer(game.Player2Id));
            _databaseManager.SaveGame(game);
        }

        public void UpdateGamePlayers(string player1, string player2)
        {
            _currentGame.Player1Id = _playersManager.GetPlayerId(player1);
            _currentGame.Player2Id = _playersManager.GetPlayerId(player2);
        }

        public List<Game> GetAllGames()
        {
            return _games;
        }

        public void SaveGame()
        {
            SaveGame(_currentGame);
        }
    }
}
