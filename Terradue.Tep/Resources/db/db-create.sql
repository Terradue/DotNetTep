-- VERSION 1.0.34

USE $MAIN$;

/*****************************************************************************/

-- Add Api key on usr
ALTER TABLE usr
ADD COLUMN apikey varchar(45) NULL default NULL COMMENT 'User api key';
-- RESULT

-- Add log path in config
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('log-path', 'string', 'Log path', 'Log path', '/usr/local/tep/webserver/sites/tep/log', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('catalog-baseurl', 'string', 'Catalog baseurl', 'Catalog baseurl', 'https://catalog.terradue.com', '0');
-- RESULT

/*****************************************************************************/
