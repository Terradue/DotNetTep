USE $MAIN$;

/*****************************************************************************/

-- Add community default role ... \
ALTER TABLE domain ADD COLUMN id_role_default INT(10) NOT NULL DEFAULT 0;
-- RESULT

