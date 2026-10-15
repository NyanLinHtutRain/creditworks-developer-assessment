import { categoryIcons } from '../lib/categoryIcons.ts'
import './CategoryBadge.css'

interface CategoryBadgeProps {
  name: string
  icon: string
}

// The name is always shown beside the icon, so categories that share an icon stay distinguishable.
export function CategoryBadge({ name, icon }: CategoryBadgeProps) {
  const entry = categoryIcons[icon]

  return (
    <span className="category-badge">
      {entry && <entry.Icon size={20} aria-hidden="true" />}
      {name}
    </span>
  )
}
