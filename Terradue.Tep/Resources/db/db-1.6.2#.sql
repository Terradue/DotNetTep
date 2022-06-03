USE $MAIN$;

/*****************************************************************************/

-- Add config
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPendingEmailBody', 'string', 'Email template to notify user is pending in community', 'Email subject to notify user is pending in community', 'Dear user,\n\nyou have requested to join the community $(COMMUNITY).\n\nYour request is being processed, please wait for the Content Authority to approve it.\nYou will receive a notification email once done.\n\nBest Regards', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPendingEmailSubject', 'string', 'Email subject to notify user is pending in community', 'Email subject to notify user is pending in community', '[$(SITENAME)] - Join community $(COMMUNITY)', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPendingRemoveEmailBody', 'string', 'Email template to notify user pending has been denied from community', 'Email subject to notify user pending has been denied from community', 'Dear user,\n\nyou have requested to join the community $(COMMUNITY).\nUnfortunately, we cannot proceed for the following reason:\n $(REASON).\n\nWith our deepest apologies.\nBest Regards', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPendingRemoveEmailSubject', 'string', 'Email subject to notify user pending has been denied from community', 'Email subject to notify user pending has been denied from community', '[$(SITENAME)] - Join community $(COMMUNITY)', '0');
UPDATE config SET value= 'Dear user,\n\nyou have been removed from the community $(COMMUNITY) for the following reason:\n $(REASON).\n\nBest Regards' WHERE name = 'CommunityRemoveEmailBody';
-- RESULT


