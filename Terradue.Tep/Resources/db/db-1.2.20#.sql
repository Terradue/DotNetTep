USE $MAIN$;

/*****************************************************************************/

-- Update config
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('wps3input-format', 'string', 'wps3 input fixed format value', 'wps3 input fixed format value', 'atom', '0');
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('wps3input-downloadorigin', 'string', 'wps3 input fixed download origin value', 'wps3 input fixed download origin value', '[terradue]', '0');
-- RESULT