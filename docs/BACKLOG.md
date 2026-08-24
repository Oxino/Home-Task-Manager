# MVP

## Phase 1

Fondations

- Solution Backend
- Solution Frontend
- PostgreSQL
- Docker Compose
- Authentification par sélection de profil (sans email ni mot de passe, ADR-006)

## Phase 2

Gestion des utilisateurs

- CRUD utilisateurs
- Gestion admin

## Phase 3

Modèles de tâches

- CRUD TaskTemplate
- Gestion fréquences
- Gestion rappels

## Phase 4

Instances

- Génération automatique
- Gestion statuts
- Historique

## Phase 5

Moteur d'affectation

- Interface de stratégie
- IntelligentV1
- Explications
- Paramètres d'algorithme administrables (score, multiplicateurs de priorité, fenêtre d'équité,
  pénalité anti-répétition — BR-058)

## Phase 6

Notifications

- Notifications navigateur
- Préférences utilisateur

## Phase 7

Optimisation UX

- Dashboard
- Mobile responsive

## Hors MVP

- Statistiques avancées / tendances
- Réaffectation automatique sur indisponibilité (BR-042, R-05)
- Prise en compte des absences dans le moteur (`UserAbsence`, BR-025, R-06)
- Gamification
- IA
- Application mobile
- Synchronisation externe

---

## Périmètre du MVP (validé)

### Gestion des utilisateurs

- CRUD utilisateurs.
- Distinction administrateur / utilisateur via `IsAdmin` (ADR-010).

### Gestion des tâches

- L'administrateur gère complètement toutes les tâches (créer, modifier, supprimer/archiver).
- L'utilisateur peut créer des tâches et modifier uniquement celles dont il est l'auteur
  (BR-037, ADR-007).
- Toute nouvelle tâche créée (par l'admin ou un utilisateur) devient disponible pour les
  **futures** affectations et ne modifie pas l'affectation de la semaine en cours (BR-047).

### Affectation hebdomadaire

- Génération d'une répartition de tâches à la semaine.
- Chaque affectation est liée à une semaine ; une tâche quotidienne obligatoire génère
  7 instances (ADR-008, BR-038).
- Relancer une affectation **écrase** l'affectation existante de la semaine (ADR-013, BR-041).

### Parcours utilisateur

- Se connecter et choisir son profil.
- Voir ses tâches de la semaine.
- Compléter ses tâches (état modifiable durant la semaine, figé à la clôture — ADR-011, BR-043).
- Créer de nouvelles tâches et modifier les siennes.
- Comprendre pourquoi une tâche lui a été affectée (justification, BR-033).

### Parcours administrateur

- Toutes les possibilités d'un utilisateur.
- Interface d'administration pour gérer l'ensemble des données de l'application.
- Relancer une affectation de tâches.

---

## Clarifications de périmètre (analyse de cohérence)

> Ces précisions résolvent des ambiguïtés relevées dans `RISKS_AND_EDGE_CASES.md`.

### Statistiques (R-25)

- **Inclus au MVP** : indicateurs de base des tableaux de bord (BR-029, BR-030) —
  nombre de tâches, complétées, restantes, pourcentage d'avancement.
- **Hors MVP** : statistiques avancées, tendances, historiques analytiques.

### Absences (R-06 / BR-025)

- Reportées **hors V1** : l'entité `UserAbsence` et l'exclusion des absents par le moteur ne font
  pas partie du premier MVP. À prévoir dans une version future.

### Réaffectation automatique sur indisponibilité (R-05 / BR-042)

- Reportée **hors V1** : à prévoir dans une version future.

### Historique

- Aucune purge : conservation intégrale de l'historique (ADR-004, BR-057).

### Entités transverses à prévoir (Phase 4/5)

- `Week`, `SystemSetting`, `AuditLog`, `TaskComment`, `UserPreferences` complet
  (voir `DOMAIN_MODEL.md` et ADR-008 / ADR-009).