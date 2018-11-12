USE $MAIN$;

/*****************************************************************************/

-- Adding config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-api-baseurl', 'string', 'jira-api-baseurl', 'jira-api-baseurl', 'https://helpdesk.terradue.com', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-api-username', 'string', 'jira-api-username', 'jira-api-username', 'TBD', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-api-password', 'string', 'jira-api-password', 'jira-api-password', 'TBD', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-helpdesk-serviceDeskId', 'string', 'jira-helpdesk-serviceDeskId', 'jira-helpdesk-serviceDeskId', '17', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-helpdesk-requestTypeId', 'string', 'jira-helpdesk-requestTypeId', 'jira-helpdesk-requestTypeId', '165', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-helpdesk-components', 'string', 'jira-helpdesk-components', 'jira-helpdesk-components', 'GEP', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-helpdesk-labels', 'string', 'jira-helpdesk-labels', 'jira-helpdesk-labels', 'GEP', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('jira-helpdesk-customfield-ProcessingServiceLabel', 'string', 'jira-helpdesk-customfield-ProcessingServiceLabel', 'jira-helpdesk-customfield-ProcessingServiceLabel', 'customfield_10237', '1');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('wpsjob-support-mode', 'string', 'wpsjob-support-mode', 'wpsjob-support-mode', 'jira', '1');
-- RESULT
