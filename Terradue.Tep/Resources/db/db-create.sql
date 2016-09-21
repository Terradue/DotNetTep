-- VERSION 0.1

USE $MAIN$;

/*****************************************************************************/

-- Add Api key on usr
ALTER TABLE usr
ADD COLUMN apikey varchar(45) NULL default NULL COMMENT 'User api key';
-- RESULT

/*****************************************************************************/
