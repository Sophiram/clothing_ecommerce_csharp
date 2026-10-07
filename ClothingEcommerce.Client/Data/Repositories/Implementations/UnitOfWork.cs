using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Concurrent;
using WebApplication_ClothingEcommerce.Data.Repositories.Interfaces;

namespace WebApplication_ClothingEcommerce.Data.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _currentTransaction;
        private readonly ConcurrentDictionary<Type, object> _repositories = new();

        private ICartRepository? _cartRepository;
        private IProductRepository? _productRepository;
        private ICategoryRepository? _categoryRepository;
        private IBrandRepository? _brandRepository;
        private IOrderRepository? _orderRepository;
        private ICustomerRepository? _customerRepository;
        private IReviewRepository? _reviewRepository;
        private IWishlistRepository? _wishlistRepository;
        private IInventoryRepository? _inventoryRepository;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public ICartRepository Carts => _cartRepository ??= new CartRepository(_context);
        public IProductRepository Products => _productRepository ??= new ProductRepository(_context);
        public ICategoryRepository Categories => _categoryRepository ??= new CategoryRepository(_context);
        public IBrandRepository Brands => _brandRepository ??= new BrandRepository(_context);
        public IOrderRepository Orders => _orderRepository ??= new OrderRepository(_context);
        public ICustomerRepository Customers => _customerRepository ??= new CustomerRepository(_context);
        public IReviewRepository Reviews => _reviewRepository ??= new ReviewRepository(_context);
        public IWishlistRepository Wishlists => _wishlistRepository ??= new WishlistRepository(_context);
        public IInventoryRepository Inventories => _inventoryRepository ??= new InventoryRepository(_context);

        public IRepository<T> Repository<T>() where T : class
        {
            return (IRepository<T>)_repositories.GetOrAdd(typeof(T), _ => new Repository<T>(_context));
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync()
        {
            if (_currentTransaction != null) return;
            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_currentTransaction != null)
                {
                    await _currentTransaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            try
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.RollbackAsync();
                }
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public void Dispose()
        {
            _currentTransaction?.Dispose();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
