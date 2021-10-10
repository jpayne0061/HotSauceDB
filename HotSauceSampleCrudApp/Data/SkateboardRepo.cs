using HotSauceDbOrm;
using HotSauceSampleCrudApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HotSauceSampleCrudApp.Data
{
    public interface ISkateboardRepo
    {
        int AddSkateboard(Skateboard skateboard);
        Skateboard GetSkateboardById(int skateboardId);
        List<Skateboard> GetAllSkateboards();
        List<Skateboard> GetSkateboardByPriceRange(decimal lowPrice, decimal highPrice);
        void UpdateSkateboardPrice(Skateboard skateboard, decimal price);
        void MarkSkateboardAsDeleted(Skateboard skateboard);
    }

    public class SkateboardRepo : ISkateboardRepo
    {
        private readonly Executor _executor;

        public SkateboardRepo()
        {
            _executor = Executor.GetInstance();
        }

        public int AddSkateboard(Skateboard skateboard)
        {
            skateboard.DateListed = DateTime.Now;

            _executor.Insert(skateboard);

            return skateboard.SkateboardId;
        }
        public Skateboard GetSkateboardById(int skateboardId)
        {
            string query = $"SELECT * FROM Skateboard WHERE SkateboardId = {skateboardId} AND Deleted = false";

            return _executor.Read<Skateboard>(query).FirstOrDefault();
        }

        public List<Skateboard> GetAllSkateboards()
        {
            string query = $"SELECT * FROM Skateboard";

            return _executor.Read<Skateboard>(query);
        }

        public List<Skateboard> GetSkateboardByPriceRange(decimal lowPrice, decimal highPrice)
        {
            string query = $"SELECT * FROM Skateboard WHERE Price >= {lowPrice} AND Price <= {highPrice} AND Deleted = false";

            return _executor.Read<Skateboard>(query);
        }
        public void UpdateSkateboardPrice(Skateboard skateboard, decimal price)
        {
            skateboard.Price = price;

            _executor.Update(skateboard);
        }
        public void MarkSkateboardAsDeleted(Skateboard skateboard)
        {
            skateboard.Deleted = true;

            _executor.Update(skateboard);
        }
    }
}
