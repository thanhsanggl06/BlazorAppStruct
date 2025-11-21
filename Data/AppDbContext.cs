using Shared.Entities.Table;
using Microsoft.EntityFrameworkCore;
using Shared.Entities.Dtos; // DTOs

namespace Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<TodoItemDto> TodoItemDtos => Set<TodoItemDto>(); // keyless query type for SP/FromSql

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var todo = modelBuilder.Entity<TodoItem>();
        todo.ToTable("TodoItems");
        todo.HasKey(x => x.Id);
        todo.Property(x => x.Title).IsRequired().HasMaxLength(200);
        todo.Property(x => x.IsDone).HasDefaultValue(false);
        todo.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        // Keyless DTO mapping (no table/view mapping to avoid shared-table conflicts)
        var dto = modelBuilder.Entity<TodoItemDto>();
        dto.HasNoKey();
        dto.ToView(null); // not mapped to a real table or view
        dto.Property(p => p.Id);
        dto.Property(p => p.Title);
        dto.Property(p => p.IsDone);
        dto.Property(p => p.CreatedAt);
    }
}
