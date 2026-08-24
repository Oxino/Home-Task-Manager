# Vision

## Présentation du projet

L'application est une plateforme de gestion et de suivi des tâches hebdomadaires permettant :

- d'affecter automatiquement des tâches aux utilisateurs ;
- de suivre leur réalisation semaine après semaine ;
- de permettre aux utilisateurs de déclarer l'avancement réel de leurs tâches ;
- de fournir aux administrateurs une vision globale de l'activité ;
- d'automatiser les règles d'affectation grâce à un moteur configurable ;
- de conserver un historique complet des affectations et réalisations.

L'objectif principal est de réduire les opérations manuelles de gestion, d'améliorer le suivi opérationnel et de fournir des indicateurs fiables sur l'activité réalisée.

---

# Objectifs métier

L'application doit permettre de :

- Centraliser l'ensemble des tâches à réaliser.
- Répartir automatiquement les tâches entre les utilisateurs.
- Garantir une répartition équilibrée des charges de travail.
- Permettre un suivi simple et rapide des réalisations.
- Mesurer le niveau de complétude des tâches.
- Conserver un historique complet des réalisations.
- Donner de la visibilité aux administrateurs sur l'activité globale.
- Faciliter le pilotage de l'activité grâce à des tableaux de bord.

---

# Principes de fonctionnement

Le système fonctionne par semaine.

Chaque semaine :

- les tâches sont affectées aux utilisateurs ;
- les utilisateurs réalisent leurs travaux ;
- ils peuvent mettre à jour l'état d'avancement des tâches ;
- les administrateurs supervisent l'activité ;
- les statistiques sont calculées et historisées.

L'ensemble des données doit être conservé dans le temps afin de permettre la consultation des semaines précédentes.

---

# Types d'utilisateurs

## Utilisateur standard

Un utilisateur standard peut :

- Se connecter à l'application.
- Consulter ses tâches affectées.
- Consulter les tâches des semaines précédentes.
- Modifier l'état de complétude de ses tâches.
- Ajouter des commentaires sur ses tâches.
- Consulter son historique personnel.
- Visualiser ses statistiques personnelles.

Il ne peut pas :

- Modifier les utilisateurs.
- Modifier les paramètres globaux.
- Modifier les tâches des autres utilisateurs.
- Modifier les règles d'affectation.

---

## Administrateur

Un administrateur peut :

- Gérer les utilisateurs.
- Gérer les tâches.
- Gérer les affectations.
- Forcer une réaffectation.
- Gérer les paramètres applicatifs.
- Consulter l'ensemble des statistiques.
- Consulter tous les historiques.
- Configurer le moteur d'affectation.

---

# Fonctionnalités principales

## Authentification

Le système repose sur une authentification simple par **sélection de profil** (usage familial
local, ADR-003 / ADR-006) :

- sélection du profil parmi les profils actifs ;
- gestion des rôles (administrateur / utilisateur via `IsAdmin`, ADR-010) ;
- gestion des autorisations ;
- déconnexion.

Aucune adresse email ni mot de passe n'est requis.

---

## Gestion des tâches

Les tâches constituent l'objet principal de l'application.

Une tâche peut :

- être créée ;
- être modifiée ;
- être désactivée ;
- être archivée ;
- être supprimée logiquement.

Chaque tâche pourra être affectée automatiquement ou manuellement à un utilisateur.

---

## Gestion de la complétude

Une tâche possède un état de complétude.

Un utilisateur doit pouvoir, à tout moment durant la semaine :

- définir une tâche comme complétée ;
- retirer le statut complétée ;
- modifier cet état autant de fois que nécessaire.

Exemple :

- Lundi : tâche non complétée.
- Mardi : tâche complétée.
- Jeudi : tâche repassée en non complétée.
- Vendredi : tâche de nouveau complétée.

Le système doit toujours afficher l'état courant.

Tous les changements doivent être historisés.

---

## Historique

L'application doit conserver l'historique complet :

- des affectations ;
- des changements de statut ;
- des changements de complétude ;
- des modifications administratives ;
- des suppressions logiques ;
- des réaffectations.

Chaque événement doit être traçable.

---

# Gestion des semaines

Les utilisateurs doivent pouvoir consulter :

- la semaine en cours ;
- les semaines précédentes ;
- les affectations passées ;
- les tâches réalisées ;
- les tâches non réalisées.

Les administrateurs doivent pouvoir :

- consulter l'activité par semaine ;
- consulter les statistiques historiques ;
- visualiser les tendances de réalisation.

---

# Gestion des utilisateurs

## Création d'un utilisateur

Un administrateur peut créer un utilisateur.

Informations requises :

- nom d'affichage (DisplayName) ;
- rôle (administrateur / utilisateur) ;
- disponibilité ;
- statut actif/inactif.

Aucune adresse email n'est requise (authentification par sélection de profil).

---

## Modification d'un utilisateur

Un administrateur peut :

- modifier ses informations ;
- modifier son rôle ;
- désactiver son accès ;
- réactiver son accès.

---

## Consultation

Un administrateur doit pouvoir consulter :

- la liste complète des utilisateurs ;
- les tâches affectées ;
- les statistiques individuelles ;
- la dernière connexion ;
- le statut du compte.

---

# Gestion des tâches

## Création

Un administrateur comme un utilisateur peut créer une tâche.

- l'administrateur peut créer, modifier et supprimer **toutes** les tâches ;
- l'utilisateur peut créer des tâches et modifier **uniquement les siennes** (BR-037, ADR-007).

### Champs obligatoires

- titre ;
- description ;
- priorité ;
- pénibilité ;
- statut ;
- fréquence.

---

## Modification

Un administrateur peut modifier :

- le titre ;
- la description ;
- la priorité ;
- les règles d'affectation ;
- la fréquence ;
- le statut.

---

## Désactivation

Une tâche peut être :

- active ;
- inactive ;
- archivée.

---

# Modèle métier

## Utilisateur

### Champs obligatoires

- Id
- DisplayName
- IsAdmin
- StatutActif
- Disponibilité

### Champs de suivi

- DateCréation
- DernièreConnexion

---

## Tâche

### Champs obligatoires

- Id
- Titre
- Description
- Priorité
- Pénibilité
- Statut
- Fréquence
- Actif

### Champs de suivi

- DateCréation
- DateModification
- DateArchivage

---

## Affectation

### Champs obligatoires

- Id
- UtilisateurId
- TâcheId
- Semaine

### Champs de suivi

- Complétée
- DateComplétion
- CommentaireUtilisateur
- DernièreModification

---

# Tableaux de bord

## Tableau de bord utilisateur

L'utilisateur doit voir :

- ses tâches affectées ;
- ses tâches complétées ;
- ses tâches restantes ;
- sa progression hebdomadaire ;
- son historique récent.

---

## Tableau de bord administrateur

L'administrateur doit voir :

- nombre total d'utilisateurs ;
- nombre total de tâches ;
- taux de complétude global ;
- répartition des tâches ;
- statistiques par semaine ;
- utilisateurs actifs ;
- utilisateurs inactifs ;
- historique des affectations.

---

# Notifications

Le système pourra envoyer des notifications concernant :

- les nouvelles affectations ;
- les rappels de tâches ;
- les changements de statut ;
- les informations importantes relatives à l'activité.

---

# User Stories

## Utilisateur

### Consultation des tâches

**En tant qu'utilisateur**

Je souhaite consulter mes tâches affectées

**Afin de**

Connaître le travail qui m'est attribué.

---

### Gestion de la complétude

**En tant qu'utilisateur**

Je souhaite pouvoir modifier l'état complété/non complété d'une tâche à n'importe quel moment durant la semaine

**Afin de**

Refléter fidèlement l'avancement réel de mon activité.

---

### Consultation de l'historique

**En tant qu'utilisateur**

Je souhaite consulter les semaines précédentes

**Afin de**

Retrouver mes réalisations passées.

---

### Ajout de commentaires

**En tant qu'utilisateur**

Je souhaite ajouter un commentaire à une tâche

**Afin de**

Apporter un contexte complémentaire à son exécution.

---

## Administrateur

### Gestion des utilisateurs

**En tant qu'administrateur**

Je souhaite gérer les utilisateurs

**Afin de**

Maintenir les informations de la plateforme à jour.

---

### Gestion des tâches

**En tant qu'administrateur**

Je souhaite gérer toutes les tâches

**Afin de**

Garantir leur qualité et leur pertinence.

---

### Gestion des paramètres

**En tant qu'administrateur**

Je souhaite gérer les paramètres de l'application

**Afin de**

Adapter le comportement de la plateforme aux besoins métier.

---

### Supervision globale

**En tant qu'administrateur**

Je souhaite visualiser les indicateurs globaux

**Afin de**

Piloter efficacement l'activité.

---

# Assignment Engine

## Titre

# Document de Référence des Règles d'Affectation

---

## Objectif

Ce document centralise l'ensemble des règles métier utilisées pour attribuer automatiquement les tâches aux utilisateurs.

Le moteur d'affectation doit :

- garantir une répartition cohérente ;
- équilibrer la charge de travail ;
- respecter les contraintes métier ;
- être traçable ;
- être extensible.

---

## Cas de gestion couverts

Le moteur doit permettre à terme d'implémenter les mécanismes suivants :

### Affectation simple

Attribution directe d'une tâche à un utilisateur.

### Affectation par rôle

Attribution selon le rôle de l'utilisateur.

### Affectation par groupe

Attribution à un sous-ensemble d'utilisateurs.

### Affectation par compétence

Attribution selon les compétences déclarées.

### Rotation

Répartition tournante entre plusieurs utilisateurs.

### Répartition équilibrée

Distribution visant à équilibrer la charge globale.

### Priorisation

Traitement préférentiel de certaines tâches.

### Exclusions

Exclusion d'utilisateurs selon certains critères.

### Réaffectation automatique

Réattribution lorsqu'un utilisateur devient indisponible.

### Gestion des absences

Prise en compte des congés et indisponibilités.

### Gestion de la surcharge

Éviter qu'un utilisateur dépasse un seuil défini.

---

## Structure attendue de chaque règle

Chaque règle doit documenter :

- son objectif ;
- son contexte d'utilisation ;
- ses données d'entrée ;
- sa logique de décision ;
- les exceptions ;
- les résultats attendus ;
- des exemples métiers.

---

# Critères de succès

Le projet sera considéré comme réussi lorsque :

- les utilisateurs pourront suivre leurs tâches simplement ;
- les administrateurs pourront gérer l'application sans intervention technique ;
- les affectations seront historisées ;
- la complétude sera traçable ;
- les statistiques seront disponibles ;
- les règles d'affectation seront configurables ;
- la plateforme restera extensible pour les futures évolutions.