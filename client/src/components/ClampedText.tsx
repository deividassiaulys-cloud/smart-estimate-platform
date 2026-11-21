/**
 * A name shown on at most 2 lines, ending with "…" when longer (spec 020, FR-020-2). The full text stays in the page,
 * so screen readers read it, and it appears as a tooltip on hover.
 */
export default function ClampedText({ text }: { text: string }) {
  return (
    <span className="line-clamp-2 break-words whitespace-normal" title={text}>
      {text}
    </span>
  )
}
