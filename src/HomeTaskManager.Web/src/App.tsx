import { useEffect, useMemo, useState } from 'react'
import './App.css'

type User = { id: string; displayName: string; isAdmin: boolean; isActive: boolean; availabilityPercentage: number }
type Template = { id: string; title: string; description?: string; priority: string }
type Instance = { id: string; taskTemplateId: string; plannedDate: string; dueDate: string; isCompleted: boolean; isCarryOver: boolean; assignedUserId?: string }

const demoUsers: User[] = [
  { id: 'julie', displayName: 'Julie', isAdmin: true, isActive: true, availabilityPercentage: 100 },
  { id: 'nicolas', displayName: 'Nicolas', isAdmin: false, isActive: true, availabilityPercentage: 80 },
]
const demoTemplates: Template[] = [
  { id: 'dishes', title: 'Vider le lave-vaisselle', description: 'Ranger la vaisselle propre avant le petit-déjeuner.', priority: 'Normal' },
  { id: 'plants', title: 'Arroser les plantes', description: 'Vérifier les plantes du salon et de la cuisine.', priority: 'Low' },
  { id: 'vacuum', title: 'Aspirateur', description: 'Passer l’aspirateur dans les pièces de vie.', priority: 'Critical' },
  { id: 'shopping', title: 'Courses de la semaine', description: 'Préparer et récupérer les courses.', priority: 'Normal' },
]
const demoInstances: Instance[] = [
  { id: 'i1', taskTemplateId: 'dishes', plannedDate: '2026-08-24', dueDate: '2026-08-24', isCompleted: true, isCarryOver: false, assignedUserId: 'julie' },
  { id: 'i2', taskTemplateId: 'plants', plannedDate: '2026-08-25', dueDate: '2026-08-30', isCompleted: false, isCarryOver: false, assignedUserId: 'julie' },
  { id: 'i3', taskTemplateId: 'vacuum', plannedDate: '2026-08-29', dueDate: '2026-08-30', isCompleted: false, isCarryOver: true, assignedUserId: 'julie' },
  { id: 'i4', taskTemplateId: 'shopping', plannedDate: '2026-08-27', dueDate: '2026-08-30', isCompleted: false, isCarryOver: false, assignedUserId: 'nicolas' },
]

function isoWeek(date = new Date()) {
  const value = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
  const day = value.getUTCDay() || 7
  value.setUTCDate(value.getUTCDate() + 4 - day)
  const start = new Date(Date.UTC(value.getUTCFullYear(), 0, 1))
  return { year: value.getUTCFullYear(), week: Math.ceil((((value.getTime() - start.getTime()) / 86400000) + 1) / 7) }
}

function App() {
  const [users, setUsers] = useState<User[]>([])
  const [templates, setTemplates] = useState<Template[]>([])
  const [instances, setInstances] = useState<Instance[]>([])
  const [currentUser, setCurrentUser] = useState<User | null>(null)
  const [view, setView] = useState('Semaine')
  const [filter, setFilter] = useState('all')
  const [selected, setSelected] = useState<Instance | null>(null)
  const [notice, setNotice] = useState('')
  const [loading, setLoading] = useState(true)
  const week = isoWeek()
  const api = import.meta.env.VITE_API_URL ?? ''

  useEffect(() => {
    const load = async () => {
      try {
        const responses = await Promise.all([fetch(`${api}/api/users`), fetch(`${api}/api/task-templates`), fetch(`${api}/api/weeks/${week.year}/${week.week}/instances`)]);
        if (responses.some((response) => !response.ok)) throw new Error('API unavailable')
        const [userResponse, templateResponse, instanceResponse] = responses
        const instanceData = await instanceResponse.json()
        setUsers(await userResponse.json()); setTemplates(await templateResponse.json()); setInstances(instanceData.instances ?? instanceData)
      } catch { setUsers(demoUsers); setTemplates(demoTemplates); setInstances(demoInstances) } finally { setLoading(false) }
    }
    load()
  }, [api, week.year, week.week])

  const visible = useMemo(() => {
    const mine = currentUser?.isAdmin ? instances : instances.filter((item) => item.assignedUserId === currentUser?.id)
    return filter === 'open' ? mine.filter((item) => !item.isCompleted) : filter === 'done' ? mine.filter((item) => item.isCompleted) : mine
  }, [currentUser, filter, instances])
  const completed = instances.filter((item) => item.isCompleted).length
  const progress = instances.length ? Math.round(completed / instances.length * 100) : 0
  const template = (id: string) => templates.find((item) => item.id === id)
  const user = (id?: string) => users.find((item) => item.id === id)

  async function toggle(item: Instance) {
    const next = !item.isCompleted
    setInstances((items) => items.map((entry) => entry.id === item.id ? { ...entry, isCompleted: next } : entry))
    setSelected((entry) => entry?.id === item.id ? { ...entry, isCompleted: next } : entry)
    setNotice(next ? 'Tâche marquée comme terminée' : 'Tâche rouverte')
    try { await fetch(`${api}/api/task-instances/${item.id}/completion`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ requestingUserId: currentUser?.id, isCompleted: next }) }) } catch { /* local demo mode */ }
    window.setTimeout(() => setNotice(''), 2200)
  }

  if (!currentUser) return <main className="profile-screen"><div className="profile-intro"><span className="eyebrow">HOME TASK MANAGER</span><h1>La maison tourne mieux quand la charge est partagée.</h1><p>Choisissez votre profil pour retrouver les tâches de cette semaine.</p></div><div className="profile-panel"><span className="section-kicker">ACCÈS FOYER</span><h2>Qui êtes-vous ?</h2>{loading ? <p className="muted">Chargement des profils...</p> : <div className="profile-list">{users.map((entry) => <button className="profile-choice" key={entry.id} onClick={() => setCurrentUser(entry)}><span className="avatar">{entry.displayName[0]}</span><span><strong>{entry.displayName}</strong><small>{entry.isAdmin ? 'Administrateur' : 'Participant'}</small></span><b>→</b></button>)}</div>}</div></main>

  return <main className="app-shell"><aside className="sidebar"><div className="brand"><span className="brand-mark">⌂</span><span>Home<br /><b>Task Manager</b></span></div><nav><button className={view === 'Semaine' ? 'nav-item active' : 'nav-item'} onClick={() => setView('Semaine')}>◷ <span>Ma semaine</span></button><button className={view === 'Historique' ? 'nav-item active' : 'nav-item'} onClick={() => setView('Historique')}>↗ <span>Historique</span></button>{currentUser.isAdmin && <><div className="nav-label">PILOTAGE</div><button className="nav-item" onClick={() => setView('Tâches')}>□ <span>Tâches</span></button><button className="nav-item" onClick={() => setView('Participants')}>♧ <span>Participants</span></button><button className="nav-item" onClick={() => setNotice('Le moteur est prêt pour cette semaine')}>✦ <span>Moteur d’affectation</span></button></>}</nav><div className="sidebar-footer"><div className="mini-profile"><span className="avatar small">{currentUser.displayName[0]}</span><span><strong>{currentUser.displayName}</strong><small>{currentUser.isAdmin ? 'Admin' : 'Participant'}</small></span></div><button className="logout" onClick={() => setCurrentUser(null)} title="Changer de profil">↪</button></div></aside><section className="content"><header className="topbar"><div><span className="breadcrumb">TABLEAU DE BORD <i>/</i> SEMAINE ACTIVE</span><h1>{view}</h1></div><div className="week-switcher"><button aria-label="Semaine précédente">←</button><span><b>Semaine {week.week}</b><small>18 - 24 août 2026</small></span><button aria-label="Semaine suivante">→</button></div></header>{notice && <div className="notice">✓ {notice}</div>}{view === 'Semaine' ? <><section className="hero-banner"><div><span className="section-kicker light">VOTRE FOYER · SEMAINE {week.week}</span><h2>On avance, une tâche après l’autre.</h2><p>{instances.length - completed} tâches restent à faire. La répartition est en place.</p></div><div className="progress-ring" style={{ '--progress': `${progress * 3.6}deg` } as React.CSSProperties}><strong>{progress}<small>%</small></strong><span>complété</span></div></section><section className="stats-row"><div className="stat"><span className="stat-icon coral">✓</span><span><b>{completed}</b><small>Terminées</small></span></div><div className="stat"><span className="stat-icon yellow">○</span><span><b>{instances.length - completed}</b><small>À faire</small></span></div><div className="stat"><span className="stat-icon teal">↗</span><span><b>{users.length}</b><small>Participants actifs</small></span></div><div className="stat"><span className="stat-icon lavender">8</span><span><b>8 sem.</b><small>Fenêtre d’équité</small></span></div></section><div className="workspace-grid"><section className="tasks-panel"><div className="panel-heading"><div><span className="section-kicker">VUE OPÉRATIONNELLE</span><h2>{currentUser.isAdmin ? 'Toutes les tâches' : 'Mes tâches'}</h2></div><button className="primary-button" onClick={() => setNotice('Création de tâche disponible dans la prochaine étape')}>+ Nouvelle tâche</button></div><div className="filter-row">{[['all', 'Toutes'], ['open', 'À faire'], ['done', 'Terminées']].map(([value, label]) => <button className={filter === value ? 'filter active' : 'filter'} key={value} onClick={() => setFilter(value)}>{label}</button>)}<span className="task-count">{visible.length} éléments</span></div><div className="task-list">{visible.map((item) => <article className={item.isCompleted ? 'task completed' : 'task'} key={item.id}><button className="check-button" onClick={() => toggle(item)} aria-label="Modifier la complétude">{item.isCompleted ? '✓' : ''}</button><div className="task-main"><div className="task-title-line"><h3>{template(item.taskTemplateId)?.title ?? 'Tâche sans titre'}</h3>{item.isCarryOver && <span className="tag carry">REPORTÉE</span>}{template(item.taskTemplateId)?.priority === 'Critical' && <span className="tag critical">CRITIQUE</span>}</div><p>{template(item.taskTemplateId)?.description}</p><div className="task-meta">◷ Pour le {new Date(item.dueDate).toLocaleDateString('fr-FR', { day: 'numeric', month: 'short' })}{currentUser.isAdmin && <> · {user(item.assignedUserId)?.displayName}</>}</div></div><button className="detail-button" onClick={() => setSelected(item)} aria-label="Voir le détail">···</button></article>)}</div></section><aside className="insight-panel"><div className="panel-heading"><span className="section-kicker">ÉQUILIBRE DE LA CHARGE</span><h2>La répartition en bref</h2></div>{users.map((entry, index) => { const count = instances.filter((item) => item.assignedUserId === entry.id).length; return <div className="balance" key={entry.id}><div className="balance-name"><span className={`avatar ${index ? 'blue' : ''}`}>{entry.displayName[0]}</span><b>{entry.displayName}</b><span>{count} tâches</span></div><div className="bar"><i style={{ width: `${Math.max(12, count / Math.max(instances.length, 1) * 100)}%` }} /></div><small>{entry.availabilityPercentage}% de disponibilité</small></div>})}<div className="why-box"><span>✦</span><div><b>Pourquoi cette tâche ?</b><p>Le moteur équilibre les points, la disponibilité et la rotation sur les 8 dernières semaines.</p></div></div></aside></div></> : <section className="empty-view"><span className="section-kicker">{view.toUpperCase()}</span><h2>Cette vue arrive juste après la semaine active.</h2><p>La navigation et les permissions sont déjà prêtes pour l’ajouter au prochain écran.</p></section>}</section>{selected && <div className="modal-backdrop" onClick={() => setSelected(null)}><section className="task-modal" onClick={(event) => event.stopPropagation()}><button className="modal-close" onClick={() => setSelected(null)}>×</button><span className="section-kicker">DÉTAIL DE L’AFFECTATION</span><h2>{template(selected.taskTemplateId)?.title}</h2><p>{template(selected.taskTemplateId)?.description}</p><div className="explanation"><span>✦</span><div><b>Une affectation pensée pour l’équilibre</b><p>Cette tâche tient compte de la charge actuelle, de votre disponibilité et de la rotation observée sur les dernières semaines.</p></div></div><button className="primary-button wide" onClick={() => toggle(selected)}>{selected.isCompleted ? 'Rouvrir la tâche' : 'Marquer comme terminée'}</button></section></div>}</main>
}

export default App
