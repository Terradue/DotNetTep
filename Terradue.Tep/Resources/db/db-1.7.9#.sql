USE $MAIN$;

/*****************************************************************************/

-- Add config
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('terrapi-share-workspace', 'string', 'terrapi share workspace default name', 'terrapi share workspace default name', 'bios-${USERNAME}-private-workspace', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('terrapi-share-url', 'string', 'terrapi share url', 'terrapi share url', 'https://api.terradue.com/core/v2/storage/workspaces/${WORKSPACEID}/share', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('terrapi-publish-url', 'string', 'terrapi publish url', 'terrapi publish url', 'https://api.terradue.com/core/v2/services/datacast/cast', '0');
-- RESULT

-- Add unshare url
ALTER TABLE wpsjob ADD COLUMN `unshare_url` varchar(400) NULL DEFAULT NULL;
-- RESULT