USE $MAIN$;

/*****************************************************************************/

-- Adding config...\
ALTER TABLE wpsjob ADD COLUMN nbresults INT(11) UNSIGNED NULL;
-- RESULT

-- Adding action...\
INSERT INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`, `time_interval`) VALUES ('RefreshWpsjobStatus', 'Refresh wpsjob status', 'This action refresh the status of ongoing wps jobs', 'Terradue.Tep.Actions, Terradue.Tep', 'RefreshWpsjobStatus',1,'2h');
INSERT INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`, `time_interval`) VALUES ('RefreshWpsjobResultNb', 'Refresh wpsjob nb results', 'This action refresh the nb of results of wps jobs for which nb results is not set', 'Terradue.Tep.Actions, Terradue.Tep', 'RefreshWpsjobResultNb',1,'2h');
-- RESULT