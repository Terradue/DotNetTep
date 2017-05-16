USE $MAIN$;

/*****************************************************************************/

-- Add discuss config ... \
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('discussBaseUrl', 'string', 'Discuss base url', 'Discuss base url', 'http://discuss.terradue.com', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('discussApiKey', 'string', 'Discuss api key', 'Discuss api key', 'TO_BE_UPDATED', '0');
-- RESULT

/*****************************************************************************/

-- Update table resource
ALTER TABLE resource 
CHANGE COLUMN `location` `location` TEXT NOT NULL COMMENT 'Resource location, e.g. URI' ;
-- RESULT

-- Update table activity
ALTER TABLE activity 
ADD COLUMN params VARCHAR(200) NULL DEFAULT NULL;
-- RESULT

-- Add roles ... \
UPDATE `role` SET `name`='Content Authority', `description`='Content Authority role' WHERE `identifier`='manager';
INSERT INTO `role` (`identifier`, `name`, `description`) VALUES ('enduser', 'End User', 'Default user role');
INSERT INTO `role` (`identifier`, `name`, `description`) VALUES ('expert', 'Expert User', 'Default expert user role');
INSERT INTO `role` (`identifier`, `name`, `description`) VALUES ('resadmin', 'Resource Administrator', 'Resource Administrator role');
INSERT INTO `role` (`identifier`, `name`, `description`) VALUES ('dataprovider', 'Data Provider', 'Data Provider role');
INSERT INTO `role` (`identifier`, `name`, `description`) VALUES ('ictprovider', 'ICT Provider', 'ICT Provider role');
-- RESULT

-- Add privileges for Content Authority ...\
SET @role_id = (SELECT id FROM role WHERE identifier='manager');
INSERT INTO role_priv (id_role, id_priv) SELECT @role_id, id FROM priv WHERE identifier IN (
	'datapackage-M',
	'datapackage-c',
	'datapackage-d',
	'datapackage-m',
	'datapackage-p',
	'datapackage-s',
	'datapackage-v',
	'service-M',
	'service-s',
	'service-u',
	'service-v',
	'usr-s',
	'wpsjob-c',
	'wpsjob-d',
	'wpsjob-m',
	'wpsjob-p',
	'wpsjob-s',
	'wpsjob-v'
);
-- RESULT

-- Add privileges for End User ...\
SET @role_id = (SELECT id FROM role WHERE identifier='enduser');
INSERT INTO role_priv (id_role, id_priv) SELECT @role_id, id FROM priv WHERE identifier IN (
	'datapackage-p',
	'datapackage-s',
	'datapackage-v',
	'service-s',
	'service-u',
	'service-v',
	'wpsjob-p',
	'wpsjob-s',
	'wpsjob-v'
);
-- RESULT

-- Add privileges for Expert User ...\
SET @role_id = (SELECT id FROM role WHERE identifier='expert');
INSERT INTO role_priv (id_role, id_priv) SELECT @role_id, id FROM priv WHERE identifier IN (
	'datapackage-p',
	'datapackage-s',
	'datapackage-v',
	'service-s',
	'service-u',
	'service-v',
	'wpsjob-p',
	'wpsjob-s',
	'wpsjob-v'
);
-- RESULT

-- Add privileges for Resource Administrator ...\
SET @role_id = (SELECT id FROM role WHERE identifier='resadmin');
INSERT INTO role_priv (id_role, id_priv) SELECT @role_id, id FROM priv WHERE identifier IN (
	'datapackage-p',
	'datapackage-s',
	'datapackage-v',
	'service-s',
	'service-u',
	'service-v',
	'wpsjob-p',
	'wpsjob-s',
	'wpsjob-v'
);
-- RESULT

-- Add privileges for Data Provider ...\
SET @role_id = (SELECT id FROM role WHERE identifier='dataprovider');
INSERT INTO role_priv (id_role, id_priv) SELECT @role_id, id FROM priv WHERE identifier IN (
	'datapackage-p',
	'datapackage-s',
	'datapackage-v',
	'service-s',
	'service-u',
	'service-v',
	'wpsjob-p',
	'wpsjob-s',
	'wpsjob-v'
);
-- RESULT

-- Add privileges for ICT Provider ...\
SET @role_id = (SELECT id FROM role WHERE identifier='ictprovider');
INSERT INTO role_priv (id_role, id_priv) SELECT @role_id, id FROM priv WHERE identifier IN (
	'datapackage-p',
	'datapackage-s',
	'datapackage-v',
	'service-s',
	'service-u',
	'service-v',
	'wpsjob-p',
	'wpsjob-s',
	'wpsjob-v'
);
-- RESULT