import { Header, HeaderName, HeaderNavigation, HeaderMenuItem } from '@carbon/react'
import { useState, type MouseEvent } from 'react'
import './App.css'
import { CategoriesPage } from './features/categories/CategoriesPage.tsx'
import { VehiclesPage } from './features/vehicles/VehiclesPage.tsx'

type Page = 'vehicles' | 'categories'

export default function App() {
  const [page, setPage] = useState<Page>('vehicles')

  const navigate = (target: Page) => (event: MouseEvent) => {
    event.preventDefault()
    setPage(target)
  }

  return (
    <>
      <Header aria-label="Vehicle Manager">
        <HeaderName href="#" prefix="">
          Vehicle Manager
        </HeaderName>
        <HeaderNavigation aria-label="Sections">
          <HeaderMenuItem href="#" isCurrentPage={page === 'vehicles'} onClick={navigate('vehicles')}>
            Vehicles
          </HeaderMenuItem>
          <HeaderMenuItem href="#" isCurrentPage={page === 'categories'} onClick={navigate('categories')}>
            Categories
          </HeaderMenuItem>
        </HeaderNavigation>
      </Header>
      <main className="app-content">{page === 'vehicles' ? <VehiclesPage /> : <CategoriesPage />}</main>
    </>
  )
}
