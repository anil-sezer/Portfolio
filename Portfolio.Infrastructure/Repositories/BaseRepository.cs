using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Repositories;

public abstract class BaseRepository<T>(PortfolioDbContext dbContext)
    where T : EntityBase
{
    protected readonly PortfolioDbContext dbContext = dbContext;

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await dbContext.Set<T>().FindAsync(id);
    }
    
    public virtual async Task<T> CreateAsync(T entity)
    {
        await dbContext.Set<T>().AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }
    
    public virtual async Task<T> UpdateAsync(T entity)
    {
        dbContext.Set<T>().Update(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }
    
    public virtual async Task DeleteAsync(T entity)
    {
        dbContext.Set<T>().Remove(entity);
        await dbContext.SaveChangesAsync();
    }
    
    public virtual async Task RemoveRowsAsync(List<int> ids)
    {
        var rowsToRemove = dbContext.Set<T>()
            .Where(x => ids.Contains(x.Id));

        dbContext.Set<T>().RemoveRange(rowsToRemove);
        await dbContext.SaveChangesAsync();
    }
}
