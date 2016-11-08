-- VERSION 1.1

USE $MAIN$;

/*****************************************************************************/

-- Changing REST URL keywords for WPS-related classes ... \
UPDATE type SET keyword='cr/wps' WHERE class='Terradue.Portal.WpsProvider, Terradue.Portal';
UPDATE type SET keyword='service/wps' WHERE class='Terradue.Portal.WpsProcessOffering, Terradue.Portal';
-- RESULT

-- Changing REST URL keywords for news-related classes ... \
UPDATE type SET keyword='twitter' WHERE class='Terradue.News.TwitterNews, Terradue.News';
UPDATE type SET keyword='tumblr' WHERE class='Terradue.News.TumblrNews, Terradue.News';
-- RESULT

/*****************************************************************************/

-- Updating custom User type ... \
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Portal.User, Terradue.Portal');
UPDATE type SET custom_class = 'Terradue.Tep.UserTep, Terradue.Tep' WHERE id = @type_id;
-- RESULT

-- Update priv for User ... \
SET @priv_pos = (SELECT MAX(pos) FROM priv);
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'user-login', 'l', @priv_pos + 1, 'User: Login', 1);
-- RESULT

/*****************************************************************************/

-- Adding type for WPS jobs ... \
CALL add_type($ID$, 'Terradue.Tep.WpsJob, Terradue.Tep', NULL, 'Wps Job', 'Wps Job', 'job/wps');
-- RESULT
SET @type_id = (SELECT LAST_INSERT_ID());

-- Adding privileges for WPS jobs ... \
SET @priv_pos = (SELECT MAX(pos) FROM priv);
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'wpsjob-v', 'v', @priv_pos + 1, 'WpsJob: view', 1),
    (@type_id, 'wpsjob-c', 'c', @priv_pos + 2, 'WpsJob: create', 1),
    (@type_id, 'wpsjob-s', 's', @priv_pos + 3, 'WpsJob: search', 1),
    (@type_id, 'wpsjob-m', 'm', @priv_pos + 4, 'WpsJob: change', 1),
    (@type_id, 'wpsjob-d', 'd', @priv_pos + 5, 'WpsJob: delete', 1),
    (@type_id, 'wpsjob-p', 'p', @priv_pos + 6, 'WpsJob: make public', 1)
;
-- RESULT

/*****************************************************************************/

-- Adding configuration variables ... \
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES 
    ('EmailConfirmedNotification', 'string', 'Email confirmed notification to support', 'Email confirmed notification to support', 'Dear support,\n\nThis is an automatic email to inform you that user $(USERNAME) has just confirmed his email address ($(EMAIL)) on the TEP platform.\n', '0'),
    ('DataGatewayBaseUrl', 'string', 'Data Gateway Base Url', 'Data Gateway Base Url', 'https://store.terradue.com', '0'),
    ('DomainThematicPrefix', 'string', 'Domain identifier prefix for Thematic groups', 'Domain identifier prefix for Thematic groups', 'hydro-', '0'),
    ('log-path', 'string', 'Log path', 'Log path', '/usr/local/tep/webserver/sites/tep/log', '0'),
    ('catalog-baseurl', 'string', 'Catalog baseurl', 'Catalog baseurl', 'https://catalog.terradue.com', '0')
;
UPDATE config SET value='Dear user,\n\nYour account $(USERNAME) has been created on $(SITENAME).\nWe must verify the authenticity of your email address.\nTo do so, please click on the following link: $(ACTIVATIONURL)\n\nWith our best regards, the Operations Support team at Terradue' WHERE name='RegistrationMailBody';
UPDATE config SET value='[$(SITENAME)] - Registration' WHERE name='RegistrationMailSubject';
-- RESULT

/*****************************************************************************/

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
) Engine=InnoDB COMMENT 'WPS jobs';

CREATE TABLE wpsjob_perm (
    id_wpsjob int unsigned NOT NULL COMMENT 'FK: wpsjob set',
    id_usr int unsigned COMMENT 'FK: User',
    id_grp int unsigned COMMENT 'FK: Group',
    CONSTRAINT fk_wpsjob_perm_wpsjob FOREIGN KEY (id_wpsjob) REFERENCES wpsjob(id) ON DELETE CASCADE,
    CONSTRAINT fk_wpsjob_perm_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_wpsjob_perm_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'User/group permissions on WPS jobs';

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

/*****************************************************************************/

-- Update feature size... \
ALTER TABLE feature
    CHANGE COLUMN title title varchar(50) NOT NULL ,
    CHANGE COLUMN description description varchar(400) NULL DEFAULT NULL,
    CHANGE COLUMN image_url image_url varchar(2000) NULL DEFAULT NULL,
    CHANGE COLUMN button_link button_link varchar(2000) NULL DEFAULT NULL 
;
-- RESULT


/*****************************************************************************/

-- Add Api key on usr ... \
ALTER TABLE usr
    ADD COLUMN apikey varchar(45) NULL default NULL COMMENT 'User api key'
;
-- RESULT

/*****************************************************************************/

-- Add Owner role ... \
INSERT INTO role (identifier, name, description) VALUES ('owner', 'owner', 'Default role for every user to be able to use his own domain');
SET @role_id = (SELECT LAST_INSERT_ID());
INSERT INTO role_priv (id_role, id_priv) SELECT @role_id, id FROM priv WHERE identifier IN (
    'datapackage-v',
    'datapackage-c',
    'datapackage-m',
    'datapackage-M',
    'datapackage-d',
    'datapackage-s',
    'wpsjob-v',
    'wpsjob-c',
    'wpsjob-m',
    'wpsjob-p',
    'wpsjob-d',
    'wpsjob-s'
);
-- RESULT

-- Add Visitor role ... \
INSERT INTO role (identifier, name, description) VALUES ('visitor', 'visitor', 'Visitor role');
SET @role_id = (SELECT LAST_INSERT_ID());
INSERT INTO role_priv (id_role, id_priv) SELECT @role_id, id FROM priv WHERE identifier IN (
    'datapackage-v',
    'datapackage-s',
    'wpsjob-v',
    'wpsjob-s'
);
-- RESULT

-- Add Starter role ... \
INSERT INTO role (identifier, name, description) VALUES ('starter', 'starter', 'Starter role');
SET @role_id = (SELECT LAST_INSERT_ID());
INSERT INTO role_priv (id_role, id_priv) SELECT @role_id, id FROM priv WHERE identifier IN (
    'datapackage-v',
    'datapackage-s',
    'wpsjob-v',
    'wpsjob-s',
    'service-v',
    'service-s'
);
-- RESULT

-- Add Explorer role ... \
INSERT INTO role (identifier, name, description) VALUES ('explorer', 'explorer', 'Explorer role');
SET @role_id = (SELECT LAST_INSERT_ID());
INSERT INTO role_priv (id_role, id_priv) SELECT @role_id, id FROM priv WHERE identifier IN (
    'datapackage-v',
    'datapackage-s',
    'wpsjob-v',
    'wpsjob-s',
    'service-v',
    'service-s'
);
-- RESULT

/*****************************************************************************/