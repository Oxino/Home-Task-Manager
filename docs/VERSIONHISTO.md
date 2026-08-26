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

### V10 — Journal d'audit générique

- Entité `AuditLog` (BR-011, BR-017, BR-033).
- Port `IAuditLogger` (écriture) et `IAuditLogRepository` (lecture), constantes
  `AuditEntityTypes` / `AuditActions`.
- Traçage branché sur : création de profil, création/modification de modèle de tâche,
  exécution du moteur d'affectation, changement de complétude, clôture de semaine,
  modification des paramètres du moteur.
- `AuditLogsController` (consultation par entité).
- Nettoyage du code d'exemple du template : suppression de `WeatherForecast` et de
  `WeatherForecastController`, et remplacement de la requête `/weatherforecast/` dans
  `HomeTaskManager.Api.http` par des requêtes sur les endpoints réels.

### V11 — Première interface frontend

- Création du projet `src/HomeTaskManager.Web` avec React, TypeScript et Vite.
- Écran de sélection de profil conforme à l'authentification locale (ADR-006).
- Tableau de bord de la semaine active avec indicateurs de complétude et équilibre de charge.
- Liste filtrable des instances, affichage des tâches reportées et prioritaires, bascule de
  complétude et détail de la justification d'affectation.
- Navigation conditionnée au rôle administrateur et base responsive desktop/mobile.
- Proxy de développement Vite vers l'API locale (`http://localhost:5251`) et jeu de données de
  démonstration non persistant lorsque l'API est indisponible.
- Build frontend vérifié avec `npm run build`.
- Génération locale de 20 semaines historiques (ISO 16 à 35 de 2026) via l’endpoint de
  développement `POST /api/development/seed-history`, avec vérification de l’idempotence.
- Séparation de l’interface en espace personnel et administration : `/week`, `/history`,
  `/assignments`, `/instances`, `/tasks`, `/participants` et `/assignment-engine`.
- Ajout de la réaffectation et de l’annulation manuelles des instances, protégées admin et
  historisées.
- Ajout de la gestion des profils inactifs et de leur réactivation depuis `/participants`.
- Ajout du cycle de vie des modèles de tâches : liste admin active/inactive et activation ou
  désactivation logique via `PUT /api/task-templates/{id}/active`.
- Ajout de l’annulation admin des instances via `PUT /api/task-instances/{id}/cancel`, avec
  confirmation frontend et verrouillage des instances terminées ou de semaines clôturées.
- Cloisonnement serveur de l’historique : les participants ne reçoivent que leurs propres
  événements d’audit, tandis que l’admin conserve la vue globale.
- Ajout de la configuration persistante des rappels par modèle de tâche, avec les routes
  `GET/POST /api/task-templates/{id}/reminders` et le panneau frontend associé.
- Ajout d’une boîte de notifications personnelle avec distinction lu/non lu, marquage individuel,
  marquage global et suppression des notifications lues.
- Les affectations du moteur alimentent désormais cette boîte avec une notification par
  participant affecté.
- Ajout du traitement automatique des rappels échus via `ProcessReminderNotificationsUseCase`
  et `ReminderNotificationBackgroundService`, avec respect des préférences utilisateur et
  déduplication des notifications par instance et rappel.

### V12 — Services et Analytics

- Abstraction `INotificationService` pour envoyer les notifications (push et reminders).
- Intégration du service de notification dans le processor des rappels et le moteur d'affectation.
- `SystemClock` (implémentation concrète de `IClock`) enregistrée comme Singleton.
- Trois use cases d'analytics :
  - `GetWeekStatisticsUseCase` : charge par participant et semaine.
  - `GetCompletionStatisticsUseCase` : taux de complétude sur 28 jours.
  - `GetUserLoadDistributionUseCase` : distribution de charge sur 20 semaines historiques.
- `AnalyticsController` : trois endpoints admin pour les statistiques.
- Tests d'idempotence validés.
- Backward compatibility maintenue sur les constructeurs optionnels.

### V13 — Commentaires avancés, migrations EF officielles et finition UX analytics *(version actuelle)*

- Ajout de la réaction sur commentaires : entité `Reaction`, repository, use cases
  (`AddReactionUseCase`, `RemoveReactionUseCase`, `ListReactionsForCommentUseCase`) et
  endpoints API `POST/DELETE/GET` sur `/api/comments/...`.
- Support du threading de commentaires avec `ParentCommentId` sur `TaskComment`.
- Migration de l'initialisation base de données vers EF Core migrations : suppression du
  bootstrap SQL manuel au démarrage API, remplacement par `Database.Migrate()` en développement.
- Génération de la migration initiale (`InitialCreate`) et snapshot EF Core.
- Ajout d'un dashboard analytics frontend (`/analytics`) branché sur les endpoints admin,
  avec vue responsive (KPI, tableaux, indicateurs de complétion, états vides/chargement).
- Raffinement des tests `DailyFlexible` (permissions, semaine clôturée, multi-exécutions),
  plus tests dédiés des réactions.

### V13.1 — Stabilisation notifications, données de démonstration et UX d'erreur

- Ajout de liens explicites avec `instanceId` dans les notifications d'affectation et de rappel,
  avec compatibilité pour les anciens liens utilisant un fragment URL.
- Ouverture automatique de l'instance de tâche depuis une notification, après chargement de la
  semaine concernée.
- Ajout du backfill des liens de notifications historiques et d'un endpoint de seed complet
  pour un foyer de démonstration avec deux utilisateurs, tâches, historique et scénarios variés.
- Séparation frontend entre erreurs réseau/API indisponible, erreurs d'authentification et
  messages métier renvoyés par l'API.
- Correction d'un warning React lié à une mise à jour d'état parent pendant le rendu des
  notifications.
- Refonte de la navigation temporelle : le dashboard reste sur la semaine active, les semaines
  passées sont ouvertes depuis l'historique avec un bandeau de contexte et un retour rapide.
- Ajout d'un calendrier mensuel signalant les jours ayant enregistré des activités.
- Suite validée à **95 tests** : 76 Application et 19 Domain.

### V13.2 — Finition UX, pagination et règles de report

- Ajout de la date de complétion sur `TaskInstance`, avec restauration cohérente lors de la
  réouverture et backfill des événements historiques de complétion.
- Ajout d'un calendrier mensuel des complétions dans Analytics, filtrable par participant,
  avec chargement différé du mois affiché, détails journaliers et états de chargement.
- Ajout de la préférence utilisateur de format de date (`Weekday` ou `Numeric`) et migration
  associée.
- Ajout de filtres dashboard par titre, fréquence, date et retard, avec indication visuelle des
  tâches en retard et action de réinitialisation.
- Ajout de la pagination côté API pour l'historique d'audit et les notifications, réduisant le
  volume chargé par le navigateur.
- Ajout d'un scénario de démonstration administrateur pour les reports, complétions et
  notifications, ainsi que les informations d'origine des tâches reportées.
- Règle métier appliquée et testée : une instance déjà reportée non terminée ne peut pas être
  reportée une seconde fois et est exclue des calculs historiques du moteur.
- Ouverture des tâches du dashboard et des entrées navigables de l'historique par double-clic.
- Catalogue des modèles de tâches enrichi d'un filtre actif/archivé; l'archivage reste logique
  afin de préserver l'historique.
- Navigation de l'historique étendue aux huit semaines récentes, semaine active incluse et
  signalée visuellement.
- Actualisation silencieuse des notifications toutes les 60 secondes, avec rechargement de la
  page affichée uniquement lorsqu'une nouvelle notification ou un changement de non-lues est
  détecté.
- Ajout des types de tâches répétables **Flexible** et **Hebdomadaire fluide**. Le premier crée
  l'occurrence suivante après une réalisation; le second gère des jours actifs multiples et la
  validation de la semaine à la clôture.
- Les jours actifs sont persistés par migration EF et configurables dans les formulaires de
  création et de modification, avec une explication de fonctionnement pour chaque type.
- Suite validée à **97 tests** : 78 Application et 19 Domain.

---

## Etat actuel (fin V13.2)

La version V13.2 introduit deux types de tâches répétables : **Flexible**, avec une seule
instance qui avance de jour en jour, et **Hebdomadaire fluide**, configurable par jours actifs
avec validation de la semaine à la clôture. Ces règles sont persistées, exposées par l'API et
expliquées dans les formulaires de création et de modification.

Le backend couvre l'intégralité du flux MVP documenté de bout en bout avec analytics :

```text
Profils → Modeles de taches → Generation de semaine → Moteur d'affectation
        → Completude utilisateur → Cloture / report → Audit transversal
        → Notifications & Analytics
```

- **78 tests unitaires** (Application), **19 tests** (Domain), tous verts.
- Build complet backend et frontend (`npm run build`) sans erreur bloquante.
- Les statistiques sont disponibles backend **et** frontend via la page `/analytics`.
- Les commentaires avancés couvrent désormais le threading et les réactions.
- Les migrations EF Core sont en place et appliquées au démarrage en environnement développement.
- Les notifications d'affectation et de rappel ouvrent directement l'instance ciblée.
- Le jeu de démonstration complet et le backfill des anciennes notifications sont disponibles
  via les endpoints réservés au développement.
- Les tâches terminées exposent leur date de complétion séparément de leur échéance.
- Le calendrier des activités se trouve dans Analytics et charge uniquement le mois affiché,
  avec filtre participant et skeleton pendant le chargement.
- L'historique est paginé par pages de 10 événements, avec une distinction explicite entre
  l'historique personnel et le journal d'audit administrateur; il permet aussi de rejoindre les
  semaines récentes, dont la semaine active.
- Les modèles archivés sont conservés pour l'historique, filtrables dans le catalogue et
  réactivables par un administrateur.
- Les notifications sont paginées et vérifiées périodiquement sans rechargement visible inutile.

---

## Ancien état (fin V12)

- **70 tests unitaires** (Application), **19 tests** (Domain), tous verts.
- Analytics backend disponible (use cases + controller), mais pas encore la vue frontend.
- Pas encore de migration EF Core matérialisée ni activation au démarrage.

---

## Reste à faire — ordre prioritaire

- [x] Gestion des modèles de tâches : archivage logique, filtrage des états et cohérence des
  parcours CRUD exposés.
- [ ] Poursuite de la gestion de l'historique : couverture end-to-end de l'historique personnel
  et du journal d'audit.
- [ ] Revue sécurité pré-exposition réseau (validation d'entrée, limites payload, durcissement
  endpoints admin, politique CORS/cookies).
- [ ] Tests d'intégration API bout en bout (`WebApplicationFactory`) en complément des tests
  unitaires Application/Domain.
- [ ] Tests end-to-end frontend (desktop/mobile) sur les parcours critiques.

## Reste à faire — versions futures

### V14 — Notifications avancées

- Web Push navigateur multi-appareils (VAPID, abonnement client et persistance des subscriptions),
  retry policy et télémétrie de délivrabilité.
- Préférences utilisateur plus fines (types d'événements, plages horaires).

### V15 — Complément frontend

- Écrans restants : gestion complète des tâches, administration des participants, paramètres et
  historique.
- Appels API typés, aucune règle métier côté client (conforme `ARCHITECTURE.md` §2.2).
- Justification d'affectation visible par l'utilisateur (UC-ENG-02, BR-033).

### V16 — Optimisation UX

- Amélioration des vues historiques et des interactions CRUD.
- Tests end-to-end desktop/mobile.

### V17 — Déploiement

- Bascule de SQLite vers PostgreSQL en production (ADR-019), validation des différences de types.
- Conteneurisation Docker Compose (ADR-018 — non utilisé en développement, cible de déploiement
  uniquement, Raspberry Pi 5).

### Hors périmètre (reporté au-delà du MVP, voir `BACKLOG.md`)

- Gestion des absences (`UserAbsence`) et exclusion des absents par le moteur (BR-025, R-06).
- Réaffectation automatique sur indisponibilité (BR-042, R-05).
- Statistiques avancées et tendances historiques.
- Gamification, IA générative, application mobile native, synchronisation externe.
