const moneyFormat = new Intl.NumberFormat('lt-LT', { style: 'currency', currency: 'EUR' })
const quantityFormat = new Intl.NumberFormat('lt-LT', { maximumFractionDigits: 3 })
const percentFormat = new Intl.NumberFormat('lt-LT', { maximumFractionDigits: 2 })

/** A money amount in euros, e.g. "25,50 €". */
export function formatMoney(amount: number): string {
  return moneyFormat.format(amount)
}

/** A quantity or norm with up to 3 decimal places, e.g. "0,25". */
export function formatQuantity(value: number): string {
  return quantityFormat.format(value)
}

/** A percentage with up to 2 decimal places, e.g. "12,5 %". */
export function formatPercent(value: number): string {
  return `${percentFormat.format(value)} %`
}

/**
 * A number for a form field, written the Lithuanian way (decimal comma, no thousands separator), e.g. "918,75".
 * With `decimals`, exactly that many decimal places, e.g. money "4,50".
 */
export function toInputNumber(value: number, decimals?: number): string {
  return (decimals === undefined ? String(value) : value.toFixed(decimals)).replace('.', ',')
}
