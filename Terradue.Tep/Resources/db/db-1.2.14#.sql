USE $MAIN$;

/*****************************************************************************/

-- Update config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('log-hideException', 'bool', 'Hide exception in error log', 'Hide exception in error log', '1', '0');
-- RESULT