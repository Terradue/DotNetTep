USE $MAIN$;

/*****************************************************************************/

-- Add wpsjob logs
ALTER TABLE wpsjob ADD COLUMN `logs` TEXT NULL DEFAULT NULL;
-- RESULT