
USE $MAIN$;

/*****************************************************************************/

-- Adding Agent action...\
INSERT INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`, `time_interval`) VALUES ('CleanDeposit', 'Clean accouting Deposit', 'This action set as closed the deposit without any transaction for more than a certain number of days', 'Terradue.Tep.Actions, Terradue.Tep', 'CleanDeposit',1,'1D');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('accounting-deposit-maxDays', 'double', 'accounting deposit max days lifetime', 'accounting deposit max days lifetime', '30', '1');
-- RESULT

