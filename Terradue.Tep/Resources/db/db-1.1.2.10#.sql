USE $MAIN$;

/*****************************************************************************/

-- Adding config...\
ALTER TABLE rate 
CHANGE COLUMN `unit` `unit` BIGINT UNSIGNED NULL DEFAULT NULL COMMENT 'rate unit' ;
ALTER TABLE rate 
CHANGE COLUMN `cost` `cost` DOUBLE UNSIGNED NULL DEFAULT NULL COMMENT 'rate cost' ;
-- RESULT

