  import { useEffect, useMemo, useState, type FormEvent } from "react";
  import "./App.css";
  import { AnalyticsPanel } from "./components/AnalyticsPanel";

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
  createdByUserId?: string;
  isActive?: boolean;
};
type Instance = {
  id: string;
  taskTemplateId: string;
  dueDate: string;
  isCompleted: boolean;
  isCarryOver: boolean;
  status?: string;
  assignedUserId?: string;
};
type AuditLog = { id: string; entityType: string; entityId: string; action: string; performedByUserId: string; oldValue?: string; newValue?: string; performedAt: string };
type AssignmentRun = { id: string; weekId: string; strategyName: string; triggeredByUserId: string; isAutomatic: boolean; notes?: string; generatedAt: string };
type TaskComment = { id: string; taskInstanceId: string; authorUserId: string; content: string; createdAt: string };
type NotificationItem = { id: string; title: string; message: string; link?: string; isRead: boolean; createdAt: string };
type FlexibleExecution = { id: string; executionDate: string; createdAt: string };
type WeekSummary = { year: number; week: number; total: number; completed: number; startDate: string; endDate: string };
type WeekStatus = "Active" | "Closed";

type TaskFormProps = {
  api: string;
  user: User;
  templates: Template[];
  onCreated: (template: Template) => void;
};

const demoUsers: User[] = [
  {
    id: "julie",
    displayName: "Julie",
    isAdmin: false,
    isActive: true,
    availabilityPercentage: 100,
  },
  {
    id: "nicolas",
    displayName: "Nicolas",
    isAdmin: true,
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
    createdByUserId: "julie",
  },
  {
    id: "plants",
    title: "Arroser les plantes",
    description: "Vérifier les plantes du salon et de la cuisine.",
    priority: "Low",
    createdByUserId: "nicolas",
  },
  {
    id: "vacuum",
    title: "Aspirateur",
    description: "Passer l’aspirateur dans les pièces de vie.",
    priority: "Critical",
    createdByUserId: "julie",
  },
  {
    id: "shopping",
    title: "Courses de la semaine",
    description: "Préparer et récupérer les courses.",
    priority: "Normal",
    createdByUserId: "nicolas",
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

function weekStart(year: number, week: number) {
  const januaryFourth = new Date(year, 0, 4);
  const monday = new Date(januaryFourth);
  monday.setDate(januaryFourth.getDate() - (januaryFourth.getDay() || 7) + 1 + (week - 1) * 7);
  return monday;
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
  "/preferences": "Préférences",
  "/notifications": "Notifications",
  "/assignments": "Affectations",
  "/instances": "Instances",
  "/tasks": "Tâches",
  "/analytics": "Statistiques",
  "/participants": "Participants",
  "/assignment-engine": "Moteur d’affectation",
};

function viewFromPath(pathname: string) {
  return viewRoutes[pathname] ?? "Semaine";
}

function extractTaskTitleFromNotification(message: string) {
  const marker = " vous a été affectée";
  const markerIndex = message.indexOf(marker);
  if (markerIndex <= 0) return null;
  const value = message.slice(0, markerIndex).trim();
  return value.length > 0 ? value : null;
}

async function readApiError(response: Response, fallback: string) {
  try {
    const payload = await response.json();
    if (payload && typeof payload.error === "string" && payload.error.trim()) {
      return payload.error;
    }
  } catch {
  }
  return fallback;
}

const frequencyHelp: Record<string, string> = {
  DailyMandatory: "Une instance est créée pour chacun des 7 jours de la semaine.",
  DailyFlexible: "Une seule instance est créée : la tâche doit être réalisée au moins une fois dans la semaine, et peut l’être plusieurs fois.",
  Weekly: "Une instance est créée pour la semaine sélectionnée.",
  Monthly: "Une instance est créée sur la semaine mensuelle définie par le moteur.",
  OneTime: "Une seule instance est créée ; la tâche n’est pas récurrente.",
};
const priorityOrder: Record<string, number> = { Critical: 0, Normal: 1, Low: 2 };
const priorityLabels: Record<string, string> = { Critical: "Critique", Normal: "Normale", Low: "Faible" };
const frequencyLabels: Record<string, string> = { DailyMandatory: "Quotidienne obligatoire", DailyFlexible: "Quotidienne flexible", Weekly: "Hebdomadaire", Monthly: "Mensuelle", OneTime: "Ponctuelle" };

function FieldInfo({ text }: { text: string }) {
  return <span className="field-info"><button type="button" aria-label="Afficher l’explication" className="info-button">i</button><span className="info-tooltip">{text}</span></span>;
}

function ReminderDialog({ api, template, initialOpen = false }: { api: string; template: Template; initialOpen?: boolean }) {
  const [open, setOpen] = useState(initialOpen); const [reminders, setReminders] = useState<{ id: string; triggerType: string; offsetValue: number; offsetUnit: string; isEnabled: boolean }[]>([]); const [trigger, setTrigger] = useState("EndOfDay"); const [offset, setOffset] = useState("0"); const [unit, setUnit] = useState("Hour"); const [error, setError] = useState("");
  async function load() { const response = await fetch(`${api}/api/task-templates/${template.id}/reminders`); if (response.ok) setReminders(await response.json()); }
  useEffect(() => { if (initialOpen) void load(); }, [initialOpen, template.id]);
  async function add(event: FormEvent) { event.preventDefault(); const value = Number(offset); if (!Number.isInteger(value) || value < 0) return setError("Le décalage doit être un entier positif ou nul."); try { const response = await fetch(`${api}/api/task-templates/${template.id}/reminders`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ triggerType: trigger, offsetValue: value, offsetUnit: unit }) }); if (!response.ok) throw new Error("Rappel impossible"); const reminder = await response.json(); setReminders((items) => [...items, reminder]); setError(""); } catch (reason) { setError(reason instanceof Error ? reason.message : "Rappel impossible"); } }
  async function toggle(reminder: typeof reminders[number]) { const response = await fetch(`${api}/api/task-templates/${template.id}/reminders/${reminder.id}/active`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ isEnabled: !reminder.isEnabled }) }); if (!response.ok) return setError("Le rappel n’a pas pu être modifié."); const updated = await response.json(); setReminders((items) => items.map((item) => item.id === updated.id ? updated : item)); }
  return <><button className="secondary-button" onClick={() => { setOpen(true); void load(); }}>Rappels</button>{open && <div className="modal-backdrop" onClick={() => setOpen(false)}><section className="task-modal form-modal" onClick={(event) => event.stopPropagation()}><button className="modal-close" onClick={() => setOpen(false)}>×</button><span className="section-kicker">RAPPELS</span><h2>{template.title}</h2><div className="reminder-list">{reminders.length === 0 ? <p className="muted">Aucun rappel configuré.</p> : reminders.map((reminder) => <div className={reminder.isEnabled ? "reminder-row" : "reminder-row disabled"} key={reminder.id}><div><b>{reminder.triggerType === "EndOfDay" ? "Fin de journée" : "Fin de semaine"}</b><span>{reminder.offsetValue} {reminder.offsetUnit === "Hour" ? "heure(s)" : "jour(s)"} avant</span></div><button className="secondary-button" onClick={() => toggle(reminder)}>{reminder.isEnabled ? "Désactiver" : "Réactiver"}</button></div>)}</div><form onSubmit={add} className="reminder-form"><label>Moment<select value={trigger} onChange={(event) => setTrigger(event.target.value)}><option value="EndOfDay">Fin de journée</option><option value="EndOfWeek">Fin de semaine</option></select></label><label>Décalage<input type="number" min="0" value={offset} onChange={(event) => setOffset(event.target.value)} /></label><label>Unité<select value={unit} onChange={(event) => setUnit(event.target.value)}><option value="Hour">Heures</option><option value="Day">Jours</option></select></label>{error && <p className="form-error">{error}</p>}<button className="primary-button wide" type="submit">Ajouter le rappel</button></form></section></div>}</>
}

function CreateTaskDialog({ api, user, templates, onCreated }: TaskFormProps) {
  const [open, setOpen] = useState(false);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [priority, setPriority] = useState("Normal");
  const [frequencyType, setFrequencyType] = useState("Weekly");
  const [difficulty, setDifficulty] = useState("50");
  const [duration, setDuration] = useState("30");
  const [reminderEnabled, setReminderEnabled] = useState(false);
  const [reminderTrigger, setReminderTrigger] = useState("EndOfWeek");
  const [reminderOffset, setReminderOffset] = useState("0");
  const [reminderUnit, setReminderUnit] = useState("Hour");
  const [error, setError] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    const score = Number(difficulty);
    if (!title.trim()) return setError("Le titre est obligatoire.");
    if (templates.some((template) => template.title.trim().toLocaleLowerCase() === title.trim().toLocaleLowerCase())) return setError("Une tâche portant ce titre existe déjà.");
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
      const created = await response.json();
      if (reminderEnabled) {
        const reminderResponse = await fetch(`${api}/api/task-templates/${created.id}/reminders`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ triggerType: reminderTrigger, offsetValue: Number(reminderOffset), offsetUnit: reminderUnit }) });
        if (!reminderResponse.ok) throw new Error("La tâche est créée, mais le rappel n’a pas pu être enregistré.");
      }
      onCreated(created);
    } catch (reason) {
      return setError(reason instanceof Error ? reason.message : "API indisponible : la tâche n’a pas été enregistrée.");
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
              <FieldInfo text="Le titre doit être unique, même pour une tâche inactive, et sera visible dans les affectations." />
              <input
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                autoFocus
              />
            </label>
            <label>
              Description
              <FieldInfo text="Décris le résultat attendu pour que le participant puisse agir sans clarification." />
              <textarea
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                rows={2}
              />
            </label>
            <div className="form-grid">
              <label>
                Priorité
                <FieldInfo text="Pondère la charge : faible 0,5, normale 1, critique 1,5 par défaut. Ces valeurs sont réglables par l’administrateur." />
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
                <FieldInfo text={frequencyHelp[frequencyType]} />
                <select
                  value={frequencyType}
                  onChange={(event) => setFrequencyType(event.target.value)}
                >
                  <option value="DailyMandatory">Quotidienne obligatoire</option>
                  <option value="DailyFlexible">Quotidienne flexible</option>
                  <option value="Weekly">Hebdomadaire</option>
                  <option value="Monthly">Mensuelle</option>
                  <option value="OneTime">Ponctuelle</option>
                </select>
              </label>
              <label>
                Pénibilité
                <FieldInfo text="Score entier obligatoire compris entre 1 et 100. Plus il est élevé, plus la tâche pèse dans l’équilibrage." />
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
                <FieldInfo text="Durée estimée en minutes, minimum 1. Elle influence le score proportionnellement à la durée de référence." />
                <input
                  type="number"
                  min="1"
                  value={duration}
                  onChange={(event) => setDuration(event.target.value)}
                />
              </label>
            </div>
            <div className="inline-reminder"><label className="checkbox-label"><input type="checkbox" checked={reminderEnabled} onChange={(event) => setReminderEnabled(event.target.checked)} /> Ajouter un rappel</label>{reminderEnabled && <div className="reminder-form"><label>Moment<select value={reminderTrigger} onChange={(event) => setReminderTrigger(event.target.value)}><option value="EndOfDay">Fin de journée</option><option value="EndOfWeek">Fin de semaine</option></select></label><label>Délai<input type="number" min="0" value={reminderOffset} onChange={(event) => setReminderOffset(event.target.value)} /></label><label>Unité<select value={reminderUnit} onChange={(event) => setReminderUnit(event.target.value)}><option value="Hour">Heures</option><option value="Day">Jours</option></select></label></div>}</div>
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

function EditTaskDialog({ api, user, template, templates, authorName, onUpdated }: { api: string; user: User; template: Template; templates: Template[]; authorName?: string; onUpdated: (template: Template) => void }) {
  const [open, setOpen] = useState(false);
  const [title, setTitle] = useState(template.title);
  const [description, setDescription] = useState(template.description ?? "");
  const [priority, setPriority] = useState(template.priority || "Normal");
  const [frequencyType, setFrequencyType] = useState(template.frequencyType || "Weekly");
  const [difficulty, setDifficulty] = useState(String(template.manualDifficultyScore ?? 50));
  const [duration, setDuration] = useState(String(template.estimatedDurationMinutes ?? 30));
  const [remindersOpen, setRemindersOpen] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    const score = Number(difficulty);
    if (!title.trim()) return setError("Le titre est obligatoire.");
    if (templates.some((item) => item.id !== template.id && item.title.trim().toLocaleLowerCase() === title.trim().toLocaleLowerCase())) return setError("Une tâche portant ce titre existe déjà.");
    if (!Number.isInteger(score) || score < 1 || score > 100) return setError("La pénibilité doit être comprise entre 1 et 100.");
    const payload = { title: title.trim(), description: description.trim() || null, manualDifficultyScore: score, priority, frequencyType, estimatedDurationMinutes: Number(duration) || null, requestingUserId: user.id };
    try {
      const response = await fetch(`${api}/api/task-templates/${template.id}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload) });
      if (!response.ok) throw new Error((await response.json()).error ?? "Modification impossible");
      onUpdated(await response.json());
    } catch (reason) {
      return setError(reason instanceof Error ? reason.message : "API indisponible : la modification n’a pas été enregistrée.");
    }
    setOpen(false); setError("");
  }

  return <><button className="secondary-button" onClick={() => setOpen(true)}>Modifier</button>{open && <div className="modal-backdrop" onClick={() => setOpen(false)}><form className="task-modal form-modal" onSubmit={submit} onClick={(event) => event.stopPropagation()}><button type="button" className="modal-close" onClick={() => setOpen(false)}>×</button><span className="section-kicker">MODIFIER LE MODÈLE</span><h2>{template.title}</h2>{authorName && <p className="edit-author">Créée par {authorName}</p>}<label>Titre<FieldInfo text="Le titre doit rester unique, même pour une tâche inactive." /><input value={title} onChange={(event) => setTitle(event.target.value)} autoFocus /></label><label>Description<FieldInfo text="Décris le résultat attendu pour éviter les ambiguïtés." /><textarea value={description} onChange={(event) => setDescription(event.target.value)} rows={2} /></label><div className="form-grid"><label>Priorité<FieldInfo text="Pondère la charge : faible 0,5, normale 1, critique 1,5 par défaut." /><select value={priority} onChange={(event) => setPriority(event.target.value)}><option value="Low">Faible</option><option value="Normal">Normale</option><option value="Critical">Critique</option></select></label><label>Fréquence<FieldInfo text={frequencyHelp[frequencyType]} /><select value={frequencyType} onChange={(event) => setFrequencyType(event.target.value)}><option value="DailyMandatory">Quotidienne obligatoire</option><option value="DailyFlexible">Quotidienne flexible</option><option value="Weekly">Hebdomadaire</option><option value="Monthly">Mensuelle</option><option value="OneTime">Ponctuelle</option></select></label><label>Pénibilité<FieldInfo text="Entier obligatoire compris entre 1 et 100." /><input type="number" min="1" max="100" value={difficulty} onChange={(event) => setDifficulty(event.target.value)} /></label><label>Durée (min)<FieldInfo text="Minimum 1 minute; influence le score de charge." /><input type="number" min="1" value={duration} onChange={(event) => setDuration(event.target.value)} /></label></div><div className="edit-reminder"><button type="button" className="secondary-button" onClick={() => setRemindersOpen(true)}>Gérer les rappels</button>{remindersOpen && <ReminderDialog key="edit-reminders" api={api} template={template} initialOpen />}</div>{error && <p className="form-error">{error}</p>}<button className="primary-button wide" type="submit">Enregistrer</button></form></div>}</>;
}

function TaskCatalog({
  templates,
  users,
  user,
  api,
  onCreated,
  onUpdated,
}: {
  templates: Template[];
  users: User[];
  user: User;
  api: string;
  onCreated: (template: Template) => void;
  onUpdated: (template: Template) => void;
}) {
  const [allTemplates, setAllTemplates] = useState(templates);
  const [query, setQuery] = useState("");
  useEffect(() => {
    fetch(`${api}/api/task-templates/all`).then(async (response) => { if (response.ok) setAllTemplates(await response.json()) }).catch(() => undefined)
  }, [api])
  const filtered = allTemplates.filter((template) =>
    template.title.toLowerCase().includes(query.toLowerCase()),
  );
  function updateTemplate(updated: Template) {
    setAllTemplates((items) => items.map((item) => item.id === updated.id ? updated : item));
    onUpdated(updated);
  }

  async function setActive(template: Template) {
    const response = await fetch(`${api}/api/task-templates/${template.id}/active`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ isActive: !template.isActive }) });
    if (!response.ok) return;
    updateTemplate(await response.json());
  }

  const author = (id?: string) => users.find((entry) => entry.id === id)?.displayName ?? "Profil inconnu";

  return (
    <section className="catalog-panel">
      <div className="panel-heading">
        <div>
          <span className="section-kicker">PILOTAGE</span>
          <h2>Modèles de tâches</h2>
        </div>
        <CreateTaskDialog api={api} user={user} templates={allTemplates} onCreated={(created) => { setAllTemplates((items) => [...items, created]); onCreated(created) }} />
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
              <small>{template.isActive === false ? "Inactif" : "Actif"}</small>
              <EditTaskDialog api={api} user={user} template={template} templates={allTemplates} authorName={author(template.createdByUserId)} onUpdated={updateTemplate} />
              <ReminderDialog api={api} template={template} />
              <button className="secondary-button" onClick={() => setActive(template)}>{template.isActive === false ? "Réactiver" : "Désactiver"}</button>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}

function EditProfileDialog({ user, users, api, onUpdated }: { user: User; users: User[]; api: string; onUpdated: (user: User) => void }) {
  const [open, setOpen] = useState(false); const [name, setName] = useState(user.displayName); const [admin, setAdmin] = useState(user.isAdmin); const [availability, setAvailability] = useState(String(user.availabilityPercentage)); const [active, setActive] = useState(user.isActive); const [error, setError] = useState("");
  async function submit(event: FormEvent) { event.preventDefault(); const percentage = Number(availability); if (!name.trim()) return setError("Le nom d’affichage est obligatoire."); if (users.some((entry) => entry.id !== user.id && entry.displayName.trim().toLocaleLowerCase() === name.trim().toLocaleLowerCase())) return setError("Ce nom d’affichage est déjà utilisé."); if (!Number.isInteger(percentage) || percentage < 0 || percentage > 100) return setError("La disponibilité doit être comprise entre 0 et 100."); try { const response = await fetch(`${api}/api/users/${user.id}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ displayName: name.trim(), isAdmin: admin, availabilityPercentage: percentage, isActive: active }) }); if (!response.ok) throw new Error((await response.json()).error ?? "Modification impossible"); onUpdated(await response.json()); } catch (reason) { return setError(reason instanceof Error ? reason.message : "Modification impossible."); } setOpen(false); setError(""); }
  return <><button className="secondary-button" onClick={() => setOpen(true)}>Modifier</button>{open && <div className="modal-backdrop" onClick={() => setOpen(false)}><form className="task-modal form-modal" onSubmit={submit} onClick={(event) => event.stopPropagation()}><button type="button" className="modal-close" onClick={() => setOpen(false)}>×</button><span className="section-kicker">MODIFIER LE PROFIL</span><h2>{user.displayName}</h2><label>Nom d’affichage<input value={name} onChange={(event) => setName(event.target.value)} autoFocus /></label><label>Disponibilité (%)<input type="number" min="0" max="100" value={availability} onChange={(event) => setAvailability(event.target.value)} /></label><label className="checkbox-label"><input type="checkbox" checked={admin} onChange={(event) => setAdmin(event.target.checked)} /> Administrateur</label><label className="checkbox-label"><input type="checkbox" checked={active} onChange={(event) => setActive(event.target.checked)} /> Profil actif</label>{error && <p className="form-error">{error}</p>}<button className="primary-button wide" type="submit">Enregistrer</button></form></div>}</>;
}

function ProfileCatalog({ users, api, onCreated, onUpdated }: { users: User[]; api: string; onCreated: (user: User) => void; onUpdated: (user: User) => void }) {
  const [allUsers, setAllUsers] = useState(users)
  const [open, setOpen] = useState(false)
  const [displayName, setDisplayName] = useState("")
  const [isAdmin, setIsAdmin] = useState(false)
  const [availability, setAvailability] = useState("100")
  const [error, setError] = useState("")

  useEffect(() => {
    fetch(`${api}/api/users/all`).then(async (response) => { if (response.ok) setAllUsers(await response.json()) }).catch(() => undefined)
  }, [api])

  async function submit(event: FormEvent) {
    event.preventDefault()
    const percentage = Number(availability)
    if (!displayName.trim()) return setError("Le nom d’affichage est obligatoire.")
    if (!Number.isInteger(percentage) || percentage < 0 || percentage > 100) return setError("La disponibilité doit être comprise entre 0 et 100.")
    try {
      const response = await fetch(`${api}/api/users`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ displayName: displayName.trim(), isAdmin, availabilityPercentage: percentage }) })
      if (!response.ok) throw new Error((await response.json()).error ?? "Création impossible")
      const created = await response.json()
      setAllUsers((items) => [...items, created])
      onCreated(created)
    } catch (reason) {
      return setError(reason instanceof Error ? reason.message : "API indisponible : le profil n’a pas été enregistré.")
    }
    setOpen(false); setDisplayName(""); setError("")
  }

  return <section className="catalog-panel"><div className="panel-heading"><div><span className="section-kicker">PILOTAGE</span><h2>Participants</h2></div><button className="primary-button" onClick={() => setOpen(true)}>+ Nouveau profil</button></div><div className="profile-catalog-list">{allUsers.map((entry) => <article className="profile-catalog-item" key={entry.id}><span className={`avatar ${entry.isAdmin ? "" : "blue"}`}>{entry.displayName[0]}</span><div><h3>{entry.displayName}</h3><p>{entry.isAdmin ? "Administrateur" : "Participant"}</p></div><div className="profile-catalog-meta"><b>{entry.availabilityPercentage}%</b><small>{entry.isActive ? "Actif" : "Inactif"}</small></div><EditProfileDialog user={entry} users={allUsers} api={api} onUpdated={(updated) => { setAllUsers((items) => items.map((item) => item.id === updated.id ? updated : item)); onUpdated(updated) }} /></article>)}</div>{open && <div className="modal-backdrop" onClick={() => setOpen(false)}><form className="task-modal form-modal" onSubmit={submit} onClick={(event) => event.stopPropagation()}><button type="button" className="modal-close" onClick={() => setOpen(false)}>×</button><span className="section-kicker">NOUVEAU PROFIL</span><h2>Ajouter un participant</h2><label>Nom d’affichage<input value={displayName} onChange={(event) => setDisplayName(event.target.value)} autoFocus /></label><label>Disponibilité (%)<input type="number" min="0" max="100" value={availability} onChange={(event) => setAvailability(event.target.value)} /></label><label className="checkbox-label"><input type="checkbox" checked={isAdmin} onChange={(event) => setIsAdmin(event.target.checked)} /> Administrateur</label>{error && <p className="form-error">{error}</p>}<button className="primary-button wide" type="submit">Créer le profil</button></form></div>}</section>
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
      return setError(reason instanceof Error ? reason.message : "API indisponible : les paramètres n’ont pas été enregistrés.")
    }
  }

  return <form className="settings-panel" onSubmit={submit}><div className="panel-heading"><div><span className="section-kicker">CONFIGURATION ADMIN</span><h2>Moteur d’affectation</h2></div><button className="primary-button" type="submit">Enregistrer</button></div><p className="settings-intro">Ces paramètres pilotent le score des tâches et l’équilibre de la charge pour les prochaines affectations.</p><div className="settings-section"><span className="section-kicker">MULTIPLICATEURS DE PRIORITÉ</span><div className="settings-grid"><label>Faible<input type="number" min="0" step="0.05" value={settings.lowPriorityMultiplier} onChange={(event) => update("lowPriorityMultiplier", event.target.value)} /></label><label>Normale<input type="number" min="0" step="0.05" value={settings.normalPriorityMultiplier} onChange={(event) => update("normalPriorityMultiplier", event.target.value)} /></label><label>Critique<input type="number" min="0" step="0.05" value={settings.criticalPriorityMultiplier} onChange={(event) => update("criticalPriorityMultiplier", event.target.value)} /></label></div></div><div className="settings-section"><span className="section-kicker">ÉQUILIBRE HISTORIQUE</span><div className="settings-grid"><label>Durée de référence (minutes)<input type="number" min="1" value={settings.referenceDurationMinutes} onChange={(event) => update("referenceDurationMinutes", event.target.value)} /></label><label>Fenêtre d’équité (semaines)<input type="number" min="1" value={settings.equityWindowWeeks} onChange={(event) => update("equityWindowWeeks", event.target.value)} /></label><label>Pénalité anti-répétition<input type="number" min="0" step="0.05" value={settings.repetitionPenaltyWeight} onChange={(event) => update("repetitionPenaltyWeight", event.target.value)} /></label></div></div>{error && <p className="form-error">{error}</p>}{saved && <p className="settings-saved">✓ Paramètres enregistrés</p>}</form>
}

function HistoryPanel({ api, users, isAdmin, currentUserId }: { api: string; users: User[]; isAdmin: boolean; currentUserId: string }) {
  const [entries, setEntries] = useState<AuditLog[]>([]);
  const [weeks, setWeeks] = useState<WeekSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [seeding, setSeeding] = useState(false);
  const [historyPage, setHistoryPage] = useState(1);
  const historyPageSize = 10;
  useEffect(() => {
    fetch(`${api}/api/audit-logs`).then(async (response) => { if (!response.ok) throw new Error("Lecture impossible"); setEntries(await response.json()); }).catch(() => undefined).finally(() => setLoading(false));
  }, [api]);
  useEffect(() => {
    const loadWeeks = async () => {
      const requests = Array.from({ length: 20 }, (_, index) => {
        const date = new Date(); date.setDate(date.getDate() - (index + 1) * 7);
        const key = isoWeek(date);
        return fetch(`${api}/api/weeks/${key.year}/${key.week}/instances`).then(async (response) => {
          if (!response.ok) return null;
          const result = await response.json();
          const instances = result.instances ?? [];
          return { year: key.year, week: key.week, total: instances.length, completed: instances.filter((item: Instance) => item.isCompleted).length, startDate: result.week?.startDate, endDate: result.week?.endDate } as WeekSummary;
        }).catch(() => null);
      });
      setWeeks((await Promise.all(requests)).filter((week): week is WeekSummary => week !== null));
    };
    loadWeeks();
  }, [api, currentUserId]);
  const actor = (id: string) => users.find((user) => user.id === id)?.displayName ?? "Participant";
  const actionLabel = (action: string) => ({ Created: "créé", Updated: "modifié", Completed: "terminé", Reopened: "rouvert", AssignmentRun: "répartition lancée", WeekClosed: "semaine clôturée" }[action] ?? action.toLowerCase());
  const entityLabel = (type: string) => type === "TaskTemplate" ? "modèle de tâche" : type === "TaskInstance" ? "instance" : type.toLowerCase();
  const visibleEntries = isAdmin ? entries : entries.filter((entry) => entry.performedByUserId === currentUserId);
  const historyPageCount = Math.max(1, Math.ceil(visibleEntries.length / historyPageSize));
  const pagedEntries = visibleEntries.slice((historyPage - 1) * historyPageSize, historyPage * historyPageSize);
  useEffect(() => { setHistoryPage((page) => Math.min(page, historyPageCount)); }, [historyPageCount]);
  async function seedHistory() {
    setSeeding(true)
    try {
      const response = await fetch(`${api}/api/development/seed-history`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ weeks: 20 }) })
      if (!response.ok) throw new Error("Génération impossible")
      const auditResponse = await fetch(`${api}/api/audit-logs`)
      if (auditResponse.ok) setEntries(await auditResponse.json())
    } finally { setSeeding(false) }
  }
  return <section className="history-panel"><div className="panel-heading"><div><span className="section-kicker">TRAÇABILITÉ</span><h2>{isAdmin ? "Journal d’audit du foyer" : "Mon historique"}</h2></div><div className="history-actions">{isAdmin && <button className="secondary-button" disabled={seeding} onClick={seedHistory}>{seeding ? "Génération..." : "Générer 20 semaines"}</button>}<span className="history-count">{visibleEntries.length} événements</span></div></div><div className="week-history"><span className="section-kicker">20 DERNIÈRES SEMAINES</span><div className="week-history-grid">{weeks.map((period) => <button className="week-history-card" key={`${period.year}-${period.week}`} onClick={() => { window.history.pushState({}, "", `/week?year=${period.year}&week=${period.week}`); window.dispatchEvent(new PopStateEvent("popstate")); }}><b>S{period.week}</b><small>{period.total} tâches</small><strong>{period.total ? Math.round(period.completed / period.total * 100) : 0}%</strong><i style={{ width: `${period.total ? period.completed / period.total * 100 : 0}%` }} /></button>)}</div></div>{loading ? <p className="muted">Chargement de l’historique...</p> : visibleEntries.length === 0 ? <div className="history-empty"><span>◷</span><h3>Aucune activité enregistrée</h3><p>Les créations, affectations et changements de complétude apparaîtront ici.</p></div> : <><div className="history-list">{pagedEntries.map((entry) => <article className="history-item" key={entry.id}><span className="history-dot" /><div><p><b>{actor(entry.performedByUserId)}</b> a {actionLabel(entry.action)} un {entityLabel(entry.entityType)}</p>{entry.newValue && <small>{entry.newValue}</small>}<time>{new Date(entry.performedAt).toLocaleString("fr-FR", { dateStyle: "medium", timeStyle: "short" })}</time></div></article>)}</div><div className="history-pagination"><button className="secondary-button" disabled={historyPage === 1} onClick={() => setHistoryPage((page) => page - 1)}>Précédente</button><span>Page {historyPage} / {historyPageCount}</span><button className="secondary-button" disabled={historyPage === historyPageCount} onClick={() => setHistoryPage((page) => page + 1)}>Suivante</button></div></>}</section>;
}

function PreferencesPanel({ api }: { api: string }) {
  const [enabled, setEnabled] = useState(true);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  useEffect(() => { fetch(`${api}/api/users/preferences`).then(async (response) => { if (response.ok) setEnabled((await response.json()).notificationsEnabled) }).finally(() => setLoading(false)) }, [api]);
  async function toggle() { const next = !enabled; setEnabled(next); setSaving(true); try { const response = await fetch(`${api}/api/users/preferences`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ notificationsEnabled: next }) }); if (!response.ok) setEnabled(!next); } catch { setEnabled(!next); } finally { setSaving(false); } }
  return <section className="settings-panel preferences-panel"><span className="section-kicker">MON ESPACE</span><h2>Préférences</h2><p className="settings-intro">Choisis les types de notifications que tu souhaites recevoir lorsque le service de notification sera activé.</p><div className="preference-row"><div><b>Notifications du foyer</b><small>Rappels et événements liés à tes tâches.</small></div><button className={enabled ? "toggle enabled" : "toggle"} disabled={loading || saving} onClick={toggle} aria-pressed={enabled}><span /></button></div><p className="preference-status">{loading ? "Chargement..." : saving ? "Enregistrement..." : enabled ? "Activées" : "Désactivées"}</p></section>;
}

function NotificationsPanel({ api, onUnreadChange, onOpenNotification }: { api: string; onUnreadChange: (count: number) => void; onOpenNotification: (notification: NotificationItem) => void }) {
  const [notifications, setNotifications] = useState<NotificationItem[]>([]); const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false);
  const load = async () => { try { const response = await fetch(`${api}/api/notifications`); if (response.ok) { const items: NotificationItem[] = await response.json(); setNotifications(items); onUnreadChange(items.filter((item) => !item.isRead).length); } } finally { setLoading(false); } };
  useEffect(() => { void load(); }, [api]);
  async function setRead(notification: NotificationItem) { const response = await fetch(`${api}/api/notifications/${notification.id}/read`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify(!notification.isRead) }); if (response.ok) { const updated = await response.json(); setNotifications((items) => items.map((item) => item.id === updated.id ? updated : item)); onUnreadChange(notifications.map((item) => item.id === updated.id ? updated : item).filter((item) => !item.isRead).length); } }
  async function readAll() { setBusy(true); const response = await fetch(`${api}/api/notifications/read-all`, { method: "POST" }); if (response.ok) { setNotifications((items) => items.map((item) => ({ ...item, isRead: true }))); onUnreadChange(0); } setBusy(false); }
  async function deleteRead() { setBusy(true); const response = await fetch(`${api}/api/notifications/read`, { method: "DELETE" }); if (response.ok) { const remaining = notifications.filter((item) => !item.isRead); setNotifications(remaining); onUnreadChange(remaining.filter((item) => !item.isRead).length); } setBusy(false); }
  const unread = notifications.filter((notification) => !notification.isRead).length;
  return <section className="notifications-panel"><div className="panel-heading"><div><span className="section-kicker">MON ESPACE</span><h2>Notifications</h2><p className="panel-subtitle">Retrouve les rappels et événements liés à tes tâches.</p></div><span className="notification-count">{unread} non lue{unread > 1 ? "s" : ""}</span></div><div className="notification-actions"><button className="secondary-button" disabled={busy || unread === 0} onClick={readAll}>Tout marquer comme lu</button><button className="danger-button" disabled={busy || notifications.every((item) => !item.isRead)} onClick={deleteRead}>Supprimer les lues</button></div>{loading ? <p className="muted">Chargement des notifications...</p> : notifications.length === 0 ? <div className="history-empty"><span>◷</span><h3>Boîte vide</h3><p>Les notifications apparaîtront ici.</p></div> : <div className="notification-list">{notifications.map((notification) => <article className={notification.isRead ? "notification read" : "notification"} key={notification.id}><span className="notification-mark">{notification.isRead ? "✓" : "•"}</span><div><h3>{notification.link ? <a href={notification.link} onClick={(event) => { event.preventDefault(); onOpenNotification(notification); }}>{notification.title}</a> : notification.title}</h3><p>{notification.message}</p><time>{new Date(notification.createdAt).toLocaleString("fr-FR", { dateStyle: "medium", timeStyle: "short" })}</time></div><button className="secondary-button" onClick={() => setRead(notification)}>{notification.isRead ? "Non lue" : "Marquer lu"}</button></article>)}</div>}</section>;
}

function CommentsPanel({ api, instanceId, currentUser, users }: { api: string; instanceId: string; currentUser: User; users: User[] }) {
  const [comments, setComments] = useState<TaskComment[]>([]);
  const [content, setContent] = useState("");
  const [error, setError] = useState("");
  useEffect(() => { fetch(`${api}/api/task-instances/${instanceId}/comments`).then(async (response) => { if (response.ok) setComments(await response.json()) }).catch(() => undefined) }, [api, instanceId]);
  async function submit(event: FormEvent) { event.preventDefault(); if (!content.trim()) return; try { const response = await fetch(`${api}/api/task-instances/${instanceId}/comments`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ content: content.trim() }) }); if (!response.ok) throw new Error("Commentaire impossible"); const comment = await response.json(); setComments((items) => [...items, comment]); setContent(""); setError(""); } catch (reason) { setError(reason instanceof Error ? reason.message : "Commentaire impossible"); } }
  const author = (id: string) => users.find((user) => user.id === id)?.displayName ?? currentUser.displayName;
  return <div className="comments-panel"><span className="section-kicker">ÉCHANGES</span><h3>Commentaires</h3>{comments.length === 0 ? <p className="comments-empty">Aucun commentaire pour le moment.</p> : <div className="comments-list">{comments.map((comment) => <div className="comment" key={comment.id}><b>{author(comment.authorUserId)}</b><p>{comment.content}</p><time>{new Date(comment.createdAt).toLocaleString("fr-FR", { dateStyle: "medium", timeStyle: "short" })}</time></div>)}</div>}<form className="comment-form" onSubmit={submit}><textarea value={content} onChange={(event) => setContent(event.target.value)} placeholder="Ajouter une précision..." rows={2} />{error && <small className="form-error">{error}</small>}<button className="secondary-button" type="submit">Ajouter</button></form></div>;
}

function FlexibleExecutionPanel({ api, instance, onCompleted }: { api: string; instance: Instance; onCompleted: () => void }) {
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [executions, setExecutions] = useState<FlexibleExecution[]>([]);
  const [error, setError] = useState("");
  async function execute(event: FormEvent) { event.preventDefault(); try { const response = await fetch(`${api}/api/task-instances/${instance.id}/executions`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ executionDate: date }) }); if (!response.ok) throw new Error((await response.json()).error ?? "Exécution impossible"); const execution = await response.json(); setExecutions((items) => [...items, execution]); setError(""); onCompleted(); } catch (reason) { setError(reason instanceof Error ? reason.message : "Exécution impossible"); } }
  return <div className="flexible-panel"><div><span className="section-kicker">TÂCHE FLEXIBLE</span><h3>Déclarer une exécution</h3><p>Une seule exécution suffit pour atteindre l’objectif hebdomadaire. Tu peux en enregistrer plusieurs.</p></div><form onSubmit={execute}><input type="date" value={date} onChange={(event) => setDate(event.target.value)} /><button className="secondary-button" type="submit">Enregistrer</button></form>{error && <p className="form-error">{error}</p>}{executions.length > 0 && <small className="execution-count">{executions.length} exécution(s) ajoutée(s) pendant cette ouverture.</small>}</div>;
}

function AssignmentsPanel({ instances, templates, users, year, weekNumber, api, busyAction, onAction }: { instances: Instance[]; templates: Template[]; users: User[]; year: number; weekNumber: number; api: string; busyAction: string; onAction: (action: "generate" | "assign" | "close") => void }) {
  const [runs, setRuns] = useState<AssignmentRun[]>([]);
  useEffect(() => { fetch(`${api}/api/assignments/runs?year=${year}&weekNumber=${weekNumber}`).then(async (response) => { if (response.ok) setRuns(await response.json()) }).catch(() => undefined) }, [api, year, weekNumber]);
  const template = (id: string) => templates.find((item) => item.id === id)
  const user = (id?: string) => users.find((item) => item.id === id)?.displayName ?? "Non attribuée"
  return <section className="assignments-panel"><div className="panel-heading"><div><span className="section-kicker">ADMINISTRATION</span><h2>Affectations de la semaine</h2><p className="panel-subtitle">Vue globale des instances attribuées à tous les participants.</p></div><div className="admin-actions"><button className="secondary-button" disabled={Boolean(busyAction)} onClick={() => onAction("generate")}>{busyAction === "generate" ? "Génération..." : "Générer les instances"}</button><button className="primary-button" disabled={Boolean(busyAction)} onClick={() => onAction("assign")}>{busyAction === "assign" ? "Affectation..." : "Lancer l’affectation"}</button><button className="danger-button" disabled={Boolean(busyAction)} onClick={() => onAction("close")}>{busyAction === "close" ? "Clôture..." : "Clôturer"}</button></div></div><div className="assignment-summary"><b>{instances.length}</b><span>instances cette semaine</span><b>{instances.filter((item) => item.isCompleted).length}</b><span>terminées</span><b>{new Set(instances.map((item) => item.assignedUserId).filter(Boolean)).size}</b><span>participants concernés</span></div><div className="run-history"><span className="section-kicker">EXÉCUTIONS DE LA SEMAINE</span>{runs.length === 0 ? <p className="muted">Aucune exécution enregistrée.</p> : runs.map((run, index) => <div className="run-row" key={run.id}><span className="run-number">{runs.length - index}</span><div><b>{run.strategyName}</b><small>{new Date(run.generatedAt).toLocaleString("fr-FR", { dateStyle: "medium", timeStyle: "short" })}</small></div><span className="run-badge">{index === 0 ? "Dernière" : "Relance"}</span></div>)}</div><div className="assignment-list">{instances.map((item) => <article className="assignment-item" key={item.id}><span className={item.isCompleted ? "assignment-state done" : "assignment-state"}>{item.isCompleted ? "✓" : "○"}</span><div><h3>{template(item.taskTemplateId)?.title ?? "Tâche sans titre"}</h3><p>Échéance : {new Date(item.dueDate).toLocaleDateString("fr-FR", { day: "numeric", month: "long" })}{item.isCarryOver ? " · Reportée" : ""}</p></div><strong>{user(item.assignedUserId)}</strong></article>)}</div></section>
}

function InstanceAdminPanel({ instances, templates, users, onReassigned, onCancelled }: { instances: Instance[]; templates: Template[]; users: User[]; onReassigned: (instanceId: string, assignedUserId: string) => Promise<void>; onCancelled: (instanceId: string) => Promise<void> }) {
  const [assignments, setAssignments] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState("");
  const template = (id: string) => templates.find((item) => item.id === id)?.title ?? "Tâche sans titre";
  async function reassign(item: Instance) {
    const assignedUserId = assignments[item.id] ?? item.assignedUserId;
    if (!assignedUserId || assignedUserId === item.assignedUserId) return;
    setBusy(item.id);
    try { await onReassigned(item.id, assignedUserId); } finally { setBusy(""); }
  }
  async function cancel(item: Instance) { if (!window.confirm(`Annuler l’instance « ${template(item.taskTemplateId)} » ?`)) return; setBusy(item.id); try { await onCancelled(item.id); } finally { setBusy(""); } }
  return <section className="assignments-panel"><div className="panel-heading"><div><span className="section-kicker">ADMINISTRATION</span><h2>Instances de tâches</h2><p className="panel-subtitle">Toutes les occurrences générées, leur état et leur propriétaire.</p></div><span className="history-count">{instances.length} instances</span></div><div className="instance-admin-list">{instances.map((item) => { const cancelled = item.status === "Cancelled"; return <article className="instance-admin-item" key={item.id}><div className={item.isCompleted ? "assignment-state done" : "assignment-state"}>{item.isCompleted ? "✓" : "○"}</div><div><h3>{template(item.taskTemplateId)}</h3><p>Échéance : {new Date(item.dueDate).toLocaleDateString("fr-FR", { day: "numeric", month: "long" })}{item.isCarryOver ? " · Reportée" : ""}</p></div><select className="instance-owner-select" disabled={cancelled} aria-label={`Participant de ${template(item.taskTemplateId)}`} value={assignments[item.id] ?? item.assignedUserId ?? ""} onChange={(event) => setAssignments((current) => ({ ...current, [item.id]: event.target.value }))}><option value="">Non attribuée</option>{users.filter((entry) => entry.isActive).map((entry) => <option value={entry.id} key={entry.id}>{entry.displayName}</option>)}</select><button className="secondary-button" disabled={busy === item.id || cancelled} onClick={() => reassign(item)}>{busy === item.id ? "..." : "Réaffecter"}</button>{!item.isCompleted && !cancelled && <button className="danger-button" disabled={busy === item.id} onClick={() => cancel(item)}>Annuler</button>}<span className={item.isCompleted ? "instance-status done" : cancelled ? "instance-status cancelled" : "instance-status"}>{item.isCompleted ? "Terminée" : cancelled ? "Annulée" : "À faire"}</span></article>})}</div><div className="admin-note">Les réaffectations et annulations sont historisées et impossibles sur une semaine clôturée.</div></section>
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
  const [locationSearch, setLocationSearch] = useState(() => window.location.search);
  const [busyAction, setBusyAction] = useState("");
  const [weekStatus, setWeekStatus] = useState<WeekStatus>("Active");
  const [currentWeekId, setCurrentWeekId] = useState<string | null>(null);
  const [apiError, setApiError] = useState(false);
  const [apiMessage, setApiMessage] = useState("");
  const [unreadNotifications, setUnreadNotifications] = useState(0);
  const [pendingNotificationTaskTitle, setPendingNotificationTaskTitle] = useState<string | null>(null);
  const api = import.meta.env.VITE_API_URL ?? "";
  const queryParameters = new URLSearchParams(locationSearch);
  const queryYear = Number(queryParameters.get("year"));
  const queryWeek = Number(queryParameters.get("week"));
  const queryInstanceId = queryParameters.get("instanceId");
  const selectedWeek = Number.isInteger(queryYear) && Number.isInteger(queryWeek) && queryWeek > 0 ? { year: queryYear, week: queryWeek } : null;
  const week = selectedWeek ?? isoWeek(new Date(Date.now() + weekOffset * 7 * 86400000));

  useEffect(() => {
    const onPopState = () => { setView(viewFromPath(window.location.pathname)); setLocationSearch(window.location.search); };
    window.addEventListener("popstate", onPopState);
    return () => window.removeEventListener("popstate", onPopState);
  }, []);

  function navigate(path: string) {
    window.history.pushState({}, "", path);
    setView(viewFromPath(path));
    setLocationSearch(new URL(path, window.location.origin).search);
    setSelected(null);
  }

  function selectProfile(user: User) {
    setCurrentUser(user);
    navigate("/week");
    void fetch(`${api}/api/users/${user.id}/select`, { method: "POST" });
  }

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      setApiMessage("");
      try {
        const currentResponse = await fetch(`${api}/api/users/current`);
        if (currentResponse.ok) setCurrentUser(await currentResponse.json());
        const responses = await Promise.all([
          fetch(`${api}/api/users`),
          fetch(`${api}/api/task-templates`),
          fetch(`${api}/api/weeks/${week.year}/${week.week}/instances`),
        ]);
        const [userResponse, templateResponse, instanceResponse] = responses;
        if (!userResponse.ok || !templateResponse.ok) {
          throw new Error(await readApiError(userResponse.ok ? templateResponse : userResponse, "API indisponible."));
        }

        const loadedUsers = await userResponse.json();
        const loadedTemplates = await templateResponse.json();
        setUsers(loadedUsers);
        setTemplates(loadedTemplates);

        if (instanceResponse.status === 401) {
          setWeekStatus("Active");
          setCurrentWeekId(null);
          setInstances([]);
          setApiError(false);
          return;
        }

        if (!instanceResponse.ok) {
          const message = await readApiError(instanceResponse, "Lecture de la semaine impossible.");
          setWeekStatus("Active");
          setCurrentWeekId(null);
          setInstances([]);
          setApiError(false);
          setApiMessage(message);
          return;
        }

        const instanceData = await instanceResponse.json();
        const loadedInstances = instanceData?.instances ?? instanceData ?? [];

        setWeekStatus(instanceData?.week?.status ?? "Active");
        setCurrentWeekId(instanceData?.week?.id ?? null);
        setInstances(loadedInstances);

        const requestedInstanceId = queryInstanceId;
        if (requestedInstanceId) {
          const requested = loadedInstances.find((entry: Instance) => entry.id === requestedInstanceId) ?? null;
          setSelected(requested);
          if (!requested) setNotice("La tâche demandée n'est pas disponible pour cette semaine.");
        }
        else if (pendingNotificationTaskTitle) {
          const relatedTemplate = loadedTemplates.find((entry: Template) => entry.title === pendingNotificationTaskTitle);
          if (relatedTemplate) {
            const relatedInstance = loadedInstances.find((entry: Instance) => entry.taskTemplateId === relatedTemplate.id) ?? null;
            if (relatedInstance) setSelected(relatedInstance);
          }
          setPendingNotificationTaskTitle(null);
        }

        setApiError(false);
      } catch (reason) {
        setApiError(true);
        setApiMessage(reason instanceof Error ? reason.message : "API indisponible.");
        setWeekStatus("Active");
        setCurrentWeekId(null);
        setUsers(demoUsers);
        setTemplates(demoTemplates);
        setInstances(demoInstances);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [api, week.year, week.week, queryInstanceId, pendingNotificationTaskTitle]);

  function openNotification(notification: NotificationItem) {
    if (!notification.link) return;

    const target = new URL(notification.link, window.location.origin);
    const year = Number(target.searchParams.get("year"));
    const weekNumber = Number(target.searchParams.get("week"));
    const legacyInstanceId = target.hash.match(/^#instance-([0-9a-fA-F-]{36})/)?.[1];
    const instanceId = target.searchParams.get("instanceId") ?? legacyInstanceId;

    const search = Number.isInteger(year) && Number.isInteger(weekNumber) && weekNumber > 0
      ? `?year=${year}&week=${weekNumber}${instanceId ? `&instanceId=${encodeURIComponent(instanceId)}` : ""}`
      : "";

    window.history.pushState({}, "", `/week${search}`);
    setView("Semaine");
    setLocationSearch(search);
    setSelected(null);

    if (!instanceId) {
      const title = extractTaskTitleFromNotification(notification.message);
      setPendingNotificationTaskTitle(title);
    }
  }

  useEffect(() => {
    if (!currentUser) return;
    const refreshNotifications = async () => {
      const response = await fetch(`${api}/api/notifications`);
      if (response.ok) {
        const notifications: NotificationItem[] = await response.json();
        setUnreadNotifications(notifications.filter((notification) => !notification.isRead).length);
      }
    };
    void refreshNotifications();
    const timer = window.setInterval(refreshNotifications, 30000);
    return () => window.clearInterval(timer);
  }, [api, currentUser]);

  function changeWeek(offset: number) {
    setWeekOffset((current) => current + offset);
    navigate("/week");
    setNotice("Chargement de la semaine sélectionnée");
    window.setTimeout(() => setNotice(""), 1600);
  }

  async function runWeekAction(action: "generate" | "assign" | "close") {
    if (action === "close" && !window.confirm("Clôturer cette semaine ? La complétude sera figée et les tâches non terminées seront reportées.")) return;
    setBusyAction(action);
    try {
      const endpoint = action === "generate" ? `${api}/api/weeks/generate` : action === "assign" ? `${api}/api/assignments/run` : `${api}/api/weeks/${week.year}/${week.week}/close`;
      const body = action === "generate" ? { year: week.year, weekNumber: week.week } : action === "assign" ? { year: week.year, weekNumber: week.week, triggeredByUserId: currentUser?.id } : { requestingUserId: currentUser?.id };
      const response = await fetch(endpoint, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
      if (!response.ok) throw new Error((await response.json()).error ?? "Action impossible");
      const result = await response.json();
      if (result.week?.status) setWeekStatus(result.week.status);
      const refreshed = result.instances ?? result.createdInstances ?? result.carriedOverInstances;
      if (Array.isArray(refreshed)) setInstances(refreshed);
      setNotice(action === "generate" ? "Instances de la semaine générées" : action === "assign" ? "Affectation de la semaine terminée" : "Semaine clôturée, les tâches ouvertes sont reportées");
    } catch (reason) {
      setNotice(reason instanceof Error ? reason.message : "Action impossible");
    } finally {
      setBusyAction("");
      window.setTimeout(() => setNotice(""), 2400);
    }
  }

  const template = (id: string) => templates.find((item) => item.id === id);
  const visible = useMemo(() => {
    const mine = instances.filter((item) => item.assignedUserId === currentUser?.id);
    const filtered = filter === "open"
      ? mine.filter((item) => !item.isCompleted)
      : filter === "done"
        ? mine.filter((item) => item.isCompleted)
        : mine;
    return [...filtered].sort((left, right) => {
      const dueDateOrder = left.dueDate.localeCompare(right.dueDate);
      if (dueDateOrder !== 0) return dueDateOrder;
      return (priorityOrder[template(left.taskTemplateId)?.priority ?? "Normal"] ?? 1) - (priorityOrder[template(right.taskTemplateId)?.priority ?? "Normal"] ?? 1);
    });
  }, [currentUser, filter, instances, templates]);
  const personalInstances = instances.filter((item) => item.assignedUserId === currentUser?.id);
  const completed = personalInstances.filter((item) => item.isCompleted).length;
  const progress = personalInstances.length
    ? Math.round((completed / personalInstances.length) * 100)
    : 0;
  const user = (id?: string) => users.find((item) => item.id === id);

  async function toggle(item: Instance) {
    if (weekStatus === "Closed") return;
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
      const response = await fetch(`${api}/api/task-instances/${item.id}/completion`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          requestingUserId: currentUser?.id,
          isCompleted: next,
        }),
      });
      if (!response.ok) {
        throw new Error(await readApiError(response, "La complétude n’a pas été enregistrée."));
      }
    } catch (reason) {
      setInstances((items) =>
        items.map((entry) =>
          entry.id === item.id ? { ...entry, isCompleted: item.isCompleted } : entry,
        ),
      );
      setNotice(reason instanceof Error ? reason.message : "API indisponible : la complétude n’a pas été enregistrée");
    }
    window.setTimeout(() => setNotice(""), 2200);
  }

  async function reassignInstance(instanceId: string, assignedUserId: string) {
    try {
      const response = await fetch(`${api}/api/task-instances/${instanceId}/assignment`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ assignedUserId }) });
      if (!response.ok) throw new Error((await response.json()).error ?? "Réaffectation impossible");
      const updated = await response.json();
      setInstances((items) => items.map((item) => item.id === instanceId ? { ...item, assignedUserId: updated.assignedUserId } : item));
      setNotice("Instance réaffectée");
    } catch (reason) { setNotice(reason instanceof Error ? reason.message : "Réaffectation impossible"); }
  }

  async function cancelInstance(instanceId: string) {
    try {
      const response = await fetch(`${api}/api/task-instances/${instanceId}/cancel`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ requestingUserId: currentUser?.id }) });
      if (!response.ok) throw new Error((await response.json()).error ?? "Annulation impossible");
      const updated = await response.json();
      setInstances((items) => items.map((item) => item.id === instanceId ? { ...item, status: updated.status } : item));
      setNotice("Instance annulée");
    } catch (reason) { setNotice(reason instanceof Error ? reason.message : "Annulation impossible"); }
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
          {apiError && <div className="api-warning">API indisponible. {apiMessage || "Les données affichées sont locales et les modifications ne seront pas enregistrées."}</div>}
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
          <div className="nav-label personal-label">MON ESPACE</div>
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
          <button className={view === "Préférences" ? "nav-item active" : "nav-item"} onClick={() => navigate("/preferences")}>
            ⚙ <span>Préférences</span>
          </button>
          <button className={view === "Notifications" ? "nav-item active" : "nav-item"} onClick={() => navigate("/notifications")}>
            ◉ <span>Notifications</span>{unreadNotifications > 0 && <b className="notification-badge">{unreadNotifications > 99 ? "99+" : unreadNotifications}</b>}
          </button>
          {currentUser.isAdmin && (
            <>
              <div className="nav-label admin-label">ADMINISTRATION</div>
              <button className={view === "Affectations" ? "nav-item active" : "nav-item"} onClick={() => navigate("/assignments")}>
                ▦ <span>Affectations</span>
              </button>
              <button className={view === "Instances" ? "nav-item active" : "nav-item"} onClick={() => navigate("/instances")}>
                ◫ <span>Instances</span>
              </button>
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
              <button
                className={view === "Statistiques" ? "nav-item active" : "nav-item"}
                onClick={() => navigate("/analytics")}
              >
                📊 <span>Statistiques</span>
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
            onClick={() => { void fetch(`${api}/api/users/logout`, { method: "POST" }); setCurrentUser(null); navigate("/"); }}
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
              {view === "Semaine" || view === "Historique" ? "MON ESPACE" : "ADMINISTRATION"} <i>/</i> {view === "Semaine" ? "SEMAINE ACTIVE" : view.toUpperCase()}
            </span>
            <h1>{view}</h1>
          </div>
          <div className="week-switcher">
            <button aria-label="Semaine précédente" onClick={() => changeWeek(-1)}>←</button>
            <span>
              <b>Semaine {week.week}</b>
              <small>{selectedWeek ? `${weekStart(week.year, week.week).toLocaleDateString("fr-FR", { day: "numeric", month: "short" })} - ${new Date(weekStart(week.year, week.week).getTime() + 6 * 86400000).toLocaleDateString("fr-FR", { day: "numeric", month: "short" })}` : weekLabel(weekOffset)}</small>
            </span>
            <button aria-label="Semaine suivante" onClick={() => changeWeek(1)}>→</button>
          </div>
        </header>
        {notice && <div className="notice">✓ {notice}</div>}
        {apiError && <div className="api-warning page-warning">API indisponible. Démarre le serveur sur http://localhost:5251 pour enregistrer les modifications.</div>}
        {!apiError && apiMessage && <div className="api-warning page-warning">{apiMessage}</div>}
        {view === "Semaine" ? (
          <>
            <section className="hero-banner">
              <div>
                <span className="section-kicker light">
                  VOTRE FOYER · SEMAINE {week.week}
                </span>
                <h2>On avance, une tâche après l’autre.</h2>
                <p>
                  {personalInstances.length - completed} tâches restent à faire. La
                  répartition est en place.
                </p>
                <span className={weekStatus === "Closed" ? "week-status closed" : "week-status"}>{weekStatus === "Closed" ? "Semaine clôturée · lecture seule" : "Semaine active"}</span>
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
                  <b>{personalInstances.length - completed}</b>
                  <small>À faire</small>
                </span>
              </div>
              <div className="stat">
                <span className="stat-icon teal">↗</span>
                <span>
                  <b>{users.filter((user) => user.isActive).length}</b>
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
                      Mes tâches
                    </h2>
                  </div>
                  <CreateTaskDialog
                    api={api}
                    user={currentUser}
                    templates={templates}
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
                        className={weekStatus === "Closed" ? "check-button locked" : "check-button"}
                        disabled={weekStatus === "Closed"}
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
        ) : view === "Notifications" ? (
          <NotificationsPanel api={api} onUnreadChange={setUnreadNotifications} onOpenNotification={openNotification} />
        ) : view === "Préférences" ? (
          <PreferencesPanel api={api} />
        ) : view === "Historique" ? (
          <HistoryPanel api={api} users={users} isAdmin={currentUser.isAdmin} currentUserId={currentUser.id} />
        ) : view === "Affectations" ? (
          <AssignmentsPanel instances={instances} templates={templates} users={users} year={week.year} weekNumber={week.week} api={api} busyAction={busyAction} onAction={runWeekAction} />
        ) : view === "Instances" ? (
          <InstanceAdminPanel instances={instances} templates={templates} users={users} onReassigned={reassignInstance} onCancelled={cancelInstance} />
        ) : view === "Tâches" ? (
          <TaskCatalog
            templates={templates}
            users={users}
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
            onUpdated={(updated) => {
              setUsers((items) => items.map((item) => item.id === updated.id ? updated : item))
              setNotice("Profil mis à jour")
            }}
          />
        ) : view === "Statistiques" ? (
          <AnalyticsPanel api={api} weekId={currentWeekId} week={week} />
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
            <div className="task-properties"><div><span>Priorité</span><b>{priorityLabels[template(selected.taskTemplateId)?.priority ?? "Normal"]}</b></div><div><span>Fréquence</span><b>{frequencyLabels[template(selected.taskTemplateId)?.frequencyType ?? "Weekly"]}</b></div><div><span>Pénibilité</span><b>{template(selected.taskTemplateId)?.manualDifficultyScore ?? "Non renseignée"}/100</b></div><div><span>Durée estimée</span><b>{template(selected.taskTemplateId)?.estimatedDurationMinutes ? `${template(selected.taskTemplateId)?.estimatedDurationMinutes} min` : "Non renseignée"}</b></div><div><span>Échéance</span><b>{new Date(selected.dueDate).toLocaleDateString("fr-FR", { dateStyle: "long" })}</b></div><div><span>État</span><b>{selected.isCompleted ? "Terminée" : selected.status === "Cancelled" ? "Annulée" : "À faire"}</b></div></div>
            {template(selected.taskTemplateId) && (currentUser.isAdmin || template(selected.taskTemplateId)?.createdByUserId === currentUser.id) && <EditTaskDialog api={api} user={currentUser} template={template(selected.taskTemplateId)!} templates={templates} authorName={users.find((entry) => entry.id === template(selected.taskTemplateId)?.createdByUserId)?.displayName} onUpdated={(updated) => { setTemplates((items) => items.map((item) => item.id === updated.id ? updated : item)); setNotice("Tâche mise à jour") }} />}
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
            {template(selected.taskTemplateId)?.frequencyType === "DailyFlexible" && <FlexibleExecutionPanel api={api} instance={selected} onCompleted={() => { setInstances((items) => items.map((item) => item.id === selected.id ? { ...item, isCompleted: true, status: "Completed" } : item)); setSelected((item) => item ? { ...item, isCompleted: true, status: "Completed" } : item); setNotice("Exécution enregistrée") }} />}
            <CommentsPanel api={api} instanceId={selected.id} currentUser={currentUser} users={users} />
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
