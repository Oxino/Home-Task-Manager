# MVP

## Phase 1

Fondations

- Solution Backend
- Solution Frontend
- PostgreSQL
- Docker Compose
- Authentification par sélection de profil (sans email ni mot de passe, ADR-006)

## Phase 2

Gestion des utilisateurs

- CRUD utilisateurs
- Gestion admin

## Phase 3

Modèles de tâches

- CRUD TaskTemplate
- Gestion fréquences
- Gestion rappels

## Phase 4

Instances

- Génération automatique
- Gestion statuts
- Historique

## Phase 5

Moteur d'affectation

- Interface de stratégie
- IntelligentV1
- Explications
- Paramètres d'algorithme administrables (score, multiplicateurs de priorité, fenêtre d'équité,
  pénalité anti-répétition — BR-058)

## Phase 6

Notifications

- Notifications navigateur
- Préférences utilisateur

## Phase 7

Optimisation UX

- Dashboard avec indicateurs de base
- Mobile responsive

> **État : MVP étendu livré, en évolution.** Le frontend couvre désormais le tableau de
> bord, les vues d'administration principales, les notifications, les commentaires avancés
> (threading + réactions) et un écran analytics responsive. Les liens de notification ouvrent
> directement l'instance concernée, y compris pour les rappels.

## Hors MVP

- Statistiques avancées / tendances
- Réaffectation automatique sur indisponibilité (BR-042, R-05)
- Prise en compte des absences dans le moteur (`UserAbsence`, BR-025, R-06)
- Gamification
- IA
- Application mobile
- Synchronisation externe

---

## Périmètre du MVP (validé)

### Gestion des utilisateurs

- CRUD utilisateurs.
- Distinction administrateur / utilisateur via `IsAdmin` (ADR-010).

### Gestion des tâches

- L'administrateur gère complètement toutes les tâches (créer, modifier, supprimer/archiver).
- L'utilisateur peut créer des tâches et modifier uniquement celles dont il est l'auteur
  (BR-037, ADR-007).
- Toute nouvelle tâche créée (par l'admin ou un utilisateur) devient disponible pour les
  **futures** affectations et ne modifie pas l'affectation de la semaine en cours (BR-047).

### Affectation hebdomadaire

- Génération d'une répartition de tâches à la semaine.
- Chaque affectation est liée à une semaine ; une tâche quotidienne obligatoire génère
  7 instances (ADR-008, BR-038).
- Relancer une affectation **écrase** l'affectation existante de la semaine (ADR-013, BR-041).

### Parcours utilisateur

- Se connecter et choisir son profil.
- Voir ses tâches de la semaine.
- Compléter ses tâches (état modifiable durant la semaine, figé à la clôture — ADR-011, BR-043).
- Créer de nouvelles tâches et modifier les siennes.
- Comprendre pourquoi une tâche lui a été affectée (justification, BR-033).

### Parcours administrateur

- Toutes les possibilités d'un utilisateur.
- Interface d'administration pour gérer l'ensemble des données de l'application.
- Relancer une affectation de tâches.

### Interface frontend livrée

- Projet React + TypeScript + Vite dans `src/HomeTaskManager.Web`.
- Sélection de profil sans email ni mot de passe.
- Tableau de bord de la semaine active avec progression, tâches ouvertes/terminées et équilibre
  de charge par participant.
- Modification de la complétude d'une instance depuis le tableau de bord.
- Affichage de la justification d'une affectation.
- Navigation conditionnée par le rôle administrateur.
- Responsive desktop et mobile.
- Vue admin globale des affectations de la semaine.
- Vue admin des instances avec réaffectation et annulation contrôlées.
- Gestion admin des profils actifs et inactifs, avec réactivation.
- Cycle de vie des modèles de tâches avec archivage logique, filtre des modèles actifs ou
  archivés et réactivation réservée à l'administration.
- Configuration des rappels par modèle de tâche.
- Boîte de notifications personnelle : lecture, non-lues/lues et nettoyage des notifications lues.
- Génération automatique des notifications par les rappels (via scheduler périodique).
- Ouverture directe de l'instance depuis une notification d'affectation ou de rappel.
- Service de notification abstrait pour notifications push et reminders.
- Endpoints analytiques pour statistiques d'affectation et complétude (backend).
- Migration EF Core officielle et migration automatique en développement.
- Endpoint de génération d'un jeu de démonstration complet et endpoint de reprise des liens
  d'anciennes notifications.
- Historique paginé par pages de 10 événements, avec intitulé distinct pour l'historique
  personnel et le journal d'audit administrateur.
- Calendrier mensuel des activités disponible dans la page Statistiques, avec chargement différé
  du mois affiché.
- Dashboard distinguant la date d'échéance et la date de complétion des tâches terminées.
- Préférence utilisateur pour le format d'affichage des dates.
- Filtres dashboard par titre, fréquence, échéance et retard, avec action de réinitialisation.
- Ouverture des tâches du dashboard et des entrées d'historique par double-clic.
- Navigation des semaines récentes depuis l'historique, incluant la semaine active.
- Actualisation silencieuse et périodique des notifications lorsqu'une nouveauté est détectée.
- Scénario de démonstration administrateur couvrant les reports, complétions et notifications.

L'envoi Web Push réel (intégration navigateur), les tests d'intégration/end-to-end complets et
la revue sécurité avant exposition réseau restent à implémenter. Les commentaires avancés,
l'interface analytics et les parcours CRUD actuellement exposés sont implémentés.

## Prochaines étapes

1. Renforcer la couverture end-to-end de l'historique personnel et du journal d'audit.
2. Réaliser la revue sécurité avant exposition réseau.
3. Ajouter les tests d'intégration backend (`WebApplicationFactory`).
4. Revenir ensuite sur les tests end-to-end frontend.

Le Web Push réel est volontairement différé après ces étapes. Il reste une fonctionnalité V14,
hors priorité immédiate.

---

## Clarifications de périmètre (analyse de cohérence)

> Ces précisions résolvent des ambiguïtés relevées dans `RISKS_AND_EDGE_CASES.md`.

### Statistiques (R-25)

- **Inclus au MVP** : indicateurs de base des tableaux de bord (BR-029, BR-030) —
  nombre de tâches, complétées, restantes, pourcentage d'avancement.
- **Hors MVP** : statistiques avancées, tendances, historiques analytiques.

### Absences (R-06 / BR-025)

- Reportées **hors V1** : l'entité `UserAbsence` et l'exclusion des absents par le moteur ne font
  pas partie du premier MVP. À prévoir dans une version future.

### Réaffectation automatique sur indisponibilité (R-05 / BR-042)

- Reportée **hors V1** : à prévoir dans une version future.

### Historique

- Aucune purge : conservation intégrale de l'historique (ADR-004, BR-057).

### Entités transverses livrées

- `Week`, `SystemSetting`, `AuditLog`, `TaskComment`, `Reaction`, `Reminder`, `Notification` et
  `UserPreferences`
  (voir `DOMAIN_MODEL.md` et ADR-008 / ADR-009).

### Types de tâches livrés

- **Flexible** (`DailyFlexible`) : une seule instance active à la fois, démarrant le lundi;
  son échéance avance au jour suivant si elle n'est pas réalisée. Chaque réalisation est
  conservée et permet une nouvelle instance pour le prochain jour disponible.
- **Hebdomadaire fluide** (`WeeklyFlexible`) : l'administrateur choisit un ou plusieurs jours
  actifs. Une occurrence indépendante est créée pour chaque jour. Si au moins une occurrence
  est réalisée pendant la semaine, les occurrences restantes sont supprimées à la clôture;
  sinon une seule occurrence est reportée à la semaine suivante.
- Les deux comportements sont expliqués directement dans les formulaires de création et de
  modification des modèles de tâches.
- Les tâches mensuelles permettent de choisir le début, le milieu, la fin du mois ou un jour
  précis, avec adaptation automatique aux mois plus courts.
- Les tâches hebdomadaires permettent de fixer leur échéance au début, au milieu ou à la fin
  de la semaine.
- L'administrateur peut choisir entre `INTELLIGENT_V1`, `RANDOM`, `BALANCED_POINTS` et
  `BALANCED_TIME`, avec une explication du fonctionnement de chaque moteur.

### Idées futures

-  ~~Création d'un tache type 'fluide', sur laquel on est configurer sur quels jours de la semaine elle est active. Contrairement à la tache quotidienne flexible, qui elle est présente un fois dans la semaine mais peut etre répéter tous les jours, c'est a dire que quand elle est terminé une nouvelle instnace de cette tache est automatiquement créé, le principe est que l'utilisateur puisse faire cette tache au moins une fois dans la semaine, mais répétable a volonté. Elle est présente seulement 1 fois dans les taches a faire pour la date courante, si elle est complété, une nouvelle instance de la tache est créé et affectué, à faire pour l'utilisateur. Exemple : semaine 1, le lundi il fait la tache 1 (quotidienne flexible), elle apparait donc compltété le lundi, la tache 1 réapparait a nouveau mais la dead line est pour mardi. Mardi rien, la dead line passe automatiquemment a Mercredi. Mercredi rien sur la tache, la dead line passe automatiquement à jeudi. Jeudi la tache est complété, elle est marqué comme complété dans l'historique, une nouvelle tache 1 est créé avec la dead line pour Vendredi. A la fin de la semaine, cette tache ayant deja été complété au moins une fois dans la semaine, celle-ci est conscidéré comme complété, et donc, l'instances en cours non complété est supprimé. Par contre, si a la fin de la semaine, cette tache n'a jamais été complété, celles ci est reporter atomatiquement sur la semaine suivante.~~
-  ~~Amélioration de l'explication de **"pourquoi cette tache ta été attribué"**~~
-  ~~CHanger le system de navigation temporelle (avoir bt semaine courante, avoir un calendrier pour la navigation, voir les semaines qui ont des affectation)~~
-  ~~Differenter l'historique vs l'audit Log. Historique => seulement les actions de l'utilisateur courant (création d'une tache, complétion d'une tache, ajout d'un commentaire,  ). L'audit log pour l'admin, lui permet de voir toutes les actions, de tous les utilisateurs, y compris les actions non utilisateur (activation ou désactivation de tache, création participant, changement parametre moteur d'affectatuib, réaffectation de tache, génération des affectaiton etc... )~~
-  ~~Ouvrir un tache en double click~~
-  ~~Ajouter une pagination pour l'affichage de l'historique~~
-  ~~Ajouter une pagination sur les notifications~~
-  ~~Ajout de filtre sur le dashboard (filtre date, type de tache, titre, en retard). Affichage viseul des taches dont la date est dépassé~~
-  ~~Supprimer le bouton 'nouvelle tache' du dashboard, le rajouter dans la page de gestion des taches~~
-  ~~optimisation web : pour les paginations, implémenter la pagination coté API, afin d'optimiser le chargement des données coté navigateur.~~
-  ~~Amélioration du système de notification : actualisation silencieuse périodique et rechargement uniquement lorsqu'une nouveauté est détectée.~~
-  Festaure: permettre visuellement d'avoir une représentation de la décision logique d'affectation de chaque tache. Une représentation sous forme d'arbre décisionnel. Je veux voir le plans d'execution logique d'attribution de chaque tache, pourquoi cette tache a été attribué a l'utilisateur. Si c'est psk c'est trié par ordre de point, si elle ne l'a pas faite la semaine passé, si c'est une tache non faite de la semaine passé, etc.. etc.. je qu'on puisse comprendre pourquoi l'affectation a été faite. Je rappel, que le choix du moteur d'affectation est paramettrable. donc il faudra que l'arbre de décision puisse s'adapté au moteur de distribution. lors de l'implémentation, pause moi des questions si cela manque de précision, ou qu'il y a un choix technique ou d'affichage a faire, si tu as une meilleur proposition donne la moi
-  ~~Régles métier: si un tache non fini est réaffecté la nouvelle semaine, et que celle ci n'est toujours pas fin à la fin de la nouvelle semaine, cette tache n'est pas reporté. Elle n'intervient dras plus, et n'interviendra plus non plus dans les calculs de poids et répartission~~
-  reprendre ou approfondir le system de génération d'instance + affectation. 
-  ~~Historique : faire la distinction entre les actions admin et utilsiateur. Pas si c'est un utilisateur avec role admin. Une action utilisateur est : complétion de tache, réouverture de tache, création de tache, modificaiton de tache, ajout de commentaire, ajout/modificaiton rappel ,configuration des préférences. admin : suppression tache, désactivation de tache, réactivation de tache, génération d'instance, lancé une affctation, cloturer la semaine. réaffectation d'instance de tache, annuler un tache, création ou modification de participan, modification du moteur d'affectaion~~ 
- ~~quand on ouvre un formulaire type popup (formulaire création, modification de tache), si on modifie la valeur et qu'on ferme sans sauvegardé, la valeur modifier persiste.~~
- ~~Choix du moteur de répartition de tâche.~~
- déclaration absence pour un participant. (impact la réaffectation des taches)
- Bug formulaire création de tache : le formulaire de création de tache et différent de modificaiton. => Dans la création il manque la partie 'configuration' de l'échéance, pour hebdomadaire, et mensuelle, vérrifie les autres type également.