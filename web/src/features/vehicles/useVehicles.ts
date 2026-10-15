import { useEffect, useState } from 'react'
import { errorMessages } from '../../api/client.ts'
import type { SortDirection, Vehicle, VehicleSortField } from '../../api/types.ts'
import { listVehicles } from '../../api/vehicles.ts'

interface Outcome {
  // Which request this outcome belongs to.
  key: string
  vehicles: Vehicle[] | null
  errors: string[]
}

export function useVehicles(sortBy: VehicleSortField, direction: SortDirection) {
  const [attempt, setAttempt] = useState(0)
  const [outcome, setOutcome] = useState<Outcome>({ key: '', vehicles: null, errors: [] })

  const key = `${sortBy}|${direction}|${attempt}`

  useEffect(() => {
    // A slow response must not overwrite a newer one when the user changes the sort quickly.
    let current = true

    listVehicles(sortBy, direction)
      .then((vehicles) => {
        if (current) {
          setOutcome({ key, vehicles, errors: [] })
        }
      })
      .catch((error: unknown) => {
        if (current) {
          setOutcome((previous) => ({ ...previous, key, errors: errorMessages(error) }))
        }
      })

    return () => {
      current = false
    }
  }, [key, sortBy, direction])

  const loading = outcome.key !== key

  return {
    vehicles: outcome.vehicles,
    errors: loading ? [] : outcome.errors,
    loading,
    reload: () => setAttempt((count) => count + 1),
  }
}
