USE $MAIN$;

-- Add config
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('EmailConfirmedNotification', 'string', 'Email confirmed notification to support', 'Email confirmed notification to support', 'Dear support,\n\nThis is an automatic email to inform you that user $(USERNAME) has just confirmed his email address ($(EMAIL)) on the TEP platform.\n', '0');
-- RESULT

-- Add type
CALL add_type(NULL, 'Terradue.Tep.ThematicApplication, Terradue.Tep', 'Terradue.Tep.DataPackage, Terradue.Tep', 'Thematic Apps', 'Thematic Apps', 'apps');
-- RESULT

-- Update priv for Datapackage ... \
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Tep.DataPackage, Terradue.Tep');
SET @priv_pos = (SELECT MAX(pos) FROM priv);
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'datapackage-s', 's', @priv_pos + 1, 'DataPackage: search', 1);
UPDATE priv SET identifier='datapackage-v' WHERE id_type=@type_id AND operation='v';
UPDATE priv SET identifier='datapackage-c' WHERE id_type=@type_id AND operation='c';
UPDATE priv SET identifier='datapackage-m' WHERE id_type=@type_id AND operation='m';
UPDATE priv SET identifier='datapackage-M' WHERE id_type=@type_id AND operation='M';
UPDATE priv SET identifier='datapackage-d' WHERE id_type=@type_id AND operation='d';
-- RESULT

-- Update priv for Wpsjob ... \
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Tep.WpsJob, Terradue.Tep');
SET @priv_pos = (SELECT MAX(pos) FROM priv);
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'wpsjob-s', 's', @priv_pos + 1, 'WpsJob: search', 1);
UPDATE priv SET identifier='wpsjob-v' WHERE id_type=@type_id AND operation='v';
UPDATE priv SET identifier='wpsjob-c' WHERE id_type=@type_id AND operation='c';
UPDATE priv SET identifier='wpsjob-m' WHERE id_type=@type_id AND operation='m';
UPDATE priv SET identifier='wpsjob-M' WHERE id_type=@type_id AND operation='M';
UPDATE priv SET identifier='wpsjob-d' WHERE id_type=@type_id AND operation='d';
-- RESULT
