-- Fix: Rendre employe_id nullable sur la table versement
-- La comptabilité est maintenant par hammam/jour, pas par employé

-- 1. Supprimer l'ancienne contrainte FK si elle existe
DO $$ BEGIN
    ALTER TABLE versement DROP CONSTRAINT IF EXISTS "FK_versement_employe_employe_id";
    ALTER TABLE versement DROP CONSTRAINT IF EXISTS "fk_versement_employe_employe_id";
EXCEPTION WHEN OTHERS THEN NULL;
END $$;

-- 2. Rendre la colonne nullable
ALTER TABLE versement ALTER COLUMN employe_id DROP NOT NULL;

-- 3. Mettre les versements existants à NULL pour employe_id
UPDATE versement SET employe_id = NULL WHERE employe_id = '00000000-0000-0000-0000-000000000001';

-- 4. Supprimer l'ancien index unique s'il existe encore
DROP INDEX IF EXISTS "IX_versement_employe_id_date_versement";

-- 5. S'assurer que l'index unique (hammam_id, date_versement) existe
DROP INDEX IF EXISTS "IX_versement_hammam_id_date_versement";
CREATE UNIQUE INDEX "IX_versement_hammam_id_date_versement" ON versement (hammam_id, date_versement);

-- 6. Re-créer la FK en mode SET NULL
ALTER TABLE versement ADD CONSTRAINT "FK_versement_employe_employe_id"
    FOREIGN KEY (employe_id) REFERENCES employe(id) ON DELETE SET NULL;
