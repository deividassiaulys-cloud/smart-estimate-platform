import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getEstimateNames } from '../../api/estimates'
import { ApiError } from '../../api/http'
import type { EstimateNames } from '../../interfaces/estimate'
import { ui } from '../../utils/ui'

/**
 * The names-only view of an estimate for any user (FR-026): each work with its materials. The server sends
 * nothing else, so no price, quantity or status can appear here (BR-005).
 */
export default function EstimateViewPage() {
  const estimateId = Number(useParams().id)

  const [estimate, setEstimate] = useState<EstimateNames | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    getEstimateNames(estimateId)
      .then(setEstimate)
      .catch((err) =>
        setError(
          err instanceof ApiError && err.status === 404
            ? 'Sąmata nerasta.'
            : 'Nepavyko įkelti sąmatos. Bandykite dar kartą.',
        ),
      )
  }, [estimateId])

  return (
    <main className={ui.listPage}>
      <p>
        <Link className={ui.link} to="/projects">
          ← Projektai
        </Link>
      </p>

      {error && (
        <p className={ui.error} role="alert">
          {error}
        </p>
      )}
      {!estimate && !error && <p>Įkeliama…</p>}

      {estimate && (
        <>
          <h1 className={ui.h1}>{estimate.name}</h1>
          {estimate.works.length === 0 && <p>Sąmatoje dar nėra darbų.</p>}
          {estimate.works.map((work, index) => (
            <section key={index} className="mb-6">
              <h2 className={ui.h2}>{work.workName}</h2>
              {work.materialNames.length === 0 ? (
                <p>Darbas be medžiagų.</p>
              ) : (
                <ul className="list-disc pl-6">
                  {work.materialNames.map((name, materialIndex) => (
                    <li key={materialIndex}>{name}</li>
                  ))}
                </ul>
              )}
            </section>
          ))}
        </>
      )}
    </main>
  )
}
