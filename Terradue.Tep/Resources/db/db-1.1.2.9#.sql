USE $MAIN$;

/*****************************************************************************/

-- Adding config...\
ALTER TABLE wpsjob ADD COLUMN `access_key` VARCHAR(50) NULL DEFAULT NULL COMMENT 'Access key';
-- RESULT
