# RISKS_AND_EDGE_CASES

## Objectif

Ce document recense :

- les **incohérences** entre documents ;
- les **ambiguïtés** à trancher ;
- les **cas limites** oubliés ;
- les **entités manquantes** ;
- les **règles métier incomplètes** ;
- les **risques** te## Reste à formaliser (moteur INTELLIGENT_V1)

Tous les points structurants du moteur sont désormais tranchés :

- ✅ Sémantique de la disponibilité (R-08, BR-040).
- ✅ Départage déterministe en cas d'égalité (R-15, BR-048).
- ✅ Formule du score avec durée + priorité paramétrable (R-31, BR-052).
- ✅ Fenêtre d'équité paramétrable, défaut 8 semaines (R-32, BR-053).
- ✅ Anti-répétition par pénalité paramétrable (R-33, BR-054).
- ✅ Paramètres d'algorithme administrables (BR-058).

Reste à produire : le détail des formules exactes (facteur de durée, intensité de pénalité) dans
la documentation dédiée du moteur (`ASSIGNMENT_ENGINE.md`), au moment de l'implémentation.

**Mise à jour** : le moteur est désormais entièrement chiffré (facteur de durée = durée / 30 min,
poids de pénalité = 0,25, toutes valeurs paramétrables). Voir `ASSIGNMENT_ENGINE.md` et BR-052,
BR-054, BR-058. Aucun point du moteur n'est plus en suspens pour la V1.ues et fonctionnels.

Chaque entrée propose une recommandation. Les décisions retenues doivent être
consignées dans `DECISIONS.md` puis répercutées dans les documents concernés.

Légende de sévérité :

- 🔴 Bloquant : doit être tranché avant le développement du domaine.
- 🟠 Majeur : impacte le modèle ou le moteur, à trancher tôt.
- 🟡 Mineur : clarification souhaitable.

---

# 1. Incohérences entre documents

## R-01 🔴 ✅ RÉSOLU — Modèle d'authentification contradictoire

- **Constat** : `PRODUCT_VISION.md` exige email, rôles, connexion et déconnexion sécurisées ;
  `BR-002`/`BR-004` imposent un email unique et un login ; `ADR-003` retient une simple
  « authentification par sélection de profil » ; `DOMAIN_MODEL.User` ne contient **ni email
  ni mot de passe** (seulement `DisplayName`, `IsAdmin`).
- **Décision (ADR-006)** : authentification par **sélection de profil uniquement**, sans email
  ni mot de passe. L'unicité porte sur `DisplayName` (BR-002 requalifié).

## R-02 🔴 ✅ RÉSOLU — Qui peut créer / modifier une tâche ?

- **Constat** : `PRODUCT_VISION` réserve la création de tâches à l'administrateur ;
  `README` et `USER_STORIES` l'accordent à l'utilisateur.
- **Décision (ADR-007, BR-037)** : les deux profils créent des tâches ; l'utilisateur ne modifie
  que celles dont il est l'auteur ; l'admin gère toutes les tâches (créer, modifier, supprimer).

## R-03 🟠 ✅ RÉSOLU — Priorité vs Pénibilité/Difficulté

- **Constat** : `BR-026` et `PRODUCT_VISION` définissent une **priorité** ; `README` parle de
  **pénibilité** ; `DOMAIN_MODEL` n'a qu'un `ManualDifficultyScore` (1-100) et **aucune** priorité.
- **Décision (ADR-012, BR-026/BR-052/BR-056)** : ajout d'un champ `Priority` à 3 valeurs
  (Faible, Normale [défaut], Critique) qui pondère le score global de la tâche, en complément du
  `ManualDifficultyScore` (pénibilité). Une tâche `Faible` vaut moins de points qu'une `Critique`.

## R-04 🟡 — Modification d'un modèle déjà instancié

- **Constat** : rien n'indique l'effet d'une modification de modèle sur les instances existantes.
- **Recommandation** : les instances existantes ne sont **pas** rétro-modifiées (cohérent avec
  ADR-001 séparation Template/Instance). Documenter explicitement. → BR-047.

## R-13 🔴 ✅ RÉSOLU — `AssignmentRun` sans rattachement à une semaine

- **Constat** : `AssignmentRun` ne possède ni année ni numéro de semaine ; impossible de savoir
  quelle période une exécution couvre, ce qui contredit `BR-007`.
- **Décision (ADR-008, BR-038)** : introduction d'une entité `Week` (ISO-8601) ; `AssignmentRun`
  et `TaskInstance` référencent la semaine. C'est ce lien qui justifie les 7 instances d'une tâche
  quotidienne obligatoire.

## R-24 🟡 ✅ RÉSOLU — Rétention 1 an vs conservation « complète »

- **Constat** : `ADR-004` fixait la rétention à 1 an ; `PRODUCT_VISION` promet une conservation
  « dans le temps » et une suppression uniquement logique (`BR-016`).
- **Décision (ADR-004 révisé, BR-057)** : aucune purge. L'intégralité de l'historique est conservée.
  La fenêtre d'équité (BR-053) reste une fenêtre de calcul glissante, sans purge.

## R-25 🟡 ✅ RÉSOLU — Statistiques : MVP ou hors MVP ?

- **Constat** : `BACKLOG` classe les statistiques « Hors MVP » ; `PRODUCT_VISION`, `BR-029/030`
  et les critères de succès les considèrent comme essentielles.
- **Décision (BACKLOG)** : indicateurs de base des dashboards inclus au MVP ; statistiques
  avancées / tendances hors MVP.

## R-26 🟡 — Fréquences : 4 vs 5 valeurs

- **Constat** : `BR-027` liste 4 fréquences (sans distinction quotidienne obligatoire/flexible) ;
  `DOMAIN_MODEL.FrequencyType` en liste 5.
- **Recommandation** : aligner `BR-027` sur l'enum du domaine (référence). → BR-027 mis à jour.

---

# 2. Ambiguïtés à trancher

## R-07 🟠 — « Une affectation par tâche/semaine » vs quotidien obligatoire

- **Constat** : `BR-008` interdit plus d'une affectation d'une tâche à un utilisateur pour une
  semaine, mais `DailyMandatory` génère 7 instances.
- **Recommandation** : requalifier BR-008 : l'unicité porte sur `(Template, User, Semaine, Jour
  planifié)` et non sur `(Template, User, Semaine)`. → BR-008 clarifié.

## R-08 🟠 ✅ RÉSOLU — Sémantique de `AvailabilityPercentage`

- **Constat** : la manière dont la disponibilité pondère la charge n'était pas définie.
- **Décision (BR-040)** : la cible de points d'un participant est **proportionnelle** à
  `AvailabilityPercentage` (50 % → moitié des points d'un participant à 100 %). Un participant à
  0 % est exclu de la répartition.

## R-15 🟡 ✅ RÉSOLU — Départage en cas d'égalité de scores

- **Constat** : le moteur ne précisait pas comment départager deux participants à score égal.
- **Décision (BR-048, ADR-016)** : (1) nombre de tâches affectées le plus faible, (2) tâche la
  moins réalisée récemment, (3) identifiant stable. Aucun aléatoire.

## R-10 🟡 — Qui déclare une absence ?

- **Recommandation** : autoriser le participant à déclarer sa propre absence et l'admin celle
  de tous. → BR-036.

## R-11 🟡 — `IsActive` vs « archivé » pour les tâches

- **Constat** : la vision distingue actif / inactif / archivé ; le modèle n'a qu'un booléen.
- **Recommandation** : introduire un état de cycle de vie (`Active`, `Inactive`, `Archived`)
  sur `TaskTemplate`. → BR-014/BR-015 clarifiés.

## R-12 🟡 — Placement des tâches mensuelles

- **Constat** : la semaine du mois où placer une instance `Monthly` n'est pas définie.
- **Recommandation** : règle par défaut (1re semaine du mois) configurable. → BR-049.

---

# 3. Cas limites oubliés

## R-05 🔴 ⏭ REPORTÉ (hors V1) — Propriétaire d'une tâche reportée devenu inactif/absent

- **Constat** : `ADR-005` impose qu'une tâche reportée reste sur son propriétaire, mais rien
  n'est prévu si ce propriétaire est désactivé ou absent.
- **Décision** : réaffectation automatique reportée à une version future (BR-042 hors V1, BACKLOG).

## R-14 🟠 — Aucun participant actif disponible lors d'une affectation

- **Recommandation** : ne rien affecter, journaliser l'exécution comme « sans affectation » et
  alerter l'administrateur. → BR-050.

## R-16 🟠 ✅ RÉSOLU — Relance d'affectation après complétions déjà déclarées

- **Constat** : réexécuter le moteur pourrait écraser des instances déjà terminées.
- **Décision (ADR-013, BR-041)** : relancer une affectation **écrase** l'affectation existante de
  la semaine ; l'historique de l'exécution précédente reste conservé (aucune purge).

## R-17 🟡 — Exécution concurrente d'une tâche flexible par plusieurs participants

- **Recommandation** : autoriser plusieurs `DailyFlexibleExecution` par participants distincts ;
  la complétude hebdomadaire est atteinte dès la 1re exécution. → BR-044.

## R-19 🟡 — Report d'une instance annulée

- **Recommandation** : une instance `Cancelled` n'est jamais reportée. → BR-019 (report).

## R-20 🟡 — Consultation au-delà de la rétention

- **Recommandation** : afficher les périodes conservées ; message explicite au-delà. → cf. R-24.

## R-27 🟡 — Changement d'année ISO / semaine 53

- **Constat** : la gestion des semaines ISO (années à 53 semaines, bascule d'année) n'est pas traitée.
- **Recommandation** : utiliser strictement la norme ISO-8601 pour `(Year, WeekNumber)`. → BR-038.

## R-28 🟡 — Fuseaux horaires et bornes de journée/semaine

- **Constat** : les déclencheurs « fin de journée / fin de semaine » (rappels, reports) dépendent
  d'un fuseau horaire non défini.
- **Recommandation** : définir un fuseau horaire applicatif configurable (par défaut celui du foyer). → BR-051.

---

# 4. Entités manquantes

## R-23 🟠 — Entité `Week` / `Period`

- Nécessaire pour rattacher `AssignmentRun` et `TaskInstance` à une semaine ISO (voir R-13).

## R-22 🟠 — Entité `SystemSetting` / `Parameter`

- Exigée par `BR-031` mais absente du `DOMAIN_MODEL`.

## R-29 🟠 — Entité d'historique / `AuditLog`

- `BR-011`, `BR-017`, `BR-033` imposent une traçabilité complète, mais aucune entité générique
  d'audit n'est modélisée. `TaskCompletion` ne couvre que les complétions.

## R-09 🟠 ✅ RÉSOLU — Historique de complétude bidirectionnel

- **Constat** : le modèle ne permet pas d'enregistrer les « dé-complétions » (retour à non terminé),
  pourtant décrites par `BR-010`/`BR-012`.
- **Décision (ADR-011, BR-043, BR-055)** : durant la semaine active, l'état est librement
  modifiable ; à la clôture de la semaine, il est figé et verrouillé. Un journal d'audit (ADR-009)
  conserve les changements.

## R-18 🟡 — Commentaires de tâche

- `BR-013`/`PRODUCT_VISION` prévoient des commentaires, absents du modèle. Ajouter `TaskComment`
  ou un champ dédié sur `TaskInstance`.

## R-21 🟡 — `UserPreferences` incomplet

- La table `UserPreferences` du `DOMAIN_MODEL` est tronquée / non définie. À compléter (au moins
  activation des notifications).

## R-30 🟡 ✅ RÉSOLU — `Role` en tant que concept

- **Constat** : `BR-018` parle de rôles alors que le domaine n'a qu'un booléen `IsAdmin`.
- **Décision (ADR-010)** : l'application ne comporte que deux rôles (administrateur / utilisateur).
  Le booléen `IsAdmin` est retenu ; pas d'entité `Role` dédiée.

---

# 5. Règles métier incomplètes

## R-31 🟠 ✅ RÉSOLU — Calcul du `CalculatedScore`

- **Constat** : `TaskScore.CalculatedScore` doit être « recalculable » mais aucune formule n'était décrite.
- **Décision (BR-052, ADR-017, ADR-014)** :
  `CalculatedScore = ManualDifficultyScore × MultiplicateurPriorité × FacteurDurée`.
  Multiplicateurs par défaut : Faible 0.5 / Normale 1 / Critique 1.5. Mode de calcul et
  multiplicateurs paramétrables par l'administrateur (BR-058).

## R-32 🟡 ✅ RÉSOLU — Pondération historique / fenêtre glissante

- **Constat** : « 8 dernières semaines » évoqué en exemple, sans règle formelle.
- **Décision (BR-053, BR-058)** : fenêtre d'équité de 8 semaines par défaut, paramétrable par
  l'administrateur.

## R-33 🟡 ✅ RÉSOLU — Anti-répétition : définition formelle

- **Constat** : « éviter qu'une même tâche soit toujours attribuée à la même personne » non quantifié.
- **Décision (BR-054, ADR-015)** : pénalité de points (pas d'interdiction stricte), paramétrable
  et extensible via les stratégies d'algorithme.

---

# 6. Risques transverses

| Id | Risque | Sévérité | Mitigation |
|----|--------|----------|------------|
| RT-01 | Divergence doc ↔ code sur l'authentification | 🔴 | Trancher R-01 avant tout dev domaine |
| RT-02 | Moteur non testable faute de règles formelles | 🟠 | Formaliser R-08, R-15, R-31→R-33 |
| RT-03 | Perte d'auditabilité (BR-033) faute d'entité d'audit | 🟠 | Traiter R-29/R-09 |
| RT-04 | Incohérence semaine ISO / génération | 🟠 | Traiter R-13/R-27 |
| RT-05 | Dérive de périmètre MVP (stats, absences) | 🟡 | Clarifier BACKLOG (R-25, R-06) |
| RT-06 | Dépendance forte au fuseau horaire | 🟡 | Définir BR-051 |

---

# 7. Synthèse des actions prioritaires

## Points tranchés (voir DECISIONS.md)

1. ✅ Modèle d'authentification : sélection de profil sans email (R-01, ADR-006).
2. ✅ Droits de création de tâche : admin = toutes, utilisateur = les siennes (R-02, ADR-007).
3. ✅ Rattachement des affectations à une semaine ISO (R-13, ADR-008).
4. ✅ Priorité de tâche influençant le score (R-03, ADR-012).
5. ✅ Rôle via booléen `IsAdmin`, deux rôles (R-30, ADR-010).
6. ✅ Verrouillage hebdomadaire de la complétude (R-09, ADR-011).
7. ✅ Relance d'affectation = écrasement (R-16, ADR-013).
8. ✅ Conservation intégrale de l'historique, sans purge (R-24, ADR-004 révisé).

## Reporté hors V1 (voir BACKLOG.md)

- ⏭ Réaffectation automatique sur indisponibilité (R-05, BR-042).
- ⏭ Prise en compte des absences par le moteur (R-06, BR-025).

## Reste à formaliser (moteur INTELLIGENT_V1)

- 🟠 Sémantique de la disponibilité (R-08, BR-040).
- 🟡 Départage déterministe en cas d'égalité (R-15, BR-048).
- � Formule exacte du score, fenêtre d'équité, anti-répétition (R-31→R-33, BR-052/053/054).
