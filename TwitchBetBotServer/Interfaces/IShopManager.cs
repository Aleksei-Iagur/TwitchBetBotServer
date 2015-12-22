namespace PrismataTvServer.Interfaces
{
    interface IShopManager
    {
        void ShowAllItems();
        void BuyItem(int userId, string itemname);
    }
}
