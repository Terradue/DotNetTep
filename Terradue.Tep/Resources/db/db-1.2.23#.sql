USE $MAIN$;

/*****************************************************************************/

-- Adding config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-notoken-endsession-enabled', 'bool', 'Tells if we enable the end of session in case of sso token not valid anymore', 'Tells if we enable the end of session in case of sso token not valid anymore', 'true', '1');
-- RESULT
