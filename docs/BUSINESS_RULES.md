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

# BR-002 - Unicité de l'adresse email

## Description

Chaque utilisateur doit être identifié par une adresse email unique.

## Règle

Le système doit empêcher la création de plusieurs utilisateurs possédant la même adresse email.

## Cas de rejet

- Email déjà utilisé par un utilisateur actif.
- Email déjà utilisé par un utilisateur désactivé.

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

# BR-008 - Une seule affectation par tâche et par semaine

## Description

Une tâche ne peut être affectée qu'une seule fois à un même utilisateur pour une même semaine.

## Exemple

### Autorisé

Utilisateur A
Tâche X
Semaine 12

Utilisateur A
Tâche X
Semaine 13

### Refusé

Utilisateur A
Tâche X
Semaine 12

Utilisateur A
Tâche X
Semaine 12

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

- création utilisateur ;
- modification utilisateur ;
- désactivation utilisateur ;
- création tâche ;
- modification tâche ;
- archivage tâche ;
- affectation ;
- réaffectation ;
- changement de complétude.

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

Une règle future pourra exclure les utilisateurs absents de la génération.

## Statut

Future évolution.

---

# BR-026 - Priorité des tâches

## Description

Chaque tâche possède une priorité.

## Valeurs

- Faible
- Moyenne
- Haute
- Critique

---

# BR-027 - Fréquence des tâches

## Description

Une tâche possède une fréquence.

## Valeurs possibles

- Quotidienne
- Hebdomadaire
- Mensuelle
- Ponctuelle

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