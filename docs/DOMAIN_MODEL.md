# Data Model

## Introduction

Ce document décrit le modèle de données métier de Home Task Manager.

Il constitue la référence pour :

- les entités métiers
- les DTO
- les API

Les champs marqués [Required] sont obligatoires.

---

# User

Représente un participant.

## User

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| DisplayName | string | Oui |
| IsAdmin | bool | Oui |
| IsActive | bool | Oui |
| AvailabilityPercentage | int | Oui |
| CreatedAt | DateTime | Oui |
| UpdatedAt | DateTime | Oui |

## Contraintes

AvailabilityPercentage :

- minimum 0
- maximum 100

Exemples :

- 100 = pleine disponibilité
- 50 = disponibilité réduite
- 0 = indisponible

## Authentification

L'authentification se fait par **sélection de profil** (ADR-003 / ADR-006) : pas d'email, pas de
mot de passe. `DisplayName` est unique (BR-002). Le rôle est porté par le booléen `IsAdmin` :
l'application ne comporte que deux rôles, administrateur et utilisateur (ADR-010).

---

# UserAbsence

Déclare une période d'absence.

## UserAbsence

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| UserId | Guid | Oui |
| StartDate | DateOnly | Oui |
| EndDate | DateOnly | Oui |
| Reason | string | Non |
| CreatedAt | DateTime | Oui |

## Contraintes

EndDate >= StartDate

---

# TaskTemplate

Définition permanente d'une tâche.

## TaskTemplate

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| Title | string | Oui |
| Description | string | Non |
| ManualDifficultyScore | int | Oui |
| Priority | enum | Oui |
| EstimatedDurationMinutes | int | Non |
| FrequencyType | enum | Oui |
| IsActive | bool | Oui |
| CreatedByUserId | Guid | Oui |
| CreatedAt | DateTime | Oui |
| UpdatedAt | DateTime | Oui |

## Contraintes

Title unique.

ManualDifficultyScore :

- minimum 1
- maximum 100

Priority :

- valeurs : Low, Normal, Critical
- valeur par défaut : Normal
- influence le score global de la tâche (BR-052, BR-056)

EstimatedDurationMinutes :

- minimum 1

## Droits

- `CreatedByUserId` identifie l'auteur.
- Un utilisateur ne peut modifier que les tâches dont il est l'auteur ; l'administrateur peut
  modifier et supprimer toutes les tâches (BR-037, ADR-007).

---

# TaskPriority

Enum.

```csharp
Low,
Normal,
Critical
```

---

# FrequencyType

Enum.

```csharp
DailyMandatory,
DailyFlexible,
Weekly,
Monthly,
OneTime
```

---

# TaskScore

Permet d'isoler le calcul de charge.

## TaskScore

| Champ | Type | Obligatoire |
|---------|---------|---------|
| TaskTemplateId | Guid | Oui |
| ManualScore | int | Oui |
| CalculatedScore | int | Oui |
| LastCalculationDate | DateTime | Oui |

## Notes

CalculatedScore doit être recalculable.

Formule V1 : `ManualDifficultyScore × MultiplicateurPriorité × FacteurDurée` (BR-052).

Le mode de calcul et les multiplicateurs sont paramétrables par l'administrateur (BR-058) et
stockés en `SystemSetting`. Le calcul pourra évoluer sans modifier la structure.

---

# Reminder

Définition d'un rappel.

## Reminder

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| TaskTemplateId | Guid | Oui |
| TriggerType | enum | Oui |
| OffsetValue | int | Oui |
| OffsetUnit | enum | Oui |
| IsEnabled | bool | Oui |

---

# ReminderTriggerType

```csharp
EndOfDay,
EndOfWeek
```

---

# ReminderOffsetUnit

```csharp
Hour,
Day
```

---

# AssignmentStrategy

Stratégie utilisée lors d'une répartition.

## AssignmentStrategy

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| Name | string | Oui |
| Description | string | Non |
| IsActive | bool | Oui |

---

# AssignmentRun

Représente une exécution de répartition.

## AssignmentRun

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| GeneratedAt | DateTime | Oui |
| WeekId | Guid | Oui |
| StrategyId | Guid | Oui |
| TriggeredByUserId | Guid | Oui |
| IsAutomatic | bool | Oui |
| Notes | string | Non |

## Cas d'usage

Permet :

- de savoir quand une répartition a été effectuée
- de restaurer l'historique
- de comprendre les décisions

## Note de cohérence

`WeekId` rattache l'exécution à une semaine ISO-8601 (BR-038 / ADR-008). Une relance d'affectation
pour une semaine remplace l'affectation existante de cette semaine (BR-041).

---

# TaskInstance

Occurrence réelle d'une tâche.

## TaskInstance

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| TaskTemplateId | Guid | Oui |
| AssignmentRunId | Guid | Oui |
| WeekId | Guid | Oui |
| AssignedUserId | Guid | Oui |
| PlannedDate | DateOnly | Oui |
| DueDate | DateOnly | Oui |
| Status | enum | Oui |
| IsCompleted | bool | Oui |
| IsCarryOver | bool | Oui |
| ParentTaskInstanceId | Guid | Non |
| CreatedAt | DateTime | Oui |
| UpdatedAt | DateTime | Oui |

## Notes

Une tâche reportée :

- possède IsCarryOver = true
- référence éventuellement sa tâche d'origine

`IsCompleted` porte l'état courant réversible durant la semaine active (BR-043). À la clôture de
la semaine (BR-055), cet état est figé. Le `Status` (`Pending` / `Completed` / `Cancelled`)
représente le cycle de vie de l'instance ; `WeekId` rattache l'instance à sa semaine.

---

# TaskInstanceStatus

```csharp
Pending,
Completed,
Cancelled
```

---

# TaskCompletion

Historique de réalisation.

## TaskCompletion

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| TaskInstanceId | Guid | Oui |
| CompletedByUserId | Guid | Oui |
| CompletedAt | DateTime | Oui |

## Notes

Permet de conserver l'historique complet.

---

# CompletionChangeLog

Journal des changements d'état de complétude (historique bidirectionnel, BR-043).

## CompletionChangeLog

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| TaskInstanceId | Guid | Oui |
| ChangedByUserId | Guid | Oui |
| OldValue | bool | Oui |
| NewValue | bool | Oui |
| ChangedAt | DateTime | Oui |

## Notes

Enregistre chaque bascule complété ↔ non complété durant la semaine active. Après clôture
(BR-055), plus aucun enregistrement n'est ajouté pour l'instance concernée.

---

# DailyFlexibleExecution

Spécifique aux tâches quotidiennes flexibles.

Permet de conserver plusieurs exécutions sur une même semaine.

## DailyFlexibleExecution

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| TaskInstanceId | Guid | Oui |
| ExecutedByUserId | Guid | Oui |
| ExecutedAt | DateTime | Oui |

## Exemple

Arroser les plantes :

- lundi
- mercredi
- samedi

=

3 lignes dans cette table.

---

# AssignmentExplanation

Justification de l'affectation.

## AssignmentExplanation

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| TaskInstanceId | Guid | Oui |
| AssignedUserId | Guid | Oui |
| GeneratedAt | DateTime | Oui |
| ExplanationText | string | Oui |

## Exemple

```text
Attribuée à Julie car :

- Nicolas a réalisé cette tâche 4 fois sur les 8 dernières semaines
- Julie possède un score historique inférieur
- Julie est disponible à 100 %
```

---

# AssignmentMetricSnapshot

Capture des métriques utilisées lors du calcul.

## AssignmentMetricSnapshot

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| AssignmentRunId | Guid | Oui |
| UserId | Guid | Oui |
| HistoricalScore | int | Oui |
| AssignedScoreThisRun | int | Oui |
| CarryOverScore | int | Oui |
| AvailabilityPercentage | int | Oui |

## Objectif

Permet d'expliquer et d'auditer les décisions de l'algorithme.

---

# UserPreferences

Préférences d'un utilisateur.

## UserPreferences

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| UserId | Guid | Oui |
| NotificationsEnabled | bool | Oui |
| CreatedAt | DateTime | Oui |
| UpdatedAt | DateTime | Oui |

## Notes

Activation/désactivation indépendante des notifications par utilisateur (BR-045, UC-NOT-01).

---

# Notification

Notification personnelle affichée dans la boîte de réception d'un participant.

## Notification

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| UserId | Guid | Oui |
| Title | string | Oui |
| Message | string | Oui |
| Link | string | Non |
| IsRead | bool | Oui |
| ReadAt | DateTime | Non |
| CreatedAt | DateTime | Oui |

## Notes

Les notifications sont cloisonnées par utilisateur. Un participant peut les lire, les marquer
comme lues et supprimer les notifications déjà lues. La génération automatique par les rappels
et l'envoi Web Push sont des responsabilités séparées du scheduler.

---

# Entités complémentaires (issues de l'analyse de cohérence)

> Ces entités comblent des lacunes identifiées dans `RISKS_AND_EDGE_CASES.md`.
> Elles restent soumises aux décisions ADR-006 à ADR-009.

---

# Week

Représente une semaine ISO-8601 servant de période de référence.

## Week

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| Year | int | Oui |
| WeekNumber | int | Oui |
| StartDate | DateOnly | Oui |
| EndDate | DateOnly | Oui |
| Status | enum | Oui |
| ClosedAt | DateTime | Non |

## Contraintes

- `(Year, WeekNumber)` unique, conforme ISO-8601 (années à 53 semaines gérées).
- Référencée par `AssignmentRun` et `TaskInstance` (BR-038, R-13).

## WeekStatus

```csharp
Active,
Closed
```

Une semaine `Closed` verrouille la complétude de ses instances (BR-055).

---

# SystemSetting

Paramètre système configurable par un administrateur.

## SystemSetting

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| Key | string | Oui |
| Value | string | Oui |
| UpdatedByUserId | Guid | Oui |
| UpdatedAt | DateTime | Oui |

## Notes

Supporte BR-031 (paramètres d'affectation, notification, fuseau horaire BR-051, etc.).
`Key` unique. Évite les valeurs métier codées en dur.

---

# AuditLog

Journal d'audit générique des modifications métier.

## AuditLog

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| EntityType | string | Oui |
| EntityId | Guid | Oui |
| Action | string | Oui |
| PerformedByUserId | Guid | Oui |
| OldValue | string | Non |
| NewValue | string | Non |
| PerformedAt | DateTime | Oui |

## Notes

Couvre BR-011, BR-017, BR-033 et BR-043. Les changements de complétude bidirectionnels sont
enregistrés avec `Action = CompletionChanged` et `NewValue = IsCompleted=true|false`.
`Created`, `Updated`, `Closed`, `Reassigned`, `Cancelled`, `CommentAdded` et `Executed` sont
les valeurs actuellement émises par les cas d'utilisation. La catégorie utilisateur ou
administration est dérivée de l'action et de l'entité, jamais du rôle du profil auteur.

Les réactions de commentaires et les exécutions flexibles ne sont pas encore enregistrées dans
`AuditLog`.

---

# TaskComment

Commentaire associé à une instance de tâche.

## TaskComment

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| TaskInstanceId | Guid | Oui |
| AuthorUserId | Guid | Oui |
| Content | string | Oui |
| CreatedAt | DateTime | Oui |

## Notes

Supporte BR-013 (commentaires utilisateur), absent du modèle initial (R-18).