USE $MAIN$;

/*****************************************************************************/

-- Add discuss config ... \
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('discussBaseUrl', 'string', 'Discuss base url', 'Discuss base url', 'http://discuss.terradue.com', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('discussApiKey', 'string', 'Discuss api key', 'Discuss api key', '23cf44d35592b7574f651b92fe1edc920c9fd974475cdf2deca793940f703668', '0');
-- RESULT

/*****************************************************************************/

-- Update table resource
ALTER TABLE resource 
CHANGE COLUMN `location` `location` TEXT NOT NULL COMMENT 'Resource location, e.g. URI' ;
-- RESULT