using Shared.Entities.Table;
using Microsoft.EntityFrameworkCore;

namespace Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var todo = modelBuilder.Entity<TodoItem>();
        todo.ToTable("TodoItems");
        todo.HasKey(x => x.Id);
        todo.Property(x => x.Title).IsRequired().HasMaxLength(200);
        todo.Property(x => x.IsDone).HasDefaultValue(false);
        todo.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
    }
}
