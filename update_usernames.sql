-- Voir les employés actuels avec leur hammam
SELECT e.id, e.username, e.prenom, e.hammam_id, h.nom as hammam 
FROM employe e 
JOIN hammam h ON e.hammam_id = h.id 
WHERE e.role != 'Admin'
ORDER BY e.hammam_id, e.created_at;

-- Le username doit être unique globalement, donc on numérote simplement 1, 2, 3...
-- Premier employé (vero - Hammame liberte) = Utilisateur1
-- Deuxième employé (Hamza - hammam casablanca) = Utilisateur2
UPDATE employe SET username = 'Utilisateur1' WHERE id = 'd76986b5-7720-429a-ba3f-c3923fce85d9';
UPDATE employe SET username = 'Utilisateur2' WHERE id = 'ebe0dcc3-1609-4339-b1d6-e23ac6c6f5f7';

-- Vérifier le résultat
SELECT e.id, e.username, e.prenom, e.nom, h.nom as hammam 
FROM employe e 
JOIN hammam h ON e.hammam_id = h.id 
WHERE e.role != 'Admin'
ORDER BY e.username;
