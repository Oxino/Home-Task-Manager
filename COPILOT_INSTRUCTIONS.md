# COPILOT_INSTRUCTIONS

## Objectif

Tu participes au développement du projet Home Task Manager.

Home Task Manager est une application web auto-hébergée de gestion intelligente des tâches domestiques.

Ce projet n'est PAS une simple Todo List.

Le cœur du produit est un moteur de répartition capable d'équilibrer les charges de travail dans le temps.

Avant toute modification du code, tu dois toujours analyser :

- /docs/PRODUCT_VISION.md
- /docs/ARCHITECTURE.md
- /docs/DOMAIN_MODEL.md
- /docs/ASSIGNMENT_ENGINE.md
- /docs/DECISIONS.md

Ces documents sont la source de vérité du projet.

Le code doit toujours être aligné avec la documentation.

---

# Priorités

Toujours respecter cet ordre :

1. Correctitude métier
2. Maintenabilité
3. Simplicité
4. Performance
5. Optimisation prématurée à éviter

Ne jamais sacrifier la lisibilité pour gagner quelques millisecondes.

---

# Philosophie du projet

L'objectif principal est :

Répartir les tâches de la manière la plus juste possible.

L'application doit :

- réduire la charge mentale
- éviter les oublis
- conserver l'historique
- justifier ses décisions

Toutes les fonctionnalités doivent être évaluées selon ces objectifs.

Si une décision technique s'éloigne de la vision produit alors elle doit être remise en question.

---

# Règles générales de développement

Toujours :

- écrire du code lisible
- privilégier les noms explicites
- éviter les abréviations
- écrire du code facilement testable
- limiter les effets de bord
- respecter le principe de responsabilité unique

Ne jamais :

- introduire de logique métier dans l'interface utilisateur
- dupliquer du code métier
- coder en dur des valeurs métier
- utiliser de "magic strings"
- utiliser de "magic numbers"

---

# Architecture

Le projet utilise une Clean Architecture.

Respecter strictement la séparation suivante :

## Domain

Contient :

- les entités
- les value objects
- les enums
- les règles métier fondamentales

Aucune dépendance vers d'autres couches.

---

## Application

Contient :

- cas d'usage
- services métier
- interfaces
- orchestrations

Pas de dépendance directe vers la base de données.

---

## Infrastructure

Contient :

- Entity Framework
- PostgreSQL
- notifications
- stockage

Toutes les dépendances techniques doivent être ici.

---

## API

Contient :

- endpoints REST
- validation
- mapping

Aucune logique métier complexe.

---

## Frontend

Contient :

- composants React
- écrans
- appels API

Aucune règle métier complexe.

---

# Documentation

Toute décision d'architecture importante doit être documentée dans :

/docs/DECISIONS.md

Lorsqu'un nouveau concept métier apparaît :

- mettre à jour DOMAIN_MODEL.md
- mettre à jour PRODUCT_VISION.md si nécessaire

La documentation doit évoluer avec le projet.

---

# Affectation des tâches

Le moteur de répartition est la fonctionnalité la plus importante du projet.

Toute modification du moteur d'affectation doit :

- être documentée
- être testée
- être justifiée

Privilégier un code facilement extensible.

Les futurs algorithmes devront pouvoir être ajoutés sans modifier les algorithmes existants.

Utiliser le pattern Strategy.

Exemple :

IAssignmentStrategy

IntelligentV1Strategy

RandomStrategy

FutureAlgorithmStrategy

---

# Historique

L'historique est critique.

Ne jamais :

- supprimer l'historique
- modifier rétroactivement des affectations validées

L'historique est utilisé pour :

- les statistiques futures
- les calculs d'équité
- les explications d'affectation

---

# Gestion des reports

Une tâche reportée :

- reste affectée à son propriétaire
- conserve son historique
- n'est pas redistribuée automatiquement

Toute évolution doit respecter cette règle métier.

---

# Génération de code

Lorsqu'une nouvelle fonctionnalité est demandée :

Toujours :

1. Analyser la documentation.
2. Identifier l'impact sur le domaine.
3. Identifier les impacts sur la base.
4. Identifier les impacts sur l'API.
5. Identifier les impacts sur l'interface.

Avant toute implémentation importante :

proposer :

- les classes concernées
- les modifications du modèle de données
- les impacts sur les tests

---

# Base de données

Préférer :

- migrations Entity Framework
- relations explicites
- contraintes explicites

Ne jamais contourner le modèle métier pour simplifier les requêtes SQL.

Le domaine métier est prioritaire.

---

# Tests

Toute logique métier importante doit être couverte par des tests.

Priorité :

1. Tests du moteur d'affectation
2. Tests des règles métier
3. Tests des services applicatifs

Les algorithmes d'affectation doivent posséder plusieurs scénarios de tests.

Exemples :

- répartition équilibrée
- gestion des absences
- gestion des reports
- gestion de l'historique
- évitement des répétitions

---

# Frontend

Objectifs :

- simplicité
- rapidité
- lisibilité

L'utilisateur doit comprendre immédiatement :

- ce qu'il doit faire
- pourquoi une tâche lui a été attribuée

Privilégier des interfaces simples.

Éviter les écrans surchargés.

L'application doit fonctionner correctement sur :

- desktop
- tablette
- smartphone

---

# Notifications

Les notifications doivent être facultatives.

Chaque utilisateur doit pouvoir :

- activer
- désactiver

les notifications indépendamment.

Prévoir une architecture extensible permettant d'ajouter d'autres canaux de notification ultérieurement.

---

# Futur

Le projet est conçu pour évoluer.

Prévoir dès maintenant des points d'extension pour :

- nouveaux algorithmes
- statistiques
- gamification
- intelligence artificielle
- nouveaux systèmes de notification

Ne jamais implémenter ces fonctionnalités tant qu'elles ne sont pas explicitement demandées.

Mais ne pas bloquer leur intégration future.

---

# En cas de doute

Toujours privilégier :

- la simplicité
- l'explicabilité
- la maintenabilité
- la cohérence métier

Plutôt qu'une solution techniquement complexe.