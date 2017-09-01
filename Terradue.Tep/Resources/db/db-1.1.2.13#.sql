USE $MAIN$;

/*****************************************************************************/

-- Adding config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('t2portal-usr-starterPlan', 'string', 't2portal user starter Plan', 't2portal user starter Plan', 'FreeTrial', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('t2portal-usr-explorerPlan', 'string', 't2portal user starter Plan', 't2portal user explorer Plan', 'Explorer', '0');
-- RESULT
