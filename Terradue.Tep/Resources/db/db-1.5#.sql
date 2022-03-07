USE $MAIN$;

/*****************************************************************************/

-- Adding Agent action...\
INSERT IGNORE INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`, `time_interval`) VALUES ('monthlyJobReport', 'Create Monthly job Report', 'This action creates a job report every month', 'Terradue.Tep.Actions, Terradue.Tep', 'CreateJobMonthlyReport',0,'1M');
-- RESULT

-- Add config
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('agent-jobreport-headerfile', 'string', 'agent job report headerfile', 'agent job report headerfile', "", '0');
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('agent-jobreport-query', 'string', 'agent job report headerfile', 'agent job report headerfile', "", '0');
-- RESULT