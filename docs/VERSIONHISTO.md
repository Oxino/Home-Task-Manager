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
- Ajout du choix administrable du moteur de répartition avec `INTELLIGENT_V1`, `RANDOM`,
  `BALANCED_POINTS` et `BALANCED_TIME`. Chaque moteur dispose d'une explication dans la page
  de paramétrage, et le choix est persisté dans `SystemSetting`.
- `RANDOM` distribue les tâches sans historique ni charge, avec un nombre de tâches équilibré;
  les moteurs équilibrés utilisent respectivement les points ou la durée estimée.
- Analyse de conformité de `INTELLIGENT_V1` : score, disponibilité, historique, pénalité et
  départage sont conformes à la documentation; les absences restent hors périmètre V1.
- Suite validée à **106 tests** : 84 Application et 22 Domain.

### V13.3 — Correction de l'affichage de la date de complétion

- Correction du markup du détail d'une instance : `completion-date-property` est rendu dans
  `.task-properties`, après l'état, et reste en dehors du `<select>` de délégation.
- Correction de la perte du bloc par le navigateur, qui ignore les éléments `<div>` placés dans
  un `<select>`.
- Ajout d'une migration EF Core de compatibilité qui renseigne `CompletedAt` avec `UpdatedAt`
  pour les anciennes instances terminées encore dépourvues de timestamp.
- Conservation de la distinction fonctionnelle entre `DueDate` (échéance) et `CompletedAt`
  (instant de complétion), exposée dans les DTO et l'interface.
- Build frontend vérifié avec `npm run build`; aucune erreur TypeScript détectée.

### V13.4 — Notification lors d'une réaffectation

- Une réaffectation manuelle crée désormais une notification pour le nouveau responsable.
- Le message mentionne le titre de la tâche et le nom de l'administrateur qui a effectué la
  réaffectation.
- La notification contient un lien direct vers l'instance réaffectée.
- Le comportement est couvert par un test Application ciblé.

### V13.5 — Réaffectation par le responsable

- Un participant peut désormais réaffecter une instance qui lui est attribuée à un autre
  participant actif depuis le détail de la tâche.
- Un administrateur conserve la possibilité de réaffecter n'importe quelle instance depuis la
  page d'administration des instances.
- L'autorisation est contrôlée côté API: un participant ne peut pas réaffecter l'instance d'un
  autre participant.

### V13.6 — Préférences de notification avec 3 niveaux

- Système de préférences de notification à 3 niveaux : **Aucune**, **Importante**, **Toutes**.
  - **Aucune** : Aucune notification reçue.
  - **Importante** : Seulement les notifications critiques (réaffectations manuelles).
  - **Toutes** : Toutes les notifications (défaut) incluant affectations, rappels, réaffectations.
- Les notifications d'affectation et rappels (type Normal) respectent la préférence **Toutes**.
- Les notifications de réaffectation (type Important) respectent les préférences **Importante** et **Toutes**.
- Interface de préférence utilisateur mise à jour avec dropdown pour sélectionner le niveau.
- Filtre appliqué au moment de la création des notifications dans tous les use cases.

### V13.7 — Blocage de la réaffectation des tâches complétées

- Une tâche complétée ne peut plus être réaffectée (ni manuellement, ni admin).
- Validation au niveau du domaine (méthode `TaskInstance.Reassign()`) avec message d'erreur clair.
- Interface utilisateur : le bouton de réaffectation est masqué si la tâche est complétée.
- Couvert par un test Application dédié.
- Harmonisation : tous les flux de notification passent par `INotificationService` avec importance paramétrisée.

### V13.8 — Réglette de pénibilité, priorisation du jour et signalement des reports en doublon

- Le champ pénibilité des formulaires de création et de modification de modèle de tâche est
  désormais une réglette (`input type="range"`, 1 à 100) avec affichage de la valeur courante,
  remplaçant le champ numérique.
- Le dashboard sépare désormais les tâches personnelles en deux groupes : **Aujourd'hui**
  (échéance du jour ou passée) et **À venir**, chacun avec son compteur et un message dédié
  lorsque le groupe est vide.
- Les tâches reportées (`isCarryOver`) reprennent le même fond blanc que les tâches normales sur
  le dashboard ; seule l'étiquette **REPORTÉE** les distingue visuellement, pour réduire
  l'agressivité visuelle précédente.
- Nouvel indicateur **« VERSION EN RETARD CHEZ UN AUTRE »** : lorsqu'une instance normale de la
  semaine coexiste avec une instance reportée non terminée du même modèle (donc affectée à un
  autre participant), une étiquette s'affiche sur la carte de la tâche. Le détail de l'instance
  (modale) affiche en complément le nom du participant concerné et l'échéance de la version en
  retard, pour permettre aux participants de se synchroniser en dehors de l'application.

### V13.9 — Correctifs cache de session et périmètre des instances

- Correction du changement de profil : la sélection de profil attend désormais la confirmation
  serveur avant de poursuivre, puis réinitialise tout l'état lié à la session précédente
  (instances, semaine courante, notifications, messages d'erreur) au lieu de laisser des
  données de l'ancien profil s'afficher pendant que la nouvelle session se met en place.
- `GET /api/weeks/{year}/{weekNumber}/instances` ne retourne plus systématiquement toutes les
  instances à un administrateur : le périmètre par défaut est désormais toujours restreint à
  l'utilisateur courant, y compris pour un administrateur consultant son propre tableau de bord.
  La vue multi-participants devient un paramètre explicite (`includeAllParticipants=true`),
  vérifié côté serveur et réservé aux administrateurs.
- Les pages d'administration **Affectations** et **Instances** utilisent désormais une source de
  données dédiée à la vue multi-participants, synchronisée après réaffectation, annulation,
  génération et lancement d'affectation, sans régression sur leur périmètre global.
- La récupération du détail d'une instance référencée dans l'historique journalise désormais
  l'erreur rencontrée (403 ou autre) au lieu de l'ignorer silencieusement, tout en continuant
  sur les entrées suivantes sans interrompre l'affichage.

### V13.10 — Réorganisation du panneau d'action de la page Affectations

- Le mode d'emploi de la page **Affectations** reflète désormais l'ordre réel d'utilisation :
  **1. Clôturer**, **2. Générer les instances**, **3. Lancer l'affectation**, au lieu de
  présenter la clôture en dernier alors qu'elle doit précéder la génération de la nouvelle
  semaine courante.
- Les trois boutons d'action, auparavant regroupés séparément dans l'en-tête du panneau, sont
  désormais intégrés chacun dans leur cellule du mode d'emploi, à côté de l'explication de
  l'étape correspondante.
- L'option **« Annuler les tâches inachevées à la clôture »** est désormais rattachée à la
  cellule de l'étape Clôturer plutôt qu'affichée comme un bloc indépendant sous le statut de
  semaine.
- Les trois cellules d'action sont disposées horizontalement (une colonne par étape), avec un
  repli en une seule colonne sur petit écran.

### V13.11 — Répartition configurable des tâches quotidiennes obligatoires

- Nouveau réglage par modèle `DailyMandatory` : `DistributeDailyMandatory` (BR-061), modifiable
  dans les formulaires de création et de modification, visible uniquement pour cette fréquence.
- Valeur par défaut `true` : les 7 instances de la semaine sont désormais réparties entre les
  participants par le moteur d'affectation, comme n'importe quelle autre fréquence.
- Valeur `false` : comportement historique conservé, un seul tirage détermine le propriétaire
  des 7 instances de la semaine.
- Ce comportement est désormais appliqué de façon identique par les quatre moteurs
  (`INTELLIGENT_V1`, `RANDOM`, `BALANCED_POINTS`, `BALANCED_TIME`) — auparavant seul
  `INTELLIGENT_V1` gérait la responsabilité unique, les trois autres moteurs répartissaient
  systématiquement sans pouvoir la désactiver.
- Migration EF Core ajoutant la colonne avec valeur par défaut `true` pour les modèles existants.
- Tests dédiés ajoutés pour les deux modes sur `IntelligentV1Strategy`.

### V13.12 — Refonte de l'algorithme INTELLIGENT_V1 (construction + recherche locale)

- Constat issu du test manuel du jeu de données : `INTELLIGENT_V1` traitait les instances de la
  semaine dans l'ordre chronologique en une seule passe irrévocable. Une seule décision sur une
  tâche à fort score (ex. 45 pts), prise tôt dans la semaine, pouvait faire basculer
  définitivement tout le reste vers un seul participant, sans aucune correction possible —
  produisant des semaines à 100 %/0 % entre deux participants.
- Nouvel algorithme en deux temps :
  1. **Construction gloutonne par score décroissant (LPT)** : la tâche la plus lourde est
     placée en premier, pendant que les participants sont encore proches en charge, au lieu
     d'être décidée au hasard de l'ordre chronologique.
  2. **Recherche locale bornée** : le moteur cherche ensuite le meilleur déplacement d'une seule
     tâche ou le meilleur échange entre deux tâches qui réduirait la variance des charges
     normalisées entre participants, et répète jusqu'à ne plus trouver d'amélioration ou
     atteindre un plafond de 500 itérations (garde-fou de performance).
- Les 7 instances d'une tâche quotidienne obligatoire non répartie (`DistributeDailyMandatory =
  false`) restent regroupées comme une seule unité tout au long de la construction et de la
  recherche locale : elles se déplacent toujours ensemble.
- Le départage reste déterministe (BR-048), désormais appliqué à la construction et à la
  recherche locale plutôt qu'à une séquence unique de décisions.
- Formules de score, de disponibilité, de fenêtre d'équité et de pénalité anti-répétition
  inchangées (BR-052 à BR-054) : seul le mécanisme de décision a changé.
- Nouveau test dédié reproduisant le scénario régressé (historique proche, une grosse tâche
  parmi plusieurs petites) et vérifiant que les deux participants reçoivent au moins une
  instance, au lieu d'un balayage complet d'un seul côté.
- Validation manuelle sur le jeu de données de test (2 participants, 4 semaines consécutives,
  historique enchaîné) : les semaines à balayage complet ont disparu.

  | Semaine | Ancien algorithme (passe chronologique) | Nouvel algorithme (LPT + recherche locale) |
  |---------|------------------------------------------|---------------------------------------------|
  | S33 | 9 / 3 | 8 / 4 |
  | S34 | 6 / 5 | 7 / 4 |
  | S35 | 11 / 0 | 4 / 7 |
  | S36 | 12 / 0 | 7 / 5 |

- Documentation mise à jour : `ASSIGNMENT_ENGINE.md` (section INTELLIGENT_V1 réécrite) et
  BR-048 (`BUSINESS_RULES.md`).

### V13.13 — Départage rotatif pour BALANCED_POINTS et BALANCED_TIME

- Constat issu du test manuel comparatif des 4 moteurs sur le même jeu de données : `BALANCED_TIME`
  produisait exactement le même motif jour par jour sur deux semaines consécutives (S34 et S35),
  toujours en faveur du même participant. Cause identifiée : ces deux moteurs n'ont aucune
  mémoire d'une semaine à l'autre (charge repartie à zéro), et leur départage en cas d'égalité
  stricte de charge se faisait sur l'identifiant du participant — toujours le même gagnant tant
  que le jeu de tâches restait structurellement identique (mêmes durées, aucune variation).
- Nouveau départage rotatif (BR-062) : en cas d'égalité stricte de charge normalisée puis de
  nombre de tâches déjà affectées, le participant gagnant alterne parmi ceux encore à égalité au
  lieu de toujours favoriser le même identifiant. Un curseur de rotation avance à chaque égalité
  effective (au moins deux participants à égalité), ce qui distribue l'égalité en tourniquet.
- Validation manuelle du premier correctif : insuffisant. Le curseur repartait de zéro à chaque
  semaine (chaque appel de `Assign`), donc une même séquence d'égalités rejouait le même motif
  tant que le jeu de tâches restait identique — `BALANCED_TIME` reproduisait toujours exactement
  le même enchaînement jour par jour sur S34 et S35.
- Correctif complémentaire : le curseur de rotation est initialisé à partir de l'année et du
  numéro de semaine traités (`(Year × 53 + WeekNumber) % nombre de participants`), au lieu de
  repartir de zéro à chaque appel. Ce calcul reste une fonction pure de la semaine traitée, sans
  aucun état persisté entre deux exécutions — conforme au principe « sans mémoire » de ces deux
  moteurs.
- Validation manuelle après correctif complémentaire : S34 et S35 produisent désormais des motifs
  différents pour la tâche quotidienne obligatoire (ex. Alice favorisée en S34, Benoît favorisé
  en S35), au lieu d'un motif identique reconduit à l'identique chaque semaine.
- Changement localisé à `BalancedAssignment.Assign` (`BalancedStrategies.cs`), partagé par
  `BALANCED_POINTS` et `BALANCED_TIME`. Aucun changement pour `INTELLIGENT_V1` (BR-048, déjà
  déterministe par construction/recherche locale) ni pour `RANDOM` (déjà non déterministe).
- Nouveau test dédié vérifiant qu'une série de tâches de poids strictement égal ne favorise plus
  un seul participant.
- Documentation mise à jour : `ASSIGNMENT_ENGINE.md` (sections BALANCED_POINTS et BALANCED_TIME)
  et règle BR-062 (`BUSINESS_RULES.md`), précisant l'initialisation du curseur par semaine.

---

## Etat actuel (fin V13.13)

La version V13.2 introduit deux types de tâches répétables : **Flexible**, avec une seule
instance qui avance de jour en jour, et **Hebdomadaire fluide**, configurable par jours actifs
avec validation de la semaine à la clôture. Ces règles sont persistées, exposées par l'API et
expliquées dans les formulaires de création et de modification.

Elle ajoute également une date configurable aux tâches mensuelles : début, milieu, fin du mois
ou jour précis, avec plafonnement automatique au dernier jour des mois courts.

Les tâches hebdomadaires disposent désormais d'une échéance configurable au début, au milieu
ou à la fin de la semaine, exposée dans les formulaires de création et de modification.

La classification de l'historique est désormais définie par la nature de l'action plutôt que
par le rôle du profil auteur; la pagination API applique aussi ce filtre côté serveur.

Les audits de génération d'instances (`Week/Generated`) et de modification des préférences
(`UserPreferences/Updated`) sont désormais enregistrés explicitement.

Les actions d'historique liées à une tâche retournent désormais vers le dashboard de la semaine
concernée, avec sélection de l'instance; les cartes de semaines récentes restent un parcours
séparé de navigation temporelle.

L'affichage de l'historique distingue maintenant le parcours **Semaines passées** du **Journal
d'activité**. Les événements affichent le titre du modèle ou de la tâche quand il est disponible,
ainsi que la semaine d'affectation et l'échéance d'une instance, séparément de la date de
l'activité.

Le détail d'une instance affiche maintenant son participant affecté. Un administrateur peut
déléguer une instance active à un autre participant actif depuis ce détail; cette action utilise
la même règle et le même audit que la vue d'administration des instances.

Les historiques sont séparés en deux routes : `/history` pour les actions du profil courant et
`/admin-history` pour le journal global réservé aux administrateurs.

Le journal administrateur est désormais dédié aux audit logs: la navigation temporelle en a été
retirée et les filtres par catégorie, entité, action et ordre chronologique sont appliqués côté
API avant pagination.

Le choix du moteur de répartition est désormais configurable et persistant. Les moteurs
`RANDOM`, `BALANCED_POINTS` et `BALANCED_TIME` ont été ajoutés en complément de `INTELLIGENT_V1`;
la page d'administration explique leurs règles et leurs étapes de décision.

Les correctifs suivants ont complété cette évolution : compatibilité avec les anciennes
variantes de nommage des événements de préférence et classification de toutes les entités de
préférences comme actions utilisateur, quel que soit leur libellé historique.

Le backend couvre l'intégralité du flux MVP documenté de bout en bout avec analytics :

```text
Profils → Modeles de taches → Generation de semaine → Moteur d'affectation
        → Completude utilisateur → Cloture / report → Audit transversal
        → Notifications & Analytics
```

- **93 tests unitaires** (Application), **24 tests** (Domain), tous verts.
- Build complet backend et frontend (`npm run build`) sans erreur bloquante.
- Les statistiques sont disponibles backend **et** frontend via la page `/analytics`.
- Les commentaires avancés couvrent désormais le threading et les réactions.
- Les migrations EF Core sont en place et appliquées au démarrage en environnement développement.
- Les notifications d'affectation et de rappel ouvrent directement l'instance ciblée.
- Le jeu de démonstration complet et le backfill des anciennes notifications sont disponibles
  via les endpoints réservés au développement.
- Les tâches terminées exposent leur date de complétion séparément de leur échéance.
- Le détail d'une tâche terminée affiche cette date dans la grille de propriétés. Les données
  historiques sans timestamp sont compatibilisées par migration, avec `UpdatedAt` comme valeur
  de repli explicitement non exacte.
- Le calendrier des activités se trouve dans Analytics et charge uniquement le mois affiché,
  avec filtre participant et skeleton pendant le chargement.
- Le dashboard distingue les tâches du jour des tâches à venir, et signale par une étiquette
  dédiée toute tâche dont une version reportée du même modèle reste non terminée chez un autre
  participant, sans accentuer visuellement les tâches reportées elles-mêmes.
- La pénibilité d'un modèle de tâche se définit via une réglette plutôt qu'un champ numérique.
- L'historique est paginé par pages de 10 événements, avec une distinction explicite entre
  l'historique personnel et le journal d'audit administrateur; il permet aussi de rejoindre les
  semaines récentes, dont la semaine active.
- Les modèles archivés sont conservés pour l'historique, filtrables dans le catalogue et
  réactivables par un administrateur.
- Les notifications sont paginées et vérifiées périodiquement sans rechargement visible inutile.
- Le changement de profil réinitialise l'état de session avant de recharger les données, et le
  tableau de bord personnel ne retourne plus que les instances de l'utilisateur courant, même
  pour un administrateur; les vues d'administration globales conservent une source dédiée pour
  le périmètre multi-participants.
- Le panneau d'action de la page Affectations regroupe, pour chacune des trois étapes (clôturer,
  générer, affecter), son explication et son bouton dans une cellule unique, disposées
  horizontalement et dans l'ordre réel d'utilisation.
- La répartition d'une tâche quotidienne obligatoire est configurable par modèle
  (`DistributeDailyMandatory`, défaut réparti entre participants) et appliquée de façon
  identique par les quatre moteurs d'affectation.
- `INTELLIGENT_V1` construit sa répartition hebdomadaire par un algorithme glouton décroissant
  (LPT) suivi d'une recherche locale bornée, plutôt qu'une seule passe chronologique
  irrévocable, afin d'éviter qu'une décision isolée sur une tâche à fort score ne bascule toute
  la semaine vers un seul participant.

---

## Ancien état (fin V12)

- **70 tests unitaires** (Application), **19 tests** (Domain), tous verts.
- Analytics backend disponible (use cases + controller), mais pas encore la vue frontend.
- Pas encore de migration EF Core matérialisée ni activation au démarrage.

---

## Reste à faire — ordre prioritaire

- [x] Gestion des modèles de tâches : archivage logique, filtrage des états et cohérence des
  parcours CRUD exposés.
- [x] Poursuite de la gestion de l'historique : couverture end-to-end de l'historique personnel
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
