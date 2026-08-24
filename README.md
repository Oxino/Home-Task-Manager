# Home Task Manager

> Application web auto-hébergée de gestion intelligente des tâches domestiques.

## Vision

Home Task Manager est une application permettant de centraliser toutes les tâches d'un foyer et de les répartir automatiquement entre les participants.

Contrairement à une simple Todo List, l'objectif principal est de réduire la charge mentale et d'assurer une répartition équitable des tâches dans le temps.

Le système prend en compte :

- Les tâches récurrentes
- Les tâches ponctuelles
- Les absences
- Les disponibilités
- Les tâches reportées
- L'historique des réalisations
- Les habitudes de répartition

L'objectif est que les utilisateurs n'aient plus à se demander :

> "Qu'est-ce qu'il reste à faire cette semaine ?"

L'application doit être capable de répondre automatiquement à cette question.

---

# Objectifs du projet

## Objectifs principaux

- Centraliser toutes les tâches du foyer
- Réduire la charge mentale
- Répartir les tâches de manière juste
- Éviter les oublis
- Conserver un historique des réalisations
- Expliquer les décisions de répartition

## Hors objectifs

- Gamification
- Classement des utilisateurs
- Application mobile native
- Intelligence artificielle générative

Ces fonctionnalités pourront être étudiées ultérieurement.

---

# Utilisateurs

Version initiale :

- 2 participants

L'application doit cependant supporter un nombre illimité de participants.

## Rôles

### Administrateur

Peut :

- gérer les utilisateurs
- supprimer des tâches
- configurer les algorithmes
- lancer une affectation
- relancer une affectation existante

### Utilisateur

Peut :

- créer des tâches
- modifier des tâches
- consulter son historique
- consulter ses affectations
- marquer une tâche comme terminée

---

# Concepts métier

## Modèle de tâche

Un modèle représente la définition permanente d'une tâche.

Exemple :

- Aspirateur
- Vaisselle
- Tondre la pelouse

Attributs principaux :

- titre
- description
- fréquence
- pénibilité
- durée estimée

---

## Instance de tâche

Une instance représente une tâche réellement attribuée à un utilisateur pour une période donnée.

Exemple :

```text
Tâche : Aspirateur
Semaine : 2026-W35
Utilisateur : Julie
Statut : Terminée
```

Les utilisateurs travaillent uniquement sur les instances.

---

# Fréquences supportées

## Quotidienne obligatoire

Une occurrence est créée chaque jour.

Exemple :

```text
Faire la vaisselle
```

Génère :

```text
Lundi
Mardi
Mercredi
Jeudi
Vendredi
Samedi
Dimanche
```

---

## Quotidienne flexible

Une tâche doit être réalisée au moins une fois pendant la semaine.

Elle peut être réalisée plusieurs fois.

Exemple :

```text
Arroser les plantes
```

Cas possibles :

- 1 fois
- 2 fois
- 5 fois
- tous les jours

Le minimum attendu est une réalisation hebdomadaire.

---

## Hebdomadaire

Une occurrence par semaine.

---

## Mensuelle

Une occurrence par mois.

---

## Ponctuelle

Une seule occurrence.

---

# Gestion des reports

Si une tâche n'est pas terminée :

- elle reste affectée à son propriétaire
- elle reste ouverte
- elle est reportée sur la période suivante

Une tâche reportée n'est pas redistribuée automatiquement.

Elle s'ajoute aux nouvelles tâches générées.

---

# Répartition intelligente

La V1 implémente un algorithme nommé :

```text
INTELLIGENT_V1
```

Objectifs :

- équilibrer les points
- prendre en compte l'historique
- éviter les répétitions
- respecter les disponibilités
- respecter les absences

Le système devra permettre l'ajout futur de nouvelles stratégies :

- RANDOM
- BALANCED_POINTS
- BALANCED_TIME
- INTELLIGENT_V2

---

# Justification des affectations

Chaque affectation doit pouvoir être expliquée.

Exemple :

```text
Aspirateur

Attribué à Julie

Raison :

- Nicolas l'a réalisée récemment
- Julie possède moins de points historiques
- Julie est disponible cette semaine
```

Ces informations seront conservées dans l'historique.

---

# Notifications

Notifications navigateur uniquement.

Chaque utilisateur pourra :

- activer les notifications
- désactiver les notifications

Chaque tâche pourra contenir plusieurs rappels.

Déclencheurs prévus :

- fin de journée
- fin de semaine

---

# Déploiement

## Cible principale

Raspberry Pi 5

## Cibles futures

- Mini PC
- Serveur personnel

## Accès

Application web responsive :

- Desktop
- Tablette
- Smartphone

Aucune application mobile native n'est prévue.

---

# Stack technique

## Backend

- ASP.NET Core 9

## Frontend

- React
- TypeScript

## Base de données

- PostgreSQL

## Conteneurisation

- Docker
- Docker Compose

## Architecture

- Clean Architecture
- Domain Driven Design (léger)
- Architecture modulaire
- Moteur d'affectation extensible

---

# Documentation

La documentation fonctionnelle et technique est située dans le dossier :

```text
/docs
```

Documents principaux :

```text
PRODUCT_VISION.md
ARCHITECTURE.md
DOMAIN_MODEL.md
USER_STORIES.md
ASSIGNMENT_ENGINE.md
DECISIONS.md
BACKLOG.md
```

---

# État du projet

Phase actuelle :

✅ Vision produit

✅ Cahier des charges fonctionnel

✅ Architecture cible

🔄 Conception détaillée

⏳ Développement du MVP

---

# Philosophie du projet

Le but de cette application n'est pas uniquement de gérer des tâches.

Le but est de réduire la charge mentale des occupants du foyer en leur fournissant une vision claire, équitable et explicable de ce qui doit être fait.

La priorité absolue du projet est :

> Répartir les tâches de la manière la plus juste possible.