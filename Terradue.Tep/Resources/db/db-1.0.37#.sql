USE $MAIN$;

/*****************************************************************************/

-- Adding type for data packages ... \
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Tep.DataPackage, Terradue.Tep');
INSERT INTO type (id_super, pos, class, caption_sg, caption_pl, keyword) VALUES (@type_id, '0', 'Terradue.Tep.ThematicApplication, Terradue.Tep', 'Thematic Apps', 'Thematic Apps', 'apps');
-- RESULT

/*****************************************************************************/

-- Update privileges for data packages ... \
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Tep.DataPackage, Terradue.Tep');
SET @priv_pos = (SELECT MAX(pos) FROM priv);
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'datapackage-s', 's', @priv_pos + 1, 'DataPackage: search', 1)
;
UPDATE priv SET identifier='datapackage-v' WHERE id_type=@type_id AND operation='v';
UPDATE priv SET identifier='datapackage-c' WHERE id_type=@type_id AND operation='c';
UPDATE priv SET identifier='datapackage-m' WHERE id_type=@type_id AND operation='m';
UPDATE priv SET identifier='datapackage-M' WHERE id_type=@type_id AND operation='M';
UPDATE priv SET identifier='datapackage-d' WHERE id_type=@type_id AND operation='d';
-- RESULT

/*****************************************************************************/

-- Update privileges for WPS jobs ... \
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Tep.WpsJob, Terradue.Tep');
SET @priv_pos = (SELECT MAX(pos) FROM priv);
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'wpsjob-s', 's', @priv_pos + 1, 'WpsJob: search', 1),
    (@type_id, 'wpsjob-p', 'p', @priv_pos + 2, 'WpsJob: make public', 1)
;
UPDATE priv SET identifier='wpsjob-v' WHERE id_type=@type_id AND operation='v';
UPDATE priv SET identifier='wpsjob-c' WHERE id_type=@type_id AND operation='c';
UPDATE priv SET identifier='wpsjob-m' WHERE id_type=@type_id AND operation='m';
UPDATE priv SET identifier='wpsjob-M' WHERE id_type=@type_id AND operation='M';
UPDATE priv SET identifier='wpsjob-d' WHERE id_type=@type_id AND operation='d';
-- RESULT

/*****************************************************************************/

-- Add configuration variables ... \
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

-- Adding domain for wpsjob table ... \
ALTER TABLE wpsjob ADD COLUMN id_domain int unsigned COMMENT 'FK: Owning domain';
ALTER TABLE wpsjob ADD CONSTRAINT fk_wpsjob_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE SET NULL;
-- RESULT

ALTER TABLE wpsjob_priv RENAME TO wpsjob_perm;
ALTER TABLE wpsjob_perm
    DROP FOREIGN KEY fk_wpsjob_priv_wpsjob,
    DROP FOREIGN KEY fk_wpsjob_priv_usr,
    DROP FOREIGN KEY fk_wpsjob_priv_grp,
    ADD CONSTRAINT fk_wpsjob_perm_wpsjob FOREIGN KEY (id_wpsjob) REFERENCES wpsjob(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_wpsjob_perm_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_wpsjob_perm_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE
;
-- CHECKPOINT C-06f2

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