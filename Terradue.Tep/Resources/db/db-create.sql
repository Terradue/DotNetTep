-- VERSION 1.6.5

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

-- Adding type for Data package ... \
CALL add_type($ID$, 'Terradue.Tep.DataPackage, Terradue.Tep', NULL, 'DataPackage', 'DataPackage', 'data/package');
-- RESULT
SET @type_id = (SELECT LAST_INSERT_ID());

-- Update privileges for data packages ... \
SET @priv_pos = (SELECT MAX(pos) FROM priv);
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'datapackage-v', 'v', @priv_pos + 1, 'DataPackage: view', 1),
    (@type_id, 'datapackage-c', 'c', @priv_pos + 2, 'DataPackage: create', 1),
    (@type_id, 'datapackage-s', 's', @priv_pos + 3, 'DataPackage: search', 1),
    (@type_id, 'datapackage-m', 'm', @priv_pos + 4, 'DataPackage: change', 1),
    (@type_id, 'datapackage-d', 'd', @priv_pos + 5, 'DataPackage: delete', 1),
    (@type_id, 'datapackage-p', 'p', @priv_pos + 6, 'DataPackage: make public', 1)
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
    wps_name varchar(150) NOT NULL COMMENT 'FK: WPS Service name',
    process varchar(100) NOT NULL COMMENT 'Process name',
    params TEXT NOT NULL COMMENT 'Wps job parameters',
    status int NOT NULL DEFAULT 0 COMMENT 'Wps job status',
    archive_status int NOT NULL DEFAULT 0 COMMENT 'Wps job archive status',
    status_url varchar(400) NOT NULL COMMENT 'Wps job status url',
    ows_url varchar(400) NULL DEFAULT NULL COMMENT 'Wps job ows url',
    stacitem_url varchar(400) NULL DEFAULT NULL COMMENT 'Wps Stac Item url',
    share_url varchar(400) NULL DEFAULT NULL COMMENT 'Wps Share url',
    app_identifier varchar(50) NULL DEFAULT NULL COMMENT 'Wps job app identifier',
    created_time datetime NOT NULL COMMENT 'Wps created date',
    access_key VARCHAR(50) NULL DEFAULT NULL COMMENT 'Access key',
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
    'datapackage-p',
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

CREATE TABLE rolegrant_pending (
    id_usr int unsigned COMMENT 'FK: User (id_usr or id_grp must be set)',
    id_grp int unsigned COMMENT 'FK: Group (id_usr or id_grp must be set)',
    id_role int unsigned NOT NULL COMMENT 'FK: Role to which the user/group is assigned',
    id_domain int unsigned COMMENT 'FK: Domain for which the user/group has the role',
    access_key varchar(50) COMMENT 'Access key',
    CONSTRAINT fk_rolegrantpending_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_rolegrantpending_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE,
    CONSTRAINT fk_rolegrantpending_role FOREIGN KEY (id_role) REFERENCES role(id) ON DELETE CASCADE,
    CONSTRAINT fk_rolegrantpending_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Assignments of users/groups to roles for domains';

/*****************************************************************************/

INSERT INTO role (identifier, name, description) VALUES ('member', 'member', 'Community default member');
INSERT INTO role (identifier, name, description) VALUES ('pending', 'pending', 'Community pending user');
INSERT INTO `role` (`identifier`, `name`, `description`) VALUES ('MANAGER', 'Content Authority', 'Content Authority user');
INSERT INTO `role` (`identifier`, `name`, `description`) VALUES ('enduser', 'End User', 'Default user role');
INSERT INTO `role` (`identifier`, `name`, `description`) VALUES ('expert', 'Expert User', 'Default expert user role');
INSERT INTO `role` (`identifier`, `name`, `description`) VALUES ('resadmin', 'Resource Administrator', 'Resource Administrator role');
INSERT INTO `role` (`identifier`, `name`, `description`) VALUES ('dataprovider', 'Data Provider', 'Data Provider role');
INSERT INTO `role` (`identifier`, `name`, `description`) VALUES ('ictprovider', 'ICT Provider', 'ICT Provider role');

/*****************************************************************************/

-- Adding type for Communities ... \
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Portal.Domain, Terradue.Portal');
INSERT INTO type (id_super, class, caption_sg, caption_pl, keyword) VALUES (@type_id, 'Terradue.Tep.ThematicCommunity, Terradue.Tep', 'Thematic community', 'Thematic community', 'community');
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Tep.DataPackage, Terradue.Tep');
INSERT INTO type (id_super, class, caption_sg, caption_pl, keyword) VALUES (@type_id, 'Terradue.Tep.ThematicApplication, Terradue.Tep', 'Thematic application', 'Thematic application', 'application');
-- RESULT

-- RESULT

-- Adding discuss url for domains
ALTER TABLE domain 
ADD COLUMN discuss VARCHAR(200) NULL DEFAULT NULL;
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPageUrl', 'string', 'Url page for communities', 'Url page for communities', 'https://hydrology-tep.eo.esa.int/#!communities', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityJoinEmailBody', 'string', 'Email template to notify user has been added in community', 'Email template to notify user has been added in community', 'Dear user,\n\nyou have been invited to join the community $(COMMUNITY).\nYou can now find it listed in the communities page ($(LINK))./n/nBest Regards', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityJoinEmailSubject', 'string', 'Email subject to notify user has been added in community', 'Email subject to notify user has been added in community', '[$(SITENAME)] - Join community $(COMMUNITY)', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('discussBaseUrl', 'string', 'Discuss base url', 'Discuss base url', 'http://discuss.terradue.com', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('discussApiKey', 'string', 'Discuss api key', 'Discuss api key', 'TO_BE_UPDATED', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('accounting-enabled', 'bool', 'accounting enabled or not', 'accounting enabled or not', 'true', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('t2portal-usr-starterPlan', 'string', 't2portal user starter Plan', 't2portal user starter Plan', 'FreeTrial', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('t2portal-usr-explorerPlan', 'string', 't2portal user starter Plan', 't2portal user explorer Plan', 'Explorer', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('SiteNameShort', 'string', 'Site Name - short', 'Site Name - short', 'TEP', '0');
-- RESULT

-- Activities ...\
ALTER TABLE activity ADD COLUMN id_app VARCHAR(50) NULL DEFAULT NULL;
ALTER TABLE activity ADD COLUMN params VARCHAR(200) NULL DEFAULT NULL;
-- RESULT

-- Add Rate table ... \
CREATE TABLE rate (
    id int unsigned NOT NULL auto_increment,
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    id_entity int unsigned COMMENT 'Entity associated to the activity',
    id_type int unsigned COMMENT 'Entity type',
    unit BIGINT unsigned COMMENT 'rate unit',
    cost DOUBLE unsigned COMMENT 'rate cost',
    CONSTRAINT pk_rate PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'Accounting rates';
-- RESULT

-- Add Transaction table ...\
CREATE TABLE transaction (
    id int unsigned NOT NULL auto_increment,
    id_usr int unsigned NOT NULL COMMENT 'FK: User',
    reference varchar(50) NULL COMMENT 'reference',
    id_entity int unsigned COMMENT 'Entity associated to the activity',
    id_type int unsigned COMMENT 'Entity type',    
    log_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Date/time of activity creation',
    id_provider int unsigned NULL COMMENT 'FK: User',
    balance int unsigned COMMENT 'transaction balance',
    kind tinyint NOT NULL DEFAULT 0 COMMENT 'transaction kind',
    CONSTRAINT pk_transaction PRIMARY KEY (id),
    CONSTRAINT fk_transaction_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Accounting transactions';
-- RESULT

-- Adding Agent action...\
INSERT INTO action (`identifier`, `name`, `description`, `class`, `method`) VALUES ('wpsSynchro', 'Synchronize WPS', 'This action synchronize the wps providers stored in db', 'Terradue.Tep.Actions, Terradue.Tep', 'UpdateWpsProviders');
INSERT INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`) VALUES ('monthlyJobReport', 'Create Monthly job Report', 'This action creates a job report every month', 'Terradue.Tep.Actions, Terradue.Tep', 'CreateJobMonthlyReport',0);
-- RESULT

-- Add community default role ... \
ALTER TABLE domain ADD COLUMN id_role_default INT(10) NOT NULL DEFAULT 0;
-- RESULT

-- Adding Agent action...\
INSERT INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`, `time_interval`) VALUES ('CleanDeposit', 'Clean accouting Deposit', 'This action set as closed the deposit without any transaction for more than a certain number of days', 'Terradue.Tep.Actions, Terradue.Tep', 'CleanDeposit',1,'1D');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('accounting-deposit-maxDays', 'double', 'accounting deposit max days lifetime', 'accounting deposit max days lifetime', '30', '1');
-- RESULT



-- Adding unique index...\
ALTER TABLE resourceset 
CHANGE COLUMN `name` `name` VARCHAR(200) NULL DEFAULT NULL ;
ALTER TABLE resourceset 
ADD UNIQUE INDEX `uq_name_usr` (`id_usr` ASC, `name` ASC);
-- RESULT

-- Adding config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('action-jobPoolSize', 'int', 'Actions job pool size', 'Actions job pool size', '100', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('action-maxDaysJobRefresh', 'int', 'Number of days before a job nb results is set to 0 in case of errors', 'Number of days before a job nb results is set to 0 in case of errors', '30', '0');
-- RESULT

-- Adding wpsjob nbresults...\
ALTER TABLE wpsjob ADD COLUMN nbresults INT(10) NULL DEFAULT -1;
-- RESULT

-- Adding action...\
INSERT INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`, `time_interval`) VALUES ('RefreshWpsjobStatus', 'Refresh wpsjob status', 'This action refresh the status of ongoing wps jobs', 'Terradue.Tep.Actions, Terradue.Tep', 'RefreshWpsjobStatus',1,'2h');
INSERT INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`, `time_interval`) VALUES ('RefreshWpsjobResultNb', 'Refresh wpsjob nb results', 'This action refresh the nb of results of wps jobs for which nb results is not set', 'Terradue.Tep.Actions, Terradue.Tep', 'RefreshWpsjobResultNb',1,'2h');
-- RESULT

-- Add wpsjob end time...\
ALTER TABLE wpsjob ADD COLUMN `end_time` DATETIME NULL AFTER `created_time`;
-- RESULT

-- Add app_cache table...\
CREATE TABLE app_cache (
    id int unsigned NOT NULL auto_increment,
    uid varchar(50) NOT NULL COMMENT 'Unique identifier',
    cat_index varchar(50) NULL COMMENT 'index',
    id_domain int unsigned COMMENT 'FK: Owning domain',
    feed TEXT NOT NULL COMMENT 'app feed',
    searchable TEXT NULL COMMENT 'app searchable text',
    last_update datetime COMMENT 'Last update time',
    CONSTRAINT pk_appcache PRIMARY KEY (id),
    CONSTRAINT fk_appcache_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE CASCADE,
    UNIQUE INDEX `uq_uid_domain` (`uid` ASC, `id_domain` ASC)
) Engine=InnoDB COMMENT 'Thematic Apps cache';

INSERT INTO type (class, caption_sg, caption_pl, keyword) VALUES ('Terradue.Tep.ThematicApplicationCached, Terradue.Tep', 'Thematic Apps cached', 'Thematic Apps cached', 'apps');
-- RESULT

-- Adding action...\
INSERT INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`, `time_interval`) VALUES ('RefreshThematicAppsCache', 'Refresh thematic apps cached', 'This action refresh the cached thematic apps', 'Terradue.Tep.Actions, Terradue.Tep', 'RefreshThematicAppsCache',1,'1D');
-- RESULT

-- Add usrsession end time...\
ALTER TABLE usrsession ADD COLUMN `log_end` DATETIME NULL AFTER `log_time`;
-- RESULT

-- up wpsjob table...\
ALTER TABLE wpsjob 
CHANGE COLUMN `name` `name` VARCHAR(120) NOT NULL;
-- RESULT

-- up doman table...\
ALTER TABLE domain 
ADD COLUMN `email_notification` TINYINT(1) UNSIGNED NULL DEFAULT '0';
ALTER TABLE domain 
ADD COLUMN `enable_join` TINYINT(1) UNSIGNED NULL DEFAULT '0';
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityRemoveEmailBody', 'string', 'Email template to notify user has been removed in community', 'Email template to notify user has been removed in community', 'Dear user,\n\nyou have requested to join the community $(COMMUNITY).\nUnfortunately we cannot proceed for the following reason:\n ($(REASON)).\n\nWith our deepest apologies.\nBest Regards', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityRemoveEmailSubject', 'string', 'Email subject to notify user has been removed in community', 'Email subject to notify user has been removed in community', '[$(SITENAME)] - Join community $(COMMUNITY)', '0');
ALTER TABLE domain 
ADD COLUMN `contributor` VARCHAR(100) NULL DEFAULT NULL;
ALTER TABLE domain 
ADD COLUMN `contributor_icon_url` VARCHAR(200) NULL DEFAULT NULL;
-- RESULT

-- Adding JIRA config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-api-baseurl', 'string', 'jira-api-baseurl', 'jira-api-baseurl', 'https://helpdesk.terradue.com', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-api-username', 'string', 'jira-api-username', 'jira-api-username', 'TBD', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-api-password', 'string', 'jira-api-password', 'jira-api-password', 'TBD', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-helpdesk-serviceDeskId', 'string', 'jira-helpdesk-serviceDeskId', 'jira-helpdesk-serviceDeskId', '17', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-helpdesk-requestTypeId', 'string', 'jira-helpdesk-requestTypeId', 'jira-helpdesk-requestTypeId', '165', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-helpdesk-components', 'string', 'jira-helpdesk-components', 'jira-helpdesk-components', 'GEP', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-helpdesk-labels', 'string', 'jira-helpdesk-labels', 'jira-helpdesk-labels', 'GEP', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-helpdesk-customfield-ProcessingServiceLabel', 'string', 'jira-helpdesk-customfield-ProcessingServiceLabel', 'jira-helpdesk-customfield-ProcessingServiceLabel', 'customfield_10237', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('wpsjob-support-mode', 'string', 'wpsjob-support-mode', 'wpsjob-support-mode', 'jira', '1');
-- RESULT

-- Add t2 sso config...\
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('cookieID-SID', 'string', 'cookie identifier - SID', '', 'oauth_sid', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('cookieID-SUBSID', 'string', 'cookie identifier - SUBSID', '', 'oauth_sub_sid', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('cookieID-token-access', 'string', 'cookie identifier - Token access', '', 'oauthtoken_access', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('cookieID-token-refresh', 'string', 'cookie identifier - Token refresh', '', 'oauthtoken_refresh', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('cookieID-token-id', 'string', 'cookieID-token-id value', 'cookieID-token-id value', 'oauthtoken_id', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-clientId', 'string', 'Terradue SSO Client Id', 'Enter the value of the client identifier of the Terradue SSO', "TBD", '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-clientSecret', 'string', 'Terradue SSO Client Secret', 'Enter the value of the client secret password of the Terradue SSO', "TBD", '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-userInfoEndpoint', 'string', 'Terradue SSO User Info Endpoint url', 'Enter the value of the url of the User Info Endpoint of the Terradue SSO', "https://sso.terradue.com/c2id/userinfo", '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-tokenEndpoint', 'string', 'Terradue SSO Token Endpoint url', 'Enter the value of the url of the Token Endpoint of the Terradue SSO', "https://sso.terradue.com/c2id/token", '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-logoutEndpoint', 'string', 'Terradue SSO Logout Endpoint url', 'Enter the value of the url of the Logout Endpoint of the Terradue SSO', "https://sso.terradue.com/c2id/logout", '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-scopes', 'string', 'Terradue SSO default scopes', 'Enter the value of the default scopes of the Terradue SSO', "openid,profile", '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-callback', 'string', 'Terradue SSO callback url', 'Enter the value of the callback url of the Terradue SSO', "https://geohazards-tep.eo.esa.int/t2api/cb", '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-authEndpoint', 'string', 'Terradue SSO Authentication Endpoint url', 'Enter the value of the url of the Authentication Endpoint of the Terradue SSO', "https://sso.terradue.com/c2id/authz-sessions/rest/v2", '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-sessionEndpoint', 'string', 'Terradue SSO Session Endpoint url', 'Enter the value of the url of the Session Endpoint of the Terradue SSO', "https://sso.terradue.com/c2id/session-store/rest/v2/sessions", '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('oauth-authEndpoint', 'string', 'Terradue Portal Authentication Endpoint url', 'Enter the value of the url of the Authentication Endpoint of the Terradue SSO', "https://www.terradue.com/t2api/oauth", '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-configUrl', 'string', 'Terradue SSO Configuration url', 'Enter the value of the url of the Configuration of the Terradue SSO', "https://sso.terradue.com/c2id//.well-known/openid-configuration", '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-apiAccessToken', 'string', 'Terradue SSO API Access Token', 'Enter the value of the API Access token of the Terradue SSO', "TBD", '0');
-- RESULT

-- Update LDAP auth...\
UPDATE auth SET type='Terradue.Tep.TepLdapAuthenticationType, Terradue.Tep', activation_rule='3' WHERE identifier='ldap';
-- RESULT

-- Add wps version in wpsjob...\
ALTER TABLE wpsjob ADD COLUMN `wps_version` VARCHAR(30) NULL DEFAULT NULL;
-- RESULT

-- Add analytics config...\
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('analytics_nbtopusedservices', 'int', 'nb of top services to show in analytics', '', '5', '0');
-- RESULT

-- Add wps config...\
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('wpsDefaultValue_T2ResultsAnalysis', 'string', 'default value for wps input _T2ResultsAnalysis', '', 'extended', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('catalog-communityIndex', 'string', 'catalog public community Index', '', '', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('catalog-communityUsername', 'string', 'catalog public community username', '', '', '0');
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('catalog-communityApikey', 'string', 'catalog public community apikey', '', '', '0');
-- RESULT

-- Update wps process offering custom class
UPDATE type SET custom_class='Terradue.Tep.WpsProcessOfferingTep, Terradue.Tep' WHERE class='Terradue.Portal.WpsProcessOffering, Terradue.Portal';
-- RESULT

-- Update config...\
UPDATE config SET value='Dear Content Authority,\n\nthe user $(USERNAME) ($(USERMAIL)) has requested to Join the community $(COMMUNITY), with the following description:\n\n\t$(USER_REQUEST)\n\nYou can approve or refuse this request here $(LINK).\n\nYou can request information about the requesting user project to the Platform Operator.\n\nBest Regards' WHERE name='CommunityJoinEmailBody';
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityDetailPageUrl', 'string', 'Community detailed page url', '', '', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityJoinConfirmationEmailSubject', 'string', 'Community Join confirmation email subject', '', '[$(SITENAME)] - Join community $(COMMUNITY)', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityJoinConfirmationEmailBody', 'string', 'Community Join confirmation email body', '', 'Dear user,\n\nyou have been added as member of the community $(COMMUNITY).\nYou can access it directly from this link: $(LINK).\n\nBest Regards', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('log-hideException', 'bool', 'Hide exception in error log', 'Hide exception in error log', '1', '0');
-- RESULT

-- Add community sync identifier
ALTER TABLE domain ADD COLUMN `usersync_identifier` VARCHAR(50) NULL DEFAULT NULL;
-- RESULT

-- Update config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('a2shpcmpic-token', 'string', 'A2s HPC sync user token', 'A2s HPC sync user token', '', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('a2shpcmpic-sync-url', 'string', 'A2s HPC sync user sync url', 'A2s HPC sync user sync url', '', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('a2shpcdsmopt-token', 'string', 'A2s HPC sync user token', 'A2s HPC sync user token (DSM OPT)', '', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('a2shpcdsmopt-sync-url', 'string', 'A2s HPC sync user sync url', 'A2s HPC sync user sync url (DSM OPT)', '', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('metrics-job-publish-url', 'string', 'Metrics job publish url', 'Metrics job publish url', 'https://metrics.terradue.com/job/publish', '0');
-- RESULT

-- Create Terms Conditions table...\
CREATE TABLE termsconditions (
  identifier VARCHAR(100) NOT NULL,
  id_usr INT UNSIGNED NOT NULL,
  INDEX tc_usr_idx (id_usr ASC),
  CONSTRAINT tc_usr FOREIGN KEY (id_usr) REFERENCES usr (id) ON DELETE CASCADE,
  UNIQUE INDEX `id_usrid_tc` (`identifier`, `id_usr`)
);
-- RESULT

-- Update config
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('wpsjob-archive-enabled', 'bool', 'Wpsjob archive is enabled or not (if set to yes, job are not deleted but set as to be archived)', 'Wpsjob archive is enabled or not (if set to yes, job are not deleted but set as to be archived)', 'true', '0');
-- RESULT

-- Update config
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('wps3input-format', 'string', 'wps3 input fixed format value', 'wps3 input fixed format value', 'atom', '0');
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('wps3input-downloadorigin', 'string', 'wps3 input fixed download origin value', 'wps3 input fixed download origin value', '[terradue]', '0');
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('cookieID-token-id', 'string', 'cookieID-token-id value', 'cookieID-token-id value', 'oauthtoken_id', '0');
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('sso-notoken-endsession-enabled', 'bool', 'Tells if we enable the end of session in case of sso token not valid anymore', 'Tells if we enable the end of session in case of sso token not valid anymore', 'true', '1');
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('AccessTokenExpireMinutes', 'int', 'access token max expire time in minutes', 'access token max expire time in minutes', 3, '0');
-- RESULT

-- Add wpsjob logs
ALTER TABLE wpsjob ADD COLUMN `logs` TEXT NULL DEFAULT NULL;
-- RESULT

-- Add config
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('agent-jobreport-headerfile', 'string', 'agent job report headerfile', 'agent job report headerfile', "", '0');
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('agent-jobreport-query', 'string', 'agent job report headerfile', 'agent job report headerfile', "", '0');
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('ellip.editor-url', 'string', 'ellip.editor-url', 'ellip.editor-url', "https://ellip.terradue.com/#infohubs/geobrowser-apps-editor/id", '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('path.files', 'string', 'Tells the files directory path', 'Tells the files directory path', '/usr/local/gep/webserver/sites/gep/root/files', '1');
-- RESULT

-- Add config
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPendingEmailSubject', 'string', 'Email subject to notify user is pending in community', 'Email subject to notify user is pending in community', '[$(SITENAME)] - Your request to join the community $(COMMUNITY)', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPendingEmailBody', 'string', 'Email template to notify user is pending in community', 'Email subject to notify user is pending in community', 'Dear user,\n\nyou are receiving this email because you have requested to join the $(SITENAME_SHORT) community $(COMMUNITY).\n\nYour request is being reviewed by the Content Authority.\n\nYou will receive a notification email indicating the outcome of this review.\n\nBest Regards', '0');

UPDATE config SET value='[$(SITENAME)] - User request to join the community $(COMMUNITY)' WHERE name='CommunityJoinEmailSubject';
UPDATE config SET value='Dear Content Authority,\n\nthe user $(USERNAME) ($(USERMAIL)) has requested to Join the community $(COMMUNITY), with the following description:\n\n    $(USER_REQUEST)\n\nPlease approve or deny this request here $(LINK).\n\nYou can ask for additional information about the service subscription status of the requesting user by replying to this email.\n\nBest Regards' WHERE name='CommunityJoinEmailBody';

INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPendingJoinConfirmationEmailSubject', 'string', 'Email subject to notify user is pending in community', 'Email subject to notify user is pending in community', '[$(SITENAME)] - Your access to the community $(COMMUNITY)', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPendingJoinConfirmationEmailBody', 'string', 'Email template to notify user is pending in community', 'Email subject to notify user is pending in community', 'Dear user,\n\nyou have been added as member of the community $(COMMUNITY).\nYou can access it directly from this link: $(LINK).\n\nBest Regards', '0');

INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPendingRemoveEmailSubject', 'string', 'Email subject to notify user pending has been denied from community', 'Email subject to notify user pending has been denied from community', '[$(SITENAME)] - You cannot join the community $(COMMUNITY)', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPendingRemoveEmailBody', 'string', 'Email template to notify user pending has been denied from community', 'Email subject to notify user pending has been denied from community', 'Dear user,\n\nyou have requested to join the community $(COMMUNITY).\nUnfortunately, your request was cancelled by a $(SITENAME_SHORT) Content Authority for the following reason:\n $(REASON).\n\nBest Regards', '0');

UPDATE config SET value='[$(SITENAME)] - You have been added to the community $(COMMUNITY)' WHERE name='CommunityJoinConfirmationEmailSubject';
UPDATE config SET value='Dear user,\n\nyou are receiving this email because you are a registered $(SITENAME_SHORT) user with an active subscription.\n\nAccordingly to your Application Scenario $(ASD_LINK), you have been added by a $(SITENAME_SHORT) Content Authority as member of the community $(COMMUNITY).\nYou can access it directly from this link: $(LINK).\n\nBest Regards' WHERE name='CommunityJoinConfirmationEmailBody';

UPDATE config SET value='[$(SITENAME)] - End of your access to the community $(COMMUNITY)' WHERE name='CommunityRemoveEmailSubject';
UPDATE config SET value='Dear user,\n\nyou have been removed by a $(SITENAME_SHORT) Content Authority from the community $(COMMUNITY) for the following reason:\n $(REASON).\n\nYou can still access any public community on $(SITENAME_SHORT).\n\nAccess to processing services and to your past processing job results is conditioned by the status of your Application Scenario $(ASD_LINK).\n\nBest Regards' WHERE name='CommunityRemoveEmailBody';

INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('asd_link', 'string', 'ASD page link', 'ASD page link', 'https://geohazards-tep.eu/#!settings/asd', '0');
-- RESULT

-- Add service publish
ALTER TABLE service ADD COLUMN `publish_url` varchar(200) NULL DEFAULT NULL COMMENT 'service publish url';
ALTER TABLE service ADD COLUMN `publish_type` varchar(50) NULL DEFAULT NULL COMMENT 'service publish type';
-- RESULT