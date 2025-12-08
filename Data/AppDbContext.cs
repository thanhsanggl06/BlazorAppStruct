using Shared.Entities.Table;
using Microsoft.EntityFrameworkCore;
using Shared.Entities.Dtos; // DTOs

namespace Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<TodoItemDto> TodoItemDtos => Set<TodoItemDto>(); // keyless query type for SP/FromSql
    public DbSet<ApplicationLog> ApplicationLogs => Set<ApplicationLog>();

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

        // ApplicationLog entity mapping
        var log = modelBuilder.Entity<ApplicationLog>();
        log.ToTable("ApplicationLogs");
        log.HasKey(x => x.Id);
        log.Property(x => x.Timestamp).IsRequired();
        log.Property(x => x.Level).IsRequired().HasMaxLength(50);
        log.Property(x => x.Message).HasMaxLength(-1); // NVARCHAR(MAX)
        log.Property(x => x.MessageTemplate).HasMaxLength(-1);
        log.Property(x => x.Exception).HasMaxLength(-1);
        log.Property(x => x.Properties).HasMaxLength(-1);
        log.Property(x => x.LogEvent).HasMaxLength(-1);
        log.Property(x => x.CorrelationId).HasMaxLength(100);
        log.Property(x => x.RequestPath).HasMaxLength(500);
        log.Property(x => x.RequestMethod).HasMaxLength(20);
        log.Property(x => x.SourceContext).HasMaxLength(500);
        log.Property(x => x.MachineName).HasMaxLength(100);
        log.Property(x => x.EnvironmentName).HasMaxLength(50);
        log.Property(x => x.ApplicationName).HasMaxLength(100);
    }
}
