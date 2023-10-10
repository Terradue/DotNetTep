USE $MAIN$;

/*****************************************************************************/

-- Add keycloak auth...\
INSERT INTO auth (`identifier`, `name`, `description`, `type`, `enabled`, `activation_rule`, `normal_rule`, `refresh_period`) VALUES ('keycloak', 'keycloak authentication', 'keycloak authentication', 'Terradue.Tep.Controller.Auth.KeycloakAuthenticationType, Terradue.Tep.WebServer', '0', '2', '2', '0');
-- RESULT


