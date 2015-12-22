namespace PrismataTvServer.Classes
{
    public class ShopItem
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public int Price { get; set; }

        public ShopItem(string name, string desc, int price)
        {
            Name = name;
            Desc = desc;
            Price = price;
        }
    }
}
