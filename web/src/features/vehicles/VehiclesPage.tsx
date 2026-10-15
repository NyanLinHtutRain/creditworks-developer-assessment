import {
  Button,
  DataTableSkeleton,
  InlineLoading,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableHeader,
  TableRow,
} from '@carbon/react'
import { useState } from 'react'
import type { SortDirection, VehicleSortField } from '../../api/types.ts'
import { CategoryBadge } from '../../components/CategoryBadge.tsx'
import { ErrorList } from '../../components/ErrorList.tsx'
import { formatWeight } from '../../lib/format.ts'
import { useVehicles } from './useVehicles.ts'

// Category is not sortable: it is derived from the weight, so sorting by weight already orders it.
const columns: { label: string; sortBy: VehicleSortField | null }[] = [
  { label: 'Owner', sortBy: 'owner' },
  { label: 'Manufacturer', sortBy: 'manufacturer' },
  { label: 'Year', sortBy: 'year' },
  { label: 'Weight', sortBy: 'weight' },
  { label: 'Category', sortBy: null },
]

export function VehiclesPage() {
  const [sortBy, setSortBy] = useState<VehicleSortField>('owner')
  const [direction, setDirection] = useState<SortDirection>('asc')
  const { vehicles, errors, loading, reload } = useVehicles(sortBy, direction)

  const sortOn = (field: VehicleSortField) => {
    if (field === sortBy) {
      setDirection(direction === 'asc' ? 'desc' : 'asc')
    } else {
      setSortBy(field)
      setDirection('asc')
    }
  }

  if (vehicles === null && errors.length === 0) {
    return <DataTableSkeleton columnCount={columns.length} rowCount={5} showToolbar={false} />
  }

  return (
    <TableContainer title="Vehicles" description="Select a column heading to sort the list.">
      {errors.length > 0 && (
        <>
          <ErrorList title="The vehicles could not be loaded." messages={errors} />
          <Button kind="tertiary" size="sm" onClick={reload}>
            Try again
          </Button>
        </>
      )}
      {loading && vehicles !== null && <InlineLoading description="Updating..." />}
      {vehicles !== null && (
        <Table aria-label="Vehicles">
          <TableHead>
            <TableRow>
              {columns.map((column) => {
                const active = column.sortBy === sortBy

                return (
                  <TableHeader
                    key={column.label}
                    isSortable={column.sortBy !== null}
                    isSortHeader={active}
                    sortDirection={active ? (direction === 'asc' ? 'ASC' : 'DESC') : 'NONE'}
                    onClick={column.sortBy === null ? undefined : () => sortOn(column.sortBy!)}
                  >
                    {column.label}
                  </TableHeader>
                )
              })}
            </TableRow>
          </TableHead>
          <TableBody>
            {vehicles.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columns.length}>No vehicles have been added yet.</TableCell>
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
      )}
    </TableContainer>
  )
}
