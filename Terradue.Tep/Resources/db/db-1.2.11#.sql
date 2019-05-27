USE $MAIN$;

/*****************************************************************************/

-- Add config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('artifactory_repo_restriction_deploy', 'bool', 'flag to enable/disable restriction on repository view', '', '1', '0');
-- RESULT


