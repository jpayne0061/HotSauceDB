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
            Executor.GetInstance().DropDatabaseIfExists();

            //creating a table is an idempotent operation
            var executor = Executor.GetInstance();

            executor.CreateTable<Skateboard>();

            _skateboardRepo = new SkateboardRepo();

            int userSelection;

            do
            {
                userSelection = Menu();

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
                        MarkSkateboardAsDeleted();
                        break;
                }
            }
            while (userSelection != 5);
        }

        static int Menu()
        {
            Console.WriteLine("1: Create new skateboard");
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
                DisplaySkateboard(skateboard);
                Console.WriteLine();
            }
        }

        static void UpdateSkateboardPrice()
        {
            Skateboard skateboard = GetSkateboardById();
            DisplaySkateboard(skateboard);

            Console.WriteLine("Enter new price for skateboard");
            decimal newPrice = decimal.Parse(Console.ReadLine());

            _skateboardRepo.UpdateSkateboardPrice(skateboard, newPrice);

            Console.WriteLine("skateboard price has been updated");
        }

        static void MarkSkateboardAsDeleted()
        {
            Skateboard skateboard = GetSkateboardById();
            _skateboardRepo.MarkSkateboardAsDeleted(skateboard);

            Console.WriteLine("skateboard has been marked as deleted");
        }

        static Skateboard GetSkateboardById()
        {
            Console.WriteLine("Enter skateboard id");
            int skateboardId = int.Parse(Console.ReadLine());

            Skateboard skateboard = _skateboardRepo.GetSkateboardById(skateboardId);

            return skateboard;
        }

        static void DisplaySkateboard(Skateboard skateboard)
        {
            Console.WriteLine($"Skateboard id: {skateboard.SkateboardId}");
            Console.WriteLine($"Skateboard brand: {skateboard.Brand}");
            Console.WriteLine($"Skateboard price: {skateboard.Price}");
            Console.WriteLine($"Skateboard date listed: {skateboard.DateListed}");
        }
    }
}
