import { useEffect, useState } from "react";

type ParticipantLoad = {
  userId: string;
  userName: string;
  totalTasks: number;
  totalPoints: number;
  estimatedHours: number;
};

type WeekStatistics = {
  participantLoads?: ParticipantLoad[];
};

type UserCompletion = {
  userId: string;
  userName: string;
  completedTasks: number;
  totalTasks: number;
  completionPercentage: number;
};

type CompletionStatistics = {
  userCompletions?: UserCompletion[];
};

type UserLoad = {
  userId: string;
  userName: string;
  averageLoad: number;
};

type LoadDistribution = {
  userLoads?: UserLoad[];
};
type ActivityEntry = { performedAt: string; action: string; performedByUserId: string };
type ActivityUser = { id: string; displayName: string };

function safeNumber(value: number | null | undefined, digits = 1) {
  if (typeof value !== "number" || Number.isNaN(value)) return "0.0";
  return value.toFixed(digits);
}

export function AnalyticsPanel({ api, weekId, week, users }: { api: string; weekId: string | null; week: { year: number; week: number }; users: ActivityUser[] }) {
  const [weekStats, setWeekStats] = useState<WeekStatistics | null>(null);
  const [completionStats, setCompletionStats] = useState<CompletionStatistics | null>(null);
  const [loadDist, setLoadDist] = useState<LoadDistribution | null>(null);
  const [loading, setLoading] = useState(true);
  const [activityMonth, setActivityMonth] = useState(() => new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  const [activityEntries, setActivityEntries] = useState<ActivityEntry[]>([]);
  const [activityLoading, setActivityLoading] = useState(true);
  const [activityParticipantId, setActivityParticipantId] = useState("all");
  const [selectedActivityDay, setSelectedActivityDay] = useState<number | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    const load = async () => {
      if (!weekId) {
        setLoading(false);
        return;
      }

      setLoading(true);
      setError("");
      try {
        const [w, c, l] = await Promise.all([
          fetch(`${api}/api/analytics/week/${weekId}`).then((r) => (r.ok ? r.json() : null)),
          fetch(`${api}/api/analytics/completion`).then((r) => (r.ok ? r.json() : null)),
          fetch(`${api}/api/analytics/load-distribution`).then((r) => (r.ok ? r.json() : null)),
        ]);
        setWeekStats(w);
        setCompletionStats(c);
        setLoadDist(l);
      } catch {
        setError("Impossible de charger les statistiques pour le moment.");
      } finally {
        setLoading(false);
      }
    };
    void load();
  }, [api, weekId]);

  useEffect(() => {
    const from = new Date(activityMonth.getFullYear(), activityMonth.getMonth(), 1);
    const to = new Date(activityMonth.getFullYear(), activityMonth.getMonth() + 1, 1);
    setActivityLoading(true);
    fetch(`${api}/api/audit-logs?from=${encodeURIComponent(from.toISOString())}&to=${encodeURIComponent(to.toISOString())}&action=CompletionChanged`)
      .then(async (response) => { if (response.ok) { const entries: ActivityEntry[] = await response.json(); setActivityEntries(entries.filter((entry) => entry.action === "CompletionChanged")); } else setActivityEntries([]); })
      .catch(() => setActivityEntries([]))
      .finally(() => setActivityLoading(false));
  }, [api, activityMonth]);
  useEffect(() => { setSelectedActivityDay(null); }, [activityMonth, activityParticipantId]);

  if (loading) {
    return (
      <section className="analytics-panel">
        <div className="analytics-skeleton-grid">
          <div className="analytics-skeleton" />
          <div className="analytics-skeleton" />
          <div className="analytics-skeleton analytics-skeleton-wide" />
        </div>
      </section>
    );
  }

  if (!weekId) {
    return (
      <section className="analytics-panel">
        <div className="analytics-empty">
          <h3>Semaine introuvable</h3>
          <p>Génère d'abord des instances pour la semaine sélectionnée afin d'afficher les statistiques.</p>
        </div>
      </section>
    );
  }

  const participantLoads = weekStats?.participantLoads ?? [];
  const userCompletions = completionStats?.userCompletions ?? [];
  const userLoads = loadDist?.userLoads ?? [];
  const visibleActivityEntries = activityParticipantId === "all"
    ? activityEntries
    : activityEntries.filter((entry) => entry.performedByUserId === activityParticipantId);
  const firstActivityDay = new Date(activityMonth.getFullYear(), activityMonth.getMonth(), 1);
  const activityDaysInMonth = new Date(activityMonth.getFullYear(), activityMonth.getMonth() + 1, 0).getDate();
  const activityLeadingDays = (firstActivityDay.getDay() + 6) % 7;
  const activityCells = Array.from({ length: activityLeadingDays + activityDaysInMonth }, (_, index) => index < activityLeadingDays ? null : index - activityLeadingDays + 1);
  const participantName = (id: string) => users.find((user) => user.id === id)?.displayName ?? "Participant inconnu";
  const activityDetails = (day: number) => {
    const entries = visibleActivityEntries.filter((entry) => {
      const date = new Date(entry.performedAt);
      return date.getFullYear() === activityMonth.getFullYear() && date.getMonth() === activityMonth.getMonth() && date.getDate() === day;
    });
    const counts = new Map<string, number>();
    entries.forEach((entry) => counts.set(entry.performedByUserId, (counts.get(entry.performedByUserId) ?? 0) + 1));
    return [...counts.entries()].map(([userId, count]) => ({ userId, name: participantName(userId), count }));
  };

  const totalPoints = participantLoads.reduce((sum, row) => sum + row.totalPoints, 0);
  const totalHours = participantLoads.reduce((sum, row) => sum + row.estimatedHours, 0);
  const averageCompletion = userCompletions.length > 0
    ? userCompletions.reduce((sum, row) => sum + row.completionPercentage, 0) / userCompletions.length
    : 0;

  return (
    <section className="analytics-panel">
      <div className="panel-heading">
        <span className="section-kicker">ADMINISTRATION</span>
        <h2>Statistiques Analytics</h2>
        <p className="panel-subtitle">Semaine {week.week} • {week.year}</p>
      </div>

      {error && <p className="form-error">{error}</p>}

      <div className="analytics-kpis">
        <article className="analytics-kpi">
          <small>Charge totale</small>
          <b>{totalPoints}</b>
          <span>points cumulés</span>
        </article>
        <article className="analytics-kpi">
          <small>Temps estimé</small>
          <b>{safeNumber(totalHours)}h</b>
          <span>sur la semaine</span>
        </article>
        <article className="analytics-kpi">
          <small>Complétion moyenne</small>
          <b>{safeNumber(averageCompletion)}%</b>
          <span>sur 28 jours</span>
        </article>
      </div>

      <div className="analytics-grid">
        <article className="analytics-card">
          <h3>Charge semaine {week.week}</h3>
          <table className="analytics-table">
            <thead>
              <tr>
                <th>Participant</th>
                <th>Tâches</th>
                <th>Points</th>
                <th>Heures</th>
              </tr>
            </thead>
            <tbody>
              {participantLoads.length === 0 && (
                <tr>
                  <td colSpan={4} className="analytics-empty-cell">Aucune donnée</td>
                </tr>
              )}
              {participantLoads.map((row) => (
                <tr key={row.userId}>
                  <td>{row.userName}</td>
                  <td>{row.totalTasks}</td>
                  <td>{row.totalPoints}</td>
                  <td>{safeNumber(row.estimatedHours)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </article>

        <article className="analytics-card">
          <h3>Complétude (28 jours)</h3>
          <table className="analytics-table">
            <thead>
              <tr>
                <th>Participant</th>
                <th>Complétées</th>
                <th>Total</th>
                <th>Taux</th>
              </tr>
            </thead>
            <tbody>
              {userCompletions.length === 0 && (
                <tr>
                  <td colSpan={4} className="analytics-empty-cell">Aucune donnée</td>
                </tr>
              )}
              {userCompletions.map((row) => (
                <tr key={row.userId}>
                  <td>{row.userName}</td>
                  <td>{row.completedTasks}</td>
                  <td>{row.totalTasks}</td>
                  <td>
                    <div className="completion-cell">
                      <span>{safeNumber(row.completionPercentage)}%</span>
                      <div className="completion-meter" role="presentation">
                        <i style={{ width: `${Math.min(100, Math.max(0, row.completionPercentage))}%` }} />
                      </div>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </article>
      </div>

      <article className="analytics-card analytics-card-wide">
        <h3>Distribution de charge (20 semaines)</h3>
        <table className="analytics-table">
          <thead>
            <tr>
              <th>Participant</th>
              <th>Charge moyenne</th>
              <th>Indice</th>
            </tr>
          </thead>
          <tbody>
            {userLoads.length === 0 && (
              <tr>
                <td colSpan={3} className="analytics-empty-cell">Aucune donnée</td>
              </tr>
            )}
            {userLoads.map((row) => (
              <tr key={row.userId}>
                <td>{row.userName}</td>
                <td>{safeNumber(row.averageLoad, 2)} tâches</td>
                <td>
                  <span className={Math.abs(row.averageLoad - 1) < 0.2 ? "load-chip balanced" : "load-chip"}>
                    {safeNumber(row.averageLoad * 100, 0)}%
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </article>

      <article className="analytics-card activity-calendar">
        <div className="calendar-heading"><div><span className="section-kicker">COMPLÉTIONS</span><h3>Jours de complétion</h3></div><div className="calendar-controls"><select className="calendar-participant-filter" value={activityParticipantId} onChange={(event) => setActivityParticipantId(event.target.value)} aria-label="Filtrer les complétions par participant"><option value="all">Tous les participants</option>{users.map((user) => <option value={user.id} key={user.id}>{user.displayName}</option>)}</select><button type="button" className="calendar-button" aria-label="Mois précédent" onClick={() => setActivityMonth((current) => new Date(current.getFullYear(), current.getMonth() - 1, 1))}>←</button><b>{activityMonth.toLocaleDateString("fr-FR", { month: "long", year: "numeric" })}</b><button type="button" className="calendar-button" aria-label="Mois suivant" onClick={() => setActivityMonth((current) => new Date(current.getFullYear(), current.getMonth() + 1, 1))}>→</button></div></div>
        {activityLoading ? <div className="calendar-skeleton" aria-busy="true" aria-label="Chargement du calendrier"><span /><span /><span /><span /><span /><span /><span /><span /><span /><span /><span /><span /><span /><span /></div> : <><div className="calendar-grid calendar-weekdays">{["L", "M", "M", "J", "V", "S", "D"].map((day, index) => <span key={`${day}-${index}`}>{day}</span>)}</div><div className="calendar-grid">{activityCells.map((day, index) => { const details = day === null ? [] : activityDetails(day); const active = details.length > 0; const total = details.reduce((sum, item) => sum + item.count, 0); const isSelected = selectedActivityDay === day; return <div className={active ? "calendar-day-cell active" : "calendar-day-cell"} key={index}>{day !== null && <button type="button" className="calendar-day" aria-label={`${day} : ${total} complétion(s)`} aria-pressed={isSelected} onClick={() => active && setSelectedActivityDay(isSelected ? null : day)}>{day}{active && <><i /><small>{total}</small></>}</button>}</div>; })}</div>{selectedActivityDay !== null && activityDetails(selectedActivityDay).length > 0 && <div className="calendar-day-details" aria-live="polite"><strong>{selectedActivityDay} {activityMonth.toLocaleDateString("fr-FR", { month: "long" })}</strong>{activityDetails(selectedActivityDay).map((item) => <div className="calendar-participant-detail" key={item.userId}><span>{item.name}</span><b>{item.count} tâche{item.count > 1 ? "s" : ""} complétée{item.count > 1 ? "s" : ""}</b></div>)}</div>}<small className="calendar-legend"><i /> Jour de complétion d’une tâche · Cliquer pour le détail</small></>}
      </article>
    </section>
  );
}
