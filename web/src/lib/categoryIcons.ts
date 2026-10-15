import { Bus, Car, DeliveryTruck, Van } from '@carbon/icons-react'
import type { CarbonIconType } from '@carbon/icons-react'

// Keys match the API's allowed icon list; adding an icon means adding it there and here.
export const categoryIcons: Record<string, { label: string; Icon: CarbonIconType }> = {
  car: { label: 'Car', Icon: Car },
  van: { label: 'Van', Icon: Van },
  'delivery-truck': { label: 'Delivery truck', Icon: DeliveryTruck },
  bus: { label: 'Bus', Icon: Bus },
}
