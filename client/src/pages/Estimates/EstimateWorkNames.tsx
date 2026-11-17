import type { WorkNames } from '../../interfaces/estimate'
import { ui } from '../../utils/ui'

/**
 * One work and its material names as a Viewer sees them (BR-005). Inside a section the work's name is a sub-heading.
 */
export default function EstimateWorkNames({ work, inSection }: { work: WorkNames; inSection: boolean }) {
  const Heading = inSection ? 'h3' : 'h2'
  return (
    <section className="mb-6">
      <Heading className={inSection ? 'mb-2 font-medium text-heading' : ui.h2}>{work.workName}</Heading>
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
  )
}
