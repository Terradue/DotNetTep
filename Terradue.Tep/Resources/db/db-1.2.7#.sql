USE $MAIN$;

/*****************************************************************************/

-- Add analytics config...\
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('analytics_nbtopusedservices', 'int', 'nb of top services to show in analytics', '', '5', '0');
-- RESULT

-- Add searchable in app_cache...\
ALTER TABLE app_cache ADD COLUMN `searchable` TEXT NULL DEFAULT NULL;
-- RESULT
