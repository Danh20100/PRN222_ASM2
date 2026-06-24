using System.Linq.Expressions;

namespace DataAccessLayer.Repositories;

/// <summary>
/// Generic Repository contract — covers all common CRUD + query operations.
/// </summary>
public interface IGenericRepository<T> where T : class
{
    // ── Read ────────────────────────────────────────────────────────
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    // ── Write ───────────────────────────────────────────────────────
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);

    // ── Queryable (for advanced queries with Include, OrderBy, etc.) ─
    IQueryable<T> Query();
}
