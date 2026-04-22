using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.Interfaces;

namespace TicketingSystem.DAL.EF.Repositories;

public class Repository<T>(TicketingDbContext context) : IRepository<T>
    where T : class
{
    protected readonly TicketingDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(int id) =>
        await DbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() =>
        await DbSet.ToListAsync();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await DbSet.Where(predicate).ToListAsync();

    public async Task AddAsync(T entity) =>
        await DbSet.AddAsync(entity);

    public void Update(T entity) =>
        DbSet.Update(entity);

    public void Delete(T entity) =>
        DbSet.Remove(entity);
}
