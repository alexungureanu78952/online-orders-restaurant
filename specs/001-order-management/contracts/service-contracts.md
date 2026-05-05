# Service Contract: IAuthenticationService

**Purpose**: Define interface for user authentication and registration operations

## Interface Definition

```csharp
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate user with email and password
    /// </summary>
    /// <param name="email">User email (unique identifier)</param>
    /// <param name="password">Plaintext password (hashed internally)</param>
    /// <returns>Authenticated user object, or null if credentials invalid</returns>
    /// <exception cref="ArgumentException">Email or password empty</exception>
    /// <remarks>
    /// - Password hashing uses bcrypt
    /// - Returns User object with Role information
    /// - Does NOT return password hash to UI
    /// </remarks>
    Task<User> AuthenticateAsync(string email, string password);

    /// <summary>
    /// Register new user account as client
    /// </summary>
    /// <param name="firstName">User first name</param>
    /// <param name="lastName">User last name</param>
    /// <param name="email">Unique email address</param>
    /// <param name="phoneNumber">Optional phone number</param>
    /// <param name="deliveryAddress">Delivery address for orders</param>
    /// <param name="password">Plaintext password (will be hashed)</param>
    /// <returns>Newly created User object</returns>
    /// <exception cref="ArgumentException">Validation error (empty field, invalid email, password too weak)</exception>
    /// <exception cref="InvalidOperationException">Email already exists</exception>
    /// <remarks>
    /// - All parameters validated before storage
    /// - Password requirements: min 8 chars, uppercase, number
    /// - Email must be unique
    /// - Passwords bcrypt-hashed before storage
    /// - New users always created as 'Client' role
    /// </remarks>
    Task<User> RegisterAsync(string firstName, string lastName, string email, 
                              string phoneNumber, string deliveryAddress, string password);

    /// <summary>
    /// Verify user email/password combination without creating session
    /// </summary>
    Task<bool> VerifyCredentialsAsync(string email, string password);

    /// <summary>
    /// Update user's delivery address
    /// </summary>
    Task UpdateDeliveryAddressAsync(int userId, string newAddress);

    /// <summary>
    /// Get current user from session/token (application-specific)
    /// </summary>
    /// <returns>Currently authenticated user, or null if not authenticated</returns>
    Task<User> GetCurrentUserAsync();
}
```

## Operations

### AuthenticateAsync
- **Input**: email (string), password (string)
- **Output**: User object or null
- **Errors**: ArgumentException (empty fields)
- **Flow**:
  1. Validate email format
  2. Query User by email
  3. If not found, return null
  4. If found, verify password using bcrypt.Verify(password, user.PasswordHash)
  5. Update LastLoginDate
  6. Return User object (without password)

### RegisterAsync
- **Input**: firstName, lastName, email, phoneNumber, deliveryAddress, password
- **Output**: New User object
- **Errors**: ArgumentException (validation), InvalidOperationException (email exists)
- **Validations**:
  - All strings not null/empty
  - Email valid format and unique
  - Password: min 8 chars, contains uppercase, contains number
  - Names: max 100 chars each
- **Flow**:
  1. Validate all inputs
  2. Check email uniqueness
  3. Hash password using bcrypt.HashPassword(password)
  4. Create User record in database
  5. Return new User object

---

# Service Contract: IProductService

**Purpose**: Define interface for product, menu, and allergen operations

## Interface Definition

```csharp
public interface IProductService
{
    /// <summary>
    /// Get all categories
    /// </summary>
    Task<List<Category>> GetCategoriesAsync();

    /// <summary>
    /// Get products by category, ordered by name
    /// </summary>
    /// <param name="categoryId">Category identifier</param>
    /// <returns>List of products in category with images and allergens populated</returns>
    Task<List<Product>> GetProductsByCategoryAsync(int categoryId);

    /// <summary>
    /// Search products by keyword (case-insensitive) or allergen filter
    /// </summary>
    /// <param name="keyword">Search term (min 2 characters)</param>
    /// <param name="allergenName">Optional allergen to filter by</param>
    /// <param name="includeAllergen">True = products WITH allergen, False = products WITHOUT</param>
    /// <returns>List of matching products with images and allergens, grouped by category</returns>
    Task<List<Product>> SearchProductsAsync(string keyword, string allergenName = null, bool includeAllergen = true);

    /// <summary>
    /// Get product details including images and allergens
    /// </summary>
    Task<Product> GetProductDetailAsync(int productId);

    /// <summary>
    /// Create new product (employee only)
    /// </summary>
    Task<Product> CreateProductAsync(Product product, List<int> allergenIds);

    /// <summary>
    /// Update product details (employee only)
    /// </summary>
    Task UpdateProductAsync(Product product, List<int> allergenIds);

    /// <summary>
    /// Delete product (employee only, soft delete)
    /// </summary>
    Task DeleteProductAsync(int productId);

    /// <summary>
    /// Get all menus (bundles)
    /// </summary>
    Task<List<Menu>> GetMenusAsync();

    /// <summary>
    /// Get menu details including constituent products
    /// </summary>
    Task<Menu> GetMenuDetailAsync(int menuId);

    /// <summary>
    /// Get all allergens for UI dropdowns
    /// </summary>
    Task<List<Allergen>> GetAllergensAsync();
}
```

## Operations

### SearchProductsAsync
- **SQL**: Call sp_SearchProducts with parameters
- **Allergen Logic**:
  - If allergenName provided and includeAllergen = true: INNER JOIN ProductAllergen
  - If allergenName provided and includeAllergen = false: NOT IN (SELECT with allergen)
- **Grouping**: Group results by Category for UI display

---

# Service Contract: IOrderService

**Purpose**: Define interface for order management operations

## Interface Definition

```csharp
public interface IOrderService
{
    /// <summary>
    /// Create new order from cart
    /// </summary>
    /// <param name="userId">Customer placing order</param>
    /// <param name="items">List of product ID + quantity pairs</param>
    /// <param name="deliveryAddress">Delivery location (can override user default)</param>
    /// <returns>Newly created order with code and total</returns>
    /// <exception cref="InvalidOperationException">Insufficient inventory, validation error</exception>
    Task<Order> CreateOrderAsync(int userId, List<(int productId, int quantity)> items, 
                                   string deliveryAddress);

    /// <summary>
    /// Get all orders for user, sorted by date descending
    /// </summary>
    Task<List<Order>> GetUserOrdersAsync(int userId);

    /// <summary>
    /// Get all active orders (not delivered or cancelled)
    /// </summary>
    Task<List<Order>> GetUserActiveOrdersAsync(int userId);

    /// <summary>
    /// Get order details with line items
    /// </summary>
    Task<Order> GetOrderDetailAsync(int orderId);

    /// <summary>
    /// Update order status (employee only)
    /// </summary>
    /// <param name="orderId">Order to update</param>
    /// <param name="newStatus">New status value</param>
    /// <remarks>
    /// - Call sp_UpdateOrderStatus
    /// - If newStatus = 'livrata', automatically update inventory via sp_UpdateInventory
    /// - If newStatus = 'anulata', restore inventory
    /// </remarks>
    Task UpdateOrderStatusAsync(int orderId, string newStatus);

    /// <summary>
    /// Cancel active order (client operation)
    /// </summary>
    Task CancelOrderAsync(int orderId);

    /// <summary>
    /// Get all orders (employee view, sorted descending)
    /// </summary>
    Task<List<Order>> GetAllOrdersAsync();

    /// <summary>
    /// Get all active orders (employee view)
    /// </summary>
    Task<List<Order>> GetAllActiveOrdersAsync();
}
```

## Order Creation Flow

1. **Validation**:
   - User authenticated
   - Delivery address provided
   - All requested products available and in stock

2. **Inventory Check**:
   - Call sp_ValidateOrderInventory to verify sufficient stock

3. **Price Calculation**:
   - SubTotal = SUM(Product.Price * OrderItem.Quantity)
   - Call sp_ApplyOrderDiscounts to calculate:
     - ShippingFee (if SubTotal < threshold)
     - DiscountAmount (if order large or customer frequent)
   - TotalCost = SubTotal + ShippingFee - DiscountAmount

4. **Order Creation**:
   - Generate unique OrderCode (ORD-YYYYMMDD-NNNNN)
   - Create Order record with status = 'inregistrata'
   - Create OrderItem records
   - EstimatedDeliveryTime = NOW + 45 minutes (configurable)
   - Inventory NOT updated yet (only on delivery)

---

# Service Contract: IInventoryService

**Purpose**: Define interface for inventory operations

## Interface Definition

```csharp
public interface IInventoryService
{
    /// <summary>
    /// Get products approaching low stock threshold
    /// </summary>
    /// <returns>List of products with TotalQuantity <= configured threshold</returns>
    Task<List<Product>> GetLowStockProductsAsync();

    /// <summary>
    /// Update product total quantity (employee manual adjustment)
    /// </summary>
    Task UpdateProductQuantityAsync(int productId, int newQuantity);

    /// <summary>
    /// Verify sufficient inventory exists for all order items
    /// </summary>
    /// <returns>True if all items available, false otherwise</returns>
    Task<bool> VerifyInventoryAsync(List<(int productId, int quantity)> items);
}
```

---

# Service Contract: IConfigurationService

**Purpose**: Define interface for application configuration

## Interface Definition

```csharp
public interface IConfigurationService
{
    /// <summary>
    /// Load configuration from XML file and validate
    /// </summary>
    /// <exception cref="InvalidOperationException">Configuration file not found or invalid</exception>
    Task LoadConfigurationAsync();

    /// <summary>
    /// Get configuration parameter value
    /// </summary>
    T GetSetting<T>(string key);

    decimal MinOrderForFreeShipping { get; }
    decimal ShippingFee { get; }
    decimal LargeOrderDiscountThreshold { get; }
    decimal LargeOrderDiscountPercent { get; }
    int LowStockThreshold { get; }
    // etc. for all configuration values
}
```

---

# Database Operations - Stored Procedures

## Minimum 10 Required Stored Procedures

### Insert Operations (2+)
- `sp_CreateOrder` - Create order record with items
- `sp_InsertProduct` - Insert new product

### Update Operations (2+)
- `sp_UpdateOrderStatus` - Change order status, trigger inventory update
- `sp_UpdateInventory` - Reduce product quantities on delivery

### Select Operations (2+)
- `sp_GetProductsByCategory` - Retrieve category products
- `sp_SearchProducts` - Search with filters
- `sp_GetOrderDetails` - Full order information with items and customer
- `sp_GetUserOrders` - All orders for user

### Complex Operations (4+)
- `sp_GetLowStockProducts` - Products below threshold
- `sp_ApplyOrderDiscounts` - Calculate shipping and discounts
- `sp_ValidateOrderInventory` - Check stock availability
- `sp_GetAllOrders` - Employee order view
