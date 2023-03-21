USE $MAIN$;

/*****************************************************************************/

-- Add config
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('JobMaxDuration', 'int', 'JobMaxDuration', 'JobMaxDuration', 0, '0');
-- RESULT
