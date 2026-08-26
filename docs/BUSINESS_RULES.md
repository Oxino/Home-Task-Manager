# BUSINESS_RULES

## Objectif

Ce document centralise l'ensemble des règles métier de l'application.

Ces règles décrivent le comportement attendu du système indépendamment de toute implémentation technique.

---

# BR-001 - Identification unique des entités

## Description

Chaque entité métier du système doit posséder un identifiant unique et immuable.

## Entités concernées

- Utilisateur
- Tâche
- Affectation
- Historique
- Paramètre

## Règle

Un identifiant ne peut jamais être modifié après sa création.

---

# BR-002 - Unicité de l'identité utilisateur

## Description

Chaque utilisateur doit être identifié de manière unique.

> **Clarification (voir RISKS_AND_EDGE_CASES R-01 / ADR-006)** : le modèle de données
> retenu (`DOMAIN_MODEL.User`) n'utilise pas d'adresse email mais un `DisplayName`.
> L'unicité s'applique donc au `DisplayName` tant que l'authentification par sélection
> de profil (ADR-003) reste en vigueur. Si une authentification par email est adoptée
> ultérieurement, cette règle portera sur l'email.

## Règle

Le système doit empêcher la création de plusieurs utilisateurs possédant la même identité
(`DisplayName`, ou email si activé).

## Cas de rejet

- Identité déjà utilisée par un utilisateur actif.
- Identité déjà utilisée par un utilisateur désactivé.

---

# BR-003 - Utilisateurs actifs uniquement

## Description

Seuls les utilisateurs actifs peuvent recevoir des tâches.

## Règle

Lors de l'exécution du moteur d'affectation, les utilisateurs inactifs doivent être ignorés.

---

# BR-004 - Connexion

## Description

Un utilisateur inactif ne peut pas se connecter.

## Règle

Toute tentative de connexion d'un utilisateur désactivé doit être refusée.

---

# BR-005 - Existence d'une tâche

## Description

Toute affectation doit obligatoirement référencer une tâche existante.

## Règle

Il est interdit de créer une affectation sans tâche associée.

---

# BR-006 - Existence d'un utilisateur

## Description

Toute affectation doit obligatoirement référencer un utilisateur existant.

## Règle

Il est interdit de créer une affectation sans utilisateur associé.

---

# BR-007 - Affectation hebdomadaire

## Description

Une affectation est toujours rattachée à une semaine.

## Règle

Chaque affectation doit posséder :

- une année ;
- un numéro de semaine.

---

# BR-008 - Unicité d'une affectation par tâche, semaine et jour planifié

## Description

Une occurrence de tâche ne peut être affectée qu'une seule fois à un même utilisateur pour une
même semaine **et un même jour planifié**.

> **Clarification (voir RISKS_AND_EDGE_CASES R-07)** : la règle initiale « une seule affectation
> par tâche et par semaine » est incompatible avec la fréquence `DailyMandatory`, qui génère
> 7 instances par semaine. L'unicité porte donc sur le quadruplet
> `(TaskTemplate, User, Semaine, DatePlanifiée)`.

## Exemple

### Autorisé

Utilisateur A / Tâche X / Semaine 12 / Lundi
Utilisateur A / Tâche X / Semaine 12 / Mardi
Utilisateur A / Tâche X / Semaine 13 / Lundi

### Refusé

Utilisateur A / Tâche X / Semaine 12 / Lundi
Utilisateur A / Tâche X / Semaine 12 / Lundi (doublon)

---

# BR-009 - État initial d'une affectation

## Description

Toute nouvelle affectation débute non complétée.

## Valeur par défaut

Complétée = Non

---

# BR-010 - Modification de la complétude

## Description

L'utilisateur peut modifier l'état de complétude d'une tâche.

## Règle

L'utilisateur peut :

- passer une tâche à complétée ;
- repasser une tâche à non complétée ;
- modifier cet état plusieurs fois durant la semaine.

---

# BR-011 - Historisation de la complétude

## Description

Tout changement de complétude doit être conservé.

## Informations historisées

- date du changement ;
- utilisateur ;
- ancienne valeur ;
- nouvelle valeur.

---

# BR-012 - État courant

## Description

Le système doit toujours afficher le dernier état connu.

## Exemple

Historique :

- Lundi : Non complétée
- Mardi : Complétée
- Jeudi : Non complétée

État affiché :

Non complétée

---

# BR-013 - Commentaires utilisateur

## Description

Un utilisateur peut ajouter un commentaire à une affectation.

## Règle

Les commentaires sont conservés dans l'historique.

---

# BR-014 - Archivage des tâches

## Description

Une tâche peut être archivée.

## Règle

Une tâche archivée :

- ne peut plus être affectée ;
- reste visible dans l'historique.

---

# BR-015 - Désactivation des tâches

## Description

Une tâche peut être désactivée sans être supprimée.

## Règle

Une tâche inactive :

- n'apparaît plus dans les futures affectations ;
- reste présente dans l'historique.

---

# BR-016 - Suppression logique

## Description

Les données métier ne doivent jamais être supprimées physiquement.

## Règle

La suppression doit être logique.

## Objectif

Préserver l'historique complet de l'application.

---

# BR-017 - Traçabilité

## Description

Toute modification métier importante doit être tracée.

## Événements concernés

- création et modification d'un participant ;
- activation et désactivation d'un participant ;
- création et modification d'un modèle de tâche ;
- archivage ou réactivation d'un modèle de tâche ;
- lancement d'une affectation ;
- réaffectation ou annulation d'une instance ;
- changement de complétude (complétion ou réouverture) ;
- ajout de commentaire ;
- ajout ou modification d'un rappel ;
- clôture de semaine ;
- modification des paramètres du moteur d'affectation.

## Classification de l'historique

La catégorie d'un événement dépend de la nature de l'action, et non du rôle du profil qui l'a
effectuée. Un administrateur qui crée une tâche produit donc une action utilisateur, tandis
qu'un événement de gestion reste une action d'administration quel que soit son auteur.

### Actions utilisateur

- complétion ou réouverture d'une tâche ;
- création ou modification d'un modèle de tâche ;
- ajout d'un commentaire ;
- ajout ou modification d'un rappel ;
- configuration des préférences utilisateur.

### Actions d'administration

- suppression logique, désactivation ou réactivation d'un modèle de tâche ;
- génération d'instances (catégorie attendue, mais événement encore manquant dans l'audit) ;
- lancement ou relance d'une affectation ;
- clôture d'une semaine ;
- réaffectation ou annulation d'une instance ;
- création ou modification d'un participant ;
- modification des paramètres du moteur d'affectation.

La classification technique actuelle s'appuie sur `EntityType`, `Action` et `NewValue`. Les
actions de rappels et d'activation utilisent encore `Action = Updated`, avec leur détail pour
les distinguer. Les préférences, réactions de commentaires et exécutions flexibles ne créent
pas encore de `AuditLog`; la génération d'instances n'est pas non plus journalisée. Ces quatre
couvertures restent à ajouter pour atteindre une traçabilité complète.

---

# BR-018 - Gestion des rôles

## Description

Chaque utilisateur possède un rôle.

## Rôles supportés

### Utilisateur

Accès limité à ses propres données.

### Administrateur

Accès à l'ensemble du système.

---

# BR-019 - Accès aux données

## Description

Un utilisateur standard ne peut accéder qu'à ses propres affectations.

## Interdiction

Visualiser :

- les tâches des autres utilisateurs ;
- les statistiques globales ;
- les paramètres système.

---

# BR-020 - Pouvoirs administrateur

## Description

Un administrateur possède un accès complet.

## Capacités

- gérer les utilisateurs ;
- gérer les tâches ;
- gérer les affectations ;
- gérer les paramètres ;
- consulter tous les historiques.

---

# BR-021 - Génération des affectations

## Description

Les affectations peuvent être générées automatiquement.

## Source

Le moteur d'affectation.

---

# BR-022 - Réaffectation manuelle

## Description

Un administrateur peut modifier une affectation.

## Conséquence

La modification doit être historisée.

---

# BR-023 - Charge utilisateur

## Description

Le moteur d'affectation doit chercher à équilibrer la charge.

## Objectif

Éviter qu'un utilisateur reçoive systématiquement davantage de tâches.

---

# BR-024 - Exclusion des utilisateurs inactifs

## Description

Le moteur d'affectation ignore les utilisateurs inactifs.

---

# BR-025 - Gestion des absences

## Description

Le moteur d'affectation pourra exclure les utilisateurs absents sur la période concernée.

## Statut

**Hors V1** : la prise en compte des absences (`UserAbsence`) est reportée à une version future
(voir `BACKLOG.md`). L'entité reste définie dans le modèle en prévision.

## Règle (cible)

Un utilisateur dont une absence couvre la période d'une tâche ne reçoit pas de nouvelle
affectation pour cette période.

---

# BR-026 - Pénibilité et priorité des tâches

## Description

Chaque tâche possède une **pénibilité** (`ManualDifficultyScore`) et une **priorité** (`Priority`).

> **Clarification (voir RISKS_AND_EDGE_CASES R-03 / ADR-012)** : le `DOMAIN_MODEL` définit un
> `ManualDifficultyScore` (entier 1..100) et une `Priority`. Les deux interviennent dans le calcul
> du score global de la tâche (BR-052).

## Pénibilité

- `ManualDifficultyScore` : entier de 1 à 100.
- Utilisée par le moteur d'affectation pour le calcul des points.

## Priorité

- Faible
- Normale (valeur par défaut)
- Critique

Voir BR-056 pour son impact sur le score.

---

# BR-027 - Fréquence des tâches

## Description

Une tâche possède une fréquence.

## Valeurs possibles

> **Clarification (voir RISKS_AND_EDGE_CASES R-26)** : la liste ci-dessous est alignée sur
> l'enum `FrequencyType` du `DOMAIN_MODEL`, qui fait référence.

- Quotidienne obligatoire (`DailyMandatory`) — une occurrence par jour, les 7 instances de la
  semaine restant affectées au même participant.
- Quotidienne flexible (`DailyFlexible`) — au moins une occurrence par semaine, plusieurs possibles.
- Hebdomadaire (`Weekly`) — une occurrence par semaine.
- Mensuelle (`Monthly`) — une occurrence par mois.
- Ponctuelle (`OneTime`) — une occurrence unique.

---

# BR-028 - Consultation historique

## Description

Les utilisateurs peuvent consulter leurs affectations passées.

## Limitation

Uniquement leurs propres données.

---

# BR-029 - Tableau de bord utilisateur

## Description

Le tableau de bord utilisateur doit refléter les données de la semaine courante.

## Indicateurs

- nombre de tâches ;
- nombre de tâches complétées ;
- nombre de tâches restantes ;
- pourcentage d'avancement.

---

# BR-030 - Tableau de bord administrateur

## Description

Les indicateurs administrateurs couvrent l'intégralité du système.

## Indicateurs

- utilisateurs actifs ;
- utilisateurs inactifs ;
- tâches actives ;
- tâches archivées ;
- taux global de complétude ;
- statistiques hebdomadaires ;
- historique d'activité.

---

# BR-031 - Paramètres système

## Description

Les paramètres globaux sont modifiables uniquement par les administrateurs.

## Exemples

- paramètres d'affectation ;
- paramètres de notification ;
- paramètres de sécurité ;
- valeurs métier configurables.

---

# BR-032 - Cohérence des données

## Description

Les données doivent rester cohérentes à tout instant.

## Contrôles attendus

- références valides ;
- relations valides ;
- valeurs obligatoires renseignées ;
- absence de doublons fonctionnels.

---

# BR-033 - Auditabilité

## Description

Le système doit permettre une analyse complète des décisions prises.

## Objectif

Pouvoir expliquer :

- pourquoi une tâche a été affectée ;
- quand elle a été réalisée ;
- par qui elle a été modifiée ;
- quel était son état à une date donnée.

---

# Règles complémentaires (issues de l'analyse de cohérence)

> Les règles BR-034 à BR-054 comblent les lacunes identifiées dans
> `RISKS_AND_EDGE_CASES.md`. Certaines dépendent de décisions à consigner dans `DECISIONS.md`.

---

# BR-034 - Bornes de disponibilité

## Description

`AvailabilityPercentage` doit être compris entre 0 et 100 inclus.

## Règle

Toute valeur hors de l'intervalle [0..100] est rejetée. La valeur 0 signifie indisponible.

---

# BR-035 - Désactivation d'un utilisateur portant des tâches

## Description

Lorsqu'un utilisateur est désactivé, ses instances non terminées doivent être traitées.

## Règle

- Aucune nouvelle affectation ne lui est attribuée (BR-003).
- Ses instances `Pending` non terminées déclenchent une réaffectation automatique (BR-042).

---

# BR-036 - Déclaration et validité des absences

## Description

Une absence possède une date de début et une date de fin.

## Règle

- `EndDate >= StartDate` (sinon rejet).
- Un participant peut déclarer sa propre absence ; un administrateur peut déclarer celle de tous.
- Le comportement en cas de chevauchement de deux absences doit être défini (fusion ou rejet).

---

# BR-037 - Droits de création et de gestion des tâches

## Description

Les deux profils peuvent créer des tâches, avec des droits de modification différents.

## Règle

- **Administrateur** : peut créer, modifier et supprimer (logiquement) / archiver **toutes** les tâches.
- **Utilisateur** : peut créer des tâches et modifier **uniquement celles dont il est l'auteur**
  (`CreatedByUserId`).
- La configuration du moteur d'affectation est réservée à l'administrateur.

> Résout l'incohérence R-02. Confirmé par ADR-007.

---

# BR-038 - Rattachement d'une exécution à une semaine ISO

## Description

Toute génération d'affectations et toute instance doivent être rattachées à une semaine
identifiée selon la norme ISO-8601 (`Year` + `WeekNumber`).

## Règle

- `AssignmentRun` et `TaskInstance` doivent permettre de déterminer la semaine concernée.
- La numérotation suit strictement ISO-8601 (gestion des années à 53 semaines).

> Résout l'incohérence R-13 / R-27. Implique l'ajout d'un rattachement semaine au modèle.

---

# BR-039 - Report d'une tâche non terminée

## Description

Une tâche non terminée en fin de période est reportée sur la période suivante une seule fois.

## Règle

- La nouvelle instance porte `IsCarryOver = true` et référence son instance d'origine
  (`ParentTaskInstanceId`).
- Elle reste affectée au même propriétaire (ADR-005), sauf exception BR-042.
- Une instance `Cancelled` n'est jamais reportée.
- Une instance déjà reportée (`IsCarryOver = true`) qui reste non terminée à la fin de sa
  nouvelle période n'est pas reportée une seconde fois.
- Cette instance reste conservée dans l'historique, mais elle devient inactive pour la suite :
  elle ne participe plus aux affectations ni aux calculs de charge, de poids ou d'équité du
  moteur.

---

# BR-040 - Sémantique de la disponibilité dans le moteur

## Description

La disponibilité pondère la charge cible d'un participant.

## Règle

- La cible de points d'un participant est proportionnelle à son `AvailabilityPercentage`.
- Un participant à 0 % est exclu de la répartition des nouvelles tâches.

> Résout l'ambiguïté R-08.

---

# BR-041 - Relance d'affectation (écrasement)

## Description

Relancer le moteur sur une semaine régénère et **écrase** l'affectation existante de cette semaine.

## Règle

- Une nouvelle exécution pour une semaine remplace la répartition précédente de cette semaine.
- L'historique de l'exécution précédente reste conservé (aucune purge, BR-016 / ADR-004).
- Les tâches reportées restent affectées à leur propriétaire (ADR-005, BR-039), sauf exception BR-042.

> Confirmé par ADR-013. Remplace la formulation « idempotente » initiale (voir R-16).

---

# BR-042 - Réaffectation automatique sur indisponibilité

## Description

Une tâche reportée dont le propriétaire devient inactif ou absent devrait être réaffectée.

## Statut

**Hors V1** : reportée à une version future (voir `BACKLOG.md`, R-05).

## Règle (cible)

- Exception à ADR-005 : si le propriétaire est indisponible sur la période, l'instance est
  soumise au moteur pour réaffectation à un participant disponible.
- La réaffectation est historisée.

---

# BR-043 - Historique bidirectionnel et verrouillage de la complétude

## Description

Durant la semaine active, l'utilisateur peut basculer librement l'état de complétude
(complété ↔ non complété). À la clôture de la semaine, l'état est figé et devient non modifiable.

## Règle

- Pendant la semaine active : modifications libres, chaque changement peut être tracé.
- À la clôture de la semaine : l'état courant est enregistré définitivement et verrouillé
  (aucune modification ultérieure possible par l'utilisateur).
- L'état courant affiché est toujours le dernier connu (BR-012).

> Résout la lacune R-09. Confirmé par ADR-011. Voir aussi BR-055.

---

# BR-044 - Complétude des tâches quotidiennes flexibles

## Description

Une tâche quotidienne flexible est considérée satisfaite dès une exécution dans la semaine.

## Règle

- Chaque exécution est enregistrée (`DailyFlexibleExecution`).
- Plusieurs exécutions par des participants distincts sont autorisées.
- L'objectif hebdomadaire est atteint dès la première exécution.

> Résout le cas limite R-17.

---

# BR-045 - Rappels de tâche

## Description

Une tâche peut posséder plusieurs rappels.

## Règle

- Déclencheurs supportés : fin de journée, fin de semaine.
- Les rappels ne sont émis que si le participant a activé les notifications.

---

# BR-046 - Stratégie d'affectation active

## Description

Une seule stratégie d'affectation est active à un instant donné.

## Règle

- L'administrateur sélectionne la stratégie active parmi les stratégies disponibles.
- Le changement de stratégie n'affecte pas rétroactivement les affectations passées.

---

# BR-047 - Non-rétroactivité des modifications de modèle

## Description

Modifier un modèle de tâche n'altère pas les instances déjà générées.

## Règle

Les instances existantes conservent les valeurs en vigueur au moment de leur génération
(cohérent avec ADR-001).

> Résout le cas limite R-04.

---

# BR-048 - Départage déterministe du moteur

## Description

En cas d'égalité de scores entre participants, le choix doit être déterministe.

## Règle

Ordre de départage :

1. le participant ayant le **moins de tâches affectées** (charge en nombre d'occurrences) ;
2. puis celui ayant le **moins réalisé récemment la tâche concernée** (éviter la répétition) ;
3. puis un identifiant stable.

Aucun tirage aléatoire dans INTELLIGENT_V1.

> Résout l'ambiguïté R-15. Confirmé par ADR-016.

---

# BR-049 - Placement des tâches mensuelles

## Description

La semaine du mois où une tâche mensuelle est instanciée doit être déterministe.

## Règle

Par défaut, l'instance mensuelle est créée sur la première semaine ISO du mois. Ce comportement
peut être rendu configurable.

> Résout le cas limite R-12.

---

# BR-050 - Absence de participant disponible

## Description

Le moteur peut ne trouver aucun participant disponible.

## Règle

- Aucune affectation n'est produite pour les tâches concernées.
- L'exécution est journalisée comme « sans affectation » et l'administrateur est alerté.

> Résout le cas limite R-14.

---

# BR-051 - Fuseau horaire applicatif

## Description

Les bornes de journée et de semaine dépendent d'un fuseau horaire de référence.

## Règle

- Un fuseau horaire applicatif est configuré (par défaut celui du foyer).
- Les déclencheurs de rappels et de reports s'appuient sur ce fuseau.

> Résout le cas limite R-28.

---

# BR-052 - Calcul du score de charge

## Description

`CalculatedScore` doit être recalculable à partir de données du modèle.

## Règle (V1)

Le score V1 combine trois facteurs :

- la pénibilité (`ManualDifficultyScore`, 1..100) ;
- la **priorité** (`Priority`), via un multiplicateur (BR-056) ;
- la **durée estimée** (`EstimatedDurationMinutes`).

### Formule de référence (V1)

```text
CalculatedScore = ManualDifficultyScore × MultiplicateurPriorité × FacteurDurée
```

- `MultiplicateurPriorité` : Faible = 0.5, Normale = 1, Critique = 1.5 (valeurs par défaut).
- `FacteurDurée` : proportionnel à la durée estimée, normalisé sur une durée de référence.

### Facteur de durée

```text
FacteurDurée = EstimatedDurationMinutes / DuréeRéférenceMinutes
```

- `DuréeRéférenceMinutes` : **30 minutes** par défaut (paramétrable, BR-058).
- Une tâche de 30 min a donc un facteur de 1, une tâche de 60 min un facteur de 2, une tâche de
  15 min un facteur de 0,5.
- Si `EstimatedDurationMinutes` est absent, `FacteurDurée = 1` (la durée n'influence pas le score).

### Exemple

Tâche « Passer l'aspirateur » : pénibilité 40, priorité Normale (×1), durée 45 min.

```text
CalculatedScore = 40 × 1 × (45 / 30) = 40 × 1 × 1.5 = 60
```

## Paramétrage

- Les multiplicateurs de priorité sont **paramétrables par l'administrateur**.
- Le **mode de calcul du score** est lui-même paramétrable : il fait partie des paramètres
  d'algorithme de répartition sélectionnables par l'administrateur (BR-046, BR-058).
- La formule peut donc évoluer sans changer la structure du modèle.

> Résout la lacune R-31. Priorité confirmée par ADR-012. Paramétrage confirmé par ADR-014.

---

# BR-053 - Fenêtre d'équité historique

## Description

L'équilibrage s'appuie sur une fenêtre glissante d'historique.

## Règle

La fenêtre par défaut est de 8 semaines. Elle est **paramétrable par l'administrateur** (fait
partie des paramètres d'algorithme, BR-058). Cette fenêtre est une fenêtre de **calcul**
glissante et n'implique aucune purge : l'historique complet est conservé (ADR-004).

> Résout la lacune R-32.

---

# BR-054 - Anti-répétition

## Description

Le moteur limite l'attribution répétée d'une même tâche à un même participant.

## Règle

- Mécanisme retenu : **pénalité de points** (et non interdiction stricte). Le participant ayant
  réalisé récemment la tâche voit son attribution défavorisée, sans que ce soit impossible.
- Ce mécanisme fait partie des paramètres d'algorithme de répartition (BR-058) et pourra être
  affiné ou remplacé par d'autres stratégies ultérieurement.
- La pénalité ne viole jamais BR-039 (reports) ni BR-040 (disponibilité).

## Calcul de la pénalité (V1)

Lors de l'évaluation d'un candidat pour une tâche donnée, on ajoute à son score prévisionnel une
pénalité proportionnelle au nombre de fois où il a récemment réalisé cette tâche :

```text
Pénalité = OccurrencesRécentes × PoidsPénalité × ScoreTâche
```

- `OccurrencesRécentes` : nombre de réalisations de cette tâche par le candidat sur la fenêtre
  d'équité (BR-053, 8 semaines par défaut).
- `PoidsPénalité` : **0,25** par défaut (paramétrable, BR-058).
- `ScoreTâche` : le `CalculatedScore` de la tâche (BR-052).

La pénalité augmente artificiellement la charge perçue du candidat, ce qui le rend moins
prioritaire pour cette tâche sans jamais l'exclure.

### Exemple

Tâche de score 60. Julie l'a réalisée 4 fois sur les 8 dernières semaines.

```text
Pénalité = 4 × 0.25 × 60 = 60 points ajoutés au score prévisionnel de Julie pour cette tâche
```

À charge égale, la tâche sera donc préférentiellement attribuée à un autre participant.

> Résout la lacune R-33. Confirmé par ADR-015.

---

# BR-055 - Clôture hebdomadaire

## Description

Une semaine possède un cycle de vie : active puis clôturée.

## Règle

- Tant qu'une semaine est active, les états de complétude de ses instances sont modifiables.
- À la clôture (fin de semaine), les états sont figés définitivement (BR-043).
- Une semaine clôturée reste consultable en lecture seule.
- L'historique complet est conservé sans purge (ADR-004).

> Confirmé par ADR-011.

---

# BR-056 - Impact de la priorité sur le score

## Description

La priorité d'une tâche influence le nombre de points attribués à cette tâche via un multiplicateur.

## Règle

- Multiplicateurs par défaut : `Faible = 0.5`, `Normale = 1`, `Critique = 1.5`.
- La valeur par défaut d'une tâche est `Normale`.
- Les multiplicateurs sont **paramétrables par l'administrateur** (BR-058).
- Ce facteur entre dans le calcul du score global (BR-052) utilisé par le moteur d'affectation.

> Confirmé par ADR-012 et ADR-014.

---

# BR-057 - Conservation intégrale de l'historique

## Description

Aucune purge n'est effectuée : l'intégralité de l'historique est conservée.

## Règle

- Aucune suppression physique ni purge planifiée (suppression logique uniquement, BR-016).
- Les affectations, complétions et événements d'audit sont conservés indéfiniment.

> Confirmé par ADR-004 (révisé). Remplace la rétention d'un an initialement envisagée.

---

# BR-058 - Paramètres d'algorithme de répartition

## Description

Le comportement du moteur d'affectation est configurable via un ensemble de paramètres
sélectionnables par l'administrateur.

## Règle

Les paramètres suivants sont administrables (stockés en `SystemSetting`) :

- le **mode de calcul du score** (formule utilisée, BR-052) ;
- les **multiplicateurs de priorité** (Faible/Normale/Critique, BR-056) ;
- la **durée de référence** du facteur de durée (défaut 30 min, BR-052) ;
- la **fenêtre d'équité historique** (durée en semaines, défaut 8, BR-053) ;
- le **poids de la pénalité anti-répétition** (défaut 0,25, BR-054).

## Valeurs par défaut

| Paramètre | Clé | Valeur par défaut |
|-----------|-----|-------------------|
| Multiplicateur priorité Faible | `scoring.priority.low` | 0.5 |
| Multiplicateur priorité Normale | `scoring.priority.normal` | 1.0 |
| Multiplicateur priorité Critique | `scoring.priority.critical` | 1.5 |
| Durée de référence (minutes) | `scoring.duration.referenceMinutes` | 30 |
| Fenêtre d'équité (semaines) | `assignment.equityWindowWeeks` | 8 |
| Poids de la pénalité anti-répétition | `assignment.repetitionPenaltyWeight` | 0.25 |

Ces paramètres constituent la configuration de l'algorithme actif (BR-046). De nouveaux
algorithmes de répartition pourront exposer leurs propres paramètres ultérieurement.

> Confirmé par ADR-014. Réservé à l'administrateur (BR-031).