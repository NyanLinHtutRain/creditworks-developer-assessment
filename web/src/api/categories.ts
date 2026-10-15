import { api } from './client.ts'
import type { Category, CategoryRequest } from './types.ts'

export function listCategories() {
  return api.get<Category[]>('/api/categories')
}

export function createCategory(request: CategoryRequest) {
  return api.post<Category>('/api/categories', request)
}

export function updateCategory(id: number, request: CategoryRequest) {
  return api.put<Category>(`/api/categories/${id}`, request)
}

export function deleteCategory(id: number) {
  return api.delete(`/api/categories/${id}`)
}
