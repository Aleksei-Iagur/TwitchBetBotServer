using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using Apache.NMS;
using Apache.NMS.Util;
using Common.Logging;
using Newtonsoft.Json;
using PrismataTvServer.Classes;
using PrismataTvServer.Enums;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.BusControllers
{
    public class BettingController
    {
        private readonly IBettingManager _bettingManager;
        private readonly IGamesManager _gamesManager;
        private readonly ICurrencyManager _currencyManager;
        private static readonly ILog Logger = LogManager.GetLogger<BettingController>();
        private ISession _session;
        private IMessageProducer _messageProducer;
        private IConnectionFactory _connectionFactory;
        private IConnection _connection;

        public BettingController(IBettingManager bettingManager, IGamesManager gamesManager, ICurrencyManager currencyManager)
        {
            _bettingManager = bettingManager;
            _gamesManager = gamesManager;
            _currencyManager = currencyManager;
            SetUpActiveMq();
        }

        public void HandleMessage(ITextMessage message)
        {
            var destination = message.NMSDestination.ToString();

            try
            {
                switch (destination)
                {
                    case "queue://command.betting.bets.current.show":
                        _bettingManager.ShowCurrentBets();
                        break;

                    case "queue://command.betting.pool.cancel":
                        _bettingManager.CancelBetting();
                        break;

                    case "queue://command.betting.pool.create":
                        _bettingManager.CreateNewGame();
                        break;

                    case "queue://command.betting.pool.delete":
                        _bettingManager.DeletePool();
                        break;

                    case "queue://command.betting.pool.open":
                        _bettingManager.OpenBetting();
                        break;

                    case "queue://command.betting.winner.set":
                        GameResult winner;
                        Enum.TryParse(message.Text.Trim('"'), out winner);
                        _bettingManager.SetWinner(winner);
                        break;

                    case "queue://command.betting.bet.place":
                        var bet = JsonConvert.DeserializeObject<BetToPlace>(message.Text);
                        _bettingManager.PlaceBet(bet.Username, bet.BetAmount, bet.BetOn);
                        break;

                    case "queue://command.betting.bet.get":
                        var betAmount = _bettingManager.GetUserBetAmount(message.Text).ToString(CultureInfo.InvariantCulture);
                        var betOn = _bettingManager.GetUserBetOn(message.Text);
                        var getBetReply = $"{betAmount} {_currencyManager.CurrencyName} on {betOn}.";
                        var getBetResponse = _session.CreateTextMessage(getBetReply);

                        getBetResponse.NMSCorrelationID = message.NMSCorrelationID;
                        _messageProducer.Send(message.NMSReplyTo, getBetResponse);
                        break;

                    case "queue://command.betting.bet.check":
                        var username = message.Text.Substring(0, message.Text.IndexOf(':'));
                        var betAmount2 = _bettingManager.GetUserBetAmount(username).ToString(CultureInfo.InvariantCulture);
                        var betOn2 = _bettingManager.GetUserBetOn(username);

                        var checkBet = betAmount2 != "0" ?
                            $"{username}, you placed {betAmount2} {_currencyManager.CurrencyName} on {betOn2}."
                            : $"{username} has no bets.";
                        var checkBetMsg = _session.CreateTextMessage(checkBet);
                        var checkBetDestination = SessionUtil.GetDestination(_session, "command.chat.say");

                        _messageProducer.Send(checkBetDestination, checkBetMsg);
                        break;

                    case "queue://command.betting.pool.close":
                        _bettingManager.CloseBetting();
                        break;

                    case "queue://command.betting.bets.current.get":
                        var currentBets = JsonConvert.SerializeObject(GetCurrentBets());
                        var replyMsg = _session.CreateTextMessage(currentBets);
                        replyMsg.NMSCorrelationID = message.NMSCorrelationID;
                        _messageProducer.Send(message.NMSReplyTo, replyMsg);
                        break;
                }
            }
            catch (Exception exc)
            {
                Logger.Error(m => m("An error has occured while proceeding message: {0}.", message), exc);
            }
        }


        public List<string> GetCurrentBets()
        {
            if (!_bettingManager.BettingOpen && !_bettingManager.PoolLocked)
            {
                return new List<string> { "", "          Betting is closed now." };
            }

            var bets = new List<string> { "" };
            var totalBets = _gamesManager.GetTotalBetsForCurrentGame();
            var betsPercent = totalBets == 0
                ? 0
                : Math.Round((double)(_gamesManager.GetTotalBetsOn(0)) / totalBets * 100);
            var topPlayer =
                $"     Top player bets: {_gamesManager.GetNumberOfBets(GameResult.TopPlayerWin)} - {_gamesManager.GetTotalBetsOn(GameResult.TopPlayerWin)} ({betsPercent}%)";

            var betsPercentForBottom = totalBets == 0 ? 0 : 100 - betsPercent;
            var bottomPlayer =
                $"     Bottom player bets: {_gamesManager.GetNumberOfBets(GameResult.BottomPlayerWin)} - {_gamesManager.GetTotalBetsOn(GameResult.BottomPlayerWin)} ({betsPercentForBottom}%)";

            bets.Add(topPlayer);
            bets.Add(bottomPlayer);
            return bets;
        }

        private void SetUpActiveMq()
        {
            var busAddress = ConfigurationManager.ConnectionStrings["service-bus"].ConnectionString;
            Logger.Debug(m => m("Connecting to ActiveMq on {0}...", busAddress));

            var connecturi = new Uri(busAddress);

            try
            {
                _connectionFactory = new NMSConnectionFactory(connecturi);
                _connection = _connectionFactory.CreateConnection();
                _session = _connection.CreateSession();
                _messageProducer = _session.CreateProducer();

                _connection.Start();
                Logger.Debug(m => m("Connected to ActiveMq successfully."));
            }
            catch (Exception exc)
            {
                Logger.Error(m => m("Couldn't connect to ActiveMq."), exc);
            }
        }
    }
}
