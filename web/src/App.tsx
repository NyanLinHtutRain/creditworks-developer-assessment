import { ContentSwitcher, Header, HeaderName, Switch } from '@carbon/react'
import { useState, type MouseEvent } from 'react'
import './App.css'
import { CategoriesPage } from './features/categories/CategoriesPage.tsx'
import { VehiclesPage } from './features/vehicles/VehiclesPage.tsx'

type Page = 'vehicles' | 'categories'

export default function App() {
  const [page, setPage] = useState<Page>('vehicles')

  const goHome = (event: MouseEvent) => {
    event.preventDefault()
    setPage('vehicles')
  }

  // The page switch sits in the content, not the header, because Carbon hides header links on narrow screens.
  return (
    <>
      <Header aria-label="Vehicle Manager">
        <HeaderName href="#" prefix="" onClick={goHome}>
          Vehicle Manager
        </HeaderName>
      </Header>
      <main className="app-content">
        <div className="app-nav">
          <ContentSwitcher
            aria-label="Sections"
            selectedIndex={page === 'vehicles' ? 0 : 1}
            onChange={({ name }) => setPage(name as Page)}
          >
            <Switch name="vehicles" text="Vehicles" />
            <Switch name="categories" text="Categories" />
          </ContentSwitcher>
        </div>
        {page === 'vehicles' ? <VehiclesPage /> : <CategoriesPage />}
      </main>
    </>
  )
}
