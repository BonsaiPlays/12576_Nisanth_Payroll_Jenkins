using Microsoft.EntityFrameworkCore;

namespace PayrollApi.Utils
{
    public static class Pagination
    {
        public static async Task<(int total, List<T> items)> ToPagedAsync<T>(this IQueryable<T> query, int page, int pageSize)
        {
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (total, items);
        }
    }
}
