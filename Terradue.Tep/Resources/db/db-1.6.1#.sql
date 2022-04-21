USE $MAIN$;

/*****************************************************************************/

-- Adding config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('path.files', 'string', 'Tells the files directory path', 'Tells the files directory path', '/usr/local/gep/webserver/sites/gep/root/files', '1');
-- RESULT
