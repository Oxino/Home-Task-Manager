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

EstimatedDurationMinutes :

- minimum 1

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

Le calcul pourra évoluer sans modifier la structure.

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
| StrategyId | Guid | Oui |
| TriggeredByUserId | Guid | Oui |
| IsAutomatic | bool | Oui |
| Notes | string | Non |

## Cas d'usage

Permet :

- de savoir quand une répartition a été effectuée
- de restaurer l'historique
- de comprendre les décisions

---

# TaskInstance

Occurrence réelle d'une tâche.

## TaskInstance

| Champ | Type | Obligatoire |
|---------|---------|---------|
| Id | Guid | Oui |
| TaskTemplateId | Guid | Oui |
| AssignmentRunId | Guid | Oui |
| AssignedUserId | Guid | Oui |
| PlannedDate | DateOnly | Oui |
| DueDate | DateOnly | Oui |
| Status | enum | Oui |
| IsCarryOver | bool | Oui |
| ParentTaskInstanceId | Guid | Non |
| CreatedAt | DateTime | Oui |
| UpdatedAt | DateTime | Oui |

## Notes

Une tâche reportée :

- possède IsCarryOver = true
- référence éventuellement sa tâche d'origine

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
|---------|---------|