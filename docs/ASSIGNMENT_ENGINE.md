# Moteurs de repartition

## Objectif commun

Le moteur de repartition attribue les instances de taches de la semaine a des participants
actifs. Le moteur choisi est administrable depuis la page de configuration et est conserve dans
`SystemSetting` sous la cle `AssignmentEngine.StrategyName`.

Les quatre moteurs disponibles sont :

- `INTELLIGENT_V1` : equilibre la charge avec l'historique et la penalite anti-repetition ;
- `RANDOM` : repartit aleatoirement avec un nombre de taches equilibre ;
- `BALANCED_POINTS` : equilibre les scores de difficulte de la semaine ;
- `BALANCED_TIME` : equilibre les durees estimees de la semaine.

## Preparation commune

Avant l'appel de la strategie, le cas d'utilisation d'affectation :

1. verifie que le demandeur est administrateur ;
2. charge la semaine et ses instances ;
3. exclut les instances annulees ;
4. conserve les instances reportees avec leur proprietaire ; un report sans proprietaire reste
  eligible et est transmis a la strategie pour etre affecte ;
5. charge les participants actifs ;
6. prepare les modeles et les parametres ;
7. applique le moteur selectionne ;
8. enregistre le `AssignmentRun`, les affectations et les notifications.

Les participants dont `AvailabilityPercentage` vaut 0 ne sont pas eligibles. Les notifications,
l'audit de l'execution et la conservation des reports sont des responsabilites du cas
d'utilisation, pas des strategies.

## Parametrage administrateur

L'administrateur choisit le moteur dans **Configuration admin > Moteur d'affectation**. L'ecran
presente une explication specifique a chaque option et enregistre le choix avec les autres
parametres.

| Parametre | Utilise par | Defaut |
|-----------|-------------|--------|
| Moteur actif | tous | `INTELLIGENT_V1` |
| Multiplicateur priorite faible | `INTELLIGENT_V1`, `BALANCED_POINTS` | 0.5 |
| Multiplicateur priorite normale | `INTELLIGENT_V1`, `BALANCED_POINTS` | 1.0 |
| Multiplicateur priorite critique | `INTELLIGENT_V1`, `BALANCED_POINTS` | 1.5 |
| Duree de reference | `INTELLIGENT_V1`, `BALANCED_TIME` | 30 minutes |
| Fenetre d'equite | `INTELLIGENT_V1` | 8 semaines |
| Poids de penalite anti-repetition | `INTELLIGENT_V1` | 0.25 |

Les parametres qui ne concernent pas le moteur choisi restent persistes, mais n'influencent pas
les autres strategies.

---

## INTELLIGENT_V1

### Description breve

Moteur complet et moteur par defaut. Il cherche une repartition equitable dans le temps en
combinant la charge de la semaine, la disponibilite, l'historique recent et une penalite contre
les repetitions.

### Logique etapes par etapes

Pour chaque participant eligible, le moteur initialise :

- `runningScore` avec le score historique cumule ;
- `runningAssignedCount` avec le nombre historique d'instances affectees.

Il traite les instances dans l'ordre de `PlannedDate`, puis par identifiant stable. Pour chaque
instance, il :

1. retrouve son modele ;
2. calcule le score de la tache ;
3. compte les occurrences du meme modele pour chaque candidat ;
4. calcule la charge ajustee ;
5. rapporte cette charge a la disponibilite du candidat ;
6. selectionne le candidat le moins charge ;
7. affecte l'instance et actualise les compteurs ;
8. memorise le participant pour les sept instances d'une tache quotidienne obligatoire.

### Calcul du score

```text
CalculatedScore = ManualDifficultyScore x PriorityMultiplier x DurationFactor
DurationFactor   = EstimatedDurationMinutes / ReferenceDurationMinutes
```

Si la duree est absente, `DurationFactor` vaut 1. Les multiplicateurs par defaut sont : faible
0.5, normale 1.0 et critique 1.5.

### Historique et penalite

L'historique est lu sur la fenetre d'equite, par defaut les huit semaines precedant la semaine
traitee. La semaine en cours et les instances reportees sont exclues des calculs historiques.

```text
Penalty = RecentOccurrences x RepetitionPenaltyWeight x CalculatedScore
AdjustedLoad = CurrentScoreLoad + Penalty
NormalizedLoad = AdjustedLoad / (AvailabilityPercentage / 100)
```

Le candidat retenu est celui dont `NormalizedLoad` est le plus faible.

### Departage

En cas d'egalite :

1. moins d'instances affectees pendant le calcul ;
2. moins d'occurrences recentes du modele ;
3. identifiant stable du participant.

### Exemple

Pour une tache de difficulte 40, priorite normale et duree 45 minutes :

```text
Score = 40 x 1.0 x (45 / 30) = 60
```

Si Julie a realise cette tache quatre fois dans la fenetre et Nicolas zero fois :

```text
Penalite Julie   = 4 x 0.25 x 60 = 60
Penalite Nicolas = 0 x 0.25 x 60 = 0
```

A charge egale, Nicolas est choisi.

### Conformite et limites

L'implementation est conforme a la documentation pour le score, la disponibilite, la fenetre
historique, la penalite et le departage. Les absences sont encore hors perimetre V1, comme prevu
par `BR-025`. La conservation des reports est appliquee en amont par le cas d'utilisation.

---

## RANDOM

### Description breve

Moteur volontairement simple. Il ne cherche ni l'equilibre des points, ni l'equilibre du temps,
ni l'equite historique. Il repartit les taches dans un ordre aleatoire tout en garantissant un
nombre de taches equilibre entre les participants eligibles.

### Logique etapes par etapes

1. exclure les participants a disponibilite nulle ;
2. melanger les instances avec une source aleatoire ;
3. melanger les participants eligibles ;
4. affecter chaque instance au participant suivant dans la liste circulaire ;
5. recommencer au premier participant apres le dernier.

Ce moteur n'utilise ni points, ni duree, ni historique, ni penalite. Avec 7 taches et 3
participants, les volumes sont 3, 2 et 2. Le nombre de taches differe donc au maximum d'une
unite, mais une egalite parfaite est impossible lorsque la division ne tombe pas juste.

### Exemple et limites

Deux executions avec les memes taches peuvent donner des proprietaires differents. Le moteur
ne cherche pas a eviter qu'une personne recoive souvent la meme tache et ne garantit pas encore
la responsabilite unique des taches quotidiennes obligatoires.

---

## BALANCED_POINTS

### Description breve

Moteur hebdomadaire sans memoire. Il repartit les instances de la semaine courante afin de
rapprocher les charges de points entre participants.

### Logique etapes par etapes

1. exclure les participants a disponibilite nulle ;
2. ignorer l'historique et la penalite anti-repetition ;
3. calculer le score de chaque instance ;
4. trier les instances par score decroissant, puis par date et identifiant ;
5. choisir le participant dont la charge de points normalisee est minimale ;
6. affecter la tache et ajouter ses points a la charge courante ;
7. repeter jusqu'a epuisement des instances.

```text
Score = ManualDifficultyScore x PriorityMultiplier x DurationFactor
NormalizedLoad = CurrentPointLoad / (AvailabilityPercentage / 100)
```

Les multiplicateurs de priorite et la duree de reference peuvent modifier le score. La fenetre
d'equite et le poids anti-repetition sont ignores. Le moteur optimise la charge de la semaine,
pas l'equite historique ni necessairement le nombre de taches.

### Exemple et limites

Pour quatre scores 60, 40, 20 et 20, le placement glouton peut produire 80 points contre 60,
plutot que placer les deux plus grosses taches sur la meme personne. Les reports sont filtres
par le cas d'utilisation avant l'appel. La responsabilite unique des taches quotidiennes
obligatoires doit encore etre generalisee a ce moteur.

---

## BALANCED_TIME

### Description breve

Moteur hebdomadaire sans memoire qui equilibre la duree estimee des taches plutot que leur score
de difficulte.

### Logique etapes par etapes

1. exclure les participants a disponibilite nulle ;
2. ignorer l'historique, les scores de difficulte et la penalite anti-repetition ;
3. utiliser la duree estimee, ou la duree de reference si elle est absente ;
4. trier les instances par duree decroissante, puis par date et identifiant ;
5. choisir le participant dont la charge de temps normalisee est minimale ;
6. affecter la tache et ajouter sa duree a la charge courante ;
7. repeter jusqu'a epuisement des instances.

```text
TaskDuration = EstimatedDurationMinutes ?? ReferenceDurationMinutes
NormalizedTimeLoad = CurrentTimeLoad / (AvailabilityPercentage / 100)
```

Les multiplicateurs de priorite, la fenetre d'equite et la penalite sont ignores. La qualite du
resultat depend de la fiabilite des durees renseignees.

### Exemple et limites

Avec des taches de 60, 45, 30 et 15 minutes, les deux plus longues sont d'abord reparties sur
des participants differents, puis les suivantes vont a la charge de temps la plus faible. Les
absences, repetitions historiques et responsabilites quotidiennes restent hors du calcul.

---

## Comparaison des moteurs

| Critere | `INTELLIGENT_V1` | `RANDOM` | `BALANCED_POINTS` | `BALANCED_TIME` |
|---------|------------------|----------|------------------|----------------|
| Historique | oui | non | non | non |
| Score de tache | oui | non | oui | non |
| Duree estimee | oui | non | oui, dans le score | oui |
| Disponibilite | normalise la charge | eligibility uniquement | normalise les points | normalise le temps |
| Penalite anti-repetition | oui | non | non | non |
| Equilibre principal | charge globale | nombre de taches | points | temps |
| Justification | detaillee | ordre aleatoire | charge de points | charge de temps |

## Regles communes et audit

- Une instance annulee ne peut pas etre affectee.
- Une instance reportee possedant un proprietaire conserve ce proprietaire et est exclue de
  l'appel des strategies. Une instance reportee sans proprietaire est incluse afin de ne jamais
  rester non attribuee.
- Lorsqu'une periode a ete creee apres annulation forcee des taches ouvertes,
  `IgnoreHistoryForAssignment` vide l'historique et la penalite anti-repetition pour sa premiere
  affectation.
- Une relance cree un nouvel `AssignmentRun` et remplace les affectations selon les regles
  existantes.
- L'execution est auditee avec `Action = Executed` et le nom du moteur utilise.
- Une notification est creee pour le participant retenu si ses preferences l'autorisent.
- Une valeur de moteur inconnue est refusee par la configuration et ne bascule pas silencieusement
  vers un autre moteur.

## Analyse de conformite de INTELLIGENT_V1

L'implementation actuelle est conforme aux regles documentees pour le calcul du score, la
normalisation par disponibilite, l'historique sur huit semaines, la penalite anti-repetition,
le departage et l'affectation des taches quotidiennes obligatoires. Les absences restent
volontairement non implementees, conformement au hors-perimetre V1.

Les nouveaux moteurs respectent leur objectif propre, mais la contrainte de responsabilite unique
pour les taches quotidiennes obligatoires doit encore etre appliquee de maniere commune si ces
moteurs doivent traiter ces taches.

## Algorithmes futurs

L'interface `IAssignmentStrategy` permet d'ajouter de nouvelles strategies sans modifier le
contrat du moteur existant. Les pistes suivantes restent prevues :

- `RANDOM_WEIGHTED` : hasard controle par disponibilite ou rotation ;
- `BALANCED_TASK_COUNT` : equilibre strict du nombre de responsabilites, notamment pour les
'taches quotidiennes obligatoires ;
- `BALANCED_POINTS_V2` : meilleure gestion des groupes d'occurrences et des contraintes metier ;
- `BALANCED_TIME_V2` : prise en compte des plages disponibles et des durees reellement observees ;
- `FAIRNESS_V2` : equite historique multi-criteres avec explication detaillee ;
- `ABSENCE_AWARE` : exclusion et remplacement des participants absents lorsque `UserAbsence`
  sera implemente ;
- `OPTIMIZED` : resolution globale par optimisation, utile lorsque le nombre de contraintes
  augmente.

Chaque futur moteur devra preciser son objectif, ses entrees, ses etapes de decision, ses
parametres, ses regles de departage, son format de justification et ses tests de non-regression.
