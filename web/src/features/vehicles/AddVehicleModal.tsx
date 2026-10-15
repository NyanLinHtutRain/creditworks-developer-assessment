import { Modal, Select, SelectItem, Stack, TextInput } from '@carbon/react'
import { useEffect, useState, type ChangeEvent } from 'react'
import { errorMessages } from '../../api/client.ts'
import { listManufacturers } from '../../api/manufacturers.ts'
import type { Manufacturer } from '../../api/types.ts'
import { createVehicle } from '../../api/vehicles.ts'
import { ErrorList } from '../../components/ErrorList.tsx'
import { emptyVehicleForm, toVehicleRequest, validateVehicleForm, type VehicleFormValues } from './vehicleForm.ts'

interface AddVehicleModalProps {
  onClose: () => void
  onSaved: () => void
}

// Rendered only while open, so every opening starts with an empty form.
export function AddVehicleModal({ onClose, onSaved }: AddVehicleModalProps) {
  const [values, setValues] = useState<VehicleFormValues>(emptyVehicleForm)
  const [manufacturers, setManufacturers] = useState<Manufacturer[]>([])
  const [submitted, setSubmitted] = useState(false)
  const [saving, setSaving] = useState(false)
  const [serverErrors, setServerErrors] = useState<string[]>([])
  const [loadErrors, setLoadErrors] = useState<string[]>([])

  useEffect(() => {
    listManufacturers()
      .then(setManufacturers)
      .catch((error: unknown) => setLoadErrors(errorMessages(error)))
  }, [])

  const fieldErrors = submitted ? validateVehicleForm(values) : {}

  const change = (field: keyof VehicleFormValues) => (event: ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setValues((previous) => ({ ...previous, [field]: event.target.value }))

  const submit = async () => {
    setSubmitted(true)

    if (Object.keys(validateVehicleForm(values)).length > 0) {
      return
    }

    setSaving(true)
    setServerErrors([])

    try {
      await createVehicle(toVehicleRequest(values))
      onSaved()
    } catch (error) {
      setServerErrors(errorMessages(error))
      setSaving(false)
    }
  }

  return (
    <Modal
      open
      modalHeading="Add vehicle"
      primaryButtonText={saving ? 'Saving...' : 'Add vehicle'}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={saving}
      preventCloseOnClickOutside
      onRequestClose={onClose}
      onRequestSubmit={submit}
    >
      <Stack gap={6}>
        {loadErrors.length > 0 && <ErrorList title="The manufacturers could not be loaded." messages={loadErrors} />}
        {serverErrors.length > 0 && <ErrorList title="The vehicle could not be added." messages={serverErrors} />}
        <TextInput
          id="vehicle-owner"
          labelText="Owner's name"
          data-modal-primary-focus
          value={values.ownerName}
          onChange={change('ownerName')}
          invalid={!!fieldErrors.ownerName}
          invalidText={fieldErrors.ownerName}
        />
        <Select
          id="vehicle-manufacturer"
          labelText="Manufacturer"
          value={values.manufacturerId}
          onChange={change('manufacturerId')}
          invalid={!!fieldErrors.manufacturerId}
          invalidText={fieldErrors.manufacturerId}
        >
          <SelectItem value="" text="Choose a manufacturer" />
          {manufacturers.map((manufacturer) => (
            <SelectItem key={manufacturer.id} value={String(manufacturer.id)} text={manufacturer.name} />
          ))}
        </Select>
        <TextInput
          id="vehicle-year"
          labelText="Year of manufacture"
          inputMode="numeric"
          maxLength={4}
          value={values.year}
          onChange={change('year')}
          invalid={!!fieldErrors.year}
          invalidText={fieldErrors.year}
        />
        <TextInput
          id="vehicle-weight"
          labelText="Weight (kg)"
          helperText="Up to two decimal places, for example 1850.75"
          inputMode="decimal"
          value={values.weight}
          onChange={change('weight')}
          invalid={!!fieldErrors.weight}
          invalidText={fieldErrors.weight}
        />
      </Stack>
    </Modal>
  )
}
