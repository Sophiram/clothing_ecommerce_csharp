namespace WebApplication_ClothingEcommerce.Data.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        ICartRepository Carts { get; }
        IProductRepository Products { get; }
        ICategoryRepository Categories { get; }
        IBrandRepository Brands { get; }
        IOrderRepository Orders { get; }
        ICustomerRepository Customers { get; }
        IReviewRepository Reviews { get; }
        IWishlistRepository Wishlists { get; }
        IInventoryRepository Inventories { get; }

        IRepository<T> Repository<T>() where T : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
