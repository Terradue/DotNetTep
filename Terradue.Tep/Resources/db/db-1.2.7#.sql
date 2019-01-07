USE $MAIN$;

/*****************************************************************************/

-- Add analytics config...\
INSERT INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('analytics_nbtopusedservices', 'int', 'nb of top services to show in analytics', '', '5', '0');
-- RESULT

