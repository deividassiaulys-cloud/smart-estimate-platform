import { ui } from '../utils/ui'

interface SearchFieldProps {
  id: string
  label: string
  value: string
  onChange: (value: string) => void
}

/** Search field for a catalog page; the page decides when to search. */
export default function SearchField({ id, label, value, onChange }: SearchFieldProps) {
  return (
    <div className="my-4 flex max-w-[420px] flex-col gap-1">
      <label htmlFor={id}>{label}</label>
      <input id={id} type="search" className={ui.input} value={value} onChange={(event) => onChange(event.target.value)} />
    </div>
  )
}
