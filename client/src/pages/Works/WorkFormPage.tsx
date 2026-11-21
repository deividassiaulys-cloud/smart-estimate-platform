import { Link, useNavigate, useParams } from 'react-router-dom'
import { ui } from '../../utils/ui'
import WorkForm from './WorkForm'

/** Creates a work, or edits one (including its whole composition) when the route has an ID. */
export default function WorkFormPage() {
  const navigate = useNavigate()
  const { id } = useParams()
  const workId = id ? Number(id) : null

  return (
    <main className={ui.listPage}>
      <h1 className={ui.h1}>{workId === null ? 'Naujas darbas' : 'Redaguoti darbą'}</h1>
      <WorkForm
        workId={workId}
        onSaved={() => navigate('/works', { state: { notice: workId === null ? 'Darbas sukurtas.' : 'Darbas atnaujintas.' } })}
      />
      <p className={ui.formFooter}>
        <Link className={ui.link} to="/works">
          Atšaukti
        </Link>
      </p>
    </main>
  )
}
