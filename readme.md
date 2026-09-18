# DNS Zone Manager

A small ASP.NET Core web app that lets a non-technical customer manage DNS **zones**
and their **records** (query, create, modify, delete) through a browser, instead of
calling the DNS provider's API directly.

## Stack

- **Backend:** ASP.NET Core 8, MVC controllers (`Controllers/`)
- **Data:** EF Core, `UseInMemoryDatabase` (resets on every restart, per spec)
- **Frontend:** Static HTML/CSS + jQuery (`wwwroot/`), Bootstrap 5 via CDN for styling 
- **API docs:** Swagger UI at `/swagger/index.html` in development

## Running it

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet restore
dotnet run
```

Then open the URL printed in the console 'http://localhost:5184'
the app starts with zero zones.

## Project structure

```
DnsZoneManager/
├── Program.cs                    — app startup: DI registration, middleware pipeline
├── Controllers/
│   ├── ZonesController.cs        — GET/POST/DELETE /api/zones
│   └── RecordsController.cs      — POST/PUT/DELETE /api/zones/{zoneId}/records
├── Models/                       — DnsZone, DnsRecord, RecordType enum
├── Data/
│   └── DnsContext.cs             — EF Core InMemory context + relationships
├── Services/
│   ├── IDnsZoneService.cs
│   ├── DnsZoneService.cs         — all business rules and validation
│   └── ServiceResult.cs          — success/error wrapper used across the service layer
├── Dtos/                         — request/response shapes for the API
├── wwwroot/                      — the UI (index.html, css/site.css, js/app.js)
└── readme.md
```

## How it works

- **Zones** (`GET/POST/DELETE /api/zones`, `GET /api/zones/{id}`): each new zone is   automatically created with 4 NS records, since a DNS zone is never really "empty".
 Deleting a zone cascades to its records (`DeleteBehavior.Cascade` in
  `DnsContext`).
- **Records** (`POST/PUT/DELETE /api/zones/{zoneId}/records[/{id}]`): add, edit, or
  remove A / AAAA / CNAME / NS / TXT records within a zone.
- **Controllers** Every action does exactly three things: call the service,
  translate its `ServiceResult<T>` into the right HTTP status (200/201/400/404), and
  return. All business logic lives in `Services/DnsZoneService.cs` — that's the single
  place to look for "why did this get rejected."
- All validation is server-side; and errors
  are displayed to the user as plain sentences.
- The frontend uses jQuery for DOM updates, events, and AJAX (`$.ajax`); Bootstrap 5 drives the modal and toast widgets, so both are loaded
  side by side in `index.html`.

## Business rules enforced

Straight from the spec's "Assumptions" section:

- A new zone starts with (and can never drop below) **4 NS records**.
- A zone may hold **at most 10 records total**.
- Only **A, AAAA, CNAME, NS, TXT** record types are accepted (modeled as a C# enum, so
  an invalid type can't reach the database layer at all).
- **Duplicate zones** (by name) and **duplicate records** (same name + type + data
  within a zone) are rejected.

## Design notes / trade-offs

- **MVC controllers over minimal API endpoints** — with two resources (zones, and
  records nested under a zone) and 7 total routes, controllers keep each resource's
  actions grouped in one file and let `[ApiController]` handle body-binding and
  automatic 400s on malformed requests.
- **A `ServiceResult<T>` wrapper**, not exceptions, carries validation failures from the
  service layer to the controllers. Validation failures are expected, everyday outcomes
  in a CRUD app, not exceptional ones — using exceptions for control flow here would be
  slower and would blur real bugs together with "user typed a bad IP."
-

