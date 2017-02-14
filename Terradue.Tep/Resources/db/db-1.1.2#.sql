
USE $MAIN$;

/*****************************************************************************/

-- Update roles ... \
INSERT INTO role (identifier, name, description) VALUES ('member', 'member', 'Community default member');
INSERT INTO role (identifier, name, description) VALUES ('manager', 'manager', 'Community manager');
INSERT INTO role (identifier, name, description) VALUES ('pending', 'pending', 'Community pending user');
-- RESULT

