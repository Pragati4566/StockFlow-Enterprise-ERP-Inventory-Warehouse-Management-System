using System.Linq.Expressions;
using Domain.Entities;

namespace Application.IServices
{
    public interface IRepository<T> where T : AuditableEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null);
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task SoftDeleteAsync(T entity);
    }
}