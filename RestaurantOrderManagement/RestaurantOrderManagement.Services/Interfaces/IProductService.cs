using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestaurantOrderManagement.Services.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<object>> GetCategoriesAsync();
        Task<IEnumerable<object>> GetProductsByCategoryAsync(int categoryId);
    }
}
