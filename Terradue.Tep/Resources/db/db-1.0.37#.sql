USE $MAIN$;

-- Add config
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('EmailConfirmedNotification', 'string', 'Email confirmed notification to support', 'Email confirmed notification to support', 'Dear support,\n\nThis is an automatic email to inform you that user $(USERNAME) has just confirmed his email address ($(EMAIL)) on the TEP platform.\n', '0');
-- RESULT

-- Add log path in config
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('t2portal-usr-defaultPlan', 'string', 'T2 Corporate portal default plan', 'T2 Corporate portal default plan', 'Explorer', 
'0');
-- RESULT

-- resourceset name
ALTER TABLE resourceset MODIFY name VARCHAR(300);
-- RESULT
