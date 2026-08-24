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
- /docs/USE_CASES.md
- /docs/BUSINESS_RULES.md
- /docs/ASSIGNMENT_ENGINE.md
- /docs/RISKS_AND_EDGE_CASES.md
- /docs/DECISIONS.md
- /docs/BACKLOG.md

Ces documents sont la source de vérité du projet.

Le code doit toujours être aligné avec la documentation.

En cas de conflit entre documents, l'ordre de priorité est :

1. DECISIONS.md (ADR validés)
2. BUSINESS_RULES.md
3. DOMAIN_MODEL.md / ARCHITECTURE.md
4. PRODUCT_VISION.md / README.md

---

# Décisions structurantes en vigueur

Ces décisions (voir DECISIONS.md) sont actées et doivent toujours être respectées :

- **Authentification par sélection de profil** uniquement, sans email ni mot de passe
  (ADR-003, ADR-006). L'unicité utilisateur porte sur `DisplayName`.
- **Deux rôles seulement**, portés par le booléen `IsAdmin` (ADR-010).
- **Droits sur les tâches** : l'administrateur gère toutes les tâches ; l'utilisateur crée des
  tâches et ne modifie que celles dont il est l'auteur (ADR-007, BR-037).
- **Séparation TaskTemplate / TaskInstance** (ADR-001). Modifier un modèle n'altère pas les
  instances déjà générées (BR-047).
- **Chaque affectation est rattachée à une semaine ISO-8601** via l'entité `Week`
  (ADR-008, BR-038). Une tâche quotidienne obligatoire génère 7 instances.
- **Relancer une affectation écrase** l'affectation existante de la semaine (ADR-013, BR-041).
- **Complétude** : librement modifiable durant la semaine active, **figée à la clôture**
  (ADR-011, BR-043, BR-055).
- **Priorité de tâche** (Faible / Normale / Critique, défaut Normale) influençant le score
  (ADR-012, BR-052, BR-056).
- **Aucune purge** : l'historique est conservé intégralement (ADR-004, BR-057).
- **Moteur paramétrable** par l'administrateur (score, priorités, fenêtre d'équité, pénalité)
  (ADR-014, BR-058).

---

# Hors périmètre V1 (ne pas implémenter sans demande explicite)

- Prise en compte des absences dans le moteur (`UserAbsence`, BR-025).
- Réaffectation automatique sur indisponibilité (BR-042).
- Statistiques avancées et tendances.
- Gamification, IA, application mobile native, canaux de notification additionnels.

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
- mettre à jour BUSINESS_RULES.md si une règle est concernée
- mettre à jour ARCHITECTURE.md si la structure technique évolue
- mettre à jour PRODUCT_VISION.md si nécessaire

La documentation doit évoluer avec le projet et rester cohérente entre tous les fichiers.

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

## Calcul du score (V1)

```text
Score    = ManualDifficultyScore × MultiplicateurPriorité × (Durée / DuréeRéférence)
Pénalité = OccurrencesRécentes × PoidsPénalité × Score
```

La cible de points d'un participant est proportionnelle à sa disponibilité
(`AvailabilityPercentage`, BR-040). Le départage en cas d'égalité est déterministe (BR-048).

Les paramètres suivants sont administrables (SystemSetting, BR-058) et ne doivent JAMAIS être
codés en dur — valeurs par défaut :

- multiplicateurs de priorité : 0.5 / 1.0 / 1.5
- durée de référence : 30 min
- fenêtre d'équité : 8 semaines
- poids de la pénalité anti-répétition : 0.25

---

# Historique

L'historique est critique.

Ne jamais :

- supprimer l'historique
- purger des données métier
- modifier rétroactivement des affectations validées
- modifier la complétude d'une semaine clôturée (BR-055)

L'historique est utilisé pour :

- les statistiques futures
- les calculs d'équité
- les explications d'affectation
- l'auditabilité (AuditLog, CompletionChangeLog)

Toute suppression doit être **logique** uniquement (BR-016).

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