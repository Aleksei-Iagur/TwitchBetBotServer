using System.Collections.Generic;
using System.Linq;
using PrismataTvServer.Classes;
using PrismataTvServer.Enums;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Managers
{
    public class OptionsManager : IOptionsManager
    {
        private readonly IDatabaseManager _databaseManager;
        private readonly Dictionary<int, List<UserOption>> _options;

        public OptionsManager(IDatabaseManager databaseManager)
        {
            _databaseManager = databaseManager;
            _options = _databaseManager.GetAllViewersOptions();
        }

        public bool AddSecretOption(int userId)
        {
            return AddOption(userId, ViewerOptions.GreetingsBot);
        }
        
        public bool HasUserOption(int userId, ViewerOptions option)
        {
            return _options.ContainsKey(userId) && _options[userId].Any(x => x.Option == option);
        }

        public bool AddOption(int userId, ViewerOptions option)
        {
            if (_options.ContainsKey(userId) && _options[userId].Any(x=> x.Option == option))
            {
                return false;
            }

            var userOption = new UserOption(userId, option, 30, 30);

            if (!_options.ContainsKey(userId))
            {
                _options.Add(userId, new List<UserOption> { userOption });
            }
            else
            {
                if (!_options[userId].Contains(userOption))
                {
                    _options[userId].Add(userOption);
                }
            }
            
            _databaseManager.SaveOption(userOption);
            return true;
        }

        public List<UserOption> GetUserOptions(int userId)
        {
            return _options.ContainsKey(userId) ? _options[userId] : null;
        }
    }
}
