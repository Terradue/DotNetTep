-- VERSION 0.1

USE $MAIN$;

/*****************************************************************************/

-- Add Api key on usr
ALTER TABLE usr
ADD COLUMN apikey varchar(45) NULL default NULL COMMENT 'User api key';
-- RESULT

-- Add log path in config
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('log-path', 'string', 'Log path', 'Log path', '/usr/local/tep/webserver/sites/tep/log', '0');
-- RESULT

/*****************************************************************************/
