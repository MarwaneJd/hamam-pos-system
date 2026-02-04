-- Corriger le nom arabe avec le bon encodage
UPDATE hammam SET "NomArabe" = 'حمام الحرية' WHERE nom = 'Hammame liberte';
SELECT nom, "NomArabe" FROM hammam;
