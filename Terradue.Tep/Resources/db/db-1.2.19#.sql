USE $MAIN$;

/*****************************************************************************/

-- Adding domain for wpsjob table ... \
ALTER TABLE wpsjob ADD COLUMN archive_status int NOT NULL DEFAULT 0 COMMENT 'Wps job archive status';
-- RESULT

-- Update config
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('wpsjob-archive-enabled', 'bool', 'Wpsjob archive is enabled or not (if set to yes, job are not deleted but set as to be archived)', 'Wpsjob archive is enabled or not (if set to yes, job are not deleted but set as to be archived)', 'true', '0');
-- RESULT