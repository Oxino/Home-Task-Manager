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

function safeNumber(value: number | null | undefined, digits = 1) {
  if (typeof value !== "number" || Number.isNaN(value)) return "0.0";
  return value.toFixed(digits);
}

export function AnalyticsPanel({ api, weekId, week }: { api: string; weekId: string | null; week: { year: number; week: number } }) {
  const [weekStats, setWeekStats] = useState<WeekStatistics | null>(null);
  const [completionStats, setCompletionStats] = useState<CompletionStatistics | null>(null);
  const [loadDist, setLoadDist] = useState<LoadDistribution | null>(null);
  const [loading, setLoading] = useState(true);
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
    </section>
  );
}
