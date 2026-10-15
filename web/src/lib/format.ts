const weightFormat = new Intl.NumberFormat('en', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

export function formatWeight(weightKg: number) {
  return `${weightFormat.format(weightKg)} kg`
}
