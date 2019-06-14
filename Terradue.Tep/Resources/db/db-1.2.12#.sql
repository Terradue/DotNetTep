USE $MAIN$;

/*****************************************************************************/

-- Add wpsjob app identifier
ALTER TABLE wpsjob ADD COLUMN `app_identifier` VARCHAR(50) NULL DEFAULT NULL;
-- RESULT
