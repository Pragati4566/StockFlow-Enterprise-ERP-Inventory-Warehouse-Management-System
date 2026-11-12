# StockFlow

## Enterprise ERP Inventory & Warehouse Management System

StockFlow is an inventory and warehouse management backend built with **ASP.NET Core 8**, **Clean Architecture**, **Entity Framework Core**, **SignalR**, and **SQL Server**.

The system focuses on practical inventory-management challenges, including supplier-level stock traceability, FIFO allocation, concurrent sales, inter-warehouse transfers, real-time low-stock alerts, and inventory reporting.

---

## Table of Contents

- [Overview](#overview)
- [Core Features](#core-features)
- [Business Workflows](#business-workflows)
  - [Lot-Based Inventory Tracking and FIFO](#1-lot-based-inventory-tracking-and-fifo)
  - [Concurrency-Safe Sales](#2-concurrency-safe-sales)
  - [Inter-Warehouse Transfers](#3-inter-warehouse-transfers)
  - [Real-Time Low-Stock Alerts](#4-real-time-low-stock-alerts)
  - [Inventory Reporting](#5-inventory-reporting)
- [Architecture and Design Decisions](#architecture-and-design-decisions)
- [Database Design and Indexes](#database-design-and-indexes)
- [Technology Stack](#technology-stack)
- [Getting Started](#getting-started)
- [API Overview](#api-overview)
- [Testing](#testing)
- [Project Structure](#project-structure)
- [Current Scope and Assumptions](#current-scope-and-assumptions)
- [Potential Enhancements](#potential-enhancements)

---

## Overview

StockFlow is designed to help manage inventory across multiple warehouses while maintaining accurate stock levels and a clear history of inventory movements.

Its main capabilities include:

- Tracking received inventory by product, supplier, warehouse, and lot.
- Allocating stock using the **First-In, First-Out (FIFO)** method.
- Preventing overselling when multiple sales attempt to use the same available stock.
- Recording stock transfers between warehouses.
- Sending real-time alerts when stock falls below a configured threshold.
- Providing filtered and paginated inventory movement reports.

## Core Features

| Feature | Description |
|---|---|
| Product and supplier management | Maintain product, supplier, and warehouse records. |
| Lot-based tracking | Trace stock back to the lot and supplier from which it was received. |
| FIFO allocation | Allocate older inventory lots before newer lots. |
| Concurrency-safe sales | Protect stock quantities during simultaneous sales. |
| Warehouse transfers | Track stock sent from one warehouse and received at another. |
| Low-stock notifications | Detect threshold breaches and publish alerts through SignalR. |
| Inventory reports | Retrieve stock movements and product-level in/out summaries. |
| API documentation | Explore and test endpoints through Swagger. |

---

## Business Workflows

### 1. Lot-Based Inventory Tracking and FIFO

**Problem:** The same product may be purchased from different suppliers at different times. Inventory records should make it possible to identify the source of the stock that was sold.

**Implementation:**

- Each incoming shipment creates a `Lot` record containing details such as `ProductId`, `SupplierId`, `WarehouseId`, `RemainingQuantity`, `UnitCost`, and `ReceivedDate`.
- When a sale is processed, the system allocates available stock from the oldest lot first using FIFO.
- Each `StockTransaction` is associated with the relevant lot.
- This transaction history supports supplier-level and shipment-level stock traceability.

Examples of questions supported by this model include:

- How much of a product was sold from a particular supplier?
- How much stock remains from a specific shipment?

### 2. Concurrency-Safe Sales

**Problem:** When multiple users sell the same product at the same time, each request may try to consume the same available quantity. Without proper concurrency controls, this can lead to negative or inaccurate stock.

**Implementation:** Stock allocation uses multiple safeguards:

1. **Conditional updates:** An update checks that `RemainingQuantity >= @qty` before deducting stock. If the condition is not met, the update affects no rows and the sale cannot consume unavailable stock.
2. **Pessimistic locking:** SQL Server locking hints such as `UPDLOCK` and `HOLDLOCK` help serialize access to the relevant lots during a transaction.
3. **Database transactions:** Sale operations run inside a database transaction. Changes are committed only when the operation succeeds; failures are rolled back.

A concurrent-sale integration test checks that when two requests attempt to sell the final available unit, only one succeeds and the stock quantity does not become negative.

### 3. Inter-Warehouse Transfers

**Problem:** Manual stock transfers can leave inventory records inconsistent when stock is marked as sent at the source warehouse but is not properly recorded at the destination.

**Implementation:** Stock transfers follow a two-step workflow:

1. **Send:** A `TransferOrder` is created with a `Pending` status, and the quantity is deducted from the source warehouse.
2. **Receive:** When the transfer arrives, the destination warehouse confirms receipt. The received quantity is recorded as a new lot at the destination.
3. **Transaction handling:** The send and receive operations each use database transactions so that changes within an individual operation are committed or rolled back together.
4. **Quantity validation:** A received quantity that does not match the sent quantity is rejected. The transfer remains pending until the discrepancy is resolved.

The source lot and its supplier history remain traceable through the stock transaction records. For a transfer-created lot, `SupplierId` is `null` because the transferred quantity may include stock from multiple suppliers.

### 4. Real-Time Low-Stock Alerts

**Problem:** Warehouse teams need timely visibility when inventory falls below a minimum level.

**Implementation:**

- The `StockThreshold` table stores a configurable minimum quantity for each product and warehouse.
- After a sale, transfer, or stock receipt, the notification service checks the remaining quantity against the configured threshold.
- When stock falls below the threshold, an alert is pushed to connected clients through **SignalR** using the `ReceiveAlert` event.
- Alerts are also persisted in the `Notifications` table and can be retrieved through `GET /api/Notifications`.
- The project includes a simple HTML page to demonstrate real-time notifications without refreshing the page.

### 5. Inventory Reporting

The reporting functionality provides endpoints for retrieving inventory movements and product-level stock summaries.

**Available reports:**

- `GET /api/Reports/movements` — returns a paginated list of stock movements.
- `GET /api/Reports/summary` — returns aggregated incoming and outgoing quantities per product.

**Query and performance considerations:**

- Read-only queries use `AsNoTracking()`.
- DTO projections limit the data returned by reporting queries.
- Pagination uses `Skip` and `Take`, along with a total record count.
- Database indexes support common date filters, joins, and product/warehouse lookups.

---

## Architecture and Design Decisions

StockFlow follows Clean Architecture to separate business rules, application logic, infrastructure concerns, and API delivery.

| Design decision | Why it is used | Trade-off |
|---|---|---|
| Clean Architecture | Separates Domain, Application, Infrastructure, and API responsibilities and supports testability. | Introduces more initial structure and project setup. |
| Generic repository (`IRepository<T>`) | Reduces repetitive CRUD implementation. | Complex queries may need dedicated repository methods. |
| FIFO allocation | Prioritizes older inventory lots and provides a straightforward stock-allocation flow. | Other allocation strategies would require additional logic. |
| SQL Server locking (`UPDLOCK`, `HOLDLOCK`) | Helps prevent concurrent requests from allocating the same stock. | Locking can reduce concurrency under heavy contention. |
| Conditional stock updates | Ensures a quantity is available before it is deducted. | Requires callers to handle failed updates as insufficient stock. |
| Two-step warehouse transfers | Separates dispatch from destination receipt and records the transfer lifecycle. | Quantity discrepancies require resolution before completion. |
| SignalR | Provides real-time notifications using ASP.NET Core infrastructure. | Clients need a compatible real-time connection. |
| Integer primary keys | Keeps the current data model straightforward. | A distributed deployment may benefit from a different key strategy. |

---

## Database Design and Indexes

### Core tables

The database includes the following main tables:

- `Products`
- `Suppliers`
- `Warehouses`
- `Lots`
- `StockTransactions`
- `TransferOrders`
- `TransferItems`
- `StockThresholds`
- `Notifications`

### Auditing fields

Entities inherit from `AuditableEntity`, which provides common fields:

- `Id`
- `CreatedAt`
- `CreatedBy`
- `ModifiedAt`
- `ModifiedBy`
- `IsDeleted` (soft-delete indicator)

### Indexes

Indexes are configured to support frequently used filters and relationships:

| Table | Indexed column(s) | Purpose |
|---|---|---|
| `StockTransactions` | `TransactionDate` | Supports date-range filtering. |
| `StockTransactions` | `LotId` | Supports joins between transactions and lots. |
| `Lots` | `(ProductId, WarehouseId)` | Helps locate available stock by product and warehouse. |
| `Lots` | `SupplierId` | Supports supplier-based inventory queries. |
| `TransferItems` | `TransferOrderId` | Supports retrieval of items belonging to a transfer. |
| `StockThresholds` | `(ProductId, WarehouseId)` | Supports threshold lookup for a product at a warehouse. |

---

## Technology Stack

| Area | Technologies |
|---|---|
| Backend | C#, ASP.NET Core 8 Web API |
| Architecture | Clean Architecture, Domain-Driven separation of responsibilities |
| Data access | Entity Framework Core 8 |
| Database | Microsoft SQL Server |
| Real-time communication | SignalR |
| Logging | Serilog |
| API documentation | Swagger / Swashbuckle |
| Testing | xUnit |

---

## Getting Started

### Prerequisites

Make sure the following tools are installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB, Express, or Developer edition)
- Visual Studio with the NuGet Package Manager, or another compatible .NET development environment

### 1. Clone the repository

```bash
git clone <your-repository-url>
cd <your-repository-folder>
```

Replace the placeholders with your repository URL and local folder name.

### 2. Configure the database connection

Open `ERP-Stock-Module/appsettings.json` and update the `Stock` connection string to point to your SQL Server instance.

Do not commit production credentials or private connection strings to source control.

### 3. Apply Entity Framework migrations

Open **Package Manager Console** in Visual Studio:

`Tools → NuGet Package Manager → Package Manager Console`

Run:

```powershell
Add-Migration InitialCreate -StartupProject ERP-Stock-Module -Project Infrastructure
Update-Database -StartupProject ERP-Stock-Module -Project Infrastructure
```

### 4. Run the application

From Visual Studio, set `ERP-Stock-Module` as the startup project and run it, or execute:

```bash
dotnet run --project ERP-Stock-Module
```

### 5. Open Swagger

After the application starts, open the Swagger URL shown in the console. The configured local URL may be:

```text
https://localhost:7213/swagger
```

The database is seeded with sample data through `DbInitializer` when the application starts.

---

## API Overview

The API can be explored through Swagger after the application is running.

| API area | Endpoint | Purpose |
|---|---|---|
| Admin | Product, supplier, warehouse, and stock-threshold endpoints | Manage core inventory records and threshold settings. |
| Sales | `POST /api/Sales` | Process a sale and allocate stock. |
| Transfers | `POST /api/Transfers` | Create and send a warehouse transfer. |
| Transfers | `PUT /api/Transfers/.../items/{id}/receive` | Confirm receipt of a transfer item. |
| Reports | `GET /api/Reports/movements` | Retrieve paginated inventory movements. |
| Reports | `GET /api/Reports/summary` | Retrieve aggregated in/out quantities per product. |
| Notifications | `GET /api/Notifications` | Retrieve persisted low-stock notifications. |
| Stock | `GET /api/Stock/levels` | Retrieve current stock levels. |
| Stock | `POST /api/Stock/receive` | Record incoming stock. |

The exact request and response models can be inspected in the Swagger documentation.

---

## Testing

The project includes a concurrent-sale test in:

```text
Tests/ConcurrentSaleTests.cs
```

The test simulates two simultaneous attempts to allocate the last available unit. It verifies that only one sale succeeds, the other is rejected due to insufficient stock, and the remaining quantity does not become negative.

Run the test suite from the repository root:

```bash
dotnet test
```

You can also run the tests through **Test Explorer** in Visual Studio.

---

## Project Structure

The solution is organized into the following Clean Architecture layers:

```text
<repository-root>/
├── Domain/
│   └── Entities, enums, and core domain models
├── Application/
│   └── DTOs and service interfaces
├── Infrastructure/
│   └── DbContext, service implementations, SignalR hub, and migrations
├── ERP-Stock-Module/
│   └── Web API controllers and Program.cs
└── Tests/
    └── xUnit test project
```

The structure shows the logical Clean Architecture layers. The API startup project and directory are named `ERP-Stock-Module` in the current setup commands.

---

## Current Scope and Assumptions

The implementation currently follows these assumptions:

- FIFO is the default stock-allocation strategy.
- A sale may contain multiple products, but it is fulfilled from a single warehouse.
- Stock transfers are between warehouses.
- Supplier information is retained on the source lot; a transfer-created lot can have a `null` `SupplierId` when stock from multiple suppliers is combined.
- Stock thresholds are configured per product per warehouse.
- Reporting focuses on inventory movements and quantities. Financial reporting is outside the current scope.
- Authentication and authorization are not implemented in the current version. A production deployment should place the API behind an appropriate identity provider and enforce access controls.
- Primary keys currently use `int`. The key strategy can be revisited if distributed-system requirements make it necessary.

---

## Potential Enhancements

Possible next steps for the project include:

- **GUID-based keys:** Evaluate migrating primary keys from `int` to `Guid` if the deployment model requires globally unique identifiers.
- **Docker support:** Add a `Dockerfile` and `docker-compose.yml` to simplify local setup and deployment.
- **Authentication and authorization:** Integrate an identity provider and add role-based access controls before exposing the API in a production environment.

---

