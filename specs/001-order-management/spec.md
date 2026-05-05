# Feature Specification: Restaurant Order Management System

**Feature Branch**: `001-order-management`  
**Created**: May 5, 2026  
**Status**: Draft  
**Input**: Desktop application for restaurant order management with online ordering capabilities

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse Restaurant Menu (Priority: P1)

Unauthenticated users can explore the restaurant's complete menu organized by categories, viewing detailed product information including prices, portion sizes, images, and allergen warnings.

**Why this priority**: Core feature that provides value to all user types immediately. Forms the foundation for searching and ordering.

**Independent Test**: Can be tested by launching the application and verifying menu displays correctly grouped by categories with all product details visible.

**Acceptance Scenarios**:

1. **Given** application is launched, **When** user navigates to menu section, **Then** all product categories are displayed
2. **Given** categories are displayed, **When** user selects a category, **Then** products in that category appear with name, price, portion size, images, and allergen list
3. **Given** menu is displayed, **When** product stock is depleted, **Then** product shows "indisponibil" status and cannot be ordered
4. **Given** menu is displayed, **When** product belongs to a menu (bundle), **Then** both product and parent menu show unavailable status if stock is insufficient

---

### User Story 2 - Search and Filter Menu (Priority: P1)

Users can search products by name/keywords and filter by allergen presence or absence to find suitable dishes.

**Why this priority**: Core discovery feature for all users. Critical for users with dietary restrictions. Enables independent testing.

**Independent Test**: Can be tested by performing various searches (by keyword, allergen inclusion/exclusion) and verifying result accuracy and presentation.

**Acceptance Scenarios**:

1. **Given** user is viewing the menu, **When** user enters search keyword, **Then** products matching the keyword in name or description appear
2. **Given** search results are displayed, **When** results contain products from multiple categories, **Then** each category header appears only once
3. **Given** user selects allergen filter, **When** user searches "does not contain gluten", **Then** only gluten-free products appear
4. **Given** user selects allergen filter, **When** user searches "contains chicken", **Then** only products containing chicken appear
5. **Given** search has no results, **When** user performs search, **Then** "no results" message is displayed

---

### User Story 3 - Create User Account (Priority: P1)

Unauthenticated users can register as clients by providing required information and creating login credentials.

**Why this priority**: Prerequisite for ordering capability. Enables client-specific features. Independently testable.

**Independent Test**: Can be tested by creating a new account with valid information and verifying login works.

**Acceptance Scenarios**:

1. **Given** user is on registration page, **When** user fills in all required fields (name, surname, email, phone, delivery address, password), **Then** account is created
2. **Given** user enters email, **When** email already exists in system, **Then** error message appears
3. **Given** user enters password, **When** password requirements are not met, **Then** error message shows requirements
4. **Given** account is created, **When** user attempts to log in with email and password, **Then** login succeeds and user sees authenticated client interface

---

### User Story 4 - Place Order (Priority: P1)

Authenticated clients can add multiple products to an order, specify quantities, and submit orders for delivery with automatic inventory updates.

**Why this priority**: Core revenue-generating feature. Essential business capability. Independently testable.

**Independent Test**: Can be tested by authenticating as client, building an order with multiple items, and verifying order appears in system with correct details and inventory is updated.

**Acceptance Scenarios**:

1. **Given** authenticated client is viewing menu, **When** client clicks "add to order" on a product, **Then** product is added to cart with quantity 1
2. **Given** product is in cart, **When** client increases quantity, **Then** cart quantity updates
3. **Given** multiple products are in cart, **When** client reviews order, **Then** order shows all items with quantities, individual prices, and calculated total
4. **Given** cart total exceeds configured minimum for free shipping, **When** order is submitted, **Then** order is created with status "inregistrata" and no shipping fee applied
5. **Given** cart total is below minimum for free shipping, **When** order is submitted, **Then** shipping fee is added to order total
6. **Given** order is submitted, **When** order is confirmed, **Then** inventory quantities are automatically reduced by ordered amounts
7. **Given** multiple orders exist within time period (configurable), **When** recent order count exceeds configured threshold, **Then** discount is automatically applied to new order
8. **Given** order total exceeds configured amount, **When** order is submitted, **Then** configured discount percentage is applied

---

### User Story 5 - Track Order Status (Priority: P2)

Authenticated clients can view all their orders with complete details, monitor active order statuses, and receive estimated delivery times.

**Why this priority**: Provides transparency and reduces support inquiries. Independently testable with orders in system.

**Independent Test**: Can be tested by placing orders and verifying order history displays with all required details and status tracking works.

**Acceptance Scenarios**:

1. **Given** authenticated client has placed orders, **When** client views order history, **Then** all orders display with date, code, products, total cost, status, and estimated delivery time
2. **Given** order is active, **When** client views active orders section, **Then** only non-delivered, non-cancelled orders appear
3. **Given** order status is updated by employee, **When** client refreshes active orders, **Then** new status is displayed
4. **Given** order is active, **When** client clicks cancel button, **Then** order status changes to "anulata" and inventory is restored

---

### User Story 6 - Manage Orders (Priority: P2)

Employees can view all orders, update order statuses, and access customer information for order fulfillment and delivery coordination.

**Why this priority**: Operational necessity for restaurant staff. Independently testable with orders in system.

**Independent Test**: Can be tested by authenticating as employee, viewing orders, changing statuses, and verifying changes are reflected in client view.

**Acceptance Scenarios**:

1. **Given** authenticated employee is logged in, **When** employee accesses orders section, **Then** all orders appear sorted by date/time descending
2. **Given** order list is displayed, **When** employee filters for active orders, **Then** only "inregistrata", "se pregateste", and "a plecat la client" statuses appear
3. **Given** order is displayed, **When** employee views order details, **Then** customer name, phone, delivery address, order items with quantities, costs, and current status are shown
4. **Given** order is active, **When** employee changes order status, **Then** status updates and client can see change

---

### User Story 7 - Manage Inventory (Priority: P2)

Employees can view products approaching stock depletion and monitor available inventory quantities.

**Why this priority**: Prevents overselling and enables inventory planning. Independently testable.

**Independent Test**: Can be tested by checking low stock items and verifying items are correctly identified based on configured threshold.

**Acceptance Scenarios**:

1. **Given** authenticated employee is logged in, **When** employee accesses low stock section, **Then** products with inventory below configured threshold appear with product name and current quantity
2. **Given** low stock list is displayed, **When** product stock is replenished above threshold, **Then** product no longer appears in low stock list

---

### User Story 8 - Manage Menu Items (Priority: P3)

Employees can create, update, and delete product categories and products to keep menu current.

**Why this priority**: Enables menu maintenance. Provides flexibility for restaurant operations. Independently testable.

**Independent Test**: Can be tested by adding new product/category and verifying it appears in menu; modifying and verifying changes; deleting and verifying removal.

**Acceptance Scenarios**:

1. **Given** authenticated employee has management access, **When** employee creates new category, **Then** category is saved and appears in menu
2. **Given** category exists, **When** employee creates new product with category, name, price, portion size, images, and allergens, **Then** product is saved and visible in menu
3. **Given** product exists, **When** employee updates product details, **Then** changes are reflected in menu
4. **Given** product exists, **When** employee deletes product, **Then** product is removed from menu
5. **Given** menu bundle exists, **When** constituent product is deleted or restocked, **Then** bundle availability reflects constituent product availability

---

### User Story 9 - Generate Reports (Priority: P3)

Employees can generate reports on orders, revenue, and inventory for business analysis.

**Why this priority**: Supports business intelligence and decision-making. Not required for immediate MVP but adds operational value.

**Independent Test**: Can be tested by generating reports and verifying data accuracy and format.

**Acceptance Scenarios**:

1. **Given** authenticated employee has access to reports, **When** employee generates order report, **Then** report shows orders for selected period with details and totals
2. **Given** report is generated, **When** employee exports report, **Then** data is available in standard format (CSV/PDF)

---

### Edge Cases

- What happens when a customer tries to order a product that becomes unavailable between viewing and checkout?
- How does the system handle inventory updates when multiple orders for the same product are placed simultaneously?
- What happens if a delivery order is cancelled after employee has begun preparation?
- How does the system handle custom discounts that conflict with automatic discount rules?
- What happens if configured discount thresholds or fees are changed mid-transaction?
- How should the system behave if a product image fails to load?
- What happens if a menu bundle contains only one product from configured categories?

## Requirements *(mandatory)*

### Functional Requirements

#### Menu & Product Management

- **FR-001**: System MUST display all products organized by categories with name, price, portion size, images, and allergen list
- **FR-002**: System MUST mark products as "indisponibil" when stock quantity reaches zero, preventing orders
- **FR-003**: System MUST support menu bundles (multiple products at discounted price) with composition display showing individual product quantities
- **FR-004**: System MUST calculate menu bundle prices based on constituent product prices minus configured discount percentage
- **FR-005**: System MUST associate each product and menu bundle with exactly one category
- **FR-006**: System MUST support multiple allergen associations per product (0 to many)
- **FR-007**: System MUST support multiple images per product with gallery display

#### Search & Discovery

- **FR-008**: System MUST enable keyword search on product/menu names returning matching items
- **FR-009**: System MUST enable allergen-based filtering (products containing or not containing specific allergens)
- **FR-010**: System MUST display search results grouped by category, showing category header only once
- **FR-011**: System MUST support case-insensitive search

#### User Authentication & Authorization

- **FR-012**: System MUST require email and password for client login
- **FR-013**: System MUST enforce unique email addresses for user accounts
- **FR-014**: System MUST support three user roles: unauthenticated, client, employee
- **FR-015**: System MUST restrict ordering capability to authenticated clients only
- **FR-016**: System MUST restrict management and reporting functions to authenticated employees only

#### Order Management

- **FR-017**: System MUST allow clients to add multiple products with configurable quantities to a single order
- **FR-018**: System MUST allow adding multiple quantities of the same product in single order
- **FR-019**: System MUST display order summary with product list, quantities, unit prices, and total cost before submission
- **FR-020**: System MUST assign unique order code to each order upon creation
- **FR-021**: System MUST set new orders to "inregistrata" status upon creation
- **FR-022**: System MUST support order status transitions: inregistrata → se pregateste → a plecat la client → livrata (or anulata at any stage)
- **FR-023**: System MUST prevent status changes to cancelled or delivered orders
- **FR-024**: System MUST automatically update product inventory upon order delivery
- **FR-025**: System MUST restore inventory if order is cancelled
- **FR-026**: System MUST display estimated delivery time with order status
- **FR-027**: System MUST allow clients to view complete order history with date, code, items, costs, status, and delivery time
- **FR-028**: System MUST allow clients to view and monitor active orders only
- **FR-029**: System MUST allow clients to cancel active orders

#### Pricing & Discounts

- **FR-030**: System MUST calculate order subtotal from item prices and quantities
- **FR-031**: System MUST apply shipping fee (configurable amount) when order total is below configured minimum
- **FR-032**: System MUST apply automatic discount when order exceeds configured amount (discount % configurable)
- **FR-033**: System MUST apply automatic discount when client has exceeded configured number of orders within configured time period (discount % configurable)
- **FR-034**: System MUST not apply shipping fee when automatic discount conditions are met
- **FR-035**: System MUST display final order total including all fees and discounts

#### Inventory Management

- **FR-036**: System MUST track portion quantity (displayed quantity per serving) for each product
- **FR-037**: System MUST track total restaurant inventory quantity separately from portion quantity
- **FR-038**: System MUST display to employees portion quantity and current total inventory
- **FR-039**: System MUST identify and display products with inventory below configured threshold
- **FR-040**: System MUST display low-stock products with name and current quantity

#### Employee Functions

- **FR-041**: System MUST allow employees to create, update, delete products (minimum 2 entity types must support full CRUD)
- **FR-042**: System MUST allow employees to create, update, delete categories (or other entity types)
- **FR-043**: System MUST allow employees to view all orders sorted by date/time descending
- **FR-044**: System MUST allow employees to filter orders by active status only
- **FR-045**: System MUST display customer information with orders (name, surname, phone, delivery address)
- **FR-046**: System MUST allow employees to update order status
- **FR-047**: System MUST allow employees to access reporting functionality

#### Data & Configuration

- **FR-048**: System MUST read configuration values from external configuration file (discount percentages, fees, thresholds, time periods)
- **FR-049**: System MUST validate configuration file format on application startup
- **FR-050**: System MUST store all data in SQL Server database in 3NF (third normal form)

### Key Entities

- **Category**: Entity representing dish categories (breakfast, appetizers, soups, desserts, beverages, etc.). Attributes: id, name, description
- **Product**: Entity representing individual dishes with properties: id, name, price, portion_quantity (quantity per serving, e.g., 300g), total_quantity (current restaurant inventory), category_id (foreign key), image_urls (collection), allergens (many-to-many collection)
- **Menu**: Entity representing bundled products (platters, combos). Attributes: id, name, products (collection), category_id (foreign key), base_discount_percent, image_urls (collection)
- **Allergen**: Entity representing allergen types (gluten, eggs, celery, lactose, etc.). Attributes: id, name
- **ProductAllergen**: Junction table for many-to-many relationship between Product and Allergen
- **User**: Entity representing system users. Attributes: id, name, surname, email (unique), phone, delivery_address, password_hash, role (unauthenticated/client/employee)
- **Order**: Entity representing customer orders. Attributes: id, order_code (unique), user_id (foreign key), order_date, status, estimated_delivery_time, subtotal, shipping_fee, discount_amount, total_cost
- **OrderItem**: Entity representing individual items in an order. Attributes: id, order_id (foreign key), product_id (foreign key), quantity, unit_price
- **Configuration**: Entity or file storing application settings: min_order_for_free_shipping, shipping_fee, discount_percent, order_threshold_count, order_threshold_time_period, low_stock_threshold, menu_discount_percent

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Clients can browse complete menu and place order from initial launch in under 5 minutes
- **SC-002**: Product search returns accurate results matching criteria within 1 second
- **SC-003**: Order status appears updated for clients within 10 seconds of employee update
- **SC-004**: Inventory correctly reflects all orders: if order is delivered or cancelled, quantities update accurately 100% of the time
- **SC-005**: System correctly applies all configured discounts and fees per business rules with zero calculation errors
- **SC-006**: Application maintains 3NF database normalization with all functional dependencies properly enforced
- **SC-007**: Application includes minimum 10 stored procedures: 2+ inserts, 2+ updates, 2+ selects with Entity Framework integration
- **SC-008**: MVVM architecture separates presentation from business logic with data binding for all user inputs
- **SC-009**: No user interface displays table ID values; all identifiers are hidden from user view
- **SC-010**: All parameterized queries prevent SQL injection vulnerabilities

## Assumptions

- **Target Users**: Restaurant staff and local customers with basic computer literacy; assumes access to Windows desktop environment
- **Technical Environment**: .NET Framework/Core with SQL Server available; WPF compatible with Windows 10+
- **Data Volume**: Application designed for single restaurant location with 100-500 active customers and 50-200 concurrent orders
- **Configuration Management**: All business parameters (discounts, fees, thresholds) managed via external configuration file readable by application startup
- **Image Storage**: Product images stored as file paths or URLs; actual image files managed separately outside database
- **Allergen Data**: Allergen list is static/semi-static (changes infrequently) and can be pre-populated in database or configuration
- **Delivery Time Estimation**: Estimated delivery times calculated based on simple rules (e.g., current time + fixed minutes); advanced routing not required
- **Concurrent Orders**: System designed for sequential order processing without real-time concurrent order synchronization requirements
- **Authentication Scope**: Simple email/password authentication only; no third-party OAuth or SSO integration required
- **Reporting Scope**: Basic reporting (order summaries, revenue) only; advanced analytics/BI tools not required
- **Mobile/Web Access**: Initial implementation desktop-only; web and mobile access out of scope for v1
- **Internationalization**: Application may be built in Romanian or English based on team preference; internationalization not required
- **Payment Integration**: Online payment processing not required; assumes cash/card on delivery or separate payment system
