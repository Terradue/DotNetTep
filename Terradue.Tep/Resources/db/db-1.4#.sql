USE $MAIN$;

/*****************************************************************************/

-- Add config
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('AccessTokenExpireMinutes', 'int', 'access token max expire time in minutes', 'access token max expire time in minutes', 3, '0');
-- RESULT