USE $MAIN$;

/*****************************************************************************/

-- Add config
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('AccessTokenMaxExpireSeconds', 'int', 'AccessTokenMaxExpireSeconds', 'AccessTokenMaxExpireSeconds', 0, '0');
-- RESULT

ALTER TABLE wpsjob ADD COLUMN `status_urls` TEXT NOT NULL;