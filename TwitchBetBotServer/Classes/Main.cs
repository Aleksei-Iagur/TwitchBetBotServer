using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Apache.NMS;
using Apache.NMS.Util;
using Common.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PrismataTvServer.BusControllers;
using PrismataTvServer.Controllers;
using PrismataTvServer.Enums;
using PrismataTvServer.Interfaces;
using PrismataTvServer.Managers;
using StructureMap;

namespace PrismataTvServer.Classes
{
    public class Main : IMain
    {
        private static readonly ILog Logger = LogManager.GetLogger<Main>();
        private readonly string _busAddress;
        private readonly string _channelName;
        private string _user;
        private readonly int _interval;
        private readonly int _payout;
        private DateTime _time;
        private bool _handoutGiven;
        private Timer _currencyQueue;
        private List<string> _usersToLookup;
        private Thread _timerThread;
        private IDatabaseManager _databaseManager;
        private IBettingManager _bettingManager;
        private IMessageSender _messageSender;
        private ICurrencyManager _currencyManager;
        private CurrencyMessageController _currencyMessageController;
        private BettingMessageController _bettingMessageController;
        private IUsersManager _usersManager;
        private IGamesManager _gamesManager;
        private AwardsManager _awardsManager;
        private OptionsController _optionsController;
        private ShopMessageController _shopMessageController;
        private InventoryMessageController _inventoryMessageController;

        private ISession _session;
        private IConnectionFactory _connectionFactory;
        private IConnection _connection;
        private List<IMessageConsumer> _messageConsumers;
        private IMessageProducer _messageProducer;
        private SongRequestMessageController _songRequestMessageController;
        private UsersController _usersController;
        private BettingController _bettingController;


        public Main(int interval, int payout, string channelName, string busAddress)
        {
            _busAddress = busAddress;
            _channelName = channelName;
            _interval = interval;
            _payout = payout;
        }

        public void Initialize()
        {
            _messageSender = Container.GetInstance<IMessageSender>();
            _databaseManager = Container.GetInstance<IDatabaseManager>();
            _usersManager = Container.GetInstance<IUsersManager>();
            _currencyManager = Container.GetInstance<ICurrencyManager>();
            _gamesManager = Container.GetInstance<IGamesManager>();
            _bettingManager = Container.GetInstance<IBettingManager>();
            _currencyMessageController = Container.GetInstance<CurrencyMessageController>();
            _bettingMessageController = Container.GetInstance<BettingMessageController>();
            _shopMessageController = Container.GetInstance<ShopMessageController>();
            _awardsManager = Container.GetInstance<AwardsManager>();
            _optionsController = Container.GetInstance<OptionsController>();
            _inventoryMessageController = Container.GetInstance<InventoryMessageController>();
            _songRequestMessageController = Container.GetInstance<SongRequestMessageController>();
            _usersController = Container.GetInstance<UsersController>();
            _bettingController = Container.GetInstance<BettingController>();
            _usersToLookup = new List<string>();

            StartThreads();  
            SetUpActiveMq();          
        }

        private void SetUpActiveMq()
        {
            Logger.Debug(m => m("Connecting to ActiveMq on {0}...", _busAddress));

            var connecturi = new Uri(_busAddress);

            try
            {
                _connectionFactory = new NMSConnectionFactory(connecturi);
                _connection = _connectionFactory.CreateConnection();
                _session = _connection.CreateSession();
                _messageConsumers = new List<IMessageConsumer>();

                SetConsumers();
                _messageProducer = _session.CreateProducer();
                
                _connection.Start();
                Logger.Debug(m => m("Connected to ActiveMq successfully."));
            }
            catch (Exception exc)
            {
                Logger.Error(m => m("Couldn't connect to ActiveMq."), exc);
            }
        }

        private void SetConsumers()
        {
            AddConsumer("queue://command.awards.oftheday.give");
            AddConsumer("queue://command.balance.get");
            AddConsumer("queue://command.betting.bet.check");
            AddConsumer("queue://command.betting.bet.get");
            AddConsumer("queue://command.betting.bet.place");
            AddConsumer("queue://command.betting.bets.current.get");
            AddConsumer("queue://command.betting.bets.current.show");
            AddConsumer("queue://command.betting.pool.cancel");
            AddConsumer("queue://command.betting.pool.close");
            AddConsumer("queue://command.betting.pool.create");
            AddConsumer("queue://command.betting.pool.delete");
            AddConsumer("queue://command.betting.pool.open");
            AddConsumer("queue://command.betting.winner.set");
            AddConsumer("queue://command.chat.operate");
            AddConsumer("queue://command.debug.mode.enter");
            AddConsumer("queue://command.games.current.players.update");
            AddConsumer("queue://command.players.all.get");
            AddConsumer("queue://command.users.balance.check");
            AddConsumer("queue://command.users.coins.add");
            AddConsumer("queue://command.users.coins.addwithmessage");
            AddConsumer("queue://command.users.online.add");
            AddConsumer("queue://command.users.online.get");
            AddConsumer("queue://command.users.top10.get");
            AddConsumer("queue://command.users.user.check");
            AddConsumer("queue://command.users.user.coins.add");
            AddConsumer("queue://command.users.user.coins.addwithmessage");
            AddConsumer("queue://command.users.user.password.change");

            Logger.Trace(m => m("Added {0} message consumers.", _messageConsumers.Count));
        }

        private void AddConsumer(string queueName)
        {
            var endpoint = SessionUtil.GetDestination(_session, queueName);
            var consumer = _session.CreateConsumer(endpoint);
            consumer.Listener += OnMessage;

            _messageConsumers.Add(consumer);
        }

        protected void OnMessage(IMessage receivedMsg)
        {
            var message = receivedMsg as ITextMessage;
            if (message == null)
            {
                Logger.Trace(m => m("Received empty message."));
                return;
            }

            var destination = message.NMSDestination.ToString();
            Logger.Trace(m => m("Received message with destination = {0}", destination));

            try
            {
                switch (destination)
                {
                    case "queue://command.users.balance.get":
                    case "queue://command.users.coins.add":
                    case "queue://command.users.coins.addwithmessage":
                    case "queue://command.users.online.add":
                    case "queue://command.users.online.get":
                    case "queue://command.users.online.remove":
                    case "queue://command.users.top10.get":
                    case "queue://command.users.user.check":
                    case "queue://command.users.user.coins.add":
                    case "queue://command.users.user.coins.addwithmessage":
                    case "queue://command.users.user.password.change":
                        _usersController.HandleMessage(message);
                        break;

                    case "queue://command.betting.bet.get":
                    case "queue://command.betting.bet.check":
                    case "queue://command.betting.bets.current.get":
                    case "queue://command.betting.bets.current.show":
                    case "queue://command.betting.pool.open":
                    case "queue://command.betting.pool.cancel":
                    case "queue://command.betting.pool.create":
                    case "queue://command.betting.pool.delete":
                    case "queue://command.betting.winner.set":
                        _bettingController.HandleMessage(message);
                        break;

                    case "queue://command.betting.pool.close":
                        CloseBetting();
                        break;

                    case "queue://command.chat.operate":
                    case "queue://command.users.balance.check":
                    case "queue://command.betting.bet.place":
                        HandleMessage(message.Text);
                        break;

                    case "queue://command.games.current.players.update":
                        var players = JsonConvert.DeserializeObject<PlayersToUpdate>(message.Text);
                        _gamesManager.UpdateGamePlayers(players.FirstPlayer, players.SecondPlayer);
                        break;

                    case "queue://command.players.all.get":
                        var responseBody = JsonConvert.SerializeObject(GetAllPlayers());
                        var response = _session.CreateTextMessage(responseBody);

                        response.NMSCorrelationID = message.NMSCorrelationID;
                        _messageProducer.Send(message.NMSReplyTo, response);
                        break;

                    case "queue://command.awards.oftheday.give":
                        _awardsManager.AwardViewersOfTheDay();
                        break;

                    default:
                        Logger.Warn(m => m("Не был найден обработчик сообщения {0}", destination));
                        return;
                }

                Logger.Trace(m => m("Обработано сообщение {0}.", destination));
            }
            catch (Exception exc)
            {
                Logger.Error(m => m("An error has occured while proceeding message: {0}.", message), exc);
            }
        }
        
        private void StartThreads()
        {
            Logger.Debug(m => m("Start threads and timers."));

            _timerThread = new Thread(Handout);
            _timerThread.Start();

            _currencyQueue = new Timer(state => HandleCurrencyQueue(), null, Timeout.Infinite, Timeout.Infinite);

            Logger.Debug(m => m("Threads and timers started successfully."));
        }
        
        private void HandleMessage(string message)
        {
            Logger.Debug(m => m("Dispatching message: {0}.", message));
            var msg = message.Split(' ');
            
            if (msg.Length < 2) return;
            _user = msg[0].Substring(0, msg[0].Length - 1);

            switch (msg[1].ToLower())
            {
                case "!coins":
                    _currencyMessageController.Handle(msg, _user, _usersToLookup, _currencyQueue);
                    break;
                case "!gamble":
                case "!bet":
                case "!bets":
                    _bettingMessageController.Handle(msg, _user);
                    break;
                case "!secret":
                    _messageSender.SendFormat("{0}, sorry, there is no secret option today.", _user);
                    break;
                case "!buy":
                case "!shop":
                    _shopMessageController.Handle(msg, _user);
                    break;
                case "!i":
                case "!inventory":
                    _inventoryMessageController.Handle(msg, _user);
                    break;
                case "!songrequest":
                    //_songRequestMessageController.Handle(msg, _user);
                    break;
                case "!close":
                    CloseBetting();
                    break;
                default:
                    Logger.Warn(m => m("Couldn't find any controllers for message: {0}", message));
                        //_optionsController.CheckUserForGreetings(_user);
                    break;
            }
        }

        private void Print(string message)
        {
            Logger.Trace(m => m(message));
        }

        private bool CheckTime()
        {
            _time = DateTime.Now;
            var x = _time.Minute;

            if (x % _interval == 0)
            {
                //print("HANDOUT TIME!!");
                _handoutGiven = true;
                return _handoutGiven;
            }

            //print("Time doesn't match :(");
            _handoutGiven = false;
            return _handoutGiven;
        }

        private bool CheckStream()
        {
            using (var w = new WebClient())
            {
                try
                {
                    w.Proxy = null;
                    var jsonData = w.DownloadString("https://api.twitch.tv/kraken/streams/" + _channelName);
                    var stream = JObject.Parse(jsonData);
                    if (stream["stream"].HasValues)
                    {
                        Print("STREAM ONLINE");
                        return true;
                    }
                }
                catch (SocketException)
                {
                    Logger.Warn(m => m("Unable to connect to twitch API to check stream status. Skipping."));
                }
                catch (Exception e)
                {
                    var errorLog = new StreamWriter("Error_Log.log", true);
                    errorLog.WriteLine("*************Error Message (via checkStream()): " + DateTime.Now + "*********************************");
                    errorLog.WriteLine(e);
                    errorLog.WriteLine("");
                    errorLog.Close();
                }
            }

            Print("STREAM OFFLINE");
            return false;
        }

        private void HandoutCurrency()
        {
            try
            {
                _currencyManager.HandOut();
            }
            catch (Exception exc)
            {
                Logger.Error(m => m("Problem with handing out."), exc);
            }
        }

        private void Handout()
        {
            while (true)
            {
                try
                {
                    if (CheckTime() && CheckStream())
                    {
                        Print("Handout happening now! Paying everyone " + _payout + " " + _currencyManager.CurrencyName);
                        HandoutCurrency();
                    }
                    Thread.Sleep(60000);
                }
                catch (SocketException exc)
                {
                    Logger.Error(m => m("No response from twitch.  Skipping handout."), exc);
                }
                catch (Exception exc)
                {
                    Logger.Error(m => m("An error has occured while doing handout."), exc);
                }
            }
        }

        private void HandleCurrencyQueue()
        {
            if (_usersToLookup.Count == 0)
            {
                return;
            }

            var output = _currencyManager.CurrencyName + ":";
            var addComma = false;
            foreach (var person in _usersToLookup)
            {
                if (addComma){
                    output += ", ";
                }

                output += " " + person + ": " + _currencyManager.GetUserCoins(person);
                if (_bettingManager.BettingOpen)
                {
                    if (_bettingManager.IsUserInPool(person))
                    {
                        output += "[+" + _bettingManager.GetUserBetAmount(person) + " in bet]";
                    }
                }
                addComma = true;
            }
            
            ConditionalSendRawMessage(output);
            _usersToLookup.Clear();
        }
        
        private void ConditionalSendRawMessage(string message)
        {
#if !TEST
            var destination = SessionUtil.GetDestination(_session, "queue://command.chat.say");
            var msg = _session.CreateTextMessage(message);
            _messageProducer.Send(destination, msg);

            Logger.Trace(m => m("В чат отправлено сообщение: {0}", message));
#else
            Logger.Trace(m => m("В чат НЕ отправлено сообщение: {0}.", message));
#endif
        }

        public void SendMessage(string queueName)
        {
            using (var connection = _connectionFactory.CreateConnection())
            using (var session = connection.CreateSession())
            {
                var destination = SessionUtil.GetDestination(session, "queue://" + queueName);

                using (var producer = session.CreateProducer(destination))
                {
                    connection.Start();
                    producer.DeliveryMode = MsgDeliveryMode.Persistent;

                    var request = session.CreateTextMessage(string.Empty);

                    producer.Send(request);
                }
            }
        }
        
        public void ShowTop10()
        {
            _currencyManager.ShowTop10();
        }

        public int GetUsersCount()
        {
            return _usersManager.GetOnlineUsersCount();
        }

        public List<string> GetTop10()
        {
            return _currencyManager.GetTop10().Select(x => $"{x.Name} [{x.Coins}]").ToList();
        }

        public List<string> GetOnlineUsers()
        {
            return _usersManager.GetOnlineUsers().Select(x => x.Name).ToList();
        }
        
        public void SetWinner(GameResult winner)
        {
            if (winner == GameResult.Cancel)
            {
                _bettingManager.CancelBetting();
            }
            else
            {
                _bettingManager.SetWinner(winner);   
            }
        }

        public void CloseBetting(int delayInSeconds = 30)
        {
            if (_bettingManager.PoolLocked) return;

            _messageSender.Send(
                $"WARNING! Betting will be closed in {delayInSeconds} seconds, it is the right time for final bets ;)", MessagePriority.High);
            ShowCurrentBets();
            var finalBetsTimer = new System.Timers.Timer(delayInSeconds * 1000);
            finalBetsTimer.Elapsed += (sender, args) =>
            {
                _bettingManager.CloseBetting();
                finalBetsTimer.Enabled = false;
            };
            finalBetsTimer.Enabled = true;
        }

        public void OpenBetting()
        {
            _bettingManager.OpenBetting();
        }

        public void AddUser(string user)
        {
            _usersManager.SetOnline(user);
        }

        public void ShowCurrentBets()
        {
            _bettingManager.ShowCurrentBets();
        }

        public List<string> GetCurrentBets()
        {
            return _bettingManager.GetCurrentBets();
        }

        public void DeletePool()
        {
            _bettingManager.DeletePool();
        }

        public void SaveInfoToDb()
        {
            _databaseManager.UpdateOrInsertViewers(_usersManager.GetAllUsers());
        }

        public void CreateGame()
        {
            _bettingManager.CreateNewGame();
        }

        public void UpdateGamePlayers(string player1, string player2)
        {
            _gamesManager.UpdateGamePlayers(player1, player2);
        }

        public List<string> GetAllPlayers()
        {
            return _databaseManager.GetAllPlayers().Select(x => x.Name).OrderBy(x => x).ToList();
        }

        public void AddCoinsToUser(string username, int coins, bool withConfirmation)
        {
            if (withConfirmation)
            {
                _currencyManager.AddCoinsToUserWithMessage(username, coins);   
            }
            else
            {
                _currencyManager.AddCoinsToUser(username, coins);
            }
        }

        public void AddCoinsToAll(int coins, bool withConfirmation)
        {
            if (withConfirmation)
            {
                _currencyManager.AddCoinsToAllWithMessage(coins);
            }
            else
            {
                _currencyManager.AddCoinsToAllOnline(coins);
            }
        }

        public void AwardViewersOfTheStream()
        {
            _awardsManager.AwardViewersOfTheDay();
        }

        public void DebugMethod()
        {
            int x = 10;
        }

        public void Send(string text)
        {
            _messageSender.Send(text, MessagePriority.High);
        }

        public void UpdateUsersInList()
        {
            _messageSender.Send("WHO " + _channelName);
        }

        public IDatabaseManager GetDatabaseManager()
        {
            return _databaseManager;
        }

        public Container Container { get; set; }
    }
}
