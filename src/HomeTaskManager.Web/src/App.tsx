import { useEffect, useMemo, useState, type FormEvent } from "react";
import "./App.css";

type User = {
  id: string;
  displayName: string;
  isAdmin: boolean;
  isActive: boolean;
  availabilityPercentage: number;
};
type Template = {
  id: string;
  title: string;
  description?: string;
  priority: string;
  frequencyType?: string;
  manualDifficultyScore?: number;
  estimatedDurationMinutes?: number;
};
type Instance = {
  id: string;
  taskTemplateId: string;
  dueDate: string;
  isCompleted: boolean;
  isCarryOver: boolean;
  assignedUserId?: string;
};
type AuditLog = { id: string; entityType: string; entityId: string; action: string; performedByUserId: string; oldValue?: string; newValue?: string; performedAt: string };

type TaskFormProps = {
  api: string;
  user: User;
  onCreated: (template: Template) => void;
};

const demoUsers: User[] = [
  {
    id: "julie",
    displayName: "Julie",
    isAdmin: true,
    isActive: true,
    availabilityPercentage: 100,
  },
  {
    id: "nicolas",
    displayName: "Nicolas",
    isAdmin: false,
    isActive: true,
    availabilityPercentage: 80,
  },
];
const demoTemplates: Template[] = [
  {
    id: "dishes",
    title: "Vider le lave-vaisselle",
    description: "Ranger la vaisselle propre avant le petit-déjeuner.",
    priority: "Normal",
  },
  {
    id: "plants",
    title: "Arroser les plantes",
    description: "Vérifier les plantes du salon et de la cuisine.",
    priority: "Low",
  },
  {
    id: "vacuum",
    title: "Aspirateur",
    description: "Passer l’aspirateur dans les pièces de vie.",
    priority: "Critical",
  },
  {
    id: "shopping",
    title: "Courses de la semaine",
    description: "Préparer et récupérer les courses.",
    priority: "Normal",
  },
];
const demoInstances: Instance[] = [
  {
    id: "i1",
    taskTemplateId: "dishes",
    dueDate: "2026-08-24",
    isCompleted: true,
    isCarryOver: false,
    assignedUserId: "julie",
  },
  {
    id: "i2",
    taskTemplateId: "plants",
    dueDate: "2026-08-30",
    isCompleted: false,
    isCarryOver: false,
    assignedUserId: "julie",
  },
  {
    id: "i3",
    taskTemplateId: "vacuum",
    dueDate: "2026-08-30",
    isCompleted: false,
    isCarryOver: true,
    assignedUserId: "julie",
  },
  {
    id: "i4",
    taskTemplateId: "shopping",
    dueDate: "2026-08-30",
    isCompleted: false,
    isCarryOver: false,
    assignedUserId: "nicolas",
  },
];

function isoWeek(date = new Date()) {
  const value = new Date(
    Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()),
  );
  const day = value.getUTCDay() || 7;
  value.setUTCDate(value.getUTCDate() + 4 - day);
  const start = new Date(Date.UTC(value.getUTCFullYear(), 0, 1));
  return {
    year: value.getUTCFullYear(),
    week: Math.ceil(((value.getTime() - start.getTime()) / 86400000 + 1) / 7),
  };
}

function weekLabel(offset: number) {
  const monday = new Date();
  monday.setHours(0, 0, 0, 0);
  const day = monday.getDay() || 7;
  monday.setDate(monday.getDate() - day + 1 + offset * 7);
  const sunday = new Date(monday);
  sunday.setDate(monday.getDate() + 6);
  return `${monday.toLocaleDateString("fr-FR", { day: "numeric", month: "short" })} - ${sunday.toLocaleDateString("fr-FR", { day: "numeric", month: "short" })}`;
}

const viewRoutes: Record<string, string> = {
  "/week": "Semaine",
  "/history": "Historique",
  "/tasks": "Tâches",
  "/participants": "Participants",
  "/assignment-engine": "Moteur d’affectation",
};

function viewFromPath(pathname: string) {
  return viewRoutes[pathname] ?? "Semaine";
}

function CreateTaskDialog({ api, user, onCreated }: TaskFormProps) {
  const [open, setOpen] = useState(false);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState("Normal");
  const [frequencyType, setFrequencyType] = useState("Weekly");
  const [difficulty, setDifficulty] = useState("50");
  const [duration, setDuration] = useState("30");
  const [error, setError] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    const score = Number(difficulty);
    if (!title.trim()) return setError("Le titre est obligatoire.");
    if (!Number.isInteger(score) || score < 1 || score > 100)
      return setError("La pénibilité doit être comprise entre 1 et 100.");
    const payload = {
      title: title.trim(),
      description: description.trim() || null,
      manualDifficultyScore: score,
      frequencyType,
      priority,
      estimatedDurationMinutes: Number(duration) || null,
      createdByUserId: user.id,
    };
    try {
      const response = await fetch(`${api}/api/task-templates`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });
      if (!response.ok)
        throw new Error((await response.json()).error ?? "Création impossible");
      onCreated(await response.json());
    } catch (reason) {
      if (api)
        return setError(
          reason instanceof Error ? reason.message : "Création impossible.",
        );
      onCreated({
        id: `local-${Date.now()}`,
        title: payload.title,
        description: payload.description ?? "",
        priority,
      });
    }
    setOpen(false);
    setTitle("");
    setDescription("");
    setError("");
  }

  return (
    <>
      <button className="primary-button" onClick={() => setOpen(true)}>
        + Nouvelle tâche
      </button>
      {open && (
        <div className="modal-backdrop" onClick={() => setOpen(false)}>
          <form
            className="task-modal form-modal"
            onSubmit={submit}
            onClick={(event) => event.stopPropagation()}
          >
            <button
              type="button"
              className="modal-close"
              onClick={() => setOpen(false)}
            >
              ×
            </button>
            <span className="section-kicker">NOUVEAU MODÈLE</span>
            <h2>Ajouter une tâche</h2>
            <label>
              Titre
              <input
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                autoFocus
              />
            </label>
            <label>
              Description
              <textarea
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                rows={2}
              />
            </label>
            <div className="form-grid">
              <label>
                Priorité
                <select
                  value={priority}
                  onChange={(event) => setPriority(event.target.value)}
                >
                  <option value="Low">Faible</option>
                  <option value="Normal">Normale</option>
                  <option value="Critical">Critique</option>
                </select>
              </label>
              <label>
                Fréquence
                <select
                  value={frequencyType}
                  onChange={(event) => setFrequencyType(event.target.value)}
                >
                  <option value="DailyMandatory">Quotidienne</option>
                  <option value="DailyFlexible">Flexible</option>
                  <option value="Weekly">Hebdomadaire</option>
                  <option value="Monthly">Mensuelle</option>
                  <option value="OneTime">Ponctuelle</option>
                </select>
              </label>
              <label>
                Pénibilité
                <input
                  type="number"
                  min="1"
                  max="100"
                  value={difficulty}
                  onChange={(event) => setDifficulty(event.target.value)}
                />
              </label>
              <label>
                Durée (min)
                <input
                  type="number"
                  min="1"
                  value={duration}
                  onChange={(event) => setDuration(event.target.value)}
                />
              </label>
            </div>
            {error && <p className="form-error">{error}</p>}
            <button className="primary-button wide" type="submit">
              Créer la tâche
            </button>
          </form>
        </div>
      )}
    </>
  );
}

function EditTaskDialog({ api, user, template, onUpdated }: { api: string; user: User; template: Template; onUpdated: (template: Template) => void }) {
  const [open, setOpen] = useState(false);
  const [title, setTitle] = useState(template.title);
  const [description, setDescription] = useState(template.description ?? "");
  const [priority, setPriority] = useState(template.priority || "Normal");
  const [frequencyType, setFrequencyType] = useState(template.frequencyType || "Weekly");
  const [difficulty, setDifficulty] = useState(String(template.manualDifficultyScore ?? 50));
  const [duration, setDuration] = useState(String(template.estimatedDurationMinutes ?? 30));
  const [error, setError] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    const score = Number(difficulty);
    if (!title.trim()) return setError("Le titre est obligatoire.");
    if (!Number.isInteger(score) || score < 1 || score > 100) return setError("La pénibilité doit être comprise entre 1 et 100.");
    const payload = { title: title.trim(), description: description.trim() || null, manualDifficultyScore: score, priority, frequencyType, estimatedDurationMinutes: Number(duration) || null, requestingUserId: user.id };
    try {
      const response = await fetch(`${api}/api/task-templates/${template.id}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload) });
      if (!response.ok) throw new Error((await response.json()).error ?? "Modification impossible");
      onUpdated(await response.json());
    } catch (reason) {
      if (api) return setError(reason instanceof Error ? reason.message : "Modification impossible.");
      onUpdated({ ...template, title: payload.title, description: payload.description ?? "", priority, frequencyType, manualDifficultyScore: score, estimatedDurationMinutes: payload.estimatedDurationMinutes ?? undefined });
    }
    setOpen(false); setError("");
  }

  return <><button className="secondary-button" onClick={() => setOpen(true)}>Modifier</button>{open && <div className="modal-backdrop" onClick={() => setOpen(false)}><form className="task-modal form-modal" onSubmit={submit} onClick={(event) => event.stopPropagation()}><button type="button" className="modal-close" onClick={() => setOpen(false)}>×</button><span className="section-kicker">MODIFIER LE MODÈLE</span><h2>{template.title}</h2><label>Titre<input value={title} onChange={(event) => setTitle(event.target.value)} autoFocus /></label><label>Description<textarea value={description} onChange={(event) => setDescription(event.target.value)} rows={2} /></label><div className="form-grid"><label>Priorité<select value={priority} onChange={(event) => setPriority(event.target.value)}><option value="Low">Faible</option><option value="Normal">Normale</option><option value="Critical">Critique</option></select></label><label>Fréquence<select value={frequencyType} onChange={(event) => setFrequencyType(event.target.value)}><option value="DailyMandatory">Quotidienne</option><option value="DailyFlexible">Flexible</option><option value="Weekly">Hebdomadaire</option><option value="Monthly">Mensuelle</option><option value="OneTime">Ponctuelle</option></select></label><label>Pénibilité<input type="number" min="1" max="100" value={difficulty} onChange={(event) => setDifficulty(event.target.value)} /></label><label>Durée (min)<input type="number" min="1" value={duration} onChange={(event) => setDuration(event.target.value)} /></label></div>{error && <p className="form-error">{error}</p>}<button className="primary-button wide" type="submit">Enregistrer</button></form></div>}</>;
}

function TaskCatalog({
  templates,
  user,
  api,
  onCreated,
  onUpdated,
}: {
  templates: Template[];
  user: User;
  api: string;
  onCreated: (template: Template) => void;
  onUpdated: (template: Template) => void;
}) {
  const [query, setQuery] = useState("");
  const filtered = templates.filter((template) =>
    template.title.toLowerCase().includes(query.toLowerCase()),
  );
  function updateTemplate(updated: Template) {
    onUpdated(updated);
  }

  return (
    <section className="catalog-panel">
      <div className="panel-heading">
        <div>
          <span className="section-kicker">PILOTAGE</span>
          <h2>Modèles de tâches</h2>
        </div>
        <CreateTaskDialog api={api} user={user} onCreated={onCreated} />
      </div>
      <div className="catalog-toolbar">
        <input
          aria-label="Rechercher une tâche"
          placeholder="Rechercher une tâche"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
        />
        <span>{filtered.length} modèles actifs</span>
      </div>
      <div className="catalog-list">
        {filtered.map((template) => (
          <article className="catalog-item" key={template.id}>
            <div>
              <h3>{template.title}</h3>
              <p>{template.description || "Aucune description"}</p>
            </div>
            <div className="catalog-meta">
              <span
                className={`tag ${template.priority === "Critical" ? "critical" : "carry"}`}
              >
                {template.priority === "Critical"
                  ? "CRITIQUE"
                  : template.priority === "Low"
                    ? "FAIBLE"
                    : "NORMALE"}
              </span>
              <small>Actif</small>
              <EditTaskDialog api={api} user={user} template={template} onUpdated={updateTemplate} />
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}

function ProfileCatalog({ users, api, onCreated }: { users: User[]; api: string; onCreated: (user: User) => void }) {
  const [open, setOpen] = useState(false)
  const [displayName, setDisplayName] = useState("")
  const [isAdmin, setIsAdmin] = useState(false)
  const [availability, setAvailability] = useState("100")
  const [error, setError] = useState("")

  async function submit(event: FormEvent) {
    event.preventDefault()
    const percentage = Number(availability)
    if (!displayName.trim()) return setError("Le nom d’affichage est obligatoire.")
    if (!Number.isInteger(percentage) || percentage < 0 || percentage > 100) return setError("La disponibilité doit être comprise entre 0 et 100.")
    try {
      const response = await fetch(`${api}/api/users`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ displayName: displayName.trim(), isAdmin, availabilityPercentage: percentage }) })
      if (!response.ok) throw new Error((await response.json()).error ?? "Création impossible")
      onCreated(await response.json())
    } catch (reason) {
      if (api) return setError(reason instanceof Error ? reason.message : "Création impossible.")
      onCreated({ id: `local-${Date.now()}`, displayName: displayName.trim(), isAdmin, isActive: true, availabilityPercentage: percentage })
    }
    setOpen(false); setDisplayName(""); setError("")
  }

  return <section className="catalog-panel"><div className="panel-heading"><div><span className="section-kicker">PILOTAGE</span><h2>Participants</h2></div><button className="primary-button" onClick={() => setOpen(true)}>+ Nouveau profil</button></div><div className="profile-catalog-list">{users.map((user) => <article className="profile-catalog-item" key={user.id}><span className={`avatar ${user.isAdmin ? "" : "blue"}`}>{user.displayName[0]}</span><div><h3>{user.displayName}</h3><p>{user.isAdmin ? "Administrateur" : "Participant"}</p></div><div className="profile-catalog-meta"><b>{user.availabilityPercentage}%</b><small>{user.isActive ? "Actif" : "Inactif"}</small></div></article>)}</div>{open && <div className="modal-backdrop" onClick={() => setOpen(false)}><form className="task-modal form-modal" onSubmit={submit} onClick={(event) => event.stopPropagation()}><button type="button" className="modal-close" onClick={() => setOpen(false)}>×</button><span className="section-kicker">NOUVEAU PROFIL</span><h2>Ajouter un participant</h2><label>Nom d’affichage<input value={displayName} onChange={(event) => setDisplayName(event.target.value)} autoFocus /></label><label>Disponibilité (%)<input type="number" min="0" max="100" value={availability} onChange={(event) => setAvailability(event.target.value)} /></label><label className="checkbox-label"><input type="checkbox" checked={isAdmin} onChange={(event) => setIsAdmin(event.target.checked)} /> Administrateur</label>{error && <p className="form-error">{error}</p>}<button className="primary-button wide" type="submit">Créer le profil</button></form></div>}</section>
}

type EngineSettings = { lowPriorityMultiplier: number; normalPriorityMultiplier: number; criticalPriorityMultiplier: number; referenceDurationMinutes: number; equityWindowWeeks: number; repetitionPenaltyWeight: number }

function EngineSettingsPanel({ api, user, onSaved }: { api: string; user: User; onSaved: () => void }) {
  const [settings, setSettings] = useState<EngineSettings>({ lowPriorityMultiplier: 0.5, normalPriorityMultiplier: 1, criticalPriorityMultiplier: 1.5, referenceDurationMinutes: 30, equityWindowWeeks: 8, repetitionPenaltyWeight: 0.25 })
  const [error, setError] = useState("")
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    fetch(`${api}/api/settings/assignment-engine`).then(async (response) => { if (!response.ok) throw new Error("Lecture impossible") ; setSettings(await response.json()) }).catch(() => undefined)
  }, [api])

  function update(key: keyof EngineSettings, value: string) {
    setSettings((current) => ({ ...current, [key]: Number(value) }))
    setSaved(false)
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (settings.referenceDurationMinutes < 1 || settings.equityWindowWeeks < 1 || settings.repetitionPenaltyWeight < 0) return setError("Les valeurs doivent rester positives.")
    try {
      const response = await fetch(`${api}/api/settings/assignment-engine`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ ...settings, requestingUserId: user.id }) })
      if (!response.ok) throw new Error((await response.json()).error ?? "Enregistrement impossible")
      setSettings(await response.json()); setSaved(true); onSaved()
    } catch (reason) {
      if (api) return setError(reason instanceof Error ? reason.message : "Enregistrement impossible.")
      setSaved(true); onSaved()
    }
  }

  return <form className="settings-panel" onSubmit={submit}><div className="panel-heading"><div><span className="section-kicker">CONFIGURATION ADMIN</span><h2>Moteur d’affectation</h2></div><button className="primary-button" type="submit">Enregistrer</button></div><p className="settings-intro">Ces paramètres pilotent le score des tâches et l’équilibre de la charge pour les prochaines affectations.</p><div className="settings-section"><span className="section-kicker">MULTIPLICATEURS DE PRIORITÉ</span><div className="settings-grid"><label>Faible<input type="number" min="0" step="0.05" value={settings.lowPriorityMultiplier} onChange={(event) => update("lowPriorityMultiplier", event.target.value)} /></label><label>Normale<input type="number" min="0" step="0.05" value={settings.normalPriorityMultiplier} onChange={(event) => update("normalPriorityMultiplier", event.target.value)} /></label><label>Critique<input type="number" min="0" step="0.05" value={settings.criticalPriorityMultiplier} onChange={(event) => update("criticalPriorityMultiplier", event.target.value)} /></label></div></div><div className="settings-section"><span className="section-kicker">ÉQUILIBRE HISTORIQUE</span><div className="settings-grid"><label>Durée de référence (minutes)<input type="number" min="1" value={settings.referenceDurationMinutes} onChange={(event) => update("referenceDurationMinutes", event.target.value)} /></label><label>Fenêtre d’équité (semaines)<input type="number" min="1" value={settings.equityWindowWeeks} onChange={(event) => update("equityWindowWeeks", event.target.value)} /></label><label>Pénalité anti-répétition<input type="number" min="0" step="0.05" value={settings.repetitionPenaltyWeight} onChange={(event) => update("repetitionPenaltyWeight", event.target.value)} /></label></div></div>{error && <p className="form-error">{error}</p>}{saved && <p className="settings-saved">✓ Paramètres enregistrés</p>}</form>
}

function HistoryPanel({ api, users }: { api: string; users: User[] }) {
  const [entries, setEntries] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    fetch(`${api}/api/audit-logs`).then(async (response) => { if (!response.ok) throw new Error("Lecture impossible"); setEntries(await response.json()); }).catch(() => undefined).finally(() => setLoading(false));
  }, [api]);
  const actor = (id: string) => users.find((user) => user.id === id)?.displayName ?? "Participant";
  const actionLabel = (action: string) => ({ Created: "créé", Updated: "modifié", Completed: "terminé", Reopened: "rouvert", AssignmentRun: "répartition lancée", WeekClosed: "semaine clôturée" }[action] ?? action.toLowerCase());
  const entityLabel = (type: string) => type === "TaskTemplate" ? "modèle de tâche" : type === "TaskInstance" ? "instance" : type.toLowerCase();
  return <section className="history-panel"><div className="panel-heading"><div><span className="section-kicker">TRAÇABILITÉ</span><h2>Historique d’activité</h2></div><span className="history-count">{entries.length} événements</span></div>{loading ? <p className="muted">Chargement de l’historique...</p> : entries.length === 0 ? <div className="history-empty"><span>◷</span><h3>Aucune activité enregistrée</h3><p>Les créations, affectations et changements de complétude apparaîtront ici.</p></div> : <div className="history-list">{entries.map((entry) => <article className="history-item" key={entry.id}><span className="history-dot" /><div><p><b>{actor(entry.performedByUserId)}</b> a {actionLabel(entry.action)} un {entityLabel(entry.entityType)}</p>{entry.newValue && <small>{entry.newValue}</small>}<time>{new Date(entry.performedAt).toLocaleString("fr-FR", { dateStyle: "medium", timeStyle: "short" })}</time></div></article>)}</div>}</section>;
}

function App() {
  const [users, setUsers] = useState<User[]>([]);
  const [templates, setTemplates] = useState<Template[]>([]);
  const [instances, setInstances] = useState<Instance[]>([]);
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [view, setView] = useState(() => viewFromPath(window.location.pathname));
  const [filter, setFilter] = useState("all");
  const [selected, setSelected] = useState<Instance | null>(null);
  const [notice, setNotice] = useState("");
  const [loading, setLoading] = useState(true);
  const [weekOffset, setWeekOffset] = useState(0);
  const week = isoWeek(new Date(Date.now() + weekOffset * 7 * 86400000));
  const api = import.meta.env.VITE_API_URL ?? "";

  useEffect(() => {
    const onPopState = () => setView(viewFromPath(window.location.pathname));
    window.addEventListener("popstate", onPopState);
    return () => window.removeEventListener("popstate", onPopState);
  }, []);

  function navigate(path: string) {
    window.history.pushState({}, "", path);
    setView(viewFromPath(path));
    setSelected(null);
  }

  function selectProfile(user: User) {
    setCurrentUser(user);
    navigate("/week");
  }

  useEffect(() => {
    const load = async () => {
      try {
        const responses = await Promise.all([
          fetch(`${api}/api/users`),
          fetch(`${api}/api/task-templates`),
          fetch(`${api}/api/weeks/${week.year}/${week.week}/instances`),
        ]);
        if (responses.some((response) => !response.ok))
          throw new Error("API unavailable");
        const [userResponse, templateResponse, instanceResponse] = responses;
        const instanceData = await instanceResponse.json();
        setUsers(await userResponse.json());
        setTemplates(await templateResponse.json());
        setInstances(instanceData.instances ?? instanceData);
      } catch {
        setUsers(demoUsers);
        setTemplates(demoTemplates);
        setInstances(demoInstances);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [api, week.year, week.week]);

  function changeWeek(offset: number) {
    setWeekOffset((current) => current + offset);
    setNotice("Chargement de la semaine sélectionnée");
    window.setTimeout(() => setNotice(""), 1600);
  }

  const visible = useMemo(() => {
    const mine = currentUser?.isAdmin
      ? instances
      : instances.filter((item) => item.assignedUserId === currentUser?.id);
    return filter === "open"
      ? mine.filter((item) => !item.isCompleted)
      : filter === "done"
        ? mine.filter((item) => item.isCompleted)
        : mine;
  }, [currentUser, filter, instances]);
  const completed = instances.filter((item) => item.isCompleted).length;
  const progress = instances.length
    ? Math.round((completed / instances.length) * 100)
    : 0;
  const template = (id: string) => templates.find((item) => item.id === id);
  const user = (id?: string) => users.find((item) => item.id === id);

  async function toggle(item: Instance) {
    const next = !item.isCompleted;
    setInstances((items) =>
      items.map((entry) =>
        entry.id === item.id ? { ...entry, isCompleted: next } : entry,
      ),
    );
    setSelected((entry) =>
      entry?.id === item.id ? { ...entry, isCompleted: next } : entry,
    );
    setNotice(next ? "Tâche marquée comme terminée" : "Tâche rouverte");
    try {
      await fetch(`${api}/api/task-instances/${item.id}/completion`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          requestingUserId: currentUser?.id,
          isCompleted: next,
        }),
      });
    } catch {
      /* local demo mode */
    }
    window.setTimeout(() => setNotice(""), 2200);
  }

  if (!currentUser)
    return (
      <main className="profile-screen">
        <div className="profile-intro">
          <span className="eyebrow">HOME TASK MANAGER</span>
          <h1>La maison tourne mieux quand la charge est partagée.</h1>
          <p>
            Choisissez votre profil pour retrouver les tâches de cette semaine.
          </p>
        </div>
        <div className="profile-panel">
          <span className="section-kicker">ACCÈS FOYER</span>
          <h2>Qui êtes-vous ?</h2>
          {loading ? (
            <p className="muted">Chargement des profils...</p>
          ) : (
            <div className="profile-list">
              {users.map((entry) => (
                <button
                  className="profile-choice"
                  key={entry.id}
                  onClick={() => selectProfile(entry)}
                >
                  <span className="avatar">{entry.displayName[0]}</span>
                  <span>
                    <strong>{entry.displayName}</strong>
                    <small>
                      {entry.isAdmin ? "Administrateur" : "Participant"}
                    </small>
                  </span>
                  <b>→</b>
                </button>
              ))}
            </div>
          )}
        </div>
      </main>
    );

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark">⌂</span>
          <span>
            Home
            <br />
            <b>Task Manager</b>
          </span>
        </div>
        <nav>
          <button
            className={view === "Semaine" ? "nav-item active" : "nav-item"}
            onClick={() => navigate("/week")}
          >
            ◷ <span>Ma semaine</span>
          </button>
          <button
            className={view === "Historique" ? "nav-item active" : "nav-item"}
            onClick={() => navigate("/history")}
          >
            ↗ <span>Historique</span>
          </button>
          {currentUser.isAdmin && (
            <>
              <div className="nav-label">PILOTAGE</div>
              <button className={view === "Tâches" ? "nav-item active" : "nav-item"} onClick={() => navigate("/tasks")}>
                □ <span>Tâches</span>
              </button>
              <button
                className={view === "Participants" ? "nav-item active" : "nav-item"}
                onClick={() => navigate("/participants")}
              >
                ♧ <span>Participants</span>
              </button>
              <button
                className={view === "Moteur d’affectation" ? "nav-item active" : "nav-item"}
                onClick={() => navigate("/assignment-engine")}
              >
                ✦ <span>Moteur d’affectation</span>
              </button>
            </>
          )}
        </nav>
        <div className="sidebar-footer">
          <div className="mini-profile">
            <span className="avatar small">{currentUser.displayName[0]}</span>
            <span>
              <strong>{currentUser.displayName}</strong>
              <small>{currentUser.isAdmin ? "Admin" : "Participant"}</small>
            </span>
          </div>
          <button
            className="logout"
            onClick={() => { setCurrentUser(null); navigate("/"); }}
            title="Changer de profil"
          >
            ↪
          </button>
        </div>
      </aside>
      <section className="content">
        <header className="topbar">
          <div>
            <span className="breadcrumb">
              TABLEAU DE BORD <i>/</i> SEMAINE ACTIVE
            </span>
            <h1>{view}</h1>
          </div>
          <div className="week-switcher">
            <button aria-label="Semaine précédente" onClick={() => changeWeek(-1)}>←</button>
            <span>
              <b>Semaine {week.week}</b>
              <small>{weekLabel(weekOffset)}</small>
            </span>
            <button aria-label="Semaine suivante" onClick={() => changeWeek(1)}>→</button>
          </div>
        </header>
        {notice && <div className="notice">✓ {notice}</div>}
        {view === "Semaine" ? (
          <>
            <section className="hero-banner">
              <div>
                <span className="section-kicker light">
                  VOTRE FOYER · SEMAINE {week.week}
                </span>
                <h2>On avance, une tâche après l’autre.</h2>
                <p>
                  {instances.length - completed} tâches restent à faire. La
                  répartition est en place.
                </p>
              </div>
              <div
                className="progress-ring"
                style={
                  {
                    "--progress": `${progress * 3.6}deg`,
                  } as React.CSSProperties
                }
              >
                <strong>
                  {progress}
                  <small>%</small>
                </strong>
                <span>complété</span>
              </div>
            </section>
            <section className="stats-row">
              <div className="stat">
                <span className="stat-icon coral">✓</span>
                <span>
                  <b>{completed}</b>
                  <small>Terminées</small>
                </span>
              </div>
              <div className="stat">
                <span className="stat-icon yellow">○</span>
                <span>
                  <b>{instances.length - completed}</b>
                  <small>À faire</small>
                </span>
              </div>
              <div className="stat">
                <span className="stat-icon teal">↗</span>
                <span>
                  <b>{users.length}</b>
                  <small>Participants actifs</small>
                </span>
              </div>
              <div className="stat">
                <span className="stat-icon lavender">8</span>
                <span>
                  <b>8 sem.</b>
                  <small>Fenêtre d’équité</small>
                </span>
              </div>
            </section>
            <div className="workspace-grid">
              <section className="tasks-panel">
                <div className="panel-heading">
                  <div>
                    <span className="section-kicker">VUE OPÉRATIONNELLE</span>
                    <h2>
                      {currentUser.isAdmin ? "Toutes les tâches" : "Mes tâches"}
                    </h2>
                  </div>
                  <CreateTaskDialog
                    api={api}
                    user={currentUser}
                    onCreated={(created) => {
                      setTemplates((items) => [...items, created]);
                      setNotice("Tâche créée pour les prochaines affectations");
                    }}
                  />
                </div>
                <div className="filter-row">
                  {[
                    ["all", "Toutes"],
                    ["open", "À faire"],
                    ["done", "Terminées"],
                  ].map(([value, label]) => (
                    <button
                      className={filter === value ? "filter active" : "filter"}
                      key={value}
                      onClick={() => setFilter(value)}
                    >
                      {label}
                    </button>
                  ))}
                  <span className="task-count">{visible.length} éléments</span>
                </div>
                <div className="task-list">
                  {visible.map((item) => (
                    <article
                      className={item.isCompleted ? "task completed" : "task"}
                      key={item.id}
                    >
                      <button
                        className="check-button"
                        onClick={() => toggle(item)}
                        aria-label="Modifier la complétude"
                      >
                        {item.isCompleted ? "✓" : ""}
                      </button>
                      <div className="task-main">
                        <div className="task-title-line">
                          <h3>
                            {template(item.taskTemplateId)?.title ??
                              "Tâche sans titre"}
                          </h3>
                          {item.isCarryOver && (
                            <span className="tag carry">REPORTÉE</span>
                          )}
                          {template(item.taskTemplateId)?.priority ===
                            "Critical" && (
                            <span className="tag critical">CRITIQUE</span>
                          )}
                        </div>
                        <p>{template(item.taskTemplateId)?.description}</p>
                        <div className="task-meta">
                          ◷ Pour le{" "}
                          {new Date(item.dueDate).toLocaleDateString("fr-FR", {
                            day: "numeric",
                            month: "short",
                          })}
                          {currentUser.isAdmin && (
                            <> · {user(item.assignedUserId)?.displayName}</>
                          )}
                        </div>
                      </div>
                      <button
                        className="detail-button"
                        onClick={() => setSelected(item)}
                        aria-label="Voir le détail"
                      >
                        ···
                      </button>
                    </article>
                  ))}
                </div>
              </section>
              <aside className="insight-panel">
                <div className="panel-heading">
                  <span className="section-kicker">ÉQUILIBRE DE LA CHARGE</span>
                  <h2>La répartition en bref</h2>
                </div>
                {users.map((entry, index) => {
                  const count = instances.filter(
                    (item) => item.assignedUserId === entry.id,
                  ).length;
                  return (
                    <div className="balance" key={entry.id}>
                      <div className="balance-name">
                        <span className={`avatar ${index ? "blue" : ""}`}>
                          {entry.displayName[0]}
                        </span>
                        <b>{entry.displayName}</b>
                        <span>{count} tâches</span>
                      </div>
                      <div className="bar">
                        <i
                          style={{
                            width: `${Math.max(12, (count / Math.max(instances.length, 1)) * 100)}%`,
                          }}
                        />
                      </div>
                      <small>
                        {entry.availabilityPercentage}% de disponibilité
                      </small>
                    </div>
                  );
                })}
                <div className="why-box">
                  <span>✦</span>
                  <div>
                    <b>Pourquoi cette tâche ?</b>
                    <p>
                      Le moteur équilibre les points, la disponibilité et la
                      rotation sur les 8 dernières semaines.
                    </p>
                  </div>
                </div>
              </aside>
            </div>
          </>
        ) : view === "Historique" ? (
          <HistoryPanel api={api} users={users} />
        ) : view === "Tâches" ? (
          <TaskCatalog
            templates={templates}
            user={currentUser}
            api={api}
            onCreated={(created) => {
              setTemplates((items) => [...items, created])
              setNotice("Tâche créée pour les prochaines affectations")
            }}
            onUpdated={(updated) => {
              setTemplates((items) => items.map((item) => item.id === updated.id ? updated : item))
              setNotice("Modèle de tâche mis à jour")
            }}
          />
        ) : view === "Moteur d’affectation" ? (
          <EngineSettingsPanel api={api} user={currentUser} onSaved={() => setNotice("Paramètres du moteur enregistrés")} />
        ) : view === "Participants" ? (
          <ProfileCatalog
            users={users}
            api={api}
            onCreated={(created) => {
              setUsers((items) => [...items, created])
              setNotice("Profil créé")
            }}
          />
        ) : (
          <section className="empty-view">
            <span className="section-kicker">{view.toUpperCase()}</span>
            <h2>Cette vue arrive juste après la semaine active.</h2>
            <p>
              La navigation et les permissions sont déjà prêtes pour l’ajouter
              au prochain écran.
            </p>
          </section>
        )}
      </section>
      {selected && (
        <div className="modal-backdrop" onClick={() => setSelected(null)}>
          <section
            className="task-modal"
            onClick={(event) => event.stopPropagation()}
          >
            <button className="modal-close" onClick={() => setSelected(null)}>
              ×
            </button>
            <span className="section-kicker">DÉTAIL DE L’AFFECTATION</span>
            <h2>{template(selected.taskTemplateId)?.title}</h2>
            <p>{template(selected.taskTemplateId)?.description}</p>
            <div className="explanation">
              <span>✦</span>
              <div>
                <b>Une affectation pensée pour l’équilibre</b>
                <p>
                  Cette tâche tient compte de la charge actuelle, de votre
                  disponibilité et de la rotation observée sur les dernières
                  semaines.
                </p>
              </div>
            </div>
            <button
              className="primary-button wide"
              onClick={() => toggle(selected)}
            >
              {selected.isCompleted
                ? "Rouvrir la tâche"
                : "Marquer comme terminée"}
            </button>
          </section>
        </div>
      )}
    </main>
  );
}

export default App;
