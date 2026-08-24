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

Conservation de l'historique pendant 1 an.

Justification :

Permettre les calculs d'équilibrage.

---

## ADR-005

Décision :

Les tâches reportées restent affectées au même utilisateur.

Justification :

Responsabilisation et stabilité de la charge.