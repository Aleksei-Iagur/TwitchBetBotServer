using System.Collections.Generic;
using System.Configuration;
using PrismataTvServer.Classes;
using PrismataTvServer.Interfaces;
using PrismataTvServer.Managers;
using StructureMap;
using StructureMap.Configuration.DSL;

namespace PrismataTvServer.IoC
{
    public class MainRegistry : Registry
    {
        public MainRegistry()
        {
            string currencyName = ConfigurationManager.AppSettings["currency"] ?? "coins";
            string intervalString = ConfigurationManager.AppSettings["interval"] ?? "20000";
            string payoutString = ConfigurationManager.AppSettings["payout"] ?? "1";
            string connectionString = ConfigurationManager.ConnectionStrings["dbconnection"].ConnectionString;
            string channelName = ConfigurationManager.AppSettings["channel"];
            string busAdderss = ConfigurationManager.ConnectionStrings["service-bus"].ConnectionString;

            int interval = int.Parse(intervalString);
            int payout = int.Parse(payoutString);

            For<IMain>()
                .Use<Main>()
                .Ctor<int>("interval").Is(interval)
                .Ctor<int>("payout").Is(payout)
                .Ctor<string>("channelName").Is(channelName)
                .Ctor<string>("busAddress").Is(busAdderss)
                .Singleton();

            For<IBusProxy>()
                .Use<BusProxy>()
                .Ctor<string>("busAddress").Is(busAdderss)
                .Singleton();

            For<IBettingManager>().Singleton().Use<BettingManager>();

            For<ICurrencyManager>()
                .Singleton()
                .Use<CurrencyManager>()
                .Ctor<string>("currencyName")
                .Is(currencyName);

            For<IDatabaseManager>()
                .Singleton()
                .Use<DatabaseManager>()
                .Ctor<string>("connectionString")
                .Is(connectionString);

            For<IGamesManager>().Singleton().Use<GamesManager>();

            For<IMessageSender>()
                .Singleton()
                .Use<MessageSender>();

            For<IOptionsManager>().Singleton().Use<OptionsManager>();

            For<IPlayersManager>()
                .Singleton()
                .Use<PlayersManager>()
                .Ctor<List<Player>>("initList")
                .Is(x => x.GetInstance<IDatabaseManager>().GetAllPlayers());

            For<IShopManager>().Singleton().Use<ShopManager>();

            For<IUsersManager>()
                .Singleton()
                .Use<UsersManager>()
                .Ctor<IEnumerable<User>>("initialUsersList")
                .Is(x => x.GetInstance<IDatabaseManager>().GetAllUsers());
        }
    }
}
