
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