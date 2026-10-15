import type { Category, CategoryRequest } from '../../api/types.ts'

// Everything is kept as text while the user types, and converted only when the form is submitted.
export interface CategoryFormValues {
  name: string
  icon: string
  from: string
  to: string
}

export type CategoryFormErrors = Partial<Record<keyof CategoryFormValues, string>>

// Which bounds the form lets the user change. The API enforces the same rules.
export interface CategoryFormRules {
  fromLocked: boolean
  toLocked: boolean
  toRequired: boolean
}

export const addRules: CategoryFormRules = { fromLocked: false, toLocked: false, toRequired: false }

export const emptyCategoryForm: CategoryFormValues = { name: '', icon: '', from: '', to: '' }

// The lowest category must start at 0 and the highest must stay open-ended, so those bounds are fixed.
export function editRules(categories: Category[], category: Category): CategoryFormRules {
  const isHighest = categories[categories.length - 1]?.id === category.id

  return { fromLocked: categories[0]?.id === category.id, toLocked: isHighest, toRequired: !isHighest }
}

export function categoryToForm(category: Category): CategoryFormValues {
  return {
    name: category.name,
    icon: category.icon,
    from: String(category.minWeightKg),
    to: category.maxWeightKg === null ? '' : String(category.maxWeightKg),
  }
}

const weightPattern = /^\d+(\.\d{1,2})?$/

// Quick checks that save a round trip. The API repeats them and decides whether the ranges still fit together.
export function validateCategoryForm(values: CategoryFormValues, rules: CategoryFormRules): CategoryFormErrors {
  const errors: CategoryFormErrors = {}
  const from = values.from.trim()
  const to = values.to.trim()

  if (!values.name.trim()) {
    errors.name = 'Enter a name.'
  }

  if (!values.icon) {
    errors.icon = 'Choose an icon.'
  }

  if (!weightPattern.test(from)) {
    errors.from = 'Enter the starting weight in kilograms with up to two decimal places.'
  }

  if (to === '') {
    if (rules.toRequired) {
      errors.to = 'Enter an upper weight. Only the highest category has no upper limit.'
    }
  } else if (!weightPattern.test(to)) {
    errors.to = 'Enter the upper weight in kilograms with up to two decimal places, or leave it blank.'
  } else if (!errors.from && Number(to) <= Number(from)) {
    errors.to = 'The upper weight must be greater than the starting weight.'
  }

  return errors
}

export function toCategoryRequest(values: CategoryFormValues): CategoryRequest {
  const to = values.to.trim()

  return {
    name: values.name.trim(),
    icon: values.icon,
    minWeightKg: Number(values.from),
    maxWeightKg: to === '' ? null : Number(to),
  }
}
