-- VERSION 1.1

USE $MAIN$;

-- Add WpsJob table... \
CREATE TABLE wpsjob (
    id int unsigned NOT NULL auto_increment,
    id_domain int unsigned COMMENT 'FK: Owning domain',
    id_usr int unsigned NOT NULL COMMENT 'FK: User',
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    remote_identifier varchar(50) NULL DEFAULT NULL COMMENT 'Unique remote identifier',
    name varchar(100) NOT NULL COMMENT 'WPS Job name',
    wps varchar(100) NOT NULL COMMENT 'FK: WPS Service identifier',
    process varchar(100) NOT NULL COMMENT 'Process name',
    params varchar(1000) NOT NULL COMMENT 'Wps job parameters',
    status_url varchar(200) NOT NULL COMMENT 'Wps job status url',
    created_time datetime NOT NULL COMMENT 'Wps created date',
    CONSTRAINT pk_wpsjob PRIMARY KEY (id),
    CONSTRAINT fk_wpsjob_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'Wps jobs';
CALL add_type($ID$, 'Terradue.Tep.WpsJob, Terradue.Tep', NULL, 'Wps Job', 'Wps Job', 'job/wps');
SET @type_id = (SELECT LAST_INSERT_ID());
-- RESULT

-- Adding priv for Wps Job ... \
SET @priv_pos = (SELECT MAX(pos) FROM priv);
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'wpsjob-v', 'v', @priv_pos + 1, 'WpsJob: view', 1),
    (@type_id, 'wpsjob-c', 'c', @priv_pos + 2, 'WpsJob: create', 1),
    (@type_id, 'wpsjob-s', 's', @priv_pos + 3, 'WpsJob: search', 1),
    (@type_id, 'wpsjob-m', 'm', @priv_pos + 4, 'WpsJob: change', 1),
    (@type_id, 'wpsjob-d', 'd', @priv_pos + 5, 'WpsJob: delete', 1),
    (@type_id, 'wpsjob-p', 'p', @priv_pos + 6, 'WpsJob: make public', 1);
-- RESULT

-- Add Privilege table for wpsjob... \
CREATE TABLE wpsjob_priv (
    id_wpsjob int unsigned NOT NULL COMMENT 'FK: wpsjob set',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_wpsjob_priv_wpsjob FOREIGN KEY (id_wpsjob) REFERENCES wpsjob(id) ON DELETE CASCADE,
    CONSTRAINT fk_wpsjob_priv_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_wpsjob_priv_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group privileges on wpsjob';
-- RESULT

/*****************************************************************************/

-- Update custom User type... \
UPDATE type SET custom_class = 'Terradue.Tep.UserTep, Terradue.Tep' WHERE class = 'Terradue.Portal.User, Terradue.Portal';
-- RESULT

/*****************************************************************************/

-- Adding extended entity types for dataseries... \
CALL add_type($ID$, 'Terradue.Tep.DataSeries, Terradue.Tep', 'Terradue.Portal.Series, Terradue.Portal', 'tepHydro data series', 'tepHydro data series', NULL);
-- RESULT

-- Adding entity base type for data packages ... \
CALL add_type($ID$, 'Terradue.Tep.DataPackage, Terradue.Tep', NULL, 'Data Package', 'Data Packages', 'data/package');
SET @type_id = (SELECT LAST_INSERT_ID());
-- RESULT

-- Adding privileges for data packages ... \
SET @priv_pos = (SELECT MAX(pos) FROM priv);
INSERT INTO priv (id_type, identifier, operation, pos, name) VALUES
    (@type_id, 'datapackage-v', 'v', @priv_pos + 1, 'DataPackage: view'),
    (@type_id, 'datapackage-c', 'c', @priv_pos + 2, 'DataPackage: create'),
    (@type_id, 'datapackage-s', 's', @priv_pos + 3, 'DataPackage: search'),
    (@type_id, 'datapackage-m', 'm', @priv_pos + 4, 'DataPackage: change'),
    (@type_id, 'datapackage-M', 'M', @priv_pos + 5, 'DataPackage: control'),
    (@type_id, 'datapackage-d', 'd', @priv_pos + 6, 'DataPackage: delete');
-- RESULT

-- Adding extended entity types for ThematicApps... \
CALL add_type(NULL, 'Terradue.Tep.ThematicApplication, Terradue.Tep', 'Terradue.Tep.DataPackage, Terradue.Tep', 'Thematic Apps', 'Thematic Apps', 'apps');
-- RESULT

/*****************************************************************************/

-- Update priv for User ... \
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Portal.User, Terradue.Portal');
SET @priv_pos = (SELECT MAX(pos) FROM priv);
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'user-login', 'l', @priv_pos + 1, 'User: Login', 1);
-- RESULT

/*****************************************************************************/

-- Add privilege scores... \
INSERT INTO priv_score (id_priv, score_usr, score_owner) SELECT id, 1, 1 FROM priv WHERE name = 'DataPackage: view';
INSERT INTO priv_score (id_priv, score_usr, score_owner) SELECT id, 1, 1 FROM priv WHERE name = 'DataPackage: search';
INSERT INTO priv_score (id_priv, score_usr, score_owner) SELECT id, 0, 2 FROM priv WHERE name = 'DataPackage: create';
INSERT INTO priv_score (id_priv, score_usr, score_owner) SELECT id, 0, 1 FROM priv WHERE name = 'DataPackage: change';
INSERT INTO priv_score (id_priv, score_usr, score_owner) SELECT id, 0, 1 FROM priv WHERE name = 'DataPackage: delete';
INSERT INTO priv_score (id_priv, score_usr, score_owner) SELECT id, 0, 1 FROM priv WHERE name = 'DataPackage: make public';
INSERT INTO priv_score (id_priv, score_usr, score_owner) SELECT id, 1, 1 FROM priv WHERE name = 'WpsJob: view';
INSERT INTO priv_score (id_priv, score_usr, score_owner) SELECT id, 1, 1 FROM priv WHERE name = 'WpsJob: search';
INSERT INTO priv_score (id_priv, score_usr, score_owner) SELECT id, 0, 2 FROM priv WHERE name = 'WpsJob: create';
INSERT INTO priv_score (id_priv, score_usr, score_owner) SELECT id, 0, 1 FROM priv WHERE name = 'WpsJob: change';
INSERT INTO priv_score (id_priv, score_usr, score_owner) SELECT id, 0, 1 FROM priv WHERE name = 'WpsJob: delete';
INSERT INTO priv_score (id_priv, score_usr, score_owner) SELECT id, 0, 1 FROM priv WHERE name = 'WpsJob: make public';
-- RESULT

-- Update feature size... \
ALTER TABLE feature
CHANGE COLUMN `title` `title` VARCHAR(50) NOT NULL ,
CHANGE COLUMN `description` `description` VARCHAR(400) NULL DEFAULT NULL ,
CHANGE COLUMN `image_url` `image_url` VARCHAR(2000) NULL DEFAULT NULL ,
CHANGE COLUMN `button_link` `button_link` VARCHAR(2000) NULL DEFAULT NULL ;
-- RESULT


/*****************************************************************************/

-- Changing REST URL keywords for WPS-related classes ... \
UPDATE type SET keyword='cr/wps' WHERE class='Terradue.Portal.WpsProvider, Terradue.Portal';
UPDATE type SET keyword='service/wps' WHERE class='Terradue.Portal.WpsProcessOffering, Terradue.Portal';
-- Update types ... \
UPDATE type SET keyword='twitter' WHERE class='Terradue.News.TwitterNews, Terradue.News';
UPDATE type SET keyword='tumblr' WHERE class='Terradue.News.TumblrNews, Terradue.News';
-- RESULT

/*****************************************************************************/

-- Add Api key on usr... \
ALTER TABLE usr
ADD COLUMN apikey varchar(45) NULL default NULL COMMENT 'User api key';
-- RESULT

/*****************************************************************************/

-- Update config... \
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('log-path', 'string', 'Log path', 'Log path', '/usr/local/tep/webserver/sites/tep/log', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('catalog-baseurl', 'string', 'Catalog baseurl', 'Catalog baseurl', 'https://catalog.terradue.com', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('DataGatewayBaseUrl', 'string', 'Data Gateway Base Url', 'Data Gateway Base Url', 'https://store.terradue.com', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('DomainThematicPrefix', 'string', 'Domain identifier prefix for Thematic groups', 'Domain identifier prefix for Thematic groups', 'hydro-', '0');
-- RESULT

/*****************************************************************************/

-- Add Owner role
INSERT INTO role (`identifier`, `name`, `description`) VALUES ('owner', 'owner', 'Default role for every user to be able to use his own domain');
SET @role_id = (SELECT LAST_INSERT_ID());
SET @priv_id = (SELECT id FROM priv WHERE identifier='datapackage-v');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='datapackage-c');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='datapackage-m');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='datapackage-M');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='datapackage-d');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='datapackage-s');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='wpsjob-v');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='wpsjob-c');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='wpsjob-m');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='wpsjob-p');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='wpsjob-d');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='wpsjob-s');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
-- RESULT

-- Add Starter role
INSERT INTO role (`identifier`, `name`, `description`) VALUES ('starter', 'starter', 'Starter role');
SET @role_id = (SELECT LAST_INSERT_ID());
SET @priv_id = (SELECT id FROM priv WHERE identifier='datapackage-v');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='datapackage-s');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='wpsjob-v');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='wpsjob-s');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
-- RESULT

-- Add Explorer role
INSERT INTO role (`identifier`, `name`, `description`) VALUES ('explorer ', 'explorer', 'Explorer role');
SET @role_id = (SELECT LAST_INSERT_ID());
SET @priv_id = (SELECT id FROM priv WHERE identifier='datapackage-v');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='datapackage-s');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='wpsjob-v');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
SET @priv_id = (SELECT id FROM priv WHERE identifier='wpsjob-s');
INSERT INTO role_priv (`id_role`, `id_priv`) VALUES (@role_id, @priv_id);
-- RESULT
