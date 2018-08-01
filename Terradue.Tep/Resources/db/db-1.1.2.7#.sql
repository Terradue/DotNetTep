
USE $MAIN$;

/*****************************************************************************/

-- Adding config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('accounting-enabled', 'bool', 'accounting enabled or not', 'accounting enabled or not', 'true', '1');
-- RESULT
