/**
 * The message for a failure no page handles specifically: in Lithuanian, saying what could not be done, with no
 * technical details or server text (NFR-010). The server's own error texts are never shown to users.
 */
export function unexpectedError(action: string): string {
  return `Nepavyko ${action}. Bandykite dar kartą.`
}
