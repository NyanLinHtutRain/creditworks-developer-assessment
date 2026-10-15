import { Button, DataTableSkeleton, TableContainer, TableToolbar, TableToolbarContent } from '@carbon/react'
import { Add } from '@carbon/icons-react'
import { useState } from 'react'
import type { Category } from '../../api/types.ts'
import { ErrorList } from '../../components/ErrorList.tsx'
import { editRules } from './categoryForm.ts'
import { CategoryModal } from './CategoryModal.tsx'
import { CategoryTable } from './CategoryTable.tsx'
import { DeleteCategoryModal } from './DeleteCategoryModal.tsx'
import { useCategories } from './useCategories.ts'

type Dialog = { kind: 'add' } | { kind: 'edit'; category: Category } | { kind: 'delete'; category: Category }

// The category below takes over a deleted range; the one above does when the lowest is deleted.
function neighbourTakingOver(categories: Category[], deleted: Category) {
  const index = categories.findIndex((category) => category.id === deleted.id)

  return index > 0 ? categories[index - 1] : categories[1]
}

export function CategoriesPage() {
  const [dialog, setDialog] = useState<Dialog | null>(null)
  const { categories, errors, reload } = useCategories()

  const done = () => {
    setDialog(null)
    reload()
  }

  if (categories === null && errors.length === 0) {
    return <DataTableSkeleton columnCount={4} rowCount={3} showToolbar={false} />
  }

  return (
    <TableContainer
      title="Categories"
      description="A vehicle's category comes from its weight. A new category must start or end where an existing one does."
    >
      <TableToolbar aria-label="Category actions">
        <TableToolbarContent>
          <Button renderIcon={Add} onClick={() => setDialog({ kind: 'add' })}>
            Add category
          </Button>
        </TableToolbarContent>
      </TableToolbar>
      {errors.length > 0 && (
        <>
          <ErrorList title="The categories could not be loaded." messages={errors} />
          <Button kind="tertiary" size="sm" onClick={reload}>
            Try again
          </Button>
        </>
      )}
      {categories !== null && (
        <CategoryTable
          categories={categories}
          onEdit={(category) => setDialog({ kind: 'edit', category })}
          onDelete={(category) => setDialog({ kind: 'delete', category })}
        />
      )}
      {dialog?.kind === 'add' && <CategoryModal category={null} onClose={() => setDialog(null)} onSaved={done} />}
      {dialog?.kind === 'edit' && categories && (
        <CategoryModal
          category={dialog.category}
          rules={editRules(categories, dialog.category)}
          onClose={() => setDialog(null)}
          onSaved={done}
        />
      )}
      {dialog?.kind === 'delete' && categories && (
        <DeleteCategoryModal
          category={dialog.category}
          takesOver={neighbourTakingOver(categories, dialog.category)}
          onClose={() => setDialog(null)}
          onDeleted={done}
        />
      )}
    </TableContainer>
  )
}
