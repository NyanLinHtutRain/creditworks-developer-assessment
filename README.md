# Vehicle Manager

A small web application for managing vehicles and weight-based vehicle categories, built for the CreditWorks Software Engineer technical assignment.

A vehicle's category is never stored — it is resolved from the vehicle's weight against the category configuration that is current at the time of the request. Change a category's range and the vehicle list reflects it immediately.

## Stack

| Layer | Technology |
|---|---|
| API | .NET 8, ASP.NET Core Web API |
| Data access | Entity Framework Core 8 |
| Database | Microsoft SQL Server 2022 |
| Frontend | React + TypeScript (Vite), IBM Carbon Design System |
| Tests | xUnit, FluentAssertions |

## Quick start

> Documented in full once the application is in place. The intended path is `docker compose up`, which starts SQL Server, applies migrations, seeds the reference data, and serves the API and web app.

## Project layout

```
api/   .NET solution (domain, infrastructure, API, tests)
web/   React single-page application
```

## Documentation

- Setup, configuration and migrations — to follow
- Build, run and test instructions — to follow
- Architecture and database design — to follow
- Category boundary rules — to follow
- Assumptions, trade-offs and known limitations — to follow
