const moneyFormat = new Intl.NumberFormat('lt-LT', { style: 'currency', currency: 'EUR' })
const priceFormat = new Intl.NumberFormat('lt-LT', {
  style: 'currency',
  currency: 'EUR',
  minimumFractionDigits: 2,
  maximumFractionDigits: 4,
})
const quantityFormat = new Intl.NumberFormat('lt-LT', { maximumFractionDigits: 3 })
const normFormat = new Intl.NumberFormat('lt-LT', { maximumFractionDigits: 6 })
const percentFormat = new Intl.NumberFormat('lt-LT', { maximumFractionDigits: 2 })

/** A money amount in euros, e.g. "25,50 €". */
export function formatMoney(amount: number): string {
  return moneyFormat.format(amount)
}

/** A unit price (material price, labour rate) with 2 to 4 decimal places, e.g. "4,50 €", "0,0616 €" (NFR-006). */
export function formatPrice(value: number): string {
  return priceFormat.format(value)
}

/** A quantity or hours with up to 3 decimal places, e.g. "0,25". */
export function formatQuantity(value: number): string {
  return quantityFormat.format(value)
}

/** A norm (consumption norm, labour norm) with up to 6 decimal places, e.g. "0,000884" (BR-017). */
export function formatNorm(value: number): string {
  return normFormat.format(value)
}

/** A percentage with up to 2 decimal places, e.g. "12,5 %". */
export function formatPercent(value: number): string {
  return `${percentFormat.format(value)} %`
}

/**
 * A number for a form field, written the Lithuanian way (decimal comma, no thousands separator), e.g. "918,75".
 * With `minDecimals`, padded to at least that many decimal places (a price 4.5 becomes "4,50"), but never cut, so
 * saving an unchanged form keeps every stored digit (0.0616 stays "0,0616").
 */
export function toInputNumber(value: number, minDecimals = 0): string {
  const text = String(value)
  const decimals = text.includes('.') ? text.split('.')[1].length : 0
  return (decimals >= minDecimals ? text : value.toFixed(minDecimals)).replace('.', ',')
}
