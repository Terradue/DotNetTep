USE $MAIN$;

/*****************************************************************************/

-- Add usrsession end time...\
ALTER TABLE usrsession ADD COLUMN `log_end` DATETIME NULL AFTER `log_time`;
-- RESULT
