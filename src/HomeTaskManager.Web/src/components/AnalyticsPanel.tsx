import { useEffect, useState } from "react";
import { formatDurationFromHours } from "../format";

type ParticipantLoad = {
  userId: string;
  userName: string;
  totalTasks: number;
  completedTasks: number;
  openTasks: number;
  totalPoints: number;
  estimatedHours: number;
};

type WeekStatistics = { participantLoads?: ParticipantLoad[] };
type MonthStatistics = { participantLoads?: ParticipantLoad[] };

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

// ISO week helpers, kept in sync with the equivalents in App.tsx.
function isoWeek(date = new Date()) {
  const value = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
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

function shiftWeek(current: { year: number; week: number }, delta: number) {
  const start = weekStart(current.year, current.week);
  return isoWeek(new Date(start.getTime() + delta * 7 * 86400000));
}

function sumParticipantLoads(rows: ParticipantLoad[]) {
  return rows.reduce(
    (acc, row) => ({
      totalPoints: acc.totalPoints + row.totalPoints,
      estimatedHours: acc.estimatedHours + row.estimatedHours,
      completedTasks: acc.completedTasks + row.completedTasks,
      totalTasks: acc.totalTasks + row.totalTasks,
    }),
    { totalPoints: 0, estimatedHours: 0, completedTasks: 0, totalTasks: 0 },
  );
}

function LoadKpis({ rows }: { rows: ParticipantLoad[] }) {
  const totals = sumParticipantLoads(rows);
  const completion = totals.totalTasks > 0 ? (totals.completedTasks * 100) / totals.totalTasks : 0;
  return (
    <div className="analytics-kpis">
      <article className="analytics-kpi">
        <small>Charge totale</small>
        <b>{totals.totalPoints}</b>
        <span>points cumulés</span>
      </article>
      <article className="analytics-kpi">
        <small>Temps estimé</small>
        <b>{formatDurationFromHours(totals.estimatedHours)}</b>
        <span>{totals.totalTasks} tâche{totals.totalTasks > 1 ? "s" : ""} attribuée{totals.totalTasks > 1 ? "s" : ""}</span>
      </article>
      <article className="analytics-kpi">
        <small>Complétion</small>
        <b>{safeNumber(completion, 0)}%</b>
        <span>{totals.completedTasks}/{totals.totalTasks} tâches</span>
      </article>
    </div>
  );
}

function LoadTable({ rows, emptyLabel }: { rows: ParticipantLoad[]; emptyLabel: string }) {
  return (
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
        {rows.length === 0 && (
          <tr>
            <td colSpan={4} className="analytics-empty-cell">{emptyLabel}</td>
          </tr>
        )}
        {rows.map((row) => (
          <tr key={row.userId}>
            <td data-label="Participant">{row.userName}</td>
            <td data-label="Tâches">{row.totalTasks}</td>
            <td data-label="Points">{row.totalPoints}</td>
            <td data-label="Heures">{formatDurationFromHours(row.estimatedHours)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

export function AnalyticsPanel({ api, weekId, week, users }: { api: string; weekId: string | null; week: { year: number; week: number }; users: ActivityUser[] }) {
  // --- "Semaine courante" zone: navigable week loaded independently from the app's active week. ---
  const [viewedWeek, setViewedWeek] = useState(week);
  useEffect(() => { setViewedWeek(week); }, [week.year, week.week]);

  const [viewedWeekId, setViewedWeekId] = useState<string | null>(weekId);
  const [weekNotGenerated, setWeekNotGenerated] = useState(false);
  useEffect(() => {
    if (viewedWeek.year === week.year && viewedWeek.week === week.week) {
      setViewedWeekId(weekId);
      setWeekNotGenerated(false);
      return;
    }
    let cancelled = false;
    fetch(`${api}/api/weeks/${viewedWeek.year}/${viewedWeek.week}/instances?includeAllParticipants=true`)
      .then(async (response) => {
        if (cancelled) return;
        if (response.ok) {
          const data = await response.json();
          setViewedWeekId(data?.week?.id ?? null);
          setWeekNotGenerated(!data?.week?.id);
        } else {
          setViewedWeekId(null);
          setWeekNotGenerated(true);
        }
      })
      .catch(() => { if (!cancelled) { setViewedWeekId(null); setWeekNotGenerated(true); } });
    return () => { cancelled = true; };
  }, [api, viewedWeek.year, viewedWeek.week, week.year, week.week, weekId]);

  const [weekStats, setWeekStats] = useState<WeekStatistics | null>(null);
  const [weekStatsLoading, setWeekStatsLoading] = useState(true);
  useEffect(() => {
    if (!viewedWeekId) {
      setWeekStats(null);
      setWeekStatsLoading(false);
      return;
    }
    let cancelled = false;
    setWeekStatsLoading(true);
    fetch(`${api}/api/analytics/week/${viewedWeekId}`)
      .then((response) => (response.ok ? response.json() : null))
      .then((data) => { if (!cancelled) setWeekStats(data); })
      .catch(() => { if (!cancelled) setWeekStats(null); })
      .finally(() => { if (!cancelled) setWeekStatsLoading(false); });
    return () => { cancelled = true; };
  }, [api, viewedWeekId]);

  const isCurrentViewedWeek = viewedWeek.year === week.year && viewedWeek.week === week.week;

  // --- "Mois courant" zone: fixed to the current calendar month. ---
  const now = new Date();
  const currentMonthLabel = now.toLocaleDateString("fr-FR", { month: "long", year: "numeric" });
  const [monthStats, setMonthStats] = useState<MonthStatistics | null>(null);
  const [monthLoading, setMonthLoading] = useState(true);
  useEffect(() => {
    let cancelled = false;
    setMonthLoading(true);
    fetch(`${api}/api/analytics/month/${now.getFullYear()}/${now.getMonth() + 1}`)
      .then((response) => (response.ok ? response.json() : null))
      .then((data) => { if (!cancelled) setMonthStats(data); })
      .catch(() => { if (!cancelled) setMonthStats(null); })
      .finally(() => { if (!cancelled) setMonthLoading(false); });
    return () => { cancelled = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api]);

  // --- "Vue globale" zone: completion over 28 days and load distribution over 20 weeks. ---
  const [completionStats, setCompletionStats] = useState<CompletionStatistics | null>(null);
  const [loadDist, setLoadDist] = useState<LoadDistribution | null>(null);
  const [globalLoading, setGlobalLoading] = useState(true);
  const [error, setError] = useState("");
  useEffect(() => {
    let cancelled = false;
    setGlobalLoading(true);
    setError("");
    Promise.all([
      fetch(`${api}/api/analytics/completion`).then((r) => (r.ok ? r.json() : null)),
      fetch(`${api}/api/analytics/load-distribution`).then((r) => (r.ok ? r.json() : null)),
    ])
      .then(([c, l]) => {
        if (cancelled) return;
        setCompletionStats(c);
        setLoadDist(l);
      })
      .catch(() => { if (!cancelled) setError("Impossible de charger les statistiques globales pour le moment."); })
      .finally(() => { if (!cancelled) setGlobalLoading(false); });
    return () => { cancelled = true; };
  }, [api]);

  const [activityMonth, setActivityMonth] = useState(() => new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  const [activityEntries, setActivityEntries] = useState<ActivityEntry[]>([]);
  const [activityLoading, setActivityLoading] = useState(true);
  const [activityParticipantId, setActivityParticipantId] = useState("all");
  const [selectedActivityDay, setSelectedActivityDay] = useState<number | null>(null);

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

  const participantLoads = weekStats?.participantLoads ?? [];
  const monthParticipantLoads = monthStats?.participantLoads ?? [];
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

  return (
    <section className="analytics-panel">
      <div className="panel-heading">
        <div>
          <span className="section-kicker">ADMINISTRATION</span>
          <h2>Statistiques Analytics</h2>
          <p className="panel-subtitle">Charge, complétion et tendances des participants.</p>
        </div>
      </div>

      {error && <p className="form-error">{error}</p>}

      {/* Zone : semaine courante (navigable) */}
      <div className="analytics-zone">
        <div className="analytics-zone-header">
          <div>
            <span className="section-kicker">SEMAINE</span>
            <h3>{isCurrentViewedWeek ? "Semaine courante" : "Semaine consultée"} {viewedWeek.week} • {viewedWeek.year}</h3>
          </div>
          <div className="calendar-controls">
            <button type="button" className="calendar-button" aria-label="Semaine précédente" onClick={() => setViewedWeek((current) => shiftWeek(current, -1))}>←</button>
            {!isCurrentViewedWeek && (
              <button type="button" className="secondary-button analytics-week-reset" onClick={() => setViewedWeek(week)}>Revenir à la semaine courante</button>
            )}
            <button type="button" className="calendar-button" aria-label="Semaine suivante" onClick={() => setViewedWeek((current) => shiftWeek(current, 1))}>→</button>
          </div>
        </div>

        {weekStatsLoading ? (
          <div className="analytics-skeleton" />
        ) : !viewedWeekId || weekNotGenerated ? (
          <div className="analytics-empty">
            <h3>Aucune donnée</h3>
            <p>Aucune instance n'a été générée pour la semaine {viewedWeek.week} de {viewedWeek.year}.</p>
          </div>
        ) : (
          <>
            <LoadKpis rows={participantLoads} />
            <article className="analytics-card analytics-card-wide">
              <h3>Charge par participant</h3>
              <LoadTable rows={participantLoads} emptyLabel="Aucune donnée" />
            </article>
          </>
        )}
      </div>

      {/* Zone : mois courant (calendaire, fixe) */}
      <div className="analytics-zone">
        <div className="analytics-zone-header">
          <div>
            <span className="section-kicker">MOIS</span>
            <h3>Mois courant • {currentMonthLabel}</h3>
          </div>
        </div>

        {monthLoading ? (
          <div className="analytics-skeleton" />
        ) : (
          <>
            <LoadKpis rows={monthParticipantLoads} />
            <article className="analytics-card analytics-card-wide">
              <h3>Charge par participant ce mois-ci</h3>
              <LoadTable rows={monthParticipantLoads} emptyLabel="Aucune donnée" />
            </article>
          </>
        )}
      </div>

      {/* Zone : vue globale (tendances sur plusieurs semaines) */}
      <div className="analytics-zone">
        <div className="analytics-zone-header">
          <div>
            <span className="section-kicker">GLOBAL</span>
            <h3>Vue globale</h3>
          </div>
        </div>

        {globalLoading ? (
          <div className="analytics-skeleton-grid">
            <div className="analytics-skeleton" />
            <div className="analytics-skeleton" />
          </div>
        ) : (
          <div className="analytics-grid">
            <article className="analytics-card">
              <h3>Complétude (28 derniers jours)</h3>
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
                      <td data-label="Participant">{row.userName}</td>
                      <td data-label="Complétées">{row.completedTasks}</td>
                      <td data-label="Total">{row.totalTasks}</td>
                      <td data-label="Taux">
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

            <article className="analytics-card">
              <h3>Distribution de charge (20 dernières semaines)</h3>
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
                      <td data-label="Participant">{row.userName}</td>
                      <td data-label="Charge moyenne">{safeNumber(row.averageLoad, 2)} tâches</td>
                      <td data-label="Indice">
                        <span className={Math.abs(row.averageLoad - 1) < 0.2 ? "load-chip balanced" : "load-chip"}>
                          {safeNumber(row.averageLoad * 100, 0)}%
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </article>
          </div>
        )}

        <article className="analytics-card activity-calendar">
          <div className="calendar-heading"><div><span className="section-kicker">COMPLÉTIONS</span><h3>Jours de complétion</h3></div><div className="calendar-controls"><select className="calendar-participant-filter" value={activityParticipantId} onChange={(event) => setActivityParticipantId(event.target.value)} aria-label="Filtrer les complétions par participant"><option value="all">Tous les participants</option>{users.map((user) => <option value={user.id} key={user.id}>{user.displayName}</option>)}</select><button type="button" className="calendar-button" aria-label="Mois précédent" onClick={() => setActivityMonth((current) => new Date(current.getFullYear(), current.getMonth() - 1, 1))}>←</button><b>{activityMonth.toLocaleDateString("fr-FR", { month: "long", year: "numeric" })}</b><button type="button" className="calendar-button" aria-label="Mois suivant" onClick={() => setActivityMonth((current) => new Date(current.getFullYear(), current.getMonth() + 1, 1))}>→</button></div></div>
          {activityLoading ? <div className="calendar-skeleton" aria-busy="true" aria-label="Chargement du calendrier"><span /><span /><span /><span /><span /><span /><span /><span /><span /><span /><span /><span /><span /><span /></div> : <><div className="calendar-grid calendar-weekdays">{["L", "M", "M", "J", "V", "S", "D"].map((day, index) => <span key={`${day}-${index}`}>{day}</span>)}</div><div className="calendar-grid">{activityCells.map((day, index) => { const details = day === null ? [] : activityDetails(day); const active = details.length > 0; const total = details.reduce((sum, item) => sum + item.count, 0); const isSelected = selectedActivityDay === day; return <div className={active ? "calendar-day-cell active" : "calendar-day-cell"} key={index}>{day !== null && <button type="button" className="calendar-day" aria-label={`${day} : ${total} complétion(s)`} aria-pressed={isSelected} onClick={() => active && setSelectedActivityDay(isSelected ? null : day)}>{day}{active && <><i /><small>{total}</small></>}</button>}</div>; })}</div>{selectedActivityDay !== null && activityDetails(selectedActivityDay).length > 0 && <div className="calendar-day-details" aria-live="polite"><strong>{selectedActivityDay} {activityMonth.toLocaleDateString("fr-FR", { month: "long" })}</strong>{activityDetails(selectedActivityDay).map((item) => <div className="calendar-participant-detail" key={item.userId}><span>{item.name}</span><b>{item.count} tâche{item.count > 1 ? "s" : ""} complétée{item.count > 1 ? "s" : ""}</b></div>)}</div>}<small className="calendar-legend"><i /> Jour de complétion d’une tâche · Cliquer pour le détail</small></>}
        </article>
      </div>
    </section>
  );
}
