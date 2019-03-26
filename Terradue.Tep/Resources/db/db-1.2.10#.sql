USE $MAIN$;

/*****************************************************************************/

-- Add wps config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('catalog-communityUsername', 'string', 'catalog public community username', '', '', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('catalog-communityApikey', 'string', 'catalog public community apikey', '', '', '0');
-- RESULT
