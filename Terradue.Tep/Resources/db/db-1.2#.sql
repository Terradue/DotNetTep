USE $MAIN$;

/*****************************************************************************/

-- up wpsjob table...\
ALTER TABLE wpsjob 
CHANGE COLUMN `name` `name` VARCHAR(120) NOT NULL;
-- RESULT