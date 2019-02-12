USE $MAIN$;

/*****************************************************************************/

-- Update wpsjob pararm...\
ALTER TABLE wpsjob CHANGE COLUMN `params` `params` TEXT NOT NULL COMMENT 'Wps job parameters' ;
-- RESULT

