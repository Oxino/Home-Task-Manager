# USE_CASES

## Objectif

Ce document décrit les cas d'usage fonctionnels de Home Task Manager.

Il traduit la vis## UC-TSK-01 — Créer un modèle de tâche

- **Acteur** : Administrateur ou Utilisateur (ADR-007).
- **Préconditions** : session valide.
- **Scénario nominal** :
  1. Saisie de `Title`, `Description`, `FrequencyType`, `ManualDifficultyScore`, `Priority`,
     durée estimée, rappels.
  2. Le système valide l'unicité du `Title` et les bornes de score.
  3. Le modèle est créé avec `IsActive = true`, `Priority = Normale` par défaut, et
     `CreatedByUserId` = auteur.
- **Alternatifs / Erreurs** :
  - A1 — `Title` déjà utilisé : rejet (unicité).
  - A2 — `ManualDifficultyScore` hors [1..100] : rejet.
  - A3 — `EstimatedDurationMinutes < 1` : rejet.
- **Postconditions** : le modèle est disponible pour les **futures** affectations ; l'affectation
  de la semaine en cours n'est pas modifiée (BR-047).
- **Règles** : BR-001, BR-026, BR-027, BR-037, BR-056.

## UC-TSK-02 — Modifier un modèle de tâche

- **Acteur** : Administrateur (toutes les tâches) ou Utilisateur (uniquement les siennes, BR-037).
- **Cas limite** : modifier la fréquence d'un modèle déjà instancié pour la semaine — voir R-04
  (les instances existantes ne sont pas rétro-modifiées, BR-047). User Stories en scénarios détaillés,
exploitables pour la conception, le développement et les tests.

Chaque cas d'usage documente :

- un identifiant unique ;
- l'acteur principal ;
- les préconditions ;
- le déclencheur ;
- le scénario nominal ;
- les scénarios alternatifs et d'erreur ;
- les postconditions ;
- les règles métier associées (référence `BR-xxx`).

> Ce document est une **source dérivée**. En cas de conflit, l'ordre de
> priorité documentaire défini dans `COPILOT_INSTRUCTIONS.md` s'applique,
> complété par les décisions consignées dans `DECISIONS.md`.

---

## Acteurs

| Acteur | Description |
|--------|-------------|
| Participant | Membre du foyer disposant d'un accès standard. |
| Administrateur | Participant disposant de droits étendus (`IsAdmin = true`). |
| Système / Moteur d'affectation | Composant automatisé de répartition. |
| Planificateur (Scheduler) | Composant automatisé de génération périodique des instances. |

> **Note de cohérence** : le terme « Utilisateur standard » de `PRODUCT_VISION.md`,
> « Participant » du `README.md` et « User » du `DOMAIN_MODEL.md` désignent la même
> entité. Le terme retenu est **Participant**. Voir `DECISIONS.md` (ADR-006).

---

# Domaine : Authentification & Accès

## UC-AUTH-01 — Se connecter (sélection de profil)

- **Acteur** : Participant
- **Préconditions** : le participant existe et est actif (`IsActive = true`).
- **Déclencheur** : le participant ouvre l'application.
- **Scénario nominal** :
  1. Le système affiche la liste des profils actifs.
  2. Le participant sélectionne son profil.
  3. Le système ouvre une session et met à jour `DernièreConnexion`.
- **Alternatifs / Erreurs** :
  - A1 — Profil inactif : le profil n'est pas proposé (BR-003, BR-004).
  - A2 — Aucun profil actif : le système affiche un message et propose la création d'un premier administrateur (voir UC-USR-01, cas d'amorçage).
- **Postconditions** : session ouverte, contexte de rôle chargé.
- **Règles** : BR-003, BR-004, BR-018.
- **Décision (ADR-006)** : authentification par sélection de profil uniquement, sans email ni
  mot de passe. L'unicité utilisateur porte sur `DisplayName`.

---

## UC-AUTH-02 — Se déconnecter

- **Acteur** : Participant
- **Préconditions** : session ouverte.
- **Scénario nominal** : le participant se déconnecte ; la session est invalidée.
- **Postconditions** : aucune donnée de session résiduelle.

---

# Domaine : Gestion des participants

## UC-USR-01 — Créer un participant

- **Acteur** : Administrateur
- **Préconditions** : session administrateur active.
- **Scénario nominal** :
  1. L'administrateur saisit `DisplayName`, rôle, disponibilité par défaut, statut actif.
  2. Le système valide l'unicité fonctionnelle du profil.
  3. Le participant est créé avec un identifiant immuable.
- **Alternatifs / Erreurs** :
  - A1 — Identité déjà utilisée (`DisplayName`) : rejet (BR-002).
  - A2 — `AvailabilityPercentage` hors bornes [0..100] : rejet.
- **Postconditions** : participant disponible pour affectation s'il est actif.
- **Règles** : BR-001, BR-002, BR-018, BR-034.

---

## UC-USR-02 — Modifier un participant

- **Acteur** : Administrateur
- **Scénario nominal** : mise à jour des informations, du rôle, du statut ou de la disponibilité.
- **Règles** : BR-017 (traçabilité), BR-032.
- **Cas limite** : réduire `AvailabilityPercentage` en cours de semaine — voir R-08.

## UC-USR-03 — Désactiver / réactiver un participant

- **Acteur** : Administrateur
- **Scénario nominal** : bascule de `IsActive`.
- **Conséquences** :
  - désactivation : exclusion des futures affectations (BR-003, BR-024) ;
  - les instances déjà affectées et non terminées doivent être traitées (voir UC-ENG-05 et R-05).
- **Règles** : BR-003, BR-004, BR-017, BR-035.

## UC-USR-04 — Déclarer une absence

- **Statut** : ⏭ **Hors V1** — la gestion des absences est reportée à une version future
  (BR-025, BACKLOG). L'entité `UserAbsence` reste définie en prévision.
- **Acteur (cible)** : Administrateur ou Participant.
- **Scénario nominal (cible)** :
  1. Saisie de `StartDate` et `EndDate` (`EndDate >= StartDate`).
  2. Le système enregistre l'absence.
- **Règles** : BR-036 (cible).

---

# Domaine : Modèles de tâches

## UC-TSK-01 — Créer un modèle de tâche

- **Acteur** : à trancher — Administrateur (PRODUCT_VISION) **ou** Participant (README/USER_STORIES). Voir R-02 / ADR-007.
- **Préconditions** : session valide.
- **Scénario nominal** :
  1. Saisie de `Title`, `Description`, `FrequencyType`, `ManualDifficultyScore`, durée estimée, rappels.
  2. Le système valide l'unicité du `Title` et les bornes de score.
  3. Le modèle est créé avec `IsActive = true`.
- **Alternatifs / Erreurs** :
  - A1 — `Title` déjà utilisé : rejet (unicité).
  - A2 — `ManualDifficultyScore` hors [1..100] : rejet.
  - A3 — `EstimatedDurationMinutes < 1` : rejet.
- **Règles** : BR-001, BR-026 (priorité vs difficulté — voir R-03), BR-027, BR-037.

## UC-TSK-02 — Modifier un modèle de tâche

- **Acteur** : Administrateur (référence PRODUCT_VISION) — à aligner avec UC-TSK-01.
- **Cas limite** : modifier la fréquence d'un modèle déjà instancié pour la semaine — voir R-04.

## UC-TSK-03 — Désactiver un modèle de tâche

- **Scénario** : `IsActive = false`.
- **Conséquences** : n'apparaît plus dans les futures générations, reste dans l'historique.
- **Règles** : BR-015.

## UC-TSK-04 — Archiver un modèle de tâche

- **Scénario** : passage à l'état archivé.
- **Conséquences** : ne peut plus être affecté, reste visible dans l'historique.
- **Règles** : BR-014, BR-016 (suppression logique).
- **⚠ Ambiguïté** : distinction entre `IsActive=false` et « archivé » non modélisée dans
  `DOMAIN_MODEL` (`TaskTemplate` n'a qu'un booléen `IsActive`). Voir R-11.

---

# Domaine : Génération des instances

## UC-GEN-01 — Générer les instances d'une semaine

- **Acteur** : Planificateur (automatique) ou Administrateur (manuel).
- **Préconditions** : au moins un modèle actif ; semaine cible identifiée.
- **Scénario nominal** :
  1. Le système détermine la semaine cible (année + numéro ISO).
  2. Pour chaque modèle actif, il crée les `TaskInstance` selon la `FrequencyType` :
     - `DailyMandatory` → 7 instances (une par jour) ;
     - `DailyFlexible` → 1 instance, exécutable plusieurs fois (voir UC-CMP-03) ;
     - `Weekly` → 1 instance ;
     - `Monthly` → 1 instance sur la semaine cible du mois (règle à préciser, R-12) ;
     - `OneTime` → 1 instance unique.
  3. Les tâches reportées de la semaine précédente sont conservées (UC-GEN-02).
  4. Le moteur d'affectation est invoqué (UC-ENG-01).
- **Règles** : BR-007, BR-008 (unicité par jour pour DailyMandatory, R-07), BR-021, BR-027.
- **Rattachement semaine** : `AssignmentRun` et `TaskInstance` référencent la semaine (entité
  `Week`, ADR-008, BR-038). C'est ce lien qui justifie les 7 instances d'une tâche quotidienne
  obligatoire.

## UC-GEN-02 — Reporter une tâche non terminée

- **Acteur** : Système
- **Déclencheur** : fin de période avec instances non terminées.
- **Scénario nominal** :
    1. L'instance non terminée est reportée sur la période suivante si elle n'a encore jamais été
      reportée.
  2. La nouvelle instance porte `IsCarryOver = true` et `ParentTaskInstanceId`.
  3. La tâche **reste affectée au même participant** (ADR-005, BR-non défini → BR-039).
- **Cas limites** :
  - Propriétaire devenu inactif/absent : voir R-05.
  - Report d'une instance `DailyMandatory` (quotidienne) : comportement à définir, R-07.
  - Instance déjà reportée et toujours non terminée : elle reste dans l'historique et est exclue
    des futures affectations et des calculs du moteur ; aucun second report n'est créé.
- **Règles** : ADR-005, BR-039.

---

# Domaine : Moteur d'affectation

## UC-ENG-01 — Répartir les nouvelles tâches (INTELLIGENT_V1)

- **Acteur** : Système / Moteur
- **Préconditions** : instances de la semaine générées ; participants actifs identifiés.
- **Scénario nominal** (aligné sur `ASSIGNMENT_ENGINE.md`) :
  1. Identifier les participants actifs (BR-003, BR-024).
  2. (Hors V1) Exclure les participants absents sur la période (BR-025 — reporté).
  3. Conserver les tâches reportées sur leur propriétaire (ADR-005).
  4. Calculer la charge historique (`AssignmentMetricSnapshot`).
  5. Identifier les nouvelles tâches à répartir.
  6. Équilibrer les points prévisionnels pondérés par `AvailabilityPercentage`.
  7. Réduire les répétitions (éviter la même tâche à la même personne).
  8. Produire une justification (`AssignmentExplanation`).
- **Alternatifs / Erreurs** :
  - A1 — Aucun participant actif disponible : voir R-14 (aucune affectation possible).
  - A2 — Un seul participant disponible : toutes les tâches lui sont affectées.
  - A3 — Égalité parfaite de scores : règle de départage à définir (R-15).
- **Postconditions** : chaque nouvelle instance possède un `AssignedUserId` et une explication.
- **Règles** : BR-021, BR-023, BR-024, BR-040 (sémantique disponibilité).

## UC-ENG-02 — Consulter la justification d'une affectation

- **Acteur** : Participant / Administrateur
- **Scénario** : affichage du texte de `AssignmentExplanation` et des métriques.
- **Règles** : BR-033.

## UC-ENG-03 — Relancer une affectation (réexécution)

- **Acteur** : Administrateur
- **Scénario** : nouvelle exécution du moteur pour une semaine. La nouvelle affectation
  **écrase** l'affectation existante de la semaine (ADR-013, BR-041).
- **Postcondition** : l'historique de l'exécution précédente reste conservé (aucune purge).
- **Règles** : BR-022, BR-041.

## UC-ENG-04 — Réaffecter manuellement une instance

- **Acteur** : Administrateur, ou participant actuellement responsable de l'instance
- **Scénario** : changement de `AssignedUserId` sur une instance.
- **Postconditions** : historisation de la réaffectation (BR-017, BR-022) et création d'une
  notification pour le nouveau responsable, mentionnant l'administrateur réaffecteur et
  contenant un lien vers l'instance.

## UC-ENG-05 — Réaffectation automatique sur indisponibilité

- **Acteur** : Système
- **Déclencheur** : un participant devient inactif ou absent alors qu'il porte des instances non terminées.
- **Statut** : ⏭ **Hors V1** — reporté à une version future (BR-042, BACKLOG, R-05).

---

# Domaine : Complétude & exécution

## UC-CMP-01 — Marquer une instance comme terminée

- **Acteur** : Participant (propriétaire de l'instance)
- **Préconditions** : instance affectée au participant, statut `Pending`.
- **Scénario nominal** :
  1. Le participant marque l'instance terminée.
  2. Le système crée une `TaskCompletion` et passe `Status = Completed`.
- **Règles** : BR-009, BR-010, BR-011.
- **Postcondition complémentaire** : `CompletedAt` contient l'instant UTC de la complétion et
  est exposé dans le détail de l'instance, indépendamment de `DueDate`.

## UC-CMP-02 — Modifier l'état de complétude durant la semaine

- **Acteur** : Participant
- **Scénario** : durant la semaine active, le participant bascule librement l'état
  (complété ↔ non complété), autant de fois que nécessaire.
- **Postcondition** : à la clôture de la semaine, l'état courant est figé et verrouillé
  (ADR-011, BR-043, BR-055) : plus aucune modification possible.
- **Règles** : BR-010, BR-011, BR-012, BR-043, BR-055.
- Lors d'une réouverture, `CompletedAt` redevient nul ; lors d'une nouvelle complétion, il est
  remplacé par le nouvel instant de complétion.

## UC-CMP-03 — Déclarer une exécution d'une tâche quotidienne flexible

- **Acteur** : Participant
- **Scénario** : ajout d'une `DailyFlexibleExecution` (date d'exécution).
- **Règle de complétude** : l'objectif hebdomadaire est atteint dès la 1re exécution.
- **Cas limite** : plusieurs participants exécutent la même instance flexible — R-17.
- **Règles** : BR-044.

## UC-CMP-04 — Ajouter un commentaire à une instance

- **Acteur** : Participant
- **Scénario** : ajout d'un `TaskComment` rattaché à l'instance (entité définie dans `DOMAIN_MODEL`).
- **Règles** : BR-013.

## UC-CMP-05 — Annuler une instance

- **Acteur** : Administrateur (ou Participant, à trancher)
- **Scénario** : passage à `Status = Cancelled`.
- **Cas limite** : une instance annulée doit-elle être reportée ? Non (R-19).

---

# Domaine : Consultation & Historique

## UC-HIS-01 — Consulter mes tâches de la semaine en cours

- **Acteur** : Participant
- **Règles** : BR-019, BR-029.

## UC-HIS-02 — Consulter mes semaines précédentes

- **Acteur** : Participant
- **Cas limite** : au-delà de la période de rétention (ADR-004 : 1 an) — R-20.
- **Règles** : BR-028.

## UC-HIS-03 — Consulter l'historique global (admin)

- **Acteur** : Administrateur
- **Règles** : BR-020, BR-030, BR-033.

## UC-HIS-04 — Classer les événements de l'historique

- **Acteur** : système, lors de l'affichage de l'historique.
- **Principe** : la catégorie est déterminée par l'action métier et l'entité, jamais par le
  rôle du profil qui a déclenché l'action.
- **Actions utilisateur** : complétion ou réouverture, création ou modification d'une tâche,
  ajout de commentaire, ajout ou modification d'un rappel, configuration des préférences.
- **Actions d'administration** : suppression logique, désactivation ou réactivation d'une tâche,
  génération d'instances, lancement d'une affectation, clôture de semaine, réaffectation ou
  annulation d'instance, création ou modification de participant et modification du moteur.
- **Règles** : BR-017, BR-028, BR-033.

Les actions actuellement persistées dans `AuditLog` sont `Created`, `Updated`, `Closed`,
`CompletionChanged`, `Reassigned`, `Cancelled`, `CommentAdded`, `Executed` et `Generated`. Les rappels et
les changements d'activation sont encore encodés comme `Updated` et identifiés par `NewValue`.
Les réactions et les exécutions flexibles doivent encore être journalisées pour compléter la
couverture.

## UC-HIS-05 — Revenir au dashboard depuis une action de tâche

- **Acteur** : Participant ou Administrateur.
- **Règle** : une entrée d'audit liée à une `TaskInstance` ouvre le dashboard de la semaine de
  l'instance, avec l'instance sélectionnée si elle est encore accessible.
- **Distinction** : les cartes des semaines récentes ouvrent un dashboard de période sans
  sélection d'instance; elles restent distinctes de la liste des actions d'audit.
- **Accès** : la page `/instances` reste réservée à l'administration.

## UC-HIS-06 — Consulter deux espaces d'historique

- **Historique personnel (`/history`)** : chaque profil ne voit que les événements qu'il a
  effectués. Ce comportement s'applique aussi à un profil administrateur.
- **Journal d'audit (`/admin-history`)** : réservé aux administrateurs, il affiche tous les
  événements du foyer et conserve les filtres de catégorie utilisateur/administration ainsi que
  les filtres d'entité/action, le tri chronologique et la pagination serveur. Il ne contient pas
  le module de navigation des semaines passées.

---

# Domaine : Notifications

## UC-NOT-01 — Activer / désactiver ses notifications

- **Acteur** : Participant
- **Scénario** : bascule de `NotificationsEnabled` dans `UserPreferences` (entité complétée
  dans `DOMAIN_MODEL`).

## UC-NOT-02 — Configurer les rappels d'une tâche

- **Acteur** : Créateur du modèle
- **Scénario** : ajout de `Reminder` (fin de journée / fin de semaine).
- **Règles** : BR-045.

---

# Domaine : Administration & Paramètres

## UC-ADM-01 — Configurer la stratégie d'affectation active

- **Acteur** : Administrateur
- **Scénario** : sélection d'une `AssignmentStrategy` active.
- **Règles** : BR-031, BR-046.

## UC-ADM-02 — Gérer les paramètres système

- **Acteur** : Administrateur
- **Scénario** : lecture/modification des `SystemSetting` (entité définie dans `DOMAIN_MODEL`).
- **Règles** : BR-031.

---

# Matrice de traçabilité (extrait)

| Use Case | User Story source | Règles |
|----------|-------------------|--------|
| UC-AUTH-01 | Vision / Auth | BR-003, BR-004 |
| UC-TSK-01 | « créer une tâche » | BR-001, BR-026, BR-027 |
| UC-GEN-01 | « lancer une affectation » | BR-021, BR-007 |
| UC-ENG-01 | Répartition intelligente | BR-023, BR-024 |
| UC-ENG-02 | « connaître la raison » | BR-033 |
| UC-CMP-01 | « marquer terminée » | BR-009, BR-010 |
| UC-HIS-02 | « réalisations passées » | BR-028 |
