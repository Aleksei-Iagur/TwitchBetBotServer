using StructureMap;

namespace PrismataTvServer.Interfaces
{
    public interface IMain
    {
        Container Container { get; set; }
        void Initialize();
    }
}