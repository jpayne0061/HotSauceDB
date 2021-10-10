using HotSauceSampleCrudApp.Models;
using System.Collections.Generic;

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
}
