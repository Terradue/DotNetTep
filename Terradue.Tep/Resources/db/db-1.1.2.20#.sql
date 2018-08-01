USE $MAIN$;

/*****************************************************************************/

-- Add app cache index...\
ALTER TABLE app_cache ADD COLUMN `cat_index` VARCHAR(50) NULL AFTER `uid`;
-- RESULT
