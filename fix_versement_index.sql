-- Fix: change unique index from (employe_id, date_versement) to (hammam_id, date_versement)
-- because versements are now per hammam per day, not per employee per day
DROP INDEX IF EXISTS "IX_versement_employe_id_date_versement";
-- The new index was already created, just ensure it exists:
CREATE UNIQUE INDEX IF NOT EXISTS "IX_versement_hammam_id_date_versement" ON versement (hammam_id, date_versement);
