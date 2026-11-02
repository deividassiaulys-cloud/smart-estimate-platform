import { ui } from '../../utils/ui'

interface MaterialSearchProps {
  value: string
  onChange: (value: string) => void
}

/** Search field for the material catalog; the page decides when to search. */
export default function MaterialSearch({ value, onChange }: MaterialSearchProps) {
  return (
    <div className="my-4 flex max-w-[420px] flex-col gap-1">
      <label htmlFor="material-search">Paieška pagal pavadinimą</label>
      <input
        id="material-search"
        type="search"
        className={ui.input}
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
    </div>
  )
}
