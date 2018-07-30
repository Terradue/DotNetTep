
USE $MAIN$;

/*****************************************************************************/

-- Update roles ... \
INSERT INTO role (identifier, name, description) VALUES ('member', 'member', 'Community default member');
INSERT INTO role (identifier, name, description) VALUES ('manager', 'manager', 'Community manager');
INSERT INTO role (identifier, name, description) VALUES ('pending', 'pending', 'Community pending user');
-- RESULT

-- Adding type for Communities ... \
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Portal.Domain, Terradue.Portal');
INSERT INTO type (id_super, class, caption_sg, caption_pl, keyword) VALUES (@type_id, 'Terradue.Tep.ThematicCommunity, Terradue.Tep', 'Thematic community', 'Thematic community', 'community');
-- RESULT

-- Adding discuss url for domains
ALTER TABLE domain 
ADD COLUMN discuss VARCHAR(200) NULL DEFAULT NULL;
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPageUrl', 'string', 'Url page for communities', 'Url page for communities', 'https://hydrology-tep.eo.esa.int/#!communities', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityJoinEmailBody', 'string', 'Email template to notify user has been added in community', 'Email template to notify user has been added in community', 'Dear user,\n\nyou have been invited to join the community $(COMMUNITY).\nYou can now find it listed in the communities page ($(LINK)).\n\nBest Regards', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityJoinEmailSubject', 'string', 'Email subject to notify user has been added in community', 'Email subject to notify user has been added in community', '[$(SITENAME)] - Join community $(COMMUNITY)', '0');
-- RESULT

-- Activities ...\
ALTER TABLE activity ADD COLUMN id_app VARCHAR(50) NULL DEFAULT NULL;
-- RESULT

-- Update privileges for data packages ... \
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Tep.DataPackage, Terradue.Tep');
SET @priv_pos = (SELECT MAX(pos) FROM priv);
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'datapackage-p', 'p', @priv_pos + 1, 'DataPackage: make public', 1)
;
-- RESULT

/*****************************************************************************/