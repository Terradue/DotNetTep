
USE $MAIN$;

/*****************************************************************************/

-- Adding Agent action...\
INSERT INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`) VALUES ('wpsSynchro', 'Synchronize WPS', 'This action synchronize the wps providers stored in db', 'Terradue.Tep.Actions, Terradue.Tep', 'UpdateWpsProviders',1);
-- RESULT