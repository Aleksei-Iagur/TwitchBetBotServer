using System;
using System.Collections.Generic;
using System.Linq;
using PrismataTvServer.Classes;
using PrismataTvServer.Enums;
using PrismataTvServer.Interfaces;

namespace PrismataTvServer.Managers
{
    public class UsersManager : IUsersManager
    {
        readonly List<User> _allUsers;
        readonly List<User> _onlineUsers = new List<User>();
        readonly IOptionsManager _optionsManager;
        private readonly IDatabaseManager _databaseManager;

        public UsersManager(IEnumerable<User> initialUsersList, IOptionsManager optionsManager, IDatabaseManager databaseManager)
        {
            _allUsers = new List<User>(initialUsersList);
            _onlineUsers = new List<User>();
            _optionsManager = optionsManager;
            _databaseManager = databaseManager;
        }

        public User GetUserByName(string username)
        {
            var user = _allUsers.Find(x => x.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user != null) return user;

            var userId = _databaseManager.CreateNewUser(username);
            user = new User { Id = userId, Name = username, Coins = 5, UserLevel = 0 };

            lock (_allUsers)
            {
                _allUsers.Add(user);
            }

            return user;
        }

        public int GetUserId(string username)
        {
            var user = GetUserByName(username);
            return user != null ? user.Id : -1;
        }

        public User GetUserById(int userId)
        {
            return _allUsers.Find(x => x.Id == userId);
        }
        
        public void SetOnline(string username)
        {
            if (IsUserBot(username) || _onlineUsers.Any(x => x.Name.Equals(username, StringComparison.OrdinalIgnoreCase))) return;
            lock (_onlineUsers)
            {
                _onlineUsers.Add(GetUserByName(username));   
            }
        }

        private bool IsUserBot(string username)
        {
            return username == "Moobot" || username == "Nightbot" || username == "Prismatatv";
        }

        public void SetOffline(string username)
        {
            var user = _onlineUsers.Find(x => x.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user == null) return;
            
            lock (_onlineUsers)
            {
                _onlineUsers.Remove(user);
            }
        }

        public int GetUserCoins(int userId)
        {
            return GetUserById(userId).Coins;
        }

        public int GetUserCoins(string username)
        {
            return GetUserCoins(GetUserId(username));
        }

        public List<User> GetOnlineUsers()
        {
            return _onlineUsers;
        }

        public List<User> GetAllUsers()
        {
            return _allUsers;
        }

        public int GetOnlineUsersCount()
        {
            return _onlineUsers.Count;
        }

        public bool UserExists(string username)
        {
            return _allUsers.Exists(x => x.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public UserRoles GetUserLevel(string username)
        {
            return GetUserByName(username).UserLevel;
        }

        public void SetUserLevel(string username, UserRoles level)
        {
            GetUserByName(username).UserLevel = level;
        }

        public bool BuySecretOption(string username)
        {
            return _optionsManager.AddSecretOption(GetUserId(username));
        }

        public bool BuyOption(string username, ViewerOptions option)
        {
            return _optionsManager.AddOption(GetUserId(username), option);
        }

        public string GetUserName(int userId)
        {
            return _allUsers.Find(x => x.Id == userId).Name;
        }

        public bool AreUserCredentialsCorrect(Credentials credentials)
        {
            return _allUsers.Exists(x => x.Name == credentials.Username && x.Password == credentials.Password);
        }

        public void ChangePasswordForUser(string password, string username)
        {
            var user = GetUserByName(username);
            user.Password = password;
            _databaseManager.ChangeUserPassword(user);
        }

        public List<User> GetTop10()
        {
            return _allUsers.OrderByDescending(x => x.Coins).Take(10).ToList();
        }
    }
}
