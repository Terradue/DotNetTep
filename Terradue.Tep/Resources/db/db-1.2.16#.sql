USE $MAIN$;

/*****************************************************************************/

-- Update config...\
UPDATE config set name = 'a2shpcmpic-token' WHERE name = 'a2shpc-token';
UPDATE config set name = 'a2shpcmpic-sync-url' WHERE name = 'a2shpc-sync-url';
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('a2shpcdsmopt-token', 'string', 'A2s HPC sync user token', 'A2s HPC sync user token (DSM OPT)', '', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('a2shpcdsmopt-sync-url', 'string', 'A2s HPC sync user sync url', 'A2s HPC sync user sync url (DSM OPT)', '', '0');
-- RESULT