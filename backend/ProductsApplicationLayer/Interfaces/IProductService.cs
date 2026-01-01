using ProductsApplicationLayer.ViewModals;

namespace ProductsApplicationLayer;

public interface IProductService
{
    IEnumerable<ProductViewModal> GetAllProducts();
}