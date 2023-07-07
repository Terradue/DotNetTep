USE $MAIN$;

/*****************************************************************************/

-- Add config
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('T2-sso-auth-identifier', 'string', 'T2-sso-auth-identifier', 'T2-sso-auth-identifier', 'ldap', '0');
-- RESULT
