# Design notes

How the solution is built, why, and what it leaves out. Setup and run steps are in the [README](README.md).

## Architecture

A small layered API with a separate React app. Dependencies point inward, toward Domain.

| Project | Holds | References |
|---|---|---|
| `VehicleManager.Domain` | Entities and all business rules as plain code. No packages. | none |
| `VehicleManager.Infrastructure` | EF Core `DbContext`, table configuration, migrations, seed data | Domain |
| `VehicleManager.Api` | Minimal API endpoints, request and response contracts, services, error handling | Domain, Infrastructure |

- **Business rules** (category lookup, range validation, category changes, vehicle validation) are pure functions in Domain, so they are unit-tested without a database.
- **Services** in the Api project (`CategoryService`, `VehicleService`) do the orchestration: load, apply a rule, save in a transaction. They use the `DbContext` directly. A separate application layer or a repository would add files without adding behaviour at this size.
- **Web app:** React, TypeScript and Carbon. One folder per feature (`vehicles`, `categories`), plain `fetch`, no router or state library. In development Vite forwards `/api` to the API, so the browser sees one origin and the API needs no CORS.

## Database design

| Table | Columns | Constraints |
|---|---|---|
| `Manufacturers` | Id, Name (100) | Unique name |
| `Categories` | Id, Name (50), Icon (16), MinWeightKg, MaxWeightKg (nullable) | Unique name, `MinWeightKg >= 0`, `MaxWeightKg` null or greater than `MinWeightKg` |
| `Vehicles` | Id, OwnerName (100), ManufacturerId, YearOfManufacture, WeightKg | Foreign key to `Manufacturers` (restrict delete), `WeightKg > 0` |

Weights are `decimal(8,2)`: two decimal places, up to 999,999.99 kg.

Decisions:

- **The category is calculated, not stored.** `Vehicles` has no category column. The category is found from the weight each time the list is read. Nothing can go out of date when categories change, and there is no update step to get wrong.
- **Manufacturers are rows, not code.** The five names exist only in the seed data. Vehicles refer to them by id, the API serves the list, and the dropdown is built from it. Adding a sixth is a new row. There is no admin screen for it because the list rarely changes.
- **Ranges** are stored as a lower bound and an optional upper bound. A null upper bound means "and above".
- **Icons** are stored as a short key (`car`, `van`, `delivery-truck`, `bus`). The API accepts only these keys and the web app maps each to a Carbon icon.
- **Gaps cannot be checked by the database,** since that rule spans several rows. The check constraints cover each row, and the category service covers the whole set (see below).
- **Setup:** the initial migration creates the schema and the seed data (five manufacturers, three categories). In Development the API applies it on startup.

## Category rules

### Boundary rule

A category **includes** its From weight and **excludes** its To weight: `From <= weight < To`. The top category has no To. This lives in one method, `Category.Includes`, and everything else uses it.

| Weight | Category |
|---|---|
| 499.99 kg | Light |
| 500.00 kg | Medium |
| 2,499.99 kg | Medium |
| 2,500.00 kg | Heavy |

Every weight, including one exactly on a boundary, belongs to exactly one category.

### Valid set

A configuration is valid when the lowest category starts at 0, each category ends where the next one starts, and exactly one category, the highest, has no upper bound. Names must be unique and icons must come from the list. `CategoryConfigurationValidator` checks the whole set and reports every problem, with plain messages such as "A gap exists between 500.00 kg and 600.00 kg".

### Changes are boundary moves

Moving only one range can never be valid on its own: raising the end of Medium alone leaves a gap before Heavy. So a change moves the shared boundary as one step.

- **Edit:** changing a category's From also moves the To of the one below. Changing its To moves the From of the one above. The lowest must start at 0 and the highest must stay open-ended.
- **Create:** the new range must start or end where an existing category does and fit inside it. That category shrinks to make room.
- **Delete:** the category below takes over the range. If the lowest is deleted, the one above drops to 0. The last category cannot be deleted.

Each change is built in memory, validated as a whole set, and saved in one serializable transaction. If validation fails, nothing is saved and the API returns the reasons. A random-sequence test applies many changes and checks the set is valid after each.

### Existing vehicles

Because the category is calculated on read, the list always shows the current configuration. Example from the assignment: a 2,200 kg vehicle is Medium. Set Medium's To to 2,000 kg and Heavy starts at 2,000 kg, so the same vehicle is now Heavy. A unit test and an integration test cover this.

## Error handling

- Expected failures use one shape, `application/problem+json`, with an `errors` list of messages ready to show to a user. The web app shows them in the form.
- **400** invalid input or an invalid category configuration. **404** a category that does not exist. **409** a save that conflicted with another update. **500** anything unexpected.
- Responses never contain stack traces or SQL. Details go to the server log. An unknown URL or unreadable body gets a problem response in every environment.
- The server validates everything. The checks in the browser only save a round trip.

## Testing

| Kind | Count | What it covers |
|---|---|---|
| Unit (`VehicleManager.UnitTests`) | 100 | Category lookup and boundary values, gaps, overlaps, every change type, an invariant test over random changes, vehicle validation |
| Integration (`VehicleManager.IntegrationTests`) | 25 | The real API on SQL Server: create, edit and delete categories, vehicle creation, sorting on every field, a boundary move changing a vehicle's category, error responses |

The integration tests use SQL Server in the Docker container, not SQLite, because EF Core's SQLite provider cannot sort by `decimal` columns. Each class gets a throwaway database that is dropped afterwards. The web app has no automated tests; it was checked by hand and in a headless browser.

## Assumptions

- Weight is at most 999,999.99 kg, from the column type.
- Year of manufacture runs from 1886 to next year, since a model can be built a year ahead of release.
- A new category must start or end where an existing one does. This follows from keeping the set gap-free and avoids inventing a name for a leftover piece.
- Icons are a fixed list of four and can repeat. They are labels chosen by the user. The app does not tie an icon to a weight, because the assignment does not define one.
- Category names are unique, ignoring case.
- Sorting is on the server. Ties break by id so the order is stable.

## Left out on purpose

- **No authentication or authorisation.** Anyone who can reach the API can change categories.
- No paging or rate limiting. The vehicle list returns every row.
- No edit or delete for vehicles, since the assignment asks only to add and view.
- No admin screen for manufacturers.
- No CORS. The web app and API are meant to share an origin, as the Vite proxy does in development.
- Local-only settings: the `sa` login and `TrustServerCertificate=True`. A real deployment needs a login with minimal rights and a valid certificate. `AllowedHosts` is `*`.
- Migrations apply automatically only in Development.
- Simultaneous category edits are kept safe by a serializable transaction. In a test with 30 edits at once the categories stayed valid. When SQL Server cancels one request as a deadlock victim, that request returns 500 instead of the 409 described above, and nothing is saved for it.
- The .NET 8 note is in the README.

## What I would improve next

- Paging and a search box on the vehicle list.
- Edit and delete for vehicles.
- Authentication, with category changes limited to an admin role.
- Frontend tests (Vitest and Testing Library).
- Return 409 for deadlocks, with a test that sends parallel requests.
- A continuous integration pipeline that builds and runs all tests.
- Upgrade to .NET 10.
