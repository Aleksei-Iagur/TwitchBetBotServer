using System.Collections.Generic;
using PrismataTvServer.Classes;
using PrismataTvServer.Enums;

namespace PrismataTvServer.Interfaces
{
    public interface IUsersManager
    {
        int GetUserId(string username);
        User GetUserById(int userId);
        User GetUserByName(string username);
        void SetOnline(string username);
        void SetOffline(string username);
        int GetUserCoins(int userId);
        int GetUserCoins(string username);
        List<User> GetOnlineUsers();
        List<User> GetAllUsers();
        int GetOnlineUsersCount();
        bool UserExists(string username);
        UserRoles GetUserLevel(string username);
        void SetUserLevel(string username, UserRoles level);
        bool BuySecretOption(string username);
        bool BuyOption(string username, ViewerOptions option);
        string GetUserName(int userId);
        bool AreUserCredentialsCorrect(Credentials credentials);
        void ChangePasswordForUser(string password, string username);
        List<User> GetTop10();
    }
}
