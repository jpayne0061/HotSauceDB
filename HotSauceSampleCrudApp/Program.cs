using HotSauceDbOrm;
using HotSauceSampleCrudApp.Data;
using HotSauceSampleCrudApp.Models;
using System;

namespace HotSauceSampleCrudApp
{
    class Program
    {
        static ISkateboardRepo _skateboardRepo;

        static void Main(string[] args)
        {
            Startup();

            _skateboardRepo = new SkateboardRepo();

            Loop();
        }

        static void Loop()
        {
            while (true)
            {
                int userSelection = Menu();

                if (userSelection == 5)
                {
                    break;
                }

                switch (userSelection)
                {
                    case 1:
                        AddNewSkateBoard();
                        break;
                    case 2:
                        ShowAllSkateboards();
                        break;
                    case 3:
                        UpdateSkateboardPrice();
                        break;
                    case 4:
                        break;
                }
            }
        }

        static int Menu()
        {
            Console.WriteLine("1: List new skateboard");
            Console.WriteLine("2: Show all skateboards");
            Console.WriteLine("3: Update skateboard price");
            Console.WriteLine("4: Mark skateboard as deleted");
            Console.WriteLine("5: Quit");

            return int.Parse(Console.ReadLine());
        }

        static void AddNewSkateBoard()
        {
            Skateboard skateboard = new Skateboard();

            Console.WriteLine("Please enter brand name");
            skateboard.Brand = Console.ReadLine();

            Console.WriteLine("Please enter price");
            skateboard.Price = decimal.Parse(Console.ReadLine());

            _skateboardRepo.AddSkateboard(skateboard);
        }

        static void ShowAllSkateboards()
        {
            var skateboards = _skateboardRepo.GetAllSkateboards();

            foreach (var skateboard in skateboards)
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                DisplaySkateboard(skateboard);
                Console.WriteLine();
                Console.ResetColor();
            }
        }

        static void UpdateSkateboardPrice()
        {
            Console.WriteLine("Enter skateboard id");
            int skateboardId = int.Parse(Console.ReadLine());

            Console.ForegroundColor = ConsoleColor.Yellow;
            Skateboard skateboard = _skateboardRepo.GetSkateboardById(skateboardId);
            DisplaySkateboard(skateboard);
            Console.ResetColor();

            Console.WriteLine("Enter new price for skateboard");
            decimal newPrice = decimal.Parse(Console.ReadLine());

            _skateboardRepo.UpdateSkateboardPrice(skateboard, newPrice);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("skateboard price has been updated!");
            Console.ResetColor();
        }

        static void DisplaySkateboard(Skateboard skateboard)
        {
            Console.WriteLine($"Skateboard id: {skateboard.SkateboardId}");
            Console.WriteLine($"Skateboard brand: {skateboard.Brand}");
            Console.WriteLine($"Skateboard price: {skateboard.Price}");
            Console.WriteLine($"Skateboard date listed: {skateboard.DateListed}");
        }

        static void Startup()
        {
            Executor executor = Executor.GetInstance();

            //creating a table is an omnipotent operation
            executor.CreateTable<Skateboard>();
        }
    }
}
