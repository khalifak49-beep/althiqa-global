using System.Linq.Expressions;
using HomeMaids.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeMaids.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;

    public GenericRepository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(object id) => await DbSet.FindAsync(id);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync() => await DbSet.AsNoTracking().ToListAsync();

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await DbSet.AsNoTracking().Where(predicate).ToListAsync();

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        => await DbSet.FirstOrDefaultAsync(predicate);

    public virtual IQueryable<T> Query() => DbSet.AsQueryable();

    public virtual async Task AddAsync(T entity) => await DbSet.AddAsync(entity);

    public virtual async Task AddRangeAsync(IEnumerable<T> entities) => await DbSet.AddRangeAsync(entities);

    public virtual void Update(T entity) => DbSet.Update(entity);

    public virtual void Remove(T entity) => DbSet.Remove(entity);

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        => predicate == null ? await DbSet.CountAsync() : await DbSet.CountAsync(predicate);
}
