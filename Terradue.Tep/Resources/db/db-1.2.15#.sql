USE $MAIN$;

/*****************************************************************************/

-- Add community sync identifier
ALTER TABLE domain ADD COLUMN `usersync_identifier` VARCHAR(50) NULL DEFAULT NULL;
-- RESULT

-- Update config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('a2shpc-token', 'string', 'A2s HPC sync user token', 'A2s HPC sync user token', '', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('a2shpc-sync-url', 'string', 'A2s HPC sync user sync url', 'A2s HPC sync user sync url', '', '0');
-- RESULT