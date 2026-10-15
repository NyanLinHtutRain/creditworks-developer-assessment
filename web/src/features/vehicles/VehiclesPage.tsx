import {
  Button,
  DataTableSkeleton,
  InlineLoading,
  TableContainer,
  TableToolbar,
  TableToolbarContent,
} from '@carbon/react'
import { Add } from '@carbon/icons-react'
import { useState } from 'react'
import type { SortDirection, VehicleSortField } from '../../api/types.ts'
import { ErrorList } from '../../components/ErrorList.tsx'
import { AddVehicleModal } from './AddVehicleModal.tsx'
import { useVehicles } from './useVehicles.ts'
import { VehicleTable } from './VehicleTable.tsx'

export function VehiclesPage() {
  const [sortBy, setSortBy] = useState<VehicleSortField>('owner')
  const [direction, setDirection] = useState<SortDirection>('asc')
  const [adding, setAdding] = useState(false)
  const { vehicles, errors, loading, reload } = useVehicles(sortBy, direction)

  const saved = () => {
    setAdding(false)
    reload()
  }

  const sortOn = (field: VehicleSortField) => {
    if (field === sortBy) {
      setDirection(direction === 'asc' ? 'desc' : 'asc')
    } else {
      setSortBy(field)
      setDirection('asc')
    }
  }

  if (vehicles === null && errors.length === 0) {
    return <DataTableSkeleton columnCount={5} rowCount={5} showToolbar={false} />
  }

  return (
    <TableContainer title="Vehicles" description="Select a column heading to sort the list.">
      <TableToolbar aria-label="Vehicle actions">
        <TableToolbarContent>
          <Button renderIcon={Add} onClick={() => setAdding(true)}>
            Add vehicle
          </Button>
        </TableToolbarContent>
      </TableToolbar>
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
        <VehicleTable vehicles={vehicles} sortBy={sortBy} direction={direction} onSort={sortOn} />
      )}
      {adding && <AddVehicleModal onClose={() => setAdding(false)} onSaved={saved} />}
    </TableContainer>
  )
}
