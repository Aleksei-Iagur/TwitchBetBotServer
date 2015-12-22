using PrismataTvServer.Enums;

namespace PrismataTvServer.Classes
{
    public class UserOption
    {
        public int UserId { get; set; }
        public ViewerOptions Option { get; set; }
        public int Days { get; set; }
        public int ExpiresIn { get; set; }

        public UserOption(int userId, ViewerOptions option, int days, int expiresIn)
        {
            UserId = userId;
            Option = option;
            Days = days;
            ExpiresIn = expiresIn;
        }
    }
}
