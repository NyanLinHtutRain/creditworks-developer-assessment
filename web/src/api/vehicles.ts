import { api } from './client.ts'
import type { SortDirection, Vehicle, VehicleRequest, VehicleSortField } from './types.ts'

export function listVehicles(sortBy: VehicleSortField, direction: SortDirection) {
  const query = new URLSearchParams({ sortBy, direction })

  return api.get<Vehicle[]>(`/api/vehicles?${query}`)
}

export function createVehicle(request: VehicleRequest) {
  return api.post<Vehicle>('/api/vehicles', request)
}
