# ARCHITECTURE

> Document d'architecture cible de **Home Task Manager**.
> Conception technique uniquement — aucun code.
> Source de vérité : `PRODUCT_VISION.md`, `DOMAIN_MODEL.md`, `BUSINESS_RULES.md`,
> `ASSIGNMENT_ENGINE.md`, `DECISIONS.md`.

---

# 1. Vue d'ensemble

Home Task Manager est une application web auto-hébergée de gestion et de répartition
intelligente des tâches domestiques.

Caractéristiques structurantes :

- application **mono-foyer**, auto-hébergée (cible : Raspberry Pi 5) ;
- **backend** ASP.NET Core 9 en **Clean Architecture** ;
- **frontend** React + TypeScript, responsive ;
- **base de données** PostgreSQL ;
- déploiement via **Docker Compose** ;
- cœur métier : un **moteur d'affectation extensible** (pattern Strategy).

## Principes directeurs

1. Correctitude métier avant tout (COPILOT_INSTRUCTIONS).
2. Séparation stricte des responsabilités (Clean Architecture).
3. Historique immuable, jamais purgé (ADR-004, BR-057).
4. Décisions d'affectation traçables et explicables (BR-033).
5. Extensibilité : nouveaux algorithmes, canaux de notification, statistiques.

---

# 2. Architecture logicielle

## 2.1 Vue en couches (Clean Architecture)

```text
┌───────────────────────────────────────────────────────────────┐
│                          Frontend (React)                      │
│         Écrans, composants, appels API, état local             │
└───────────────────────────────▲───────────────────────────────┘
                                 │ HTTP/JSON (REST)
┌───────────────────────────────┴───────────────────────────────┐
│                              API                               │
│   Endpoints REST, validation, mapping DTO, authz, DI          │
└───────────────────────────────▲───────────────────────────────┘
                                 │
┌───────────────────────────────┴───────────────────────────────┐
│                          Application                           │
│  Use cases, services métier, interfaces (ports), orchestration │
└───────────────────────────────▲───────────────────────────────┘
                                 │
┌───────────────────────────────┴───────────────────────────────┐
│                            Domain                             │
│   Entités, value objects, enums, règles métier, moteur (abstr.)│
└───────────────────────────────────────────────────────────────┘
                                 ▲
                                 │ implémente les ports
┌───────────────────────────────┴───────────────────────────────┐
│                        Infrastructure                         │
│  EF Core, PostgreSQL, notifications, scheduler, settings, time │
└───────────────────────────────────────────────────────────────┘
```

Règle de dépendance : les flèches pointent vers l'intérieur. Le **Domain** ne dépend de rien ;
l'**Infrastructure** et l'**API** dépendent de l'**Application** et du **Domain** via des
interfaces (inversion de dépendance).

## 2.2 Responsabilités par couche

### Domain

- Entités et agrégats (voir `DOMAIN_MODEL.md`).
- Value objects : `WeekKey (Year, WeekNumber)`, `Score`, `Percentage`, `DifficultyScore`.
- Enums : `FrequencyType`, `TaskPriority`, `TaskInstanceStatus`, `WeekStatus`,
  `ReminderTriggerType`, `ReminderOffsetUnit`.
- Règles métier invariantes (bornes, unicité fonctionnelle, transitions d'état).
- Abstraction du moteur : `IAssignmentStrategy` (contrat), objets de calcul de score.
- **Aucune** dépendance technique.

### Application

- **Use cases** (un par cas d'usage de `USE_CASES.md`), orchestrant le domaine.
- Services métier transverses : génération d'instances, clôture hebdomadaire, calcul de score,
  production des explications.
- **Ports** (interfaces) : `IUnitOfWork`, `IUserRepository`, `ITaskTemplateRepository`,
  `ITaskInstanceRepository`, `IAssignmentRunRepository`, `IWeekRepository`,
  `ISystemSettingProvider`, `INotificationService`, `IClock`, `IAuditLogger`.
- Aucune dépendance directe à EF Core ou PostgreSQL.

### Infrastructure

- Implémentations EF Core des repositories et de l'`IUnitOfWork`.
- `DbContext`, configurations de mapping, migrations.
- `SystemSettingProvider` (lecture/écriture des paramètres).
- `NotificationService` (implémentation actuelle orientée logging), architecture multi-canal
  extensible avec port `INotificationService` prêt pour Web Push navigateur.
- `Scheduler` (génération hebdomadaire, déclenchement des rappels, clôture des semaines).
- `SystemClock` (implémente `IClock`, fuseau horaire applicatif, BR-051).

### API

- Contrôleurs / endpoints REST minces.
- Validation des requêtes (DTO), mapping DTO ↔ commandes/queries.
- Autorisation (profil courant, `IsAdmin`).
- Injection de dépendances, gestion des erreurs, journalisation.

### Frontend

- Application React + TypeScript construite avec Vite dans `src/HomeTaskManager.Web`.
- Écrans de présentation : sélection de profil, tableau de bord hebdomadaire, liste filtrable
  des instances, détail d'affectation, commentaires avec réactions, navigation administrateur,
  et vue analytics (`/analytics`).
- Le DTO d'instance expose `CompletedAt` séparément de `DueDate`; le détail rend cette date dans
  la grille `.task-properties`, en dehors de tout élément `<select>`.
- Appels REST vers l'API ASP.NET via `fetch`, avec le profil courant transmis aux commandes qui
  le requièrent (`RequestingUserId` / `TriggeredByUserId`).
- L'autorisation métier reste contrôlée par l'API ; l'état du profil côté client ne constitue
  pas une preuve d'autorisation.
- En développement, Vite relaie `/api` vers `http://localhost:5251`. L'URL peut être remplacée
  par `VITE_API_URL`.
- Un jeu de données de démonstration est utilisé uniquement si l'API est indisponible ; il ne
  constitue pas une source métier persistante.
- L'API expose en développement une commande réservée à un administrateur,
  `POST /api/development/seed-history`, pour générer des semaines historiques de test. Cette
  commande orchestre les use cases de génération et d'affectation, et ne remplace pas le
  scheduler de production.
- Les mutations admin d’instance sont exposées par `PUT /api/task-instances/{id}/assignment`
  et `PUT /api/task-instances/{id}/cancel`. Les deux actions sont contrôlées par le rôle et
  historisées.
- L’endpoint `GET /api/audit-logs` est authentifié : un participant reçoit uniquement les
  événements qu’il a effectués ; un administrateur peut consulter l’historique global. Le
  frontend classe les événements par nature d'action (`user` ou `admin`), indépendamment du
  rôle du profil auteur; le paramètre `category` conserve cette pagination côté API. `/history`
  force la portée personnelle, y compris pour un administrateur, tandis que `/admin-history`
  expose le journal global aux administrateurs. Le journal admin n'effectue pas de chargement de
  semaines et accepte aussi `entityType`, `action` et `sort=oldest|newest` avant pagination.
  Les préférences existantes restent compatibles avec les variantes historiques de leur
  `EntityType`.
- Les rappels sont configurés au niveau du modèle via `GET/POST /api/task-templates/{id}/reminders`.
  Leur stockage est persistant ; `ProcessReminderNotificationsUseCase` est exécuté par un
  `BackgroundService` périodique et crée des notifications idempotentes pour les rappels échus.
  Les liens générés contiennent l'identifiant de l'instance afin que le frontend puisse ouvrir
  directement la tâche concernée. L’envoi Web Push reste séparé.
- La boîte de notifications personnelles est exposée par `GET /api/notifications`, avec les
  actions `PUT /api/notifications/{id}/read`, `POST /api/notifications/read-all` et
  `DELETE /api/notifications/read`. Chaque requête est filtrée par l’identité de session.
- Les statistiques admin sont exposées par `GET /api/analytics/week/{weekId}`,
  `GET /api/analytics/completion` et `GET /api/analytics/load-distribution`.
- Les réactions de commentaires sont exposées par `POST /api/comments/{commentId}/reactions`,
  `DELETE /api/comments/reactions/{reactionId}` et `GET /api/comments/{commentId}/reactions`.
- La lecture authentifiée de `GET /api/task-instances/{id}` renvoie l'instance et sa semaine;
  elle permet à l'historique de rejoindre `/week?year=&week=&instanceId=` sans exposer la page
  d'administration des instances aux participants.
- Une affectation produite par le moteur crée une notification personnelle pour le participant
  retenu, avec un lien vers la semaine et l'instance concernées.
- Aucune règle métier complexe : l'interface présente les états et déclenche les cas d'usage
  exposés par l'API.

## 2.3 Style applicatif

- Approche **use-case / CQRS léger** : séparation commandes (écriture) / queries (lecture)
  sans event sourcing.
- **Pattern Strategy** pour le moteur (`IAssignmentStrategy`) : `IntelligentV1Strategy` en V1,
  extensible (`RandomStrategy`, `BalancedPointsStrategy`, `BalancedTimeStrategy`, `IntelligentV2`).
- **Unit of Work + Repository** pour les frontières de persistance.
- **Audit transversal** : chaque commande mutant une entité importante écrit un `AuditLog`.

---

# 3. Modèle de base de données

## 3.1 Vue relationnelle (entités principales)

```text
User ──< UserAbsence
User ──< UserPreferences (1-1)
User ──< TaskTemplate (CreatedByUserId)

TaskTemplate ──1─ TaskScore
TaskTemplate ──< Reminder
TaskTemplate ──< TaskInstance

Week ──< AssignmentRun
Week ──< TaskInstance

AssignmentStrategy ──< AssignmentRun
AssignmentRun ──< TaskInstance
AssignmentRun ──< AssignmentMetricSnapshot

TaskInstance ──< TaskCompletion
TaskInstance ──< CompletionChangeLog
TaskInstance ──< DailyFlexibleExecution
TaskInstance ──< TaskComment
TaskInstance ──1 AssignmentExplanation
TaskInstance ──? TaskInstance (ParentTaskInstanceId, self-ref carry-over)

SystemSetting  (clé/valeur global)
AuditLog       (générique, EntityType + EntityId)
```

## 3.2 Tables et clés (synthèse)

| Table | PK | FK principales | Notes |
|-------|----|----------------|-------|
| `User` | Id | — | `DisplayName` unique, `IsAdmin`, `AvailabilityPercentage` |
| `UserAbsence` | Id | UserId | `EndDate >= StartDate` (hors V1) |
| `UserPreferences` | Id | UserId (unique) | `NotificationsEnabled` |
| `TaskTemplate` | Id | CreatedByUserId | `Title` unique, `Priority`, `FrequencyType` |
| `TaskScore` | TaskTemplateId | TaskTemplateId | `CalculatedScore` recalculable |
| `Reminder` | Id | TaskTemplateId | `TriggerType`, `OffsetValue/Unit` |
| `Week` | Id | — | `(Year, WeekNumber)` unique, `Status` |
| `AssignmentStrategy` | Id | — | `IsActive` |
| `AssignmentRun` | Id | WeekId, StrategyId, TriggeredByUserId | exécution |
| `TaskInstance` | Id | TaskTemplateId, AssignmentRunId, WeekId, AssignedUserId, ParentTaskInstanceId? | occurrence |
| `TaskCompletion` | Id | TaskInstanceId, CompletedByUserId | réalisation |
| `CompletionChangeLog` | Id | TaskInstanceId, ChangedByUserId | bascules complété↔non |
| `DailyFlexibleExecution` | Id | TaskInstanceId, ExecutedByUserId | exécutions multiples |
| `TaskComment` | Id | TaskInstanceId, AuthorUserId | commentaires |
| `AssignmentExplanation` | Id | TaskInstanceId, AssignedUserId | justification |
| `AssignmentMetricSnapshot` | Id | AssignmentRunId, UserId | métriques d'audit |
| `SystemSetting` | Id | UpdatedByUserId | `Key` unique |
| `AuditLog` | Id | PerformedByUserId | `EntityType` + `EntityId` |

## 3.3 Index recommandés

- `User(DisplayName)` unique.
- `TaskTemplate(Title)` unique ; `TaskTemplate(IsActive)`.
- `Week(Year, WeekNumber)` unique.
- `TaskInstance(WeekId, AssignedUserId)` ; `TaskInstance(TaskTemplateId, WeekId, PlannedDate)`
  unique fonctionnel (BR-008, unicité par jour planifié).
- `AssignmentRun(WeekId)`.
- `AuditLog(EntityType, EntityId)` ; `AuditLog(PerformedAt)`.
- `SystemSetting(Key)` unique.

## 3.4 Règles de persistance

- **Suppression logique uniquement** (`IsActive` / statut), jamais de DELETE physique (BR-016).
- **Aucune purge** de l'historique (BR-057).
- Contraintes explicites (FK, unicité, checks de bornes) déclarées en base (COPILOT_INSTRUCTIONS).
- Migrations gérées par EF Core.
- Toutes les dates techniques en UTC ; conversion via fuseau applicatif (BR-051) en présentation.

## 3.5 Cycle de vie d'une semaine

```text
[Génération] → Week(Active) → instances Pending/Completed modifiables
     │
     │ (fin de semaine, scheduler)
     ▼
Week(Closed) → complétude figée (BR-055), lecture seule
```

---

# 4. Moteur d'affectation

## 4.1 Contrat

```text
IAssignmentStrategy
  AssignmentResult Assign(AssignmentContext context)

AssignmentContext = {
  Week, ParticipantsActifs, TâchesNouvelles, TâchesReportées,
  HistoriqueParUtilisateur (fenêtre d'équité), Paramètres (SystemSetting)
}

AssignmentResult = {
  Affectations[] (TaskInstance → User),
  Explications[] (AssignmentExplanation),
  Snapshots[] (AssignmentMetricSnapshot)
}
```

## 4.2 Pipeline INTELLIGENT_V1

Conforme à `ASSIGNMENT_ENGINE.md` :

1. Participants actifs (BR-003).
2. (Hors V1) Exclure les absents (BR-025).
3. Conserver les reports sur leur propriétaire (ADR-005).
4. Calculer la charge historique (fenêtre d'équité, défaut 8 semaines).
5. Identifier les nouvelles tâches.
6. Équilibrer les points, cible ∝ disponibilité (BR-040).
7. Réduire les répétitions via pénalité (BR-054).
8. Produire les justifications (BR-033).

## 4.3 Calcul du score

```text
ScoreTâche = ManualDifficultyScore × MultiplicateurPriorité × (Durée / DuréeRéférence)
Pénalité   = OccurrencesRécentes × PoidsPénalité × ScoreTâche
```

Paramètres administrables (`SystemSetting`, BR-058) — valeurs par défaut :

| Clé | Défaut |
|-----|--------|
| `scoring.priority.low/normal/critical` | 0.5 / 1.0 / 1.5 |
| `scoring.duration.referenceMinutes` | 30 |
| `assignment.equityWindowWeeks` | 8 |
| `assignment.repetitionPenaltyWeight` | 0.25 |

## 4.4 Extensibilité

- Chaque stratégie est enregistrée dans le conteneur DI et sélectionnable via
  `AssignmentStrategy.IsActive`.
- Une stratégie peut exposer ses propres paramètres via `SystemSetting`.
- L'ajout d'une stratégie ne modifie jamais les stratégies existantes (Open/Closed).

---

# 5. Génération et ordonnancement

## 5.1 Composant Scheduler (Infrastructure)

Tâches planifiées :

- **Génération hebdomadaire** : création de la `Week` cible et des `TaskInstance` selon la
  fréquence des modèles actifs, puis invocation du moteur.
- **Clôture hebdomadaire** : passage de la semaine à `Closed`, gel de la complétude, report des
  instances non terminées (BR-039).
- **Rappels** : émission des notifications selon les `Reminder` (fin de journée / fin de semaine).

## 5.2 Génération d'instances par fréquence

| FrequencyType | Instances générées |
|---------------|--------------------|
| DailyMandatory | 7 (une par jour de la semaine) |
| DailyFlexible | 1 (exécutions multiples via `DailyFlexibleExecution`) |
| Weekly | 1 |
| Monthly | 1 (1re semaine ISO du mois par défaut, BR-049) |
| OneTime | 1 |

## 5.3 Relance d'affectation

Relancer pour une semaine **écrase** l'affectation existante (BR-041, ADR-013) ; l'`AssignmentRun`
précédent et ses snapshots restent conservés (historique).

---

# 6. Conception de l'API REST

## 6.1 Conventions

- Base : `/api`.
- JSON, noms de ressources au pluriel, verbes HTTP standard.
- Codes : `200/201/204` succès, `400` validation, `401/403` accès, `404` absent, `409` conflit.
- Idempotence des lectures ; commandes mutantes journalisées (audit).
- Autorisation : tout endpoint d'administration requiert `IsAdmin`.

## 6.2 Authentification (sélection de profil)

| Méthode | Route | Description | Accès |
|---------|-------|-------------|-------|
| GET | `/api/profiles` | Liste des profils actifs | public |
| POST | `/api/session` | Ouvrir une session (profileId) | public |
| DELETE | `/api/session` | Se déconnecter | connecté |
| GET | `/api/session/me` | Profil courant | connecté |

## 6.3 Utilisateurs

| Méthode | Route | Description | Accès |
|---------|-------|-------------|-------|
| GET | `/api/users` | Lister | admin |
| POST | `/api/users` | Créer | admin |
| GET | `/api/users/{id}` | Détail | admin |
| PUT | `/api/users/{id}` | Modifier | admin |
| POST | `/api/users/{id}/deactivate` | Désactiver | admin |
| POST | `/api/users/{id}/activate` | Réactiver | admin |
| GET/PUT | `/api/users/{id}/preferences` | Préférences (notifications) | soi / admin |

## 6.4 Modèles de tâches

| Méthode | Route | Description | Accès |
|---------|-------|-------------|-------|
| GET | `/api/task-templates` | Lister | connecté |
| POST | `/api/task-templates` | Créer | connecté |
| GET | `/api/task-templates/{id}` | Détail | connecté |
| PUT | `/api/task-templates/{id}` | Modifier (auteur ou admin, BR-037) | auteur/admin |
| POST | `/api/task-templates/{id}/archive` | Archiver | admin |
| POST | `/api/task-templates/{id}/deactivate` | Désactiver | admin |
| GET/PUT | `/api/task-templates/{id}/reminders` | Rappels | auteur/admin |

## 6.5 Semaines et instances

| Méthode | Route | Description | Accès |
|---------|-------|-------------|-------|
| GET | `/api/weeks/current` | Semaine courante | connecté |
| GET | `/api/weeks/{year}/{week}` | Semaine donnée | connecté |
| GET | `/api/weeks/{year}/{week}/tasks` | Instances de la semaine (admin: toutes) | connecté |
| GET | `/api/me/tasks?week=YYYY-Www` | Mes tâches de la semaine | connecté |
| POST | `/api/task-instances/{id}/complete` | Marquer terminée | propriétaire |
| POST | `/api/task-instances/{id}/uncomplete` | Retirer terminée (semaine active) | propriétaire |
| POST | `/api/task-instances/{id}/executions` | Exécution flexible | propriétaire |
| PUT | `/api/task-instances/{id}/cancel` | Annuler | admin |
| GET/POST | `/api/task-instances/{id}/comments` | Commentaires | connecté |
| GET | `/api/task-instances/{id}/explanation` | Justification d'affectation | connecté |

## 6.6 Affectation (administration)

| Méthode | Route | Description | Accès |
|---------|-------|-------------|-------|
| POST | `/api/assignments/run` | Lancer/relancer (écrase la semaine) | admin |
| PUT | `/api/task-instances/{id}/assignment` | Réaffectation manuelle | admin |
| GET | `/api/assignments/runs?week=YYYY-Www` | Historique des exécutions | admin |
| GET | `/api/strategies` | Stratégies disponibles | admin |
| PUT | `/api/strategies/active` | Sélectionner la stratégie active | admin |

## 6.7 Paramètres et tableaux de bord

| Méthode | Route | Description | Accès |
|---------|-------|-------------|-------|
| GET/PUT | `/api/settings` | Paramètres système (dont algo) | admin |
| GET | `/api/users/all` | Tous les profils, actifs et inactifs | admin |
| PUT | `/api/users/{id}` | Modifier/réactiver/désactiver un profil | admin |
| GET | `/api/dashboard/me` | Indicateurs personnels | connecté |
| GET | `/api/dashboard/admin` | Indicateurs globaux | admin |
| GET | `/api/audit-logs?entityType=&entityId=` | Journal d'audit | connecté |
| GET | `/api/audit-logs?category=user|admin&page=&pageSize=` | Historique paginé par catégorie | admin |

## 6.8 Format d'erreur (uniforme)

```json
{
  "error": {
    "code": "TASK_TITLE_ALREADY_EXISTS",
    "message": "Un modèle de tâche portant ce titre existe déjà.",
    "details": []
  }
}
```

---

# 7. Frontend

## 7.1 Écrans principaux

| Écran | Rôle | Contenu |
|-------|------|---------|
| Sélection de profil | tous | liste des profils actifs |
| Tableau de bord utilisateur | utilisateur | tâches de la semaine, progression, justification |
| Détail d'une tâche | utilisateur | complétion, exécutions flexibles, commentaires, explication |
| Mes modèles de tâches | utilisateur | création/édition de ses tâches |
| Historique personnel | utilisateur | semaines passées (lecture seule) |
| Administration – Utilisateurs | admin | CRUD utilisateurs |
| Administration – Tâches | admin | gestion de toutes les tâches |
| Administration – Affectation | admin | lancer/relancer, stratégie active, runs |
| Administration – Paramètres | admin | paramètres d'algorithme et système |
| Tableau de bord admin | admin | indicateurs globaux |

## 7.2 Principes UI

- Mobile-first, responsive (desktop / tablette / smartphone).
- Compréhension immédiate : « quoi faire » et « pourquoi » (explication d'affectation visible).
- Écrans épurés, pas de logique métier côté client.

## 7.3 État et données

- Appels API typés (TypeScript), cache de requêtes côté client.
- L'état d'affichage reflète toujours le dernier état connu (BR-012).

---

# 8. Sécurité et accès

- Authentification par sélection de profil, sans mot de passe (ADR-003 / ADR-006), adaptée à un
  réseau domestique privé.
- Autorisation basée sur le profil courant et le booléen `IsAdmin` (ADR-010).
- Cloisonnement des données : un utilisateur n'accède qu'à ses propres affectations (BR-019) ;
  l'admin accède à tout (BR-020).
- Déploiement recommandé sur réseau local ; exposition externe non prévue en V1.

---

# 9. Déploiement

## 9.1 Topologie Docker Compose

```text
┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│  frontend    │   │   backend    │   │  postgresql  │
│ (React SPA)  │──▶│ (ASP.NET 9)  │──▶│  (données)   │
└──────────────┘   └──────────────┘   └──────────────┘
        served by backend / reverse proxy
```

- **Cible principale** : Raspberry Pi 5 (ARM64).
- **Cibles futures** : mini PC, serveur personnel.
- Volumes persistants pour PostgreSQL.
- Variables d'environnement pour la configuration (connexion DB, fuseau horaire).

## 9.2 Observabilité

- Journalisation structurée (logs applicatifs).
- Journal d'audit métier (`AuditLog`) distinct des logs techniques.

---

# 10. Traçabilité architecture ↔ décisions

| Élément d'architecture | Référence |
|------------------------|-----------|
| Clean Architecture, Strategy | COPILOT_INSTRUCTIONS, ADR-002 |
| Séparation Template / Instance | ADR-001 |
| Auth par sélection de profil | ADR-003, ADR-006 |
| Historique sans purge | ADR-004, BR-057 |
| Reports sur le propriétaire | ADR-005 |
| Rattachement semaine (Week) | ADR-008, BR-038 |
| Audit + complétude bidirectionnelle | ADR-009, BR-043 |
| Rôle via booléen | ADR-010 |
| Clôture hebdomadaire | ADR-011, BR-055 |
| Priorité dans le score | ADR-012, ADR-017, BR-052 |
| Relance = écrasement | ADR-013, BR-041 |
| Paramètres d'algorithme | ADR-014, BR-058 |
| Pénalité anti-répétition | ADR-015, BR-054 |
| Départage déterministe | ADR-016, BR-048 |

---

# 11. Points hors périmètre V1 (prévus mais non implémentés)

- Prise en compte des absences dans le moteur (BR-025).
- Réaffectation automatique sur indisponibilité (BR-042).
- Statistiques avancées et tendances.
- Canaux de notification supplémentaires (au-delà du Web Push).
- Gamification, IA, application mobile native.

L'architecture réserve les points d'extension (Strategy, ports de notification, `SystemSetting`)
sans les implémenter (COPILOT_INSTRUCTIONS — section « Futur »).