import { useNavigate, useParams } from 'react-router-dom'
import { ui } from '../../utils/ui'
import CatalogSectionForm from './CatalogSectionForm'

/** Creates a catalog section, or edits one (including its whole list of works) when the route has an ID. */
export default function CatalogSectionFormPage() {
  const navigate = useNavigate()
  const { id } = useParams()
  const sectionId = id ? Number(id) : null

  return (
    <main className={ui.listPage}>
      <h1 className={ui.h1}>{sectionId === null ? 'Naujas skyrius' : 'Redaguoti skyrių'}</h1>
      <CatalogSectionForm
        sectionId={sectionId}
        onSaved={() =>
          navigate('/catalog-sections', { state: { notice: sectionId === null ? 'Skyrius sukurtas.' : 'Skyrius atnaujintas.' } })
        }
      />
    </main>
  )
}
