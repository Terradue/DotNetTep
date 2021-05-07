USE $MAIN$;

/*****************************************************************************/

-- Add wps name in wpsjob...\
ALTER TABLE wpsjob ADD COLUMN `wps_name` VARCHAR(150) NULL DEFAULT NULL;
-- RESULT
