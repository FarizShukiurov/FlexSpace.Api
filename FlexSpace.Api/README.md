# 🏢 FlexSpace REST API 

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/EF_Core-8.0-388E3C?logo=nuget&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-Documented-85EA2D?logo=swagger&logoColor=black)

**FlexSpace API** is a robust, production-ready backend designed for coworking spaces and hot-desk booking platforms. Built with modern C# and .NET 8, it focuses on strict security, data integrity, and a seamless developer experience.

## 💡 The Business Value
Whether managing a single smart office or a multi-floor coworking hub, this API ensures that **no seat is ever double-booked** and **revenue is calculated with absolute precision**. 

* **Smart Conflict Resolution:** The database actively prevents overlapping bookings.
* **Automated Billing Logic:** Server-side calculation of `TotalPrice` based on precise time differences (down to the millisecond) and workspace hourly rates.
* **Role-Based Access Control (RBAC):** Strict separation between `Admin` (workspace management, booking confirmation) and `Customer` (personal booking history, secure cancellations).

## 🛠️ Technical Highlights (For Reviewers)
I built this API following industry best practices to ensure it is secure, scalable, and easy to maintain:

* **Security First (No IDOR):** All database entities utilize `Guid` instead of sequential integers, preventing Insecure Direct Object Reference vulnerabilities.
* **Stateless Authentication:** Fully implemented JWT (JSON Web Tokens) with claim-based authorization.
* **Global Exception Handling:** Replaced standard stack traces with a custom `.NET 8 IExceptionHandler`. All unexpected errors return a clean, standardized `ProblemDetails` JSON response.
* **Rich API Documentation:** Swagger UI is fully integrated with XML comments, providing clear request/response models and schema definitions for frontend teams.
* **Optimized Database Queries:** Utilizing advanced LINQ queries for filtering available workspaces, ensuring fast response times even under heavy loads.

## 🚀 Quick Start (Docker)
No need to install databases or SDKs. The project is fully containerized.

```bash
# 1. Clone the repository
git clone [https://github.com/FarizShukiurov/FlexSpace.Api.git](https://github.com/FarizShukiurov/FlexSpace.Api.git)
cd FlexSpace.Api

# 2. Build and run via Docker
docker build -t flexspace-api -f FlexSpace.Api/Dockerfile .
docker run -p 8080:8080 flexspace-api