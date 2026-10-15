import { Button, Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@carbon/react'
import { Edit, TrashCan } from '@carbon/icons-react'
import type { Category } from '../../api/types.ts'
import { CategoryBadge } from '../../components/CategoryBadge.tsx'
import { formatWeight } from '../../lib/format.ts'

interface CategoryTableProps {
  categories: Category[]
  onEdit: (category: Category) => void
  onDelete: (category: Category) => void
}

export function CategoryTable({ categories, onEdit, onDelete }: CategoryTableProps) {
  return (
    <Table aria-label="Categories">
      <TableHead>
        <TableRow>
          <TableHeader>Category</TableHeader>
          <TableHeader>From</TableHeader>
          <TableHeader>To</TableHeader>
          <TableHeader>
            <span className="cds--visually-hidden">Actions</span>
          </TableHeader>
        </TableRow>
      </TableHead>
      <TableBody>
        {categories.map((category) => (
          <TableRow key={category.id}>
            <TableCell>
              <CategoryBadge name={category.name} icon={category.icon} />
            </TableCell>
            <TableCell>{formatWeight(category.minWeightKg)}</TableCell>
            <TableCell>{category.maxWeightKg === null ? 'and above' : formatWeight(category.maxWeightKg)}</TableCell>
            <TableCell>
              <Button
                kind="ghost"
                size="sm"
                hasIconOnly
                renderIcon={Edit}
                iconDescription={`Edit ${category.name}`}
                tooltipPosition="left"
                onClick={() => onEdit(category)}
              />
              <Button
                kind="ghost"
                size="sm"
                hasIconOnly
                renderIcon={TrashCan}
                iconDescription={`Delete ${category.name}`}
                tooltipPosition="left"
                onClick={() => onDelete(category)}
              />
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
