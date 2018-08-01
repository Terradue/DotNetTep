USE $MAIN$;

/*****************************************************************************/

-- Add accounting config ... \
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('t2-accounting-baseurl', 'string', 'Terradue accounting base url', 'Terradue accounting base url', 'https://accounting.terradue.com', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('enableAccounting', 'bool', 'Enable accounting', 'Enable accounting', 'true', '0');
-- RESULT
