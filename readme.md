# Tạo solution blazorwasm hosted (Chạy lần lượt các lệnh sau trong command promt)

### Khởi tạo solution

```
mkdir BlazorAppStruct && cd BlazorAppStruct
dotnet new sln -n BlazorAppStruct

dotnet new blazorwasm -n Client
dotnet new webapi -n Server
dotnet new classlib -n Shared
dotnet new classlib -n Data
dotnet new classlib -n Services
dotnet new razorclasslib -n Components

dotnet sln BlazorAppStruct.sln add ./Client/Client.csproj ./Server/Server.csproj ./Shared/Shared.csproj ./Data/Data.csproj ./Services/Services.csproj ./Components/Components.csproj
```

### Thêm project reference

```
dotnet add Server reference Client
dotnet add Server reference Shared
dotnet add Server reference Data
dotnet add Server reference Services
dotnet add Services reference Data
dotnet add Services reference Shared
dotnet add Client reference Shared
dotnet add Components reference Shared
dotnet add Data reference Shared
```

### Gói để Server host Blazor WASM client và dùng EF Core (Add thủ công nếu không có mạng)

```
dotnet add Server package Microsoft.AspNetCore.Components.WebAssembly.Server
dotnet add Server package Microsoft.EntityFrameworkCore
dotnet add Server package Microsoft.EntityFrameworkCore.SqlServer
dotnet add Server package Swashbuckle.AspNetCore
```

### Gói EF Core cho Data (Add thủ công nếu không có mạng)

```
dotnet add Data package Microsoft.EntityFrameworkCore
dotnet add Data package Microsoft.EntityFrameworkCore.SqlServer
dotnet add Data package Microsoft.EntityFrameworkCore.Design
```

# Tạo các file cần thiết

## 1. Tạo file AppDbContext.cs trong project Data

Tạo file `Data/AppDbContext.cs`:

```csharp
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
```

## 2. Tạo các Entity và DTO trong project Shared

### Tạo file `Shared/Entities/Table/TodoItem.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Entities.Table
{
    public class TodoItem
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public bool IsDone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

### Tạo file `Shared/Entities/Dtos/TodoItemDto.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Entities.Dtos
{
    public record TodoItemDto(
        int Id,
        string Title,
        bool IsDone,
        DateTime CreatedAt
    );

    public record CreateTodoRequest(string Title);

    public record UpdateTodoRequest(string Title, bool IsDone);
}
```

## 3. Tạo Service Layer

### Tạo file `Services/Interfaces/ITodoService.cs`:

```csharp
using Shared.Entities.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ITodoService
    {
        Task<IReadOnlyList<TodoItemDto>> GetAllAsync(CancellationToken ct = default);
        Task<TodoItemDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<TodoItemDto> CreateAsync(CreateTodoRequest request, CancellationToken ct = default);
        Task<TodoItemDto?> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    }
}
```

### Tạo file `Services/Implements/TodoService.cs`:

```csharp
using Data;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using Shared.Entities.Dtos;
using Shared.Entities.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements
{
    public class TodoService(AppDbContext db) : ITodoService
    {
        public async Task<IReadOnlyList<TodoItemDto>> GetAllAsync(CancellationToken ct = default)
        {
            return await db.TodoItems
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new TodoItemDto(x.Id, x.Title, x.IsDone, x.CreatedAt))
                .ToListAsync(ct);
        }

        public async Task<TodoItemDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await db.TodoItems
                .Where(x => x.Id == id)
                .Select(x => new TodoItemDto(x.Id, x.Title, x.IsDone, x.CreatedAt))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<TodoItemDto> CreateAsync(CreateTodoRequest request, CancellationToken ct = default)
        {
            var entity = new TodoItem
            {
                Title = request.Title.Trim(),
                IsDone = false,
                CreatedAt = DateTime.UtcNow
            };

            db.TodoItems.Add(entity);
            await db.SaveChangesAsync(ct);

            return new TodoItemDto(entity.Id, entity.Title, entity.IsDone, entity.CreatedAt);
        }

        public async Task<TodoItemDto?> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken ct = default)
        {
            var entity = await db.TodoItems.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return null;

            entity.Title = request.Title.Trim();
            entity.IsDone = request.IsDone;

            await db.SaveChangesAsync(ct);

            return new TodoItemDto(entity.Id, entity.Title, entity.IsDone, entity.CreatedAt);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await db.TodoItems.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return false;

            db.TodoItems.Remove(entity);
            await db.SaveChangesAsync(ct);
            return true;
        }
    }
}
```

## 4. Tạo API Controller

### Tạo file `Server/Controllers/TodosController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Shared.Entities.Dtos;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TodosController(ITodoService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<TodoItemDto>>> GetAll(CancellationToken ct)
            => Ok(await service.GetAllAsync(ct));

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TodoItemDto>> GetById(int id, CancellationToken ct)
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<TodoItemDto>> Create([FromBody] CreateTodoRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest("Title is required.");
            var created = await service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<TodoItemDto>> Update(int id, [FromBody] UpdateTodoRequest request, CancellationToken ct)
        {
            var updated = await service.UpdateAsync(id, request, ct);
            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await service.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}
```

## 5. Cấu hình Server Program.cs

### Cập nhật file `Server/Program.cs`:

```csharp
using Data;
using Services.Implements;
using Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// API + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
    options.UseSqlServer(connStr);
});

// App services
builder.Services.AddScoped<ITodoService, TodoService>();

// HttpClient (nếu cần)
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Host Blazor WASM client (cần package Microsoft.AspNetCore.Components.WebAssembly.Server)
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.MapControllers();

// Fallback cho client routing
app.MapFallbackToFile("index.html");

app.Run();
```

### Thêm Connection String vào `Server/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BlazorTodoDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

## 6. Tạo Blazor Client Pages

### Tạo file `Client/Pages/Todos.razor`:

```razor
@page "/todos"
@inject HttpClient Http
@using Shared.Entities.Dtos

<h3>Todos</h3>

<div class="mb-3">
    <input class="form-control" @bind="newTitle" placeholder="New todo title..."/>
    <button class="btn btn-primary mt-2" @onclick="AddAsync" disabled="@string.IsNullOrWhiteSpace(newTitle)">Add</button>
</div>

@if (loading)
{
    <p>Loading...</p>
}
else if (todos is null || todos.Count == 0)
{
    <p>No items.</p>
}
else
{
    <ul class="list-group">
        @foreach (var t in todos)
        {
            <li class="list-group-item d-flex align-items-center justify-content-between">
                <div class="form-check">
                    <input class="form-check-input" type="checkbox" checked="@t.IsDone" @onchange="(e => ToggleAsync(t, ((bool)e.Value!)))" />
                    <label class="form-check-label @(t.IsDone ? "text-decoration-line-through" : "")">@t.Title</label>
                </div>
                <button class="btn btn-sm btn-outline-danger" @onclick="() => DeleteAsync(t.Id)">Delete</button>
            </li>
        }
    </ul>
}

@code {
    private List<TodoItemDto> todos = new();
    private string? newTitle;
    private bool loading;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        loading = true;
        try
        {
            var data = await Http.GetFromJsonAsync<List<TodoItemDto>>("api/todos");
            todos = data ?? [];
        }
        finally
        {
            loading = false;
        }
    }

    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(newTitle)) return;
        var req = new CreateTodoRequest(newTitle);
        var res = await Http.PostAsJsonAsync("api/todos", req);
        if (res.IsSuccessStatusCode)
        {
            var created = await res.Content.ReadFromJsonAsync<TodoItemDto>();
            if (created is not null) todos.Insert(0, created);
            newTitle = string.Empty;
        }
    }

    private async Task ToggleAsync(TodoItemDto item, bool isDone)
    {
        var req = new UpdateTodoRequest(item.Title, isDone);
        var res = await Http.PutAsJsonAsync($"api/todos/{item.Id}", req);
        if (res.IsSuccessStatusCode)
        {
            var updated = await res.Content.ReadFromJsonAsync<TodoItemDto>();
            if (updated is not null)
            {
                var idx = todos.FindIndex(x => x.Id == updated.Id);
                if (idx >= 0) todos[idx] = updated;
            }
        }
    }

    private async Task DeleteAsync(int id)
    {
        var res = await Http.DeleteAsync($"api/todos/{id}");
        if (res.IsSuccessStatusCode)
        {
            todos.RemoveAll(x => x.Id == id);
        }
    }
}
```

## 7. Tạo Migration và Database

### Tạo Migration:

```bash
# Di chuyển vào thư mục Server (nơi có DbContext reference)
cd Server

# Tạo migration đầu tiên
dotnet ef migrations add InitialCreate --project ../Data --startup-project .

# Cập nhật database
dotnet ef database update --project ../Data --startup-project .
```

## 8. Chạy ứng dụng

```bash
# Chạy từ thư mục Server
cd Server
dotnet run

# Hoặc chạy với watch để auto reload
dotnet watch run
```

Ứng dụng sẽ chạy tại:

- https://localhost:7xxx (HTTPS)
- http://localhost:5xxx (HTTP)
- Swagger UI: https://localhost:7xxx/swagger

## 9. Cấu trúc thư mục hoàn chỉnh

```
BlazorAppStruct/
├── Client/                 # Blazor WebAssembly Client
│   ├── Pages/
│   │   └── Todos.razor    # Todo management page
│   └── Program.cs
├── Server/                 # ASP.NET Core API Server
│   ├── Controllers/
│   │   └── TodosController.cs
│   ├── Program.cs
│   └── appsettings.json
├── Shared/                 # Shared models/DTOs
│   └── Entities/
│       ├── Dtos/
│       │   └── TodoItemDto.cs
│       └── Table/
│           └── TodoItem.cs
├── Data/                   # Entity Framework DbContext
│   └── AppDbContext.cs
├── Services/               # Business Logic Layer
│   ├── Interfaces/
│   │   └── ITodoService.cs
│   └── Implements/
│       └── TodoService.cs
└── Components/             # Reusable Razor Components
```

## 10. Cập nhật Navigation Menu

### Cập nhật file `Client/Layout/NavMenu.razor` để thêm link Todos:

```razor
<div class="top-row ps-3 navbar navbar-dark">
    <div class="container-fluid">
        <a class="navbar-brand" href="">Client</a>
        <button title="Navigation menu" class="navbar-toggler" @onclick="ToggleNavMenu">
            <span class="navbar-toggler-icon"></span>
        </button>
    </div>
</div>

<div class="@NavMenuCssClass nav-scrollable" @onclick="ToggleNavMenu">
    <nav class="nav flex-column">
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="" Match="NavLinkMatch.All">
                <span class="bi bi-house-door-fill-nav-menu" aria-hidden="true"></span> Home
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="counter">
                <span class="bi bi-plus-square-fill-nav-menu" aria-hidden="true"></span> Counter
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="weather">
                <span class="bi bi-list-nested-nav-menu" aria-hidden="true"></span> Weather
            </NavLink>
        </div>
         <div class="nav-item px-3">
            <NavLink class="nav-link" href="todos" Match="NavLinkMatch.All">
                <span class="oi oi-list-rich" aria-hidden="true"></span> Todos
            </NavLink>
        </div>
    </nav>
</div>

@code {
    private bool collapseNavMenu = true;

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }
}
```

## 11. Cấu hình HttpClient cho Blazor WASM

### Cập nhật file `Client/Program.cs`:

```csharp
using Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
```

## Cài đặt Entity Framework Tools

### 1. Cài đặt EF Core Tools Global

```bash
# Cài đặt EF Core tools globally (chỉ cần chạy 1 lần trên máy)
dotnet tool install --global dotnet-ef

# Hoặc update nếu đã cài
dotnet tool update --global dotnet-ef

# Kiểm tra version
dotnet ef --version
```

### 2. Cài đặt EF Core Tools Local (Optional - cho project cụ thể)

```bash
# Tạo tool manifest (nếu chưa có)
dotnet new tool-manifest

# Cài đặt local tools
dotnet tool install dotnet-ef

# Restore tools
dotnet tool restore
```

## Database First Approach (Scaffold từ Database có sẵn)

### 1. Tạo Database và Tables trước

Tạo database và tables trong SQL Server trước khi scaffold:

```sql
-- Tạo database
CREATE DATABASE BlazorTodoDbFirst;
GO

USE BlazorTodoDbFirst;
GO

-- Tạo bảng Categories
CREATE TABLE Categories (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    Description nvarchar(500) NULL,
    CreatedAt datetime2 DEFAULT GETUTCDATE(),
    IsActive bit DEFAULT 1
);

-- Tạo bảng TodoItems với foreign key
CREATE TABLE TodoItems (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Title nvarchar(200) NOT NULL,
    Description nvarchar(1000) NULL,
    IsDone bit DEFAULT 0,
    Priority int DEFAULT 1, -- 1: Low, 2: Medium, 3: High
    DueDate datetime2 NULL,
    CategoryId int NULL,
    CreatedAt datetime2 DEFAULT GETUTCDATE(),
    UpdatedAt datetime2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);

-- Tạo bảng Users
CREATE TABLE Users (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Username nvarchar(50) NOT NULL UNIQUE,
    Email nvarchar(100) NOT NULL UNIQUE,
    FullName nvarchar(100) NOT NULL,
    CreatedAt datetime2 DEFAULT GETUTCDATE(),
    IsActive bit DEFAULT 1
);

-- Thêm sample data
INSERT INTO Categories (Name, Description) VALUES
('Work', 'Work related tasks'),
('Personal', 'Personal tasks'),
('Shopping', 'Shopping lists'),
('Health', 'Health and fitness tasks');

INSERT INTO TodoItems (Title, Description, Priority, CategoryId) VALUES
('Complete project documentation', 'Write comprehensive documentation for the Blazor project', 3, 1),
('Buy groceries', 'Milk, Bread, Eggs, Fruits', 1, 3),
('Morning exercise', 'Go for a 30-minute run', 2, 4),
('Code review', 'Review pull requests from team members', 2, 1);

INSERT INTO Users (Username, Email, FullName) VALUES
('john.doe', 'john.doe@email.com', 'John Doe'),
('jane.smith', 'jane.smith@email.com', 'Jane Smith');
```

### 2. Scaffold Database vào Project

```bash
# Di chuyển vào thư mục Server (startup project)
cd Server

# Scaffold toàn bộ database
dotnet ef dbcontext scaffold "Server=MSI\SQLEXPRESS;Database=BlazorTodoDbFirst;User ID=thanhsang;Password=123456;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True" Microsoft.EntityFrameworkCore.SqlServer --project ../Data --startup-project . --output-dir ScaffoldedEntities --context-dir . --context ScaffoldedDbContext --force

# Hoặc scaffold với các tùy chọn chi tiết hơn
dotnet ef dbcontext scaffold "Server=MSI\SQLEXPRESS;Database=BlazorTodoDbFirst;User ID=thanhsang;Password=123456;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True" Microsoft.EntityFrameworkCore.SqlServer ^
  --project ../Data ^
  --startup-project . ^
  --output-dir ScaffoldedEntities ^
  --context-dir . ^
  --context ScaffoldedDbContext ^
  --namespace Data.ScaffoldedEntities ^
  --context-namespace Data ^
  --no-pluralize ^
  --use-database-names ^
  --force

# Scaffold chỉ một số tables cụ thể
dotnet ef dbcontext scaffold "connection-string" Microsoft.EntityFrameworkCore.SqlServer --project ../Data --startup-project . --output-dir ScaffoldedEntities --context ScaffoldedDbContext --tables TodoItems --tables Categories --force
```

### 3. Các tùy chọn Scaffold quan trọng

```bash
# Các tham số thường dùng:
--project               # Project chứa DbContext
--startup-project       # Startup project (có connection string)
--output-dir            # Thư mục chứa entity classes
--context-dir           # Thư mục chứa DbContext
--context               # Tên DbContext class
--namespace             # Namespace cho entities
--context-namespace     # Namespace cho DbContext
--no-pluralize         # Không pluralize tên class
--use-database-names   # Dùng tên gốc từ database
--force                # Overwrite files nếu đã tồn tại
--tables               # Chỉ scaffold tables cụ thể
--schemas              # Chỉ scaffold schemas cụ thể
--no-onconfiguring     # Không tạo OnConfiguring method
--data-annotations     # Dùng Data Annotations thay vì Fluent API
```

### 4. Cấu trúc sau khi Scaffold

Sau khi scaffold, bạn sẽ có:

```
Data/
├── ScaffoldedDbContext.cs          # DbContext được tạo tự động
├── ScaffoldedEntities/             # Thư mục chứa entities
│   ├── Category.cs
│   ├── TodoItem.cs
│   └── User.cs
└── AppDbContext.cs                 # DbContext thủ công (nếu có)
```

### 5. Ví dụ Entity được Scaffold

File `Data/ScaffoldedEntities/TodoItem.cs` sẽ được tạo tự động:

```csharp
using System;
using System.Collections.Generic;

namespace Data.ScaffoldedEntities;

public partial class TodoItem
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsDone { get; set; }

    public int? Priority { get; set; }

    public DateTime? DueDate { get; set; }

    public int? CategoryId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Category? Category { get; set; }
}
```

File `Data/ScaffoldedEntities/Category.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace Data.ScaffoldedEntities;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<TodoItem> TodoItems { get; set; } = new List<TodoItem>();
}
```

### 6. DbContext được Scaffold

File `Data/ScaffoldedDbContext.cs`:

```csharp
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Data.ScaffoldedEntities;

namespace Data;

public partial class ScaffoldedDbContext : DbContext
{
    public ScaffoldedDbContext()
    {
    }

    public ScaffoldedDbContext(DbContextOptions<ScaffoldedDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<TodoItem> TodoItems { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=MSI\\SQLEXPRESS;Database=BlazorTodoDbFirst;User ID=thanhsang;Password=123456;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC0771234567");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime2");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TodoItem__3214EC0712345678");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime2");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DueDate).HasColumnType("datetime2");
            entity.Property(e => e.IsDone).HasDefaultValue(false);
            entity.Property(e => e.Priority).HasDefaultValue(1);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime2");

            entity.HasOne(d => d.Category).WithMany(p => p.TodoItems)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__TodoItems__Categ__12345678");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC0787654321");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534ABCDEFGH").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E4IJKLMNOP").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime2");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
```
