USE $MAIN$;

/*****************************************************************************/

-- Adding config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-api-baseurl', 'string', 'jira-api-baseurl', 'jira-api-baseurl', 'https://helpdesk.terradue.com', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-api-username', 'string', 'jira-api-username', 'jira-api-username', 'TBD', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-api-password', 'string', 'jira-api-password', 'jira-api-password', 'TBD', '1');
-- RESULT
