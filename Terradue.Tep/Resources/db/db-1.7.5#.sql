USE $MAIN$;

/*****************************************************************************/

-- Add config
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('MonthlyInactiveUserAlertSubject', 'string', 'Email subject for inactive users (monthly report)', 'Email subject for inactive users (monthly report)', '[$(SITENAME)] - Inactive users report', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('MonthlyInactiveUserAlertBody', 'string', 'Email body for inactive users (monthly report)', 'Email body for inactive users (monthly report)', 'Dear support,\n\nThis is an automatic email to inform you that the following users have been inactive in the past 30 days, having an active ASD:\n\n$(RECORDS)', '0');
-- RESULT

-- Adding Agent action...\
INSERT IGNORE INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`) VALUES ('inactiveUserReport', 'Report inactive users', 'This action report inactive users for the last month', 'Terradue.Tep.Actions, Terradue.Tep', 'MonthlyInactiveUserAlert',0);
-- RESULT


