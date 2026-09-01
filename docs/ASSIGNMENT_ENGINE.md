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

## Repartition d'une tache quotidienne obligatoire

Chaque modele `DailyMandatory` porte son propre indicateur `DistributeDailyMandatory`,
modifiable depuis le formulaire de creation et de modification du modele :

- **coche (defaut)** : les 7 instances de la semaine sont traitees independamment par le moteur,
  comme n'importe quelle autre frequence ; elles peuvent donc etre reparties entre plusieurs
  participants au cours de la semaine.
- **decoche** : la premiere instance decidee par le moteur fixe un unique propriataire pour les
  7 instances de la semaine (comportement historique).

Ce reglage est applique de maniere identique par les quatre moteurs (`INTELLIGENT_V1`, `RANDOM`,
`BALANCED_POINTS`, `BALANCED_TIME`) : aucun ne traite plus la responsabilite unique comme une
limite non couverte.

---

## INTELLIGENT_V1

### Description breve

Moteur complet et moteur par defaut. Il cherche une repartition equitable dans le temps en
combinant la charge de la semaine, la disponibilite, l'historique recent et une penalite contre
les repetitions. Contrairement a une simple passe chronologique, il construit une repartition
initiale puis la corrige avant de la valider : une decision malheureuse en debut de semaine peut
etre annulee plus tard dans le meme calcul, au lieu de rester figee.

### Logique etapes par etapes

1. **Unites de travail** : chaque instance est une unite independante, sauf les instances d'un
  modele quotidien obligatoire dont `DistributeDailyMandatory = false` : ses 7 instances sont
  regroupees en une seule unite qui se deplace toujours ensemble (memes 7 instances, un seul
  proprietaire).
2. **Construction initiale (glouton par ordre decroissant de score)** : les unites sont triees
  par score decroissant (la plus grosse d'abord), puis par date planifiee et identifiant stable
  en cas d'egalite. Chaque unite est affectee au candidat dont la charge ajustee et normalisee
  serait alors la plus faible (memes formules que ci-dessous). Traiter la plus grosse tache en
  premier, quand tous les participants sont encore proches, evite qu'elle bascule tout le reste
  de la semaine par accident d'ordre de traitement.
3. **Recherche locale bornee (correction)** : le moteur cherche ensuite, parmi tous les
  deplacements d'une seule unite vers un autre participant et tous les echanges entre deux
  unites de proprietaires differents, celui qui reduit le plus la variance des charges
  normalisees finales entre participants. Le meilleur mouvement trouve est applique, puis la
  recherche recommence, jusqu'a ce qu'aucun mouvement ne l'ameliore ou qu'un plafond de
  500 iterations soit atteint (garde-fou de performance, jamais atteint pour un foyer classique).
4. Les instances d'une meme unite quotidienne obligatoire regroupee restent toujours ensemble
  pendant toute la construction et la recherche locale : un deplacement ou un echange les
  deplace toutes en meme temps.

### Calcul du score

```text
CalculatedScore = ManualDifficultyScore x PriorityMultiplier x DurationFactor
DurationFactor   = EstimatedDurationMinutes / ReferenceDurationMinutes
```

Si la duree est absente, `DurationFactor` vaut 1. Les multiplicateurs par defaut sont : faible
0.5, normale 1.0 et critique 1.5. Pour une unite regroupee (tache quotidienne obligatoire non
repartie), le score est la somme des 7 scores individuels.

### Historique et penalite

L'historique est lu sur la fenetre d'equite, par defaut les huit semaines precedant la semaine
traitee. La semaine en cours et les instances reportees sont exclues des calculs historiques ;
les occurrences recentes utilisees pour la penalite sont donc fixees avant le calcul et ne
varient pas selon la repartition retenue cette semaine.

```text
Penalty = RecentOccurrences x RepetitionPenaltyWeight x CalculatedScore
AdjustedContribution = CalculatedScore + Penalty
NormalizedLoad(participant) = (HistoricalScore + Somme des AdjustedContribution retenues) / (AvailabilityPercentage / 100)
```

La construction choisit, a chaque etape, le candidat dont `NormalizedLoad` projete serait alors
la plus faible. La recherche locale minimise ensuite la variance de `NormalizedLoad` sur
l'ensemble des participants eligibles, y compris ceux qui n'auraient encore rien recu.

### Departage

En cas d'egalite stricte entre deux candidats ou deux mouvements :

1. moins d'unites deja affectees pendant la construction ;
2. moins d'occurrences recentes du modele ;
3. identifiant stable du participant.

Pendant la recherche locale, un mouvement n'est retenu que s'il ameliore strictement la
variance ; a amelioration egale, le premier mouvement trouve dans l'ordre stable (date
planifiee puis identifiant d'unite, puis identifiant de participant) est conserve, ce qui rend
le resultat reproductible pour les memes donnees d'entree.

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

Le calcul du score, la normalisation par disponibilite, la fenetre d'equite sur huit semaines et
la formule de penalite restent conformes a la documentation. Le mecanisme de decision a change :
il ne s'agit plus d'une seule passe chronologique irrevocable, mais d'une construction suivie
d'une recherche locale bornee qui peut revenir sur une decision. Les absences restent hors
perimetre V1, comme prevu par `BR-025`. La conservation des reports est appliquee en amont par
le cas d'utilisation. Le plafond de 500 iterations garantit un temps de calcul borne meme sur un
foyer avec de nombreux participants et modeles ; il n'a pas de raison d'etre atteint pour les
volumes hebdomadaires habituels (quelques dizaines d'instances au plus).

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
ne cherche pas a eviter qu'une personne recoive souvent la meme tache. La responsabilite unique
d'une tache quotidienne obligatoire (`DistributeDailyMandatory = false`) est respectee : un seul
tirage determine le proprietaire des 7 instances, les six autres lui sont directement affectees
sans consommer un nouveau tirage aleatoire.

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

En cas d'egalite stricte de charge normalisee puis de nombre de taches deja affectees, le
departage alterne entre les participants a egalite plutot que de toujours choisir le meme
identifiant (BR-062) : chaque nouvelle egalite fait gagner le participant suivant dans l'ordre
stable des participants encore a egalite. Le point de depart de cette rotation est calcule a
partir de l'annee et du numero de semaine traites (et non remis a zero de facon fixe), afin que
deux semaines structurellement identiques ne reproduisent pas le meme motif jour par jour.

### Exemple et limites

Pour quatre scores 60, 40, 20 et 20, le placement glouton peut produire 80 points contre 60,
plutot que placer les deux plus grosses taches sur la meme personne. Les reports sont filtres
par le cas d'utilisation avant l'appel. La responsabilite unique des taches quotidiennes
obligatoires (`DistributeDailyMandatory = false`) est respectee : le premier tirage fixe le
proprietaire des 7 instances, dont le score cumule est ajoute a sa charge au fil du tri.

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

Comme pour `BALANCED_POINTS`, une egalite stricte de charge normalisee puis de nombre de taches
fait alterner le departage entre les participants a egalite (BR-062) au lieu de toujours
favoriser le meme identifiant, avec un point de depart de rotation calcule par semaine. Ce point
est particulierement sensible ici : plusieurs taches partagent souvent la meme duree, ce qui
produit des egalites frequentes.

### Exemple et limites

Avec des taches de 60, 45, 30 et 15 minutes, les deux plus longues sont d'abord reparties sur
des participants differents, puis les suivantes vont a la charge de temps la plus faible. Les
absences et repetitions historiques restent hors du calcul. La responsabilite unique d'une tache
quotidienne obligatoire (`DistributeDailyMandatory = false`) est respectee de la meme maniere
que pour `BALANCED_POINTS`.

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
| Responsabilite unique quotidienne obligatoire | configurable (`DistributeDailyMandatory`) | configurable (`DistributeDailyMandatory`) | configurable (`DistributeDailyMandatory`) | configurable (`DistributeDailyMandatory`) |

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
normalisation par disponibilite, l'historique sur huit semaines, la penalite anti-repetition et
l'affectation des taches quotidiennes obligatoires. Les absences restent volontairement non
implementees, conformement au hors-perimetre V1.

Le mecanisme de decision a ete revu : une construction gloutonne par score decroissant (LPT),
suivie d'une recherche locale bornee, remplace la passe chronologique a decision unique et
irrevocable. Ce changement corrige un defaut observe en test manuel : une seule decision sur une
tache a fort score, prise tot dans la semaine, pouvait basculer la totalite des instances
restantes vers un seul participant sans possibilite de correction. Le departage reste
deterministe, mais ses criteres s'appliquent desormais a la construction et a la recherche
locale plutot qu'a une sequence unique de decisions.

Validation manuelle sur quatre semaines consecutives avec deux participants et un historique
enchaine : les semaines a balayage complet (11/0, 12/0) ont disparu avec le nouvel algorithme,
remplacees par des repartitions au pire ratio 2:1 (8/4, 7/4, 4/7, 7/5).

Les quatre moteurs appliquent la meme regle de responsabilite unique pour les taches
quotidiennes obligatoires, pilotee par l'indicateur `DistributeDailyMandatory` du modele de
tache plutot que par une difference de comportement entre moteurs.

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
