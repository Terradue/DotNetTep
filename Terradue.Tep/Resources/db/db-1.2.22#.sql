USE $MAIN$;

/*****************************************************************************/

-- Update config
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('cookieID-token-id', 'string', 'cookieID-token-id value', 'cookieID-token-id value', 'oauthtoken_id', '0');
-- RESULT