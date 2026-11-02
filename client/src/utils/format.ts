const moneyFormat = new Intl.NumberFormat('lt-LT', { style: 'currency', currency: 'EUR' })
const quantityFormat = new Intl.NumberFormat('lt-LT', { maximumFractionDigits: 3 })

/** A money amount in euros, e.g. "25,50 €". */
export function formatMoney(amount: number): string {
  return moneyFormat.format(amount)
}

/** A quantity or norm with up to 3 decimal places, e.g. "0,25". */
export function formatQuantity(value: number): string {
  return quantityFormat.format(value)
}
