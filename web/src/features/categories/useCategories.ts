import { useEffect, useState } from 'react'
import { listCategories } from '../../api/categories.ts'
import { errorMessages } from '../../api/client.ts'
import type { Category } from '../../api/types.ts'

interface Outcome {
  // Which request this outcome belongs to.
  attempt: number
  categories: Category[] | null
  errors: string[]
}

export function useCategories() {
  const [attempt, setAttempt] = useState(0)
  const [outcome, setOutcome] = useState<Outcome>({ attempt: -1, categories: null, errors: [] })

  useEffect(() => {
    // An older response must not overwrite a newer one.
    let current = true

    listCategories()
      .then((categories) => {
        if (current) {
          setOutcome({ attempt, categories, errors: [] })
        }
      })
      .catch((error: unknown) => {
        if (current) {
          setOutcome((previous) => ({ ...previous, attempt, errors: errorMessages(error) }))
        }
      })

    return () => {
      current = false
    }
  }, [attempt])

  const loading = outcome.attempt !== attempt

  return {
    categories: outcome.categories,
    errors: loading ? [] : outcome.errors,
    reload: () => setAttempt((count) => count + 1),
  }
}
