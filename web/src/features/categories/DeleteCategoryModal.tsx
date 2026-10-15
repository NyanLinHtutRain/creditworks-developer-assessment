import { Modal } from '@carbon/react'
import { useState } from 'react'
import { deleteCategory } from '../../api/categories.ts'
import { errorMessages } from '../../api/client.ts'
import type { Category } from '../../api/types.ts'
import { ErrorList } from '../../components/ErrorList.tsx'

interface DeleteCategoryModalProps {
  category: Category
  // The neighbour that takes over the range: the one below, or the one above when the lowest is deleted.
  takesOver: Category | undefined
  onClose: () => void
  onDeleted: () => void
}

export function DeleteCategoryModal({ category, takesOver, onClose, onDeleted }: DeleteCategoryModalProps) {
  const [deleting, setDeleting] = useState(false)
  const [errors, setErrors] = useState<string[]>([])

  const confirm = async () => {
    setDeleting(true)
    setErrors([])

    try {
      await deleteCategory(category.id)
      onDeleted()
    } catch (error) {
      setErrors(errorMessages(error))
      setDeleting(false)
    }
  }

  return (
    <Modal
      open
      danger
      modalHeading={`Delete ${category.name}?`}
      primaryButtonText={deleting ? 'Deleting...' : 'Delete category'}
      secondaryButtonText="Cancel"
      primaryButtonDisabled={deleting}
      preventCloseOnClickOutside
      onRequestClose={onClose}
      onRequestSubmit={confirm}
    >
      {errors.length > 0 && <ErrorList title="The category could not be deleted." messages={errors} />}
      <p>
        {takesOver
          ? `${takesOver.name} will take over the weight range of ${category.name}, so vehicles in that range will show ${takesOver.name}.`
          : `${category.name} is the only category, so it cannot be deleted.`}
      </p>
    </Modal>
  )
}
