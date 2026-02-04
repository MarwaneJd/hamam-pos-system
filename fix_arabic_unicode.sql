-- Supprimer l'ancienne valeur et mettre la bonne directement en bytes
UPDATE hammam SET "NomArabe" = E'\u062D\u0645\u0627\u0645 \u0627\u0644\u062D\u0631\u064A\u0629' WHERE nom = 'Hammame liberte';
SELECT nom, "NomArabe" FROM hammam;
