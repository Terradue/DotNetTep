USE $MAIN$;

/*****************************************************************************/

-- Add config
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('sso_username_field', 'string', 'sso_username_field', 'sso_username_field', 'sub', '0');
-- RESULT
