# ChecklistDropdown Component

Component Bootstrap Dropdown v?i checkbox/radio h? tr? ch?n ðõn ho?c ch?n nhi?u, t?m ki?m, và nhi?u tính nãng ti?n ích khác.

## Tính nãng

? **Ch?n ðõn ho?c ch?n nhi?u** - H? tr? c? radio button (ch?n 1) và checkbox (ch?n nhi?u)  
? **T?m ki?m** - T?m ki?m nhanh trong danh sách  
? **Ch?n t?t c?** - Nút ch?n/b? ch?n t?t c?  
? **Custom separator** - Tùy ch?nh k? t? phân cách khi hi?n th?  
? **Max display items** - Gi?i h?n s? items hi?n th?, sau ðó show "X m?c ð? ch?n"  
? **Scrollable** - T? ð?ng hi?n th? scroll khi danh sách dài  
? **Custom template** - H? tr? template tùy ch?nh cho m?i item  
? **Generic type support** - Làm vi?c v?i b?t k? ki?u d? li?u nào  
? **Clear button** - Nút xóa l?a ch?n  
? **Disabled state** - Vô hi?u hóa component  

## Cài ð?t

Component s? d?ng Bootstrap 5, ð?m b?o ð? thêm Bootstrap vào project:

```html
<!-- index.html -->
<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
```

## S? d?ng cõ b?n

### 1. Multi-Select v?i danh sách string ðõn gi?n

```razor
<ChecklistDropdown TItem="string"
                   Items="@Countries"
                   @bind-SelectedValues="@SelectedCountries"
                   PlaceholderText="Ch?n qu?c gia..." />

@code {
    private List<string> Countries = new() { "Vi?t Nam", "Hoa K?", "Nh?t B?n" };
    private List<string> SelectedCountries = new();
}
```

### 2. Single-Select (Radio)

```razor
<ChecklistDropdown TItem="string"
                   Items="@Colors"
                   @bind-SelectedValues="@SelectedColors"
                   IsSingleSelect="true"
                   PlaceholderText="Ch?n màu..." />
```

### 3. V?i t?m ki?m và ch?n t?t c?

```razor
<ChecklistDropdown TItem="string"
                   Items="@Countries"
                   @bind-SelectedValues="@SelectedCountries"
                   ShowSearch="true"
                   AllowSelectAll="true"
                   SearchPlaceholder="T?m ki?m..." />
```

### 4. Custom object v?i ValueSelector và TextSelector

```razor
<ChecklistDropdown TItem="Person"
                   Items="@People"
                   @bind-SelectedValues="@SelectedPeopleIds"
                   ValueSelector="@(p => p.Id.ToString())"
                   TextSelector="@(p => p.Name)"
                   ShowSearch="true" />

@code {
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    
    private List<Person> People = new()
    {
        new Person { Id = 1, Name = "Nguy?n Vãn A" },
        new Person { Id = 2, Name = "Tr?n Th? B" }
    };
    private List<string> SelectedPeopleIds = new();
}
```

### 5. Custom Template cho items

```razor
<ChecklistDropdown TItem="Person"
                   Items="@People"
                   @bind-SelectedValues="@SelectedPeopleIds"
                   ValueSelector="@(p => p.Id.ToString())"
                   TextSelector="@(p => p.Name)">
    <ItemTemplate>
        <div class="d-flex align-items-center">
            <span class="badge bg-primary me-2">@context.Age</span>
            <div>
                <strong>@context.Name</strong><br />
                <small class="text-muted">@context.Email</small>
            </div>
        </div>
    </ItemTemplate>
</ChecklistDropdown>
```

### 6. Custom separator và max display

```razor
<ChecklistDropdown TItem="string"
                   Items="@Technologies"
                   @bind-SelectedValues="@SelectedTechs"
                   Separator=" | "
                   MaxDisplayItems="2"
                   SelectedItemsText="{0} công ngh?" />
```

## Parameters

| Parameter | Type | Default | Mô t? |
|-----------|------|---------|-------|
| `Items` | `IEnumerable<TItem>` | `[]` | Danh sách các items |
| `SelectedValues` | `List<string>` | `[]` | Các giá tr? ð? ch?n (two-way binding) |
| `ValueSelector` | `Func<TItem, string>` | `item.ToString()` | Hàm l?y value t? item |
| `TextSelector` | `Func<TItem, string>` | `item.ToString()` | Hàm l?y text hi?n th? |
| `ItemTemplate` | `RenderFragment<TItem>` | `null` | Template tùy ch?nh cho item |
| `IsSingleSelect` | `bool` | `false` | Ch?n ðõn (radio) hay nhi?u (checkbox) |
| `Separator` | `string` | `", "` | K? t? phân cách |
| `PlaceholderText` | `string` | `"Ch?n..."` | Text khi chýa ch?n |
| `SelectedItemsText` | `string` | `"{0} m?c ð? ch?n"` | Format text khi ch?n nhi?u |
| `MaxDisplayItems` | `int` | `3` | S? items t?i ða hi?n th? |
| `ShowSearch` | `bool` | `false` | Hi?n th? ô t?m ki?m |
| `SearchPlaceholder` | `string` | `"T?m ki?m..."` | Placeholder cho search |
| `AllowSelectAll` | `bool` | `false` | Cho phép ch?n t?t c? |
| `SelectAllText` | `string` | `"Ch?n t?t c?"` | Text cho "Ch?n t?t c?" |
| `ShowClearButton` | `bool` | `true` | Hi?n th? nút xóa |
| `ClearButtonText` | `string` | `"Xóa l?a ch?n"` | Text cho nút xóa |
| `NoResultsText` | `string` | `"Không t?m th?y k?t qu?"` | Text khi không có k?t qu? |
| `MaxHeight` | `int` | `300` | Chi?u cao t?i ða (px) |
| `CssClass` | `string` | `""` | CSS class tùy ch?nh |
| `IsDisabled` | `bool` | `false` | Vô hi?u hóa component |

## Events

| Event | Type | Mô t? |
|-------|------|-------|
| `SelectedValuesChanged` | `EventCallback<List<string>>` | Khi selection thay ð?i |
| `OnDropdownOpened` | `EventCallback` | Khi dropdown m? |
| `OnDropdownClosed` | `EventCallback` | Khi dropdown ðóng |

## Ví d? nâng cao

### Dynamic items t? API

```razor
<ChecklistDropdown TItem="Product"
                   Items="@products"
                   @bind-SelectedValues="@selectedProductIds"
                   ValueSelector="@(p => p.Id.ToString())"
                   TextSelector="@(p => p.Name)"
                   ShowSearch="true"
                   AllowSelectAll="true" />

@code {
    private List<Product> products = new();
    private List<string> selectedProductIds = new();
    
    protected override async Task OnInitializedAsync()
    {
        products = await Http.GetFromJsonAsync<List<Product>>("api/products");
    }
}
```

### Validation v?i EditForm

```razor
<EditForm Model="@model" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    
    <div class="mb-3">
        <label>Ch?n danh m?c:</label>
        <ChecklistDropdown TItem="string"
                           Items="@categories"
                           @bind-SelectedValues="@model.SelectedCategories"
                           ShowSearch="true" />
        <ValidationMessage For="@(() => model.SelectedCategories)" />
    </div>
    
    <button type="submit" class="btn btn-primary">Submit</button>
</EditForm>
```

### Theo d?i thay ð?i

```razor
<ChecklistDropdown TItem="string"
                   Items="@Items"
                   SelectedValues="@selectedValues"
                   SelectedValuesChanged="@OnSelectionChanged" />

@code {
    private async Task OnSelectionChanged(List<string> newValues)
    {
        selectedValues = newValues;
        // Custom logic here
        await LoadRelatedData(newValues);
    }
}
```

## Styling

Component ði kèm v?i CSS m?c ð?nh, nhýng b?n có th? override:

```css
.checklist-dropdown .dropdown-toggle {
    background-color: #f8f9fa;
    border-color: #ced4da;
}

.checklist-dropdown .form-check:hover {
    background-color: #e9ecef;
}
```

## Browser Support

- Chrome/Edge: ?
- Firefox: ?
- Safari: ?
- Opera: ?

## Demo

Xem demo ð?y ð? t?i: `/checklist-demo`

## License

MIT License
