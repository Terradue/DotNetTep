USE $MAIN$;

/*****************************************************************************/

-- Adding Agent action...\
INSERT INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`) VALUES ('JoBStatusPolling', 'WPS job status polling', 'This action regularly checks for wps job status and update it', 'Terradue.Tep.Actions, Terradue.Tep', 'JoBStatusPolling',1);
-- RESULT