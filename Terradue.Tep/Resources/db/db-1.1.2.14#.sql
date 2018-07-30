USE $MAIN$;

/*****************************************************************************/

-- Adding config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('action-jobPoolSize', 'int', 'Actions job pool size', 'Actions job pool size', '100', '0');
-- RESULT


-- Adding wpsjob nbresults...\
ALTER TABLE wpsjob ADD COLUMN nbresults INT(10) NULL DEFAULT -1;
-- RESULT

-- Adding action...\
INSERT INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`, `time_interval`) VALUES ('RefreshWpsjobStatus', 'Refresh wpsjob status', 'This action refresh the status of ongoing wps jobs', 'Terradue.Tep.Actions, Terradue.Tep', 'RefreshWpsjobStatus',1,'2h');
INSERT INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`, `time_interval`) VALUES ('RefreshWpsjobResultNb', 'Refresh wpsjob nb results', 'This action refresh the nb of results of wps jobs for which nb results is not set', 'Terradue.Tep.Actions, Terradue.Tep', 'RefreshWpsjobResultNb',1,'2h');
-- RESULT