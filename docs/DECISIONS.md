# Architecture Decision Log

## ADR-001

Décision :

Séparer TaskTemplate et TaskInstance.

Justification :

Permet la conservation de l'historique sans modifier les modèles.

---

## ADR-002

Décision :

Prévoir plusieurs algorithmes d'affectation.

Justification :

L'application évoluera probablement vers plusieurs modes de répartition.

---

## ADR-003

Décision :

Authentification par sélection de profil.

Justification :

Usage familial local.

---

## ADR-004

Décision :

Conservation de la totalité de l'historique, sans purge.

Justification :

Aucune suppression physique ni purge planifiée. L'ensemble des affectations, complétions et
événements est conservé indéfiniment (suppression logique uniquement, BR-016). La fenêtre
d'équité du moteur (BR-053) reste une fenêtre de calcul glissante et n'implique aucune purge.

> Remplace la décision initiale de rétention à 1 an (voir RISKS_AND_EDGE_CASES R-24).

---

## ADR-005

Décision :

Les tâches reportées restent affectées au même utilisateur et ne peuvent être reportées qu'une
seule fois. Une tâche déjà reportée et toujours non terminée est conservée dans l'historique,
mais exclue des futures affectations et des calculs de charge du moteur.

Justification :

Responsabilisation et stabilité de la charge.

---

## ADR-006

Décision :

Authentification par sélection de profil uniquement (usage familial), sans email ni mot de passe.
L'unicité utilisateur porte sur `DisplayName`.

Justification :

Aligne la documentation sur un usage familial local (ADR-003) et sur le modèle de données réel
(`DOMAIN_MODEL.User` sans email). Voir RISKS_AND_EDGE_CASES R-01.

Statut : validé.

---

## ADR-007

Décision :

Les deux profils peuvent créer des tâches :

- l'administrateur peut créer, modifier et supprimer **toutes** les tâches ;
- l'utilisateur peut créer des tâches et modifier **uniquement celles dont il est l'auteur**.

La configuration du moteur d'affectation reste réservée à l'administrateur.

Justification :

Résout la contradiction entre PRODUCT_VISION et README/USER_STORIES. Voir R-02 et BR-037.

Statut : validé.

---

## ADR-008

Décision :

Rattacher explicitement chaque affectation à une semaine ISO-8601 (`Year` + `WeekNumber`),
via une entité `Week`. `AssignmentRun` et `TaskInstance` référencent la semaine concernée.

Justification :

Chaque affectation est liée à une semaine. C'est ce rattachement qui justifie la génération de
7 instances pour une tâche quotidienne obligatoire. Voir R-13 et BR-038.

Statut : validé.

---

## ADR-009

Décision :

Introduire un journal d'audit générique et un historique de complétude bidirectionnel.

Justification :

Les règles d'auditabilité (BR-011, BR-017, BR-033, BR-043) exigent de tracer les changements,
y compris les retours « terminé → non terminé ». Voir R-09 / R-29.

Statut : validé.

---

## ADR-010

Décision :

Le rôle d'administrateur est porté par un booléen `IsAdmin`. L'application ne comporte que deux
rôles (administrateur et utilisateur).

Justification :

Deux rôles seulement : un booléen est suffisant et plus simple qu'une entité `Role`. Voir R-30.

Statut : validé.

---

## ADR-011

Décision :

Verrouillage hebdomadaire de la complétude : durant la semaine active, l'utilisateur modifie
librement l'état (complété / non complété). À la clôture de la semaine, l'état est figé et
devient non modifiable.

Justification :

Reflète le fonctionnement réel : l'état vécu pendant la semaine, enregistré définitivement à la
fin. Voir R-09 et BR-055.

Statut : validé.

---

## ADR-012

Décision :

Une priorité de tâche est introduite (Faible, Normale, Critique ; défaut Normale) et influence le
score global de la tâche : une tâche « Faible » vaut moins de points qu'une tâche « Critique ».

Justification :

Permet de pondérer l'importance des tâches dans le calcul de charge du moteur. Voir R-03 et BR-052.

Statut : validé.

---

## ADR-013

Décision :

Relancer une affectation pour une semaine **écrase** l'affectation existante de cette semaine.

Justification :

Comportement attendu par l'administrateur : réexécuter régénère la répartition de la semaine.
Voir BR-041 (révisée).

Statut : validé.

---

## ADR-014

Décision :

Le moteur d'affectation est paramétrable par l'administrateur. Les paramètres incluent :
le mode de calcul du score, les multiplicateurs de priorité, la fenêtre d'équité historique et
le mécanisme de pénalité anti-répétition.

Justification :

Le calcul de la charge et les règles d'équilibrage doivent pouvoir évoluer sans redéploiement,
et différents « algorithmes » de répartition exposeront leurs propres paramètres. Voir BR-058,
BR-052, BR-053, BR-054, BR-056.

Statut : validé.

---

## ADR-015

Décision :

L'anti-répétition repose sur une **pénalité de points** (favorise la rotation) et non sur une
interdiction stricte.

Justification :

Souplesse : une même personne peut refaire une tâche si nécessaire, tout en étant défavorisée.
Fait partie des paramètres d'algorithme (BR-054, BR-058).

Statut : validé.

---

## ADR-016

Décision :

Départage en cas d'égalité de score : (1) nombre de tâches affectées le plus faible,
(2) tâche la moins réalisée récemment, (3) identifiant stable. Aucun aléatoire.

Justification :

Déterminisme et testabilité du moteur. Voir BR-048.

Statut : validé.

---

## ADR-017

Décision :

Le score V1 intègre la durée estimée :
`CalculatedScore = ManualDifficultyScore × MultiplicateurPriorité × FacteurDurée`.

Justification :

La charge réelle dépend aussi du temps passé, pas seulement de la pénibilité. Voir BR-052.

Statut : validé.

---

## ADR-018

Décision :

Docker n'est pas utilisé pour le développement (indisponible sur le poste de dev). Le
développement se fait en exécution native (`dotnet run` + `npm run dev`). La conteneurisation
Docker Compose reste la cible de **déploiement** (Raspberry Pi), traitée en fin de projet.

Justification :

Lever la dépendance à Docker en développement sans renoncer à la cible de déploiement documentée
(ARCHITECTURE.md §9).

Statut : validé.

---

## ADR-019

Décision :

Base de données **SQLite en développement**, **PostgreSQL en production**. L'accès aux données
passe exclusivement par EF Core ; seuls le provider et la chaîne de connexion changent selon
l'environnement.

Justification :

L'ORM abstrait les spécificités du SGBD ; la bascule vers PostgreSQL aura un impact minime.
Éviter toute installation lourde sur le poste de dev. Rester vigilant sur les différences
(types, `DateOnly`, contraintes) et valider PostgreSQL avant la mise en production.

Statut : validé.