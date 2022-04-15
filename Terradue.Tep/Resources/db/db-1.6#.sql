USE $MAIN$;

/*****************************************************************************/

-- Add config
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('ellip.editor-url', 'string', 'ellip.editor-url', 'ellip.editor-url', "https://ellip.terradue.com/#infohubs/geobrowser-apps-editor/id", '0');
-- RESULT