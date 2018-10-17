USE $MAIN$;

/*****************************************************************************/

-- Up domain table...\
ALTER TABLE domain 
ADD COLUMN `email_notification` TINYINT(1) UNSIGNED NULL DEFAULT '0';
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityRemoveEmailBody', 'string', 'Email template to notify user has been removed in community', 'Email template to notify user has been removed in community', 'Dear user,\n\nyou have requested to join the community $(COMMUNITY).\nUnfortunately we cannot proceed for the following reason:\n ($(REASON)).\n\nWith our deepest apologies.\nBest Regards', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityRemoveEmailSubject', 'string', 'Email subject to notify user has been removed in community', 'Email subject to notify user has been removed in community', '[$(SITENAME)] - Join community $(COMMUNITY)', '0');
-- RESULT
