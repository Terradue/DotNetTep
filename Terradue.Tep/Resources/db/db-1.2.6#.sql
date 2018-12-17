USE $MAIN$;

/*****************************************************************************/

-- Add wps version in wpsjob...\
ALTER TABLE wpsjob ADD COLUMN `wps_version` VARCHAR(30) NULL DEFAULT NULL;
-- RESULT
