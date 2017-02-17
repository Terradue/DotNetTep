
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
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Tep.DataPackage, Terradue.Tep');
INSERT INTO type (id_super, class, caption_sg, caption_pl, keyword) VALUES (@type_id, 'Terradue.Tep.ThematicCommunity, Terradue.Tep', 'Thematic community', 'Thematic community', 'community');
-- RESULT

-- Adding discuss url for domains
ALTER TABLE domain 
ADD COLUMN discuss VARCHAR(200) NULL DEFAULT NULL;
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPageUrl', 'string', 'Url page for communities', 'Url page for communities', 'https://hydrology-tep.eo.esa.int/#!communities', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityJoinEmailBody', 'string', 'Email template to notify user has been added in community', 'Email template to notify user has been added in community', 'Dear user,\n\nyou have been invited to join the community $(COMMUNITY).\nYou can now find it listed in the communities page ($(LINK))./n/nBest Regards', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityJoinEmailSubject', 'string', 'Email subject to notify user has been added in community', 'Email subject to notify user has been added in community', '[$(SITENAME)] - Join community $(COMMUNITY)', '0');
-- RESULT