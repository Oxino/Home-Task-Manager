# Assignment Engine

## Objectif

Répartir les tâches de manière équitable entre les participants.

## Principes

La charge doit être équilibrée :

- sur la semaine courante
- sur l'historique

## Version initiale

INTELLIGENT_V1

## Ordre de traitement

### Etape 1

Identifier les participants actifs.

### Etape 2

Exclure les absences.

> Hors V1 : la prise en compte des absences (`UserAbsence`) est reportée à une version future
> (BR-025). L'étape reste décrite en prévision.

### Etape 3

Conserver les tâches reportées.

Une tâche reportée reste affectée à son propriétaire.

### Etape 4

Calculer la charge historique.

### Etape 5

Identifier les nouvelles tâches à répartir.

### Etape 6

Equilibrer les points prévisionnels.

Chaque tâche possède un score (voir « Calcul du score »). La cible de points de chaque
participant est proportionnelle à sa disponibilité (`AvailabilityPercentage`, BR-040).

### Etape 7

Réduire les répétitions.

Le système doit éviter qu'une même tâche soit systématiquement attribuée à la même personne.
Mécanisme : **pénalité de points** (paramétrable), pas d'interdiction stricte (BR-054).

### Etape 8

Produire une justification.

## Calcul du score

Score d'une tâche (V1) :

```text
CalculatedScore = ManualDifficultyScore × MultiplicateurPriorité × FacteurDurée
FacteurDurée    = EstimatedDurationMinutes / DuréeRéférenceMinutes   (défaut 30 min)
```

- `ManualDifficultyScore` : pénibilité, 1..100.
- `MultiplicateurPriorité` : Faible 0.5 / Normale 1 / Critique 1.5 (défaut, paramétrable).
- `FacteurDurée` : proportionnel à la durée ; vaut 1 si la durée est absente.

Voir BR-052 et BR-056.

## Pénalité anti-répétition

Pour chaque candidat évalué sur une tâche :

```text
Pénalité = OccurrencesRécentes × PoidsPénalité × ScoreTâche   (PoidsPénalité défaut 0.25)
```

`OccurrencesRécentes` est mesuré sur la fenêtre d'équité (défaut 8 semaines). La pénalité
s'ajoute à la charge prévisionnelle du candidat pour cette tâche (BR-054).

## Départage en cas d'égalité

Ordre déterministe (BR-048) :

1. participant ayant le moins de tâches affectées ;
2. participant ayant le moins réalisé récemment la tâche concernée ;
3. identifiant stable.

## Sélection d'un candidat pour une tâche

Pour chaque tâche à répartir :

```text
ChargeAjustée(candidat) = ChargePrévisionnelle(candidat) + Pénalité(candidat, tâche)
CibleParticipant        = CibleGlobale × (AvailabilityPercentage / 100)
```

Le candidat retenu est celui dont la charge ajustée, rapportée à sa cible, est la plus faible.
En cas d'égalité, appliquer la règle de départage ci-dessus.

## Exemple de bout en bout

Contexte : 2 participants, Julie (100 %) et Nicolas (100 %). Paramètres par défaut.

Tâche « Aspirateur » : pénibilité 40, priorité Normale, durée 45 min.

```text
ScoreTâche = 40 × 1 × (45 / 30) = 60
```

Historique (8 dernières semaines) : Julie a fait « Aspirateur » 4 fois, Nicolas 0 fois.

```text
Pénalité Julie   = 4 × 0.25 × 60 = 60
Pénalité Nicolas = 0 × 0.25 × 60 = 0
```

À charge prévisionnelle égale, Nicolas obtient la tâche (charge ajustée plus faible).
Justification produite : « Attribuée à Nicolas car Julie a réalisé cette tâche 4 fois sur les
8 dernières semaines. »

## Paramètres administrables

Le comportement du moteur est configurable par l'administrateur (BR-058), notamment :

| Paramètre | Défaut |
|-----------|--------|
| Multiplicateurs de priorité (Faible / Normale / Critique) | 0.5 / 1.0 / 1.5 |
| Durée de référence (minutes) | 30 |
| Fenêtre d'équité (semaines) | 8 |
| Poids de la pénalité anti-répétition | 0.25 |

## Algorithmes futurs

- RANDOM
- BALANCED_POINTS
- BALANCED_TIME
- INTELLIGENT_V2

L'architecture doit permettre d'ajouter facilement de nouvelles stratégies, chacune pouvant
exposer ses propres paramètres.