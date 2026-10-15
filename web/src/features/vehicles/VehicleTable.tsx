import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@carbon/react'
import type { SortDirection, Vehicle, VehicleSortField } from '../../api/types.ts'
import { CategoryBadge } from '../../components/CategoryBadge.tsx'
import { formatWeight } from '../../lib/format.ts'

// Category is not sortable: it is derived from the weight, so sorting by weight already orders it.
const vehicleColumns: { label: string; sortBy?: VehicleSortField }[] = [
  { label: 'Owner', sortBy: 'owner' },
  { label: 'Manufacturer', sortBy: 'manufacturer' },
  { label: 'Year', sortBy: 'year' },
  { label: 'Weight', sortBy: 'weight' },
  { label: 'Category' },
]

interface VehicleTableProps {
  vehicles: Vehicle[]
  sortBy: VehicleSortField
  direction: SortDirection
  onSort: (field: VehicleSortField) => void
}

export function VehicleTable({ vehicles, sortBy, direction, onSort }: VehicleTableProps) {
  const carbonDirection = direction === 'asc' ? 'ASC' : 'DESC'

  return (
    <Table aria-label="Vehicles">
      <TableHead>
        <TableRow>
          {vehicleColumns.map(({ label, sortBy: field }) => {
            const active = field === sortBy

            return (
              <TableHeader
                key={label}
                isSortable={field !== undefined}
                isSortHeader={active}
                sortDirection={active ? carbonDirection : 'NONE'}
                onClick={field === undefined ? undefined : () => onSort(field)}
              >
                {label}
              </TableHeader>
            )
          })}
        </TableRow>
      </TableHead>
      <TableBody>
        {vehicles.length === 0 ? (
          <TableRow>
            <TableCell colSpan={vehicleColumns.length}>No vehicles have been added yet.</TableCell>
          </TableRow>
        ) : (
          vehicles.map((vehicle) => (
            <TableRow key={vehicle.id}>
              <TableCell>{vehicle.ownerName}</TableCell>
              <TableCell>{vehicle.manufacturerName}</TableCell>
              <TableCell>{vehicle.yearOfManufacture}</TableCell>
              <TableCell>{formatWeight(vehicle.weightKg)}</TableCell>
              <TableCell>
                {vehicle.category ? (
                  <CategoryBadge name={vehicle.category.name} icon={vehicle.category.icon} />
                ) : (
                  '—'
                )}
              </TableCell>
            </TableRow>
          ))
        )}
      </TableBody>
    </Table>
  )
}
