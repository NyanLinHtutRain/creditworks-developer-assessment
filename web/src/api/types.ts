export interface Category {
  id: number
  name: string
  icon: string
  minWeightKg: number
  // Null means the category has no upper limit.
  maxWeightKg: number | null
}

export interface Manufacturer {
  id: number
  name: string
}

export interface VehicleCategory {
  id: number
  name: string
  icon: string
}

export interface Vehicle {
  id: number
  ownerName: string
  manufacturerId: number
  manufacturerName: string
  yearOfManufacture: number
  weightKg: number
  category: VehicleCategory | null
}

export interface VehicleRequest {
  ownerName: string
  manufacturerId: number
  yearOfManufacture: number
  weightKg: number
}

export type VehicleSortField = 'owner' | 'manufacturer' | 'year' | 'weight'

export type SortDirection = 'asc' | 'desc'
