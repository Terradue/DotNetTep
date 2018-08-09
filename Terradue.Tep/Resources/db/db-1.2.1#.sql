USE $MAIN$;

/*****************************************************************************/

-- Adding config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('report-activeUsers-threshold', 'int', 'report-activeUsers-threshold', 'report-activeUsers-threshold', '2', '1');
-- RESULT
