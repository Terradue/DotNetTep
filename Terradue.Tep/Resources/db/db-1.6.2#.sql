USE $MAIN$;

/*****************************************************************************/

-- Add config
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPendingEmailSubject', 'string', 'Email subject to notify user is pending in community', 'Email subject to notify user is pending in community', '[$(SITENAME)] - Your request to join the community $(COMMUNITY)', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPendingEmailBody', 'string', 'Email template to notify user is pending in community', 'Email subject to notify user is pending in community', 'Dear user,\n\nyou are receiving this email because you have requested to join the $(SITENAME_SHORT) community $(COMMUNITY).\n\nYour request is being reviewed by the Content Authority.\n\nYou will receive a notification email indicating the outcome of this review.\n\nBest Regards', '0');
UPDATE config SET value='[$(SITENAME)] - User request to join the community $(COMMUNITY)' WHERE name='CommunityJoinEmailSubject';
UPDATE config SET value='Dear Content Authority,\n\nthe user $(USERNAME) ($(USERMAIL)) has requested to Join the community $(COMMUNITY), with the following description:\n\n    $(USER_REQUEST)\n\nPlease approve or deny this request here $(LINK).\n\nYou can ask for additional information about the service subscription status of the requesting user by replying to this email.\n\nBest Regards' WHERE name='CommunityJoinEmailBody';
UPDATE config SET value='[$(SITENAME)] - Your access to the community $(COMMUNITY)' WHERE name='CommunityJoinConfirmationEmailSubject';
UPDATE config SET value='Dear user,\n\nyou have been added as member of the community $(COMMUNITY).\nYou can access it directly from this link: $(LINK).\n\nBest Regards' WHERE name='CommunityJoinConfirmationEmailBody';
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPendingRemoveEmailSubject', 'string', 'Email subject to notify user pending has been denied from community', 'Email subject to notify user pending has been denied from community', '[$(SITENAME)] - You cannot join the community $(COMMUNITY)', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('CommunityPendingRemoveEmailBody', 'string', 'Email template to notify user pending has been denied from community', 'Email subject to notify user pending has been denied from community', 'Dear user,\n\nyou have requested to join the community $(COMMUNITY).\nUnfortunately, your request was cancelled by a $(SITENAME_SHORT) Content Authority for the following reason:\n $(REASON).\n\nBest Regards', '0');
UPDATE config SET value='[$(SITENAME)] - You have been added to the community $(COMMUNITY)' WHERE name='CommunityJoinConfirmationEmailSubject';
UPDATE config SET value='Dear user,\n\nyou are receiving this email because you are a registered $(SITENAME_SHORT) user with an active subscription.\n\nAccordingly to your Application Scenario $(LINK), you have been added by a GEP Content Authority as member of the community $(COMMUNITY).\nYou can access it directly from this link: $(LINK).\n\nBest Regards' WHERE name='CommunityJoinConfirmationEmailBody';
UPDATE config SET value='[$(SITENAME)] - End of your access to the community $(COMMUNITY)' WHERE name='CommunityRemoveEmailSubject';
UPDATE config SET value='Dear user,\n\nyou have been removed by a $(SITENAME_SHORT) Content Authority from the community $(COMMUNITY) for the following reason:\n $(REASON).\n\nYou can still access any public community on $(SITENAME_SHORT).\n\nAccess to processing services and to your past processing job results is conditioned by the status of your Application Scenario $(LINK).\n\nBest Regards' WHERE name='CommunityRemoveEmailBody';
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('asd_link', 'string', 'ASD page link', 'ASD page link', 'https://geohazards-tep.eu/#!settings/asd', '0');
-- RESULT


