USE $MAIN$;

/*****************************************************************************/

-- Add wps config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('wpsDefaultValue_T2ResultsAnalysis', 'string', 'default value for wps input _T2ResultsAnalysis', '', 'extended', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('catalog-communityIndex', 'string', 'catalog public community Index', '', '', '0');
-- RESULT

-- Add wpsjob ows_url
ALTER TABLE wpsjob ADD COLUMN `ows_url` VARCHAR(400) NULL DEFAULT NULL AFTER `status_url`;
-- RESULT

-- disable action wpsSynchro
UPDATE action SET enabled='0' WHERE identifier='wpsSynchro';
-- RESULT