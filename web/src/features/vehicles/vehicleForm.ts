import type { VehicleRequest } from '../../api/types.ts'

// Everything is kept as text while the user types, and converted only when the form is submitted.
export interface VehicleFormValues {
  ownerName: string
  manufacturerId: string
  year: string
  weight: string
}

export type VehicleFormErrors = Partial<Record<keyof VehicleFormValues, string>>

export const emptyVehicleForm: VehicleFormValues = { ownerName: '', manufacturerId: '', year: '', weight: '' }

// Quick checks that save a round trip. The API repeats and extends them, and is the source of truth (for example, the allowed year range).
export function validateVehicleForm(values: VehicleFormValues): VehicleFormErrors {
  const errors: VehicleFormErrors = {}

  if (!values.ownerName.trim()) {
    errors.ownerName = "Enter the owner's name."
  }

  if (!values.manufacturerId) {
    errors.manufacturerId = 'Choose a manufacturer.'
  }

  if (!/^\d{4}$/.test(values.year.trim())) {
    errors.year = 'Enter a four-digit year.'
  }

  const weight = values.weight.trim()

  if (!/^\d+(\.\d{1,2})?$/.test(weight)) {
    errors.weight = 'Enter the weight in kilograms with up to two decimal places.'
  } else if (Number(weight) <= 0) {
    errors.weight = 'The weight must be greater than zero.'
  }

  return errors
}

export function toVehicleRequest(values: VehicleFormValues): VehicleRequest {
  return {
    ownerName: values.ownerName.trim(),
    manufacturerId: Number(values.manufacturerId),
    yearOfManufacture: Number(values.year),
    weightKg: Number(values.weight),
  }
}
