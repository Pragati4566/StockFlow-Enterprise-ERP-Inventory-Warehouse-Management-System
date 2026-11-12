# ERP Stock Module

A **production‑quality Inventory Module** built with ASP.NET Core, Clean Architecture, Entity Framework Core, SignalR, and SQL Server.  
It solves real‑world warehouse challenges: batch‑level source tracking, concurrent sale conflicts, atomic multi‑warehouse transfers, real‑time low‑stock alerts, and high‑performance reporting.

---

## Table of Contents

1. [Business Problems & Solutions](#business-problems--solutions)  
   1.1 [Problem 1 – Tracking the Source of What They Sell](#problem-1--tracking-the-source-of-what-they-sell)  
   1.2 [Problem 2 – Concurrent Sales and Stock Accuracy](#problem-2--concurrent-sales-and-stock-accuracy)  
   1.3 [Problem 3 – Stock Transfers Between Warehouses](#problem-3--stock-transfers-between-warehouses)  
   1.4 [Problem 4 – Real‑Time Visibility](#problem-4--real‑time-visibility)  
   1.5 [Problem 5 – Reporting](#problem-5--reporting)  
2. [Architectural Decisions & Trade‑offs](#architectural-decisions--trade-offs)  
3. [Assumptions](#assumptions)  
4. [Database Schema & Indexes](#database-schema--indexes)  
5. [Running the Project Locally](#running-the-project-locally)  
6. [API Endpoints (Swagger)](#api-endpoints-swagger)  
7. [Testing](#testing)  
8. [Technologies Used](#technologies-used)  
9. [Repository Structure (Clean Architecture)](#repository-structure-clean-architecture)  
10. [Future Improvements](#future-improvements)

---

## Business Problems & Solutions

### Problem 1 – Tracking the Source of What They Sell
**Requirement**  
The client purchases the same product from different suppliers. They need to know exactly which purchase a sold quantity came from and which supplier provided it.

**Solution**  
Implemented **Lot‑based tracking with FIFO allocation**.  
- Every incoming shipment creates a `Lot` record (`ProductId`, `SupplierId`, `WarehouseId`, `RemainingQuantity`, `UnitCost`, `ReceivedDate`).  
- When a sale occurs, the system allocates stock from the oldest lot first (FIFO).  
- Each `StockTransaction` is linked to a specific `Lot`, so every sold item can be traced back to its original purchase.  
- Reports answer questions like *“How much of Product X was sold from Supplier Y?”* or *“Remaining stock from the March shipment?”*.

### Problem 2 – Concurrent Sales and Stock Accuracy
**Requirement**  
Multiple staff sell simultaneously. Negative stock occurred when two users sold the same available quantity at the same time.

**Solution**  
Three‑layer defence against over‑selling:  
1. **Atomic conditional update** – `UPDATE … WHERE RemainingQuantity >= @qty`. If the condition fails, zero rows are affected and the sale is rejected.  
2. **Pessimistic locking** – `SELECT … WITH (UPDLOCK, HOLDLOCK)` serialises access to the same lots within a transaction.  
3. **Database transaction** – every sale runs inside `BeginTransactionAsync` / `CommitAsync`. Any failure rolls back all changes.  

A concurrent integration test (see [Testing](#testing)) proves that only one of two simultaneous sales on the last unit succeeds, and stock never becomes negative.

### Problem 3 – Stock Transfers Between Warehouses
**Requirement**  
Frequent manual transfers caused mismatched quantities (source recorded “sent”, destination never recorded “received”).

**Solution**  
**Two‑phase transfer with atomic database transactions**:  
- **Phase 1 (Send)**: Creates a `TransferOrder` (status `Pending`) and deducts stock from the source warehouse.  
- **Phase 2 (Receive)**: On arrival, the destination confirms receipt. A new `Lot` is created in the destination warehouse.  
- Both phases run in separate transactions. If a network failure occurs mid‑way, the uncommitted transaction is rolled back automatically.  
- The system **rejects a received quantity that does not match the sent quantity**, keeping the transfer pending until the discrepancy is resolved. This guarantees consistent stock levels between warehouses.

### Problem 4 – Real‑Time Visibility
**Requirement**  
Managers want to see stock changes instantly and be notified when a product falls below a configurable threshold.

**Solution**  
- `StockThreshold` table stores a configurable minimum quantity per product/warehouse.  
- After every sale, transfer, or receipt, `NotificationService` checks the remaining quantity.  
- When it drops below the threshold, an alert is:  
  - **Pushed instantly** via SignalR (`ReceiveAlert`) to all connected manager clients.  
  - **Persisted** in the `Notifications` table (accessible via `GET /api/Notifications`).  
- A simple HTML page (provided in the repository) demonstrates real‑time alerts without any page refresh.

### Problem 5 – Reporting
**Requirement**  
Provide reporting endpoints that filter by warehouse, supplier, date range, and that perform well with large data.

**Solution**  
Two REST endpoints:  
- `GET /api/Reports/movements` – detailed, paginated list of stock movements.  
- `GET /api/Reports/summary` – aggregated total in/out per product.  
**Performance optimisations:**  
- `AsNoTracking()` for read‑only queries.  
- Projection to lightweight DTOs.  
- **Indexes** on `TransactionDate`, `LotId`, and composite `(ProductId, WarehouseId)`.  
- **Pagination** with `Skip/Take` and `TotalCount`.

---

## Architectural Decisions & Trade‑offs

| Decision | Reason | Trade‑off |
|----------|--------|-----------|
| **Clean Architecture** (Domain / Application / Infrastructure / API) | Clear separation of concerns, testability. | Adds initial project complexity. |
| **Generic Repository (`IRepository<T>`)** | Eliminates repetitive CRUD code. | May need specific repositories for complex queries. |
| **FIFO allocation** | Prevents stock aging, simplest physical flow. | Can be extended to LIFO or manual selection later. |
| **Pessimistic locking (`UPDLOCK, HOLDLOCK`)** | Absolute guarantee against double‑selling. | Slightly reduces concurrency under extreme load; lock scope is minimal (single product/warehouse). |
| **Two‑phase transfers with strict quantity match** | Strong consistency between warehouses. | Requires manual resolution when received quantity differs. |
| **SignalR** | Native ASP.NET Core real‑time solution. | Requires WebSocket‑compatible client. |
| **`int` primary keys** | Faster development, smaller indexes. | Less suitable for distributed systems. Architecture is ready to switch to `Guid` when needed. |

---

## Assumptions

- Default stock allocation is **FIFO**.  
- A sale can contain multiple products but is fulfilled from a **single warehouse**.  
- Stock transfers are **inter‑warehouse**; supplier information is preserved on the source lot.  
- The `SupplierId` of a transfer‑created lot is `null` because the quantity may come from multiple suppliers. Original suppliers can be traced via `StockTransaction` records.  
- `StockThreshold` is defined **per product per warehouse**.  
- Reporting is for inventory movements; financial reporting is out of scope.  
- Authentication/authorisation is not implemented – the API should be placed behind an identity provider in production.  
- The database key design (currently `int`) can be migrated to `Guid` when required; all entities inherit from a base `AuditableEntity` where the key type is centralised.

---

## Database Schema & Indexes

### Core tables
`Products`, `Suppliers`, `Warehouses`, `Lots`, `StockTransactions`, `TransferOrders`, `TransferItems`, `StockThresholds`, `Notifications`

All entities inherit from `AuditableEntity` which provides:  
- `Id`, `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy`, `IsDeleted` (soft delete).

### Indexes
I created indexes on columns that are frequently used in filters and joins to ensure reports stay fast even with millions of rows.
- Index on `TransactionDate` in `StockTransactions` for fast date‑range filtering.
- Index on `LotId` in `StockTransactions` to speed up JOINs with `Lots`.
- Composite index on `(ProductId, WarehouseId)` in `Lots` to quickly find available stock.
- Index on `SupplierId` in `Lots` for supplier‑based reports.
- Index on `TransferOrderId` in `TransferItems` to retrieve all items of a transfer.
- Composite index on `(ProductId, WarehouseId)` in `StockThresholds` for instant threshold lookup.

---

## Running the Project Locally

### Prerequisites
- .NET 8 SDK  
- SQL Server (LocalDB, Express, or Developer edition)

### Steps
1. Clone the repository.  
2. Open `ERP-Stock-Module/appsettings.json` and update the `Stock` connection string to point to your SQL Server instance.  
3. Open **Package Manager Console** in Visual Studio (Tools → NuGet Package Manager → Package Manager Console).  
4. Run the following commands to create the database and apply migrations:
   ```
   Add-Migration InitialCreate -StartupProject ERP-Stock-Module -Project Infrastructure
   Update-Database -StartupProject ERP-Stock-Module -Project Infrastructure
   ```
5. Run the application by pressing **F5** or using:
   ```
   dotnet run --project ERP-Stock-Module
   ```
6. Open `https://localhost:7213/swagger` (or the URL printed in the console).  
   The database is automatically seeded with sample data (`DbInitializer`).

---

## API Endpoints (Swagger)

Access Swagger at `/swagger` after starting the project.

The API groups are:
- **Admin** – CRUD for products, suppliers, warehouses, and stock thresholds.
- **Sales** – `POST /api/Sales` to execute a sale.
- **Transfers** – `POST /api/Transfers` to send a transfer; `PUT …/items/{id}/receive` to confirm receipt.
- **Reports** – `GET /api/Reports/movements` (paginated), `GET /api/Reports/summary`.
- **Notifications** – `GET /api/Notifications` to view stored alerts.
- **Stock** – `GET /api/Stock/levels` for current quantities; `POST /api/Stock/receive` to record incoming stock.

---

## Testing

- **Concurrent sale test**: Located in `Tests/ConcurrentSaleTests.cs`. It simulates two simultaneous allocations on a lot with quantity 1 and verifies that only one succeeds, the other fails with “insufficient stock”, and the remaining quantity stays at 0.  
- Run tests from the **Test Explorer** or using:
  ```
  dotnet test
  ```

---

## Technologies Used

- ASP.NET Core 8 (Web API)  
- Entity Framework Core 8 with SQL Server  
- SignalR for real‑time notifications  
- Serilog for structured logging  
- xUnit for tests  
- Swashbuckle (Swagger) for API documentation

---

## Repository Structure (Clean Architecture)

```
ERP-Stock-Module
├── Domain          – Entities, enums
├── Application     – DTOs, IService (interfaces)
├── Infrastructure  – DbContext, Service (implementations), SignalR hub, Migrations
├── ERP-Stock-Module – Web API (Controllers, Program.cs)
└── Tests           – xUnit test project
```

---

## Future Improvements

- **Guid keys**: Migrate primary keys from `int` to `Guid` for better distribution and security.  
- **Docker support**: Add `Dockerfile` and `docker-compose.yml` for containerised deployment.    

*Developed as part of a technical assessment for an ERP Inventory Module.*
