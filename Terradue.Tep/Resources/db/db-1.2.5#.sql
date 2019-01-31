USE $MAIN$;

/*****************************************************************************/

-- Add t2 sso config...\
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('cookieID-SID', 'string', 'cookie identifier - SID', '', 'oauth_sid', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('cookieID-SUBSID', 'string', 'cookie identifier - SUBSID', '', 'oauth_sub_sid', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('cookieID-token-access', 'string', 'cookie identifier - Token access', '', 'oauthtoken_access', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('cookieID-token-refresh', 'string', 'cookie identifier - Token refresh', '', 'oauthtoken_refresh', '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-clientId', 'string', 'Terradue SSO Client Id', 'Enter the value of the client identifier of the Terradue SSO', "TBD", '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-clientSecret', 'string', 'Terradue SSO Client Secret', 'Enter the value of the client secret password of the Terradue SSO', "TBD", '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-userInfoEndpoint', 'string', 'Terradue SSO User Info Endpoint url', 'Enter the value of the url of the User Info Endpoint of the Terradue SSO', "https://sso.terradue.com/c2id/userinfo", '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-scopes', 'string', 'Terradue SSO default scopes', 'Enter the value of the default scopes of the Terradue SSO', "openid,profile", '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-callback', 'string', 'Terradue SSO callback url', 'Enter the value of the callback url of the Terradue SSO', "https://geohazards-tep.eo.esa.int/t2api/cb", '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-authEndpoint', 'string', 'Terradue SSO Authentication Endpoint url', 'Enter the value of the url of the Authentication Endpoint of the Terradue SSO', "https://sso.terradue.com/c2id/authz-sessions/rest/v2", '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-sessionEndpoint', 'string', 'Terradue SSO Session Endpoint url', 'Enter the value of the url of the Session Endpoint of the Terradue SSO', "https://sso.terradue.com/c2id/session-store/rest/v2/sessions", '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('oauth-authEndpoint', 'string', 'Terradue Portal Authentication Endpoint url', 'Enter the value of the url of the Authentication Endpoint of the Terradue SSO', "https://www.terradue.com/t2api/oauth", '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-configUrl', 'string', 'Terradue SSO Configuration url', 'Enter the value of the url of the Configuration of the Terradue SSO', "https://sso.terradue.com/c2id//.well-known/openid-configuration", '0');
INSERT IGNORE INTO config (`name`, `type`, `caption`, `hint`, `value`, `optional`) VALUES ('sso-apiAccessToken', 'string', 'Terradue SSO API Access Token', 'Enter the value of the API Access token of the Terradue SSO', "TBD", '0');
-- RESULT

-- Update LDAP auth...\
UPDATE auth SET type='Terradue.Tep.TepLdapAuthenticationType, Terradue.Tep', activation_rule='3' WHERE identifier='ldap';
-- RESULT
