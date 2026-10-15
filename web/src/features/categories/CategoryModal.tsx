import { InlineNotification, Modal, RadioTile, Stack, TextInput, TileGroup } from '@carbon/react'
import { useState, type ChangeEvent } from 'react'
import { createCategory, updateCategory } from '../../api/categories.ts'
import { errorMessages } from '../../api/client.ts'
import type { Category } from '../../api/types.ts'
import { ErrorList } from '../../components/ErrorList.tsx'
import { categoryIcons } from '../../lib/categoryIcons.ts'
import {
  addRules,
  categoryToForm,
  emptyCategoryForm,
  neighbourNotes,
  toCategoryRequest,
  validateCategoryForm,
  type CategoryFormRules,
  type CategoryFormValues,
} from './categoryForm.ts'
import './CategoryModal.css'

interface CategoryModalProps {
  // The category being edited, or null to add a new one.
  category: Category | null
  // Every category, in weight order, to preview which neighbours a change moves.
  categories: Category[]
  rules?: CategoryFormRules
  onClose: () => void
  onSaved: () => void
}

// Rendered only while open, so every opening starts from the category's current values.
export function CategoryModal({ category, categories, rules = addRules, onClose, onSaved }: CategoryModalProps) {
  const [values, setValues] = useState<CategoryFormValues>(category ? categoryToForm(category) : emptyCategoryForm)
  const [submitted, setSubmitted] = useState(false)
  const [saving, setSaving] = useState(false)
  const [serverErrors, setServerErrors] = useState<string[]>([])

  const fieldErrors = submitted ? validateCategoryForm(values, rules) : {}
  const title = category ? 'Edit category' : 'Add category'
  const notes = neighbourNotes(categories, category, values)

  const change = (field: keyof CategoryFormValues) => (event: ChangeEvent<HTMLInputElement>) =>
    setValues((previous) => ({ ...previous, [field]: event.target.value }))

  const submit = async () => {
    setSubmitted(true)

    if (Object.keys(validateCategoryForm(values, rules)).length > 0) {
      return
    }

    setSaving(true)
    setServerErrors([])

    try {
      const request = toCategoryRequest(values)

      if (category) {
        await updateCategory(category.id, request)
      } else {
        await createCategory(request)
      }

      onSaved()
    } catch (error) {
      setServerErrors(errorMessages(error))
      setSaving(false)
    }
  }

  return (
    <Modal
      open
      modalHeading={title}
      primaryButtonText={saving ? 'Saving...' : category ? 'Save changes' : 'Add category'}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={saving}
      preventCloseOnClickOutside
      onRequestClose={onClose}
      onRequestSubmit={submit}
    >
      <Stack gap={6}>
        {serverErrors.length > 0 && <ErrorList title="The category could not be saved." messages={serverErrors} />}
        <TextInput
          id="category-name"
          labelText="Name"
          data-modal-primary-focus
          maxLength={50}
          value={values.name}
          onChange={change('name')}
          invalid={!!fieldErrors.name}
          invalidText={fieldErrors.name}
        />
        <TileGroup
          className="category-icon-picker"
          legend="Icon"
          name="category-icon"
          valueSelected={values.icon}
          onChange={(icon) => setValues((previous) => ({ ...previous, icon: String(icon) }))}
        >
          {Object.entries(categoryIcons).map(([key, { label, Icon }]) => (
            <RadioTile key={key} id={`category-icon-${key}`} value={key}>
              <span className="category-icon-tile">
                <Icon size={24} aria-hidden="true" />
                {label}
              </span>
            </RadioTile>
          ))}
        </TileGroup>
        {fieldErrors.icon && (
          <p className="category-field-error" role="alert">
            {fieldErrors.icon}
          </p>
        )}
        <TextInput
          id="category-from"
          labelText="From (kg)"
          helperText={rules.fromLocked ? 'The lowest category always starts at 0.' : 'Included in this category.'}
          inputMode="decimal"
          disabled={rules.fromLocked}
          value={values.from}
          onChange={change('from')}
          invalid={!!fieldErrors.from}
          invalidText={fieldErrors.from}
        />
        <TextInput
          id="category-to"
          labelText="To (kg)"
          helperText={
            rules.toLocked
              ? 'The highest category has no upper limit.'
              : rules.toRequired
                ? 'Not included. The next category starts here.'
                : 'Not included. Leave blank for no upper limit.'
          }
          inputMode="decimal"
          disabled={rules.toLocked}
          value={values.to}
          onChange={change('to')}
          invalid={!!fieldErrors.to}
          invalidText={fieldErrors.to}
        />
        {notes.length > 0 && (
          <InlineNotification
            className="category-change-notes"
            kind="info"
            title="Neighbouring categories will also change"
            hideCloseButton
            lowContrast
          >
            <ul>
              {notes.map((note) => (
                <li key={note}>{note}</li>
              ))}
            </ul>
          </InlineNotification>
        )}
      </Stack>
    </Modal>
  )
}
