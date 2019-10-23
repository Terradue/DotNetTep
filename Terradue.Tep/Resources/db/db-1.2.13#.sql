USE $MAIN$;

/*****************************************************************************/

-- Update config...\
UPDATE config SET value='Dear Content Authority,\n\nthe user $(USERNAME) ($(USERMAIL)) has requested to Join the community $(COMMUNITY), with the following description:\n\n\t$(USER_REQUEST)\n\nYou can approve or refuse this request here $(LINK).\n\nYou can request information about the requesting user project to the Platform Operator.\n\nBest Regards' WHERE name='CommunityJoinEmailBody';
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityDetailPageUrl', 'string', 'Community detailed page url', '', '', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityJoinConfirmationEmailSubject', 'string', 'Community Join confirmation email subject', '', '[$(SITENAME)] - Join community $(COMMUNITY)', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityJoinConfirmationEmailBody', 'string', 'Community Join confirmation email body', '', 'Dear user,\n\nyou have been added as member of the community $(COMMUNITY).\nYou can access it directly from this link: $(LINK).\n\nBest Regards', '0');
-- RESULT
