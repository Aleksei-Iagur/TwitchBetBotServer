using System;
using PrismataTvServer.Interfaces;
using PrismataTvServer.IoC;
using StructureMap;

namespace PrismataTvServer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var container = new Container(new MainRegistry());
            var main = container.GetInstance<IMain>();
            main.Container = container;
            main.Initialize();

            while (true)
            {
            }
        }
    }
}
