USE $MAIN$;

/*****************************************************************************/

-- Update config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('metrics-job-publish-url', 'string', 'Metrics job publish url', 'Metrics job publish url', 'https://metrics.terradue.com/job/publish', '0');
-- RESULT