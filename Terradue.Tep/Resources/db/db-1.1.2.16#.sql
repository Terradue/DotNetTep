USE $MAIN$;

/*****************************************************************************/

-- Adding config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('action-maxDaysJobRefresh', 'int', 'Number of days before a job nb results is set to 0 in case of errors', 'Number of days before a job nb results is set to 0 in case of errors', '30', '0');
-- RESULT
