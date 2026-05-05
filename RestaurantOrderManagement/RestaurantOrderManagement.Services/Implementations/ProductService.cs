using System.Collections.Generic;
using System.Threading.Tasks;
using RestaurantOrderManagement.Services.Interfaces;

namespace RestaurantOrderManagement.Services.Implementations
{
    public class ProductService : IProductService
    {
        public Task<IEnumerable<object>> GetCategoriesAsync()
        {
            return Task.FromResult<IEnumerable<object>>(new List<object>());
        }

        public Task<IEnumerable<object>> GetProductsByCategoryAsync(int categoryId)
        {
            return Task.FromResult<IEnumerable<object>>(new List<object>());
        }
    }
}
