using System.Collections.Generic;
using PrismataTvServer.Classes;
using PrismataTvServer.Enums;

namespace PrismataTvServer.Interfaces
{
    public interface IOptionsManager
    {
        bool AddSecretOption(int userId);
        bool HasUserOption(int userId, ViewerOptions option);
        bool AddOption(int userId, ViewerOptions option);
        List<UserOption> GetUserOptions(int userId);
    }
}
