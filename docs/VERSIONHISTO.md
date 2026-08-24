# VERSIONHISTO

> Historique chronologique du projet **Home Task Manager**, sous forme de timeline de versions.
> Ce document est un journal de bord ; il ne remplace pas `DECISIONS.md` (ADR) ni `BACKLOG.md`
> (périmètre MVP), qui restent les sources de vérité pour les décisions et la planification.

---

## Timeline

### V1 — Conception complète

- Rédaction intégrale de la documentation métier et technique :
  `PRODUCT_VISION.md`, `ARCHITECTURE.md`, `DOMAIN_MODEL.md`, `USE_CASES.md`,
  `BUSINESS_RULES.md`, `ASSIGNMENT_ENGINE.md`, `RISKS_AND_EDGE_CASES.md`,
  `DECISIONS.md`, `BACKLOG.md`, `COPILOT_INSTRUCTIONS.md`.
- Analyse de cohérence entre documents et résolution des ambiguïtés (R-01 à R-33),
  actée dans `DECISIONS.md` (ADR-001 à ADR-019).
- Définition du périmètre MVP et du hors-périmètre V1 (absences, réaffectation automatique,
  statistiques avancées, gamification, IA, mobile natif).

### V2 — Squelette du projet

- Création de la solution .NET (`HomeTaskManager.sln`) en Clean Architecture :
  `Domain`, `Application`, `Infrastructure`, `Api`.
- Création des projets de tests xUnit associés (`Domain.Tests`, `Application.Tests`).
- Mise en place des dépendances NuGet (EF Core, SQLite, Npgsql, FluentAssertions, xUnit).
- API minimale générée par le template (`WeatherForecastController`), aucune logique métier.

### V3 — Noyau du Domaine

- Enums métier : `TaskPriority`, `FrequencyType`, `TaskInstanceStatus`, `WeekStatus`.
- `DomainValidationException` pour les invariants métier.
- Value object `WeekKey` (semaine ISO-8601, gestion des années à 53 semaines).
- Entités `User` et `TaskTemplate` avec leurs règles de validation (bornes, valeurs par défaut).
- Entités `Week` (cycle Active/Closed) et `TaskInstance` (état initial, complétude, verrouillage).
- Première suite de tests unitaires Domain.

### V4 — Application & Infrastructure (fondations)

- Ports Application : `IClock`, `IUnitOfWork`, `IUserRepository`, `ITaskTemplateRepository`,
  `IWeekRepository`, `ITaskInstanceRepository`, `IAssignmentRunRepository`.
- `HomeTaskManagerDbContext` (EF Core) avec mapping et index (unicité `DisplayName`, `Title`,
  `(Year, WeekNumber)`, `(TaskTemplateId, WeekId, PlannedDate)`).
- Configuration SQLite (développement) / PostgreSQL (production) — ADR-019.
- Implémentations EF Core des repositories, injectées dans l'API.

### V5 — Gestion des profils et des tâches

- Cas d'usage `CreateUserUseCase`, `ListActiveProfilesUseCase`, `SelectProfileUseCase`
  (sélection de profil, unicité `DisplayName`, rejet des profils inactifs).
- Cas d'usage `CreateTaskTemplateUseCase`, `ListActiveTaskTemplatesUseCase`,
  `UpdateTaskTemplateUseCase` (droits auteur/admin, unicité du titre).
- `UsersController` et `TaskTemplatesController` (endpoints REST).
- `ForbiddenOperationException` introduite pour distinguer autorisation et validation métier.

### V6 — Génération des semaines et des instances

- `GenerateWeekInstancesUseCase` (UC-GEN-01) : création de la semaine si nécessaire,
  génération des instances selon la fréquence (quotidienne obligatoire/flexible,
  hebdomadaire, mensuelle, ponctuelle), idempotence, unicité (BR-008).
- `GetWeekInstancesUseCase` pour consulter une semaine générée.
- `WeeksController` (génération + consultation).

### V7 — Moteur d'affectation

- Abstraction `IAssignmentStrategy`, `AssignmentContext`, `AssignmentResult` (pattern Strategy).
- Entité `AssignmentRun` et `RunAssignmentEngineUseCase` (UC-ENG-01/03) : autorisation admin,
  écrasement lors d'une relance (ADR-013), respect des tâches reportées (ADR-005),
  gestion de l'absence de participant actif (R-14).
- Stratégie `RandomStrategy` (baseline V1), puis `IntelligentV1Strategy` : score
  (`TaskScoreCalculator`, BR-052), historique et fenêtre d'équité (BR-053), pénalité
  anti-répétition (BR-054), départage déterministe (BR-048).
- `AssignmentsController`.

### V8 — Complétude des tâches et clôture de semaine

- `SetTaskInstanceCompletionUseCase` (UC-CMP-01/02) : seul le participant assigné modifie
  sa complétude, verrouillage automatique à la clôture (BR-043/BR-055).
- `CloseWeekUseCase` (UC-GEN-02) : clôture admin, report des instances non terminées vers la
  semaine suivante (`IsCarryOver`, `ParentTaskInstanceId`, ADR-005), création de la semaine
  suivante si besoin, gestion des collisions d'unicité.
- `TaskInstancesController` (complétude), extension de `WeeksController` (clôture).

### V9 — Paramètres administrables du moteur

- Entité `SystemSetting` (clé/valeur, BR-058).
- `AssignmentEngineSettingsProvider` (lecture avec repli sur les valeurs par défaut),
  `GetAssignmentEngineSettingsUseCase`, `UpdateAssignmentEngineSettingsUseCase`
  (admin uniquement, bornes validées).
- `AssignmentEngineSettingsController`.
- Le moteur d'affectation lit désormais ses paramètres depuis la configuration persistée.

### V10 — Journal d'audit générique *(version actuelle)*

- Entité `AuditLog` (BR-011, BR-017, BR-033).
- Port `IAuditLogger` (écriture) et `IAuditLogRepository` (lecture), constantes
  `AuditEntityTypes` / `AuditActions`.
- Traçage branché sur : création de profil, création/modification de modèle de tâche,
  exécution du moteur d'affectation, changement de complétude, clôture de semaine,
  modification des paramètres du moteur.
- `AuditLogsController` (consultation par entité).

---

## État actuel (fin V10)

Le backend couvre l'intégralité du flux MVP documenté de bout en bout :

```text
Profils → Modèles de tâches → Génération de semaine → Moteur d'affectation
        → Complétude utilisateur → Clôture / report → Audit transversal
```

- **62 tests unitaires** (Domain + Application), tous verts.
- Build complet de la solution sans erreur.
- Aucune interface utilisateur (frontend) n'existe encore.
- Aucune migration EF Core n'a encore été générée (le schéma n'a pas été matérialisé en base).

---

## Reste à faire — version actuelle (V10 / finalisation backend MVP)

- [ ] Générer et valider la première migration EF Core (SQLite) et vérifier la création du schéma.
- [ ] Tests d'intégration API (bout en bout, via `WebApplicationFactory`) en complément des tests
      unitaires Application/Domain.
- [ ] Authentification/autorisation transverse au niveau API (actuellement, `RequestingUserId`
      est transmis dans le corps des requêtes ; pas de middleware de session/contexte utilisateur).
- [ ] Entité `TaskComment` (commentaires sur une instance de tâche — BR-013, R-18), absente du
      Domain à ce jour.
- [ ] `UserPreferences` complet (au minimum activation des notifications — R-21).
- [ ] `Reminder` (rappels de tâches) : entité documentée (`ReminderTriggerType`,
      `ReminderOffsetUnit`) mais non implémentée.
- [ ] Endpoints de gestion des utilisateurs (désactivation/réactivation — UC-USR-03) et de
      réaffectation manuelle d'une instance (UC-ENG-04), non couverts par un cas d'usage dédié.
- [ ] Revue de sécurité (validation des entrées API, limites de taille, protection des endpoints
      d'administration) avant toute exposition réseau.

## Reste à faire — versions futures

### V11 — Notifications (Phase 6 du BACKLOG)

- `INotificationService` (port Application) et implémentation Web Push (navigateur).
- Déclenchement des rappels (`Reminder`) via un scheduler (génération hebdomadaire, fin de
  semaine, fin de journée — BR-051, fuseau horaire applicatif).
- Préférences de notification par utilisateur.

### V12 — Frontend (React + TypeScript)

- Écrans : sélection de profil, tableau de bord semaine, gestion des tâches, administration.
- Appels API typés, aucune règle métier côté client (conforme `ARCHITECTURE.md` §2.2).
- Justification d'affectation visible par l'utilisateur (UC-ENG-02, BR-033).

### V13 — Optimisation UX (Phase 7 du BACKLOG)

- Dashboard avec indicateurs de base (nombre de tâches, complétées, restantes, % d'avancement —
  BR-029/BR-030).
- Responsive mobile.

### V14 — Déploiement

- Bascule de SQLite vers PostgreSQL en production (ADR-019), validation des différences de types.
- Conteneurisation Docker Compose (ADR-018 — non utilisé en développement, cible de déploiement
  uniquement, Raspberry Pi 5).

### Hors périmètre (reporté au-delà du MVP, voir `BACKLOG.md`)

- Gestion des absences (`UserAbsence`) et exclusion des absents par le moteur (BR-025, R-06).
- Réaffectation automatique sur indisponibilité (BR-042, R-05).
- Statistiques avancées et tendances historiques.
- Gamification, IA générative, application mobile native, synchronisation externe.
