import type { Category, CategoryRequest } from '../../api/types.ts'
import { formatWeight } from '../../lib/format.ts'

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

// Undefined when the text is not a valid weight.
function parseWeight(text: string) {
  const trimmed = text.trim()

  return weightPattern.test(trimmed) ? Number(trimmed) : undefined
}

// A preview of the neighbours that move with this category, since each boundary is shared with one.
// It stays empty when the numbers do not make sense yet; the API decides whether the change is allowed.
export function neighbourNotes(
  categories: Category[],
  category: Category | null,
  values: CategoryFormValues,
): string[] {
  const from = parseWeight(values.from)
  const to = values.to.trim() === '' ? null : parseWeight(values.to)

  if (from === undefined || to === undefined || (to !== null && to <= from)) {
    return []
  }

  return category ? editNotes(categories, category, from, to) : addNotes(categories, from, to)
}

function editNotes(categories: Category[], category: Category, from: number, to: number | null) {
  const index = categories.findIndex((candidate) => candidate.id === category.id)
  const below = categories[index - 1]
  const above = categories[index + 1]
  const notes: string[] = []

  if (below && from !== category.minWeightKg && from > below.minWeightKg) {
    notes.push(`${below.name} will now end at ${formatWeight(from)}.`)
  }

  if (above && to !== null && to !== category.maxWeightKg && (above.maxWeightKg === null || to < above.maxWeightKg)) {
    notes.push(`${above.name} will now start at ${formatWeight(to)}.`)
  }

  return notes
}

// A new category must fit inside one existing category and share exactly one edge with it.
function addNotes(categories: Category[], from: number, to: number | null) {
  const band = categories.find(
    (candidate) => from >= candidate.minWeightKg && (candidate.maxWeightKg === null || from < candidate.maxWeightKg),
  )

  if (!band || (band.maxWeightKg !== null && (to === null || to > band.maxWeightKg))) {
    return []
  }

  const sharesStart = from === band.minWeightKg
  const sharesEnd = to === band.maxWeightKg

  if (sharesStart === sharesEnd) {
    return []
  }

  return sharesStart && to !== null
    ? [`${band.name} will now start at ${formatWeight(to)}.`]
    : [`${band.name} will now end at ${formatWeight(from)}.`]
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
