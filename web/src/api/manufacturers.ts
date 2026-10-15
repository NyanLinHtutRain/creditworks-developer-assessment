import { api } from './client.ts'
import type { Manufacturer } from './types.ts'

export function listManufacturers() {
  return api.get<Manufacturer[]>('/api/manufacturers')
}
