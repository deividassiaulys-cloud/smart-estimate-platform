const table = 'w-full border-collapse'
const cell = 'border-b border-line px-3 py-2 text-left whitespace-nowrap'
const actionsCell = 'flex items-center gap-3 border-b border-line px-3 py-2'

// Below 768 px a list becomes cards: each row a bordered card, each cell a line labelled by its data-label
// (docs/ux.md §8). Only the lists use these; the editors' tables stay tables and scroll inside their frame.
const card = 'max-md:block'
const cardCell =
  'max-md:flex max-md:justify-between max-md:gap-3 max-md:border-0 max-md:px-0 max-md:py-1 max-md:whitespace-normal max-md:before:font-medium max-md:before:text-heading max-md:before:content-[attr(data-label)]'

/**
 * Tailwind class lists shared by several pages, defined once so every form, table and button looks the same.
 */
export const ui = {
  // Its own line height: the body's 145 % is inherited as a fixed length and would make a wrapped heading overlap.
  h1: 'my-5 text-[36px] leading-tight font-medium tracking-[-1.68px] text-heading lg:my-8 lg:text-[56px]',
  h2: 'mb-2 text-[20px] leading-[1.18] font-medium tracking-[-0.24px] text-heading lg:text-[24px]',
  link: 'text-[color:LinkText] underline',

  formPage: 'mx-auto w-full max-w-[452px] px-4 py-8 text-left',
  form: 'flex flex-col gap-4',
  field: 'flex flex-col gap-1',
  label: 'font-medium text-heading',
  input:
    'rounded-md border border-line bg-page px-3 py-2 text-heading aria-invalid:border-danger disabled:cursor-not-allowed disabled:opacity-60',
  fieldError: 'text-[0.875em] text-danger',
  error: 'text-danger',
  notice: 'rounded-md border border-accent-line bg-accent-soft px-3 py-2 text-heading',
  formFooter: 'mt-4',

  primaryButton: 'cursor-pointer rounded-md bg-accent px-4 py-2.5 text-white disabled:cursor-default disabled:opacity-60',
  secondaryButton:
    'cursor-pointer rounded-md border border-line bg-page px-3.5 py-2 text-heading disabled:cursor-default disabled:opacity-60',
  dangerButton:
    'cursor-pointer rounded-md border border-danger bg-danger px-3.5 py-2 text-white disabled:cursor-default disabled:opacity-60',
  dangerOutlineButton: 'cursor-pointer rounded-md border border-danger px-2.5 py-1 text-danger',
  addLink: 'rounded-md bg-accent px-3.5 py-2 text-white',

  listPage: 'w-full px-4 pt-4 pb-8 text-left',
  listHeading: 'flex flex-wrap items-center justify-between gap-3',
  tableWrapper: 'overflow-x-auto',
  table,
  cell,
  actionsCell,

  listTable: `${table} ${card}`,
  listHead: 'max-md:hidden',
  listBody: card,
  listRow: 'max-md:mb-3 max-md:block max-md:rounded-md max-md:border max-md:border-line max-md:px-3 max-md:py-2',
  listCell: `${cell} ${cardCell}`,
  /** The first cell, the item's name: the card's bold title, without a label. */
  listTitleCell: `${cell} max-md:block max-md:border-0 max-md:px-0 max-md:py-1 max-md:font-semibold max-md:whitespace-normal max-md:text-heading`,
  listActionsCell: `${actionsCell} max-md:border-0 max-md:px-0 max-md:pt-2`,
}
