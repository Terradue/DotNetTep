USE $MAIN$;

/*****************************************************************************/

-- Add wpsjob end time...\
ALTER TABLE wpsjob ADD COLUMN `end_time` DATETIME NULL AFTER `created_time`;
-- RESULT
