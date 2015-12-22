using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using Apache.NMS;
using Common.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PrismataTvServer.Classes;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.BusControllers
{
    public class UsersController
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(UsersController));

        private readonly IUsersManager _usersManager;
        private readonly ICurrencyManager _currencyManager;
        private ISession _session;
        private IMessageProducer _messageProducer;
        private IConnectionFactory _connectionFactory;
        private IConnection _connection;

        public UsersController(IUsersManager usersManager, ICurrencyManager currencyManager)
        {
            _usersManager = usersManager;
            _currencyManager = currencyManager;
            SetUpActiveMq();
        }

        public void HandleMessage(ITextMessage message)
        {
            var destination = message.NMSDestination.ToString();

            try
            {
                string responseBody;
                ITextMessage response;
                UserCoinsChanging coinsChanging;
                switch (destination)
                {
                    case "queue://command.users.coins.add":
                        _currencyManager.AddCoinsToAllOnline(int.Parse(message.Text));
                        break;
                    case "queue://command.users.coins.addwithmessage":
                        _currencyManager.AddCoinsToAllWithMessage(int.Parse(message.Text));
                        break;
                    case "queue://command.users.user.coins.add":
                        coinsChanging = JsonConvert.DeserializeObject<UserCoinsChanging>(message.Text);
                        _currencyManager.AddCoinsToUser(coinsChanging.Username, coinsChanging.CoinsDiff);
                        break;
                    case "queue://command.users.user.coins.addwithmessage":
                        coinsChanging = JsonConvert.DeserializeObject<UserCoinsChanging>(message.Text);
                        _currencyManager.AddCoinsToUserWithMessage(coinsChanging.Username, coinsChanging.CoinsDiff);
                        break;
                    case "queue://command.users.online.get":
                        responseBody = JsonConvert.SerializeObject(_usersManager.GetOnlineUsers().Select(x => x.Name));
                        response = _session.CreateTextMessage(responseBody);

                        response.NMSCorrelationID = message.NMSCorrelationID;
                        _messageProducer.Send(message.NMSReplyTo, response);
                        break;

                    case "queue://command.users.online.add":
                        AddUserToList(message.Text.Trim('"'));
                        break;

                    case "queue://command.users.online.remove":
                        RemoveUserFromList(message.Text.Trim('"'));
                        break;

                    case "queue://command.users.user.check":
                        var credentialsToCheck = ((JObject)JsonConvert.DeserializeObject(message.Text)).ToObject<Credentials>();
                        responseBody = _usersManager.AreUserCredentialsCorrect(credentialsToCheck).ToString();
                        response = _session.CreateTextMessage(responseBody);

                        response.NMSCorrelationID = message.NMSCorrelationID;
                        _messageProducer.Send(message.NMSReplyTo, response);
                        break;

                    case "queue://command.users.user.password.change":
                        var credentialsToChange = JsonConvert.DeserializeObject<Credentials>(message.Text);
                        _usersManager.ChangePasswordForUser(credentialsToChange.Password, credentialsToChange.Username);
                        break;

                    case "queue://command.users.top10.get":
                        var top10Users = _usersManager.GetTop10();
                        var top10Response = JsonConvert.SerializeObject(top10Users);
                        _messageProducer.Send(message.NMSReplyTo, top10Response);
                        break;

                    case "queue://command.users.balance.get":
                        var coins = _usersManager.GetUserCoins(message.Text);
                        var balanceResponse = _session.CreateTextMessage(coins.ToString(CultureInfo.InvariantCulture));

                        balanceResponse.NMSCorrelationID = message.NMSCorrelationID;
                        _messageProducer.Send(message.NMSReplyTo, balanceResponse);
                        break;
                }
            }
            catch (Exception exc)
            {
                Logger.Error(m => m("An error has occured while proceeding message in UsersController: {0}.", message), exc);
            }
        }

        private void AddUserToList(string username)
        {
            Logger.Trace(m => m("User {0} is now online.", username));
            _usersManager.SetOnline(username);
        }

        private void RemoveUserFromList(string username)
        {
            Logger.Trace(m => m("User {0} has gone offline.", username));
            _usersManager.SetOffline(username);
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
