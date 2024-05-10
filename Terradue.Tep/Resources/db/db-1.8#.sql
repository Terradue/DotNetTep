USE $MAIN$;

/*****************************************************************************/

SET @type_id = (SELECT id FROM type WHERE class='Terradue.Tep.ThematicApplicationCached, Terradue.Tep');

-- Add privileges for thematic applications ... \
SET @priv_pos = (SELECT MAX(pos) FROM priv);
INSERT INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'app_cache-s', 's', @priv_pos + 1, 'Thematic application: search', 1),
    (@type_id, 'app_cache-v', 'v', @priv_pos + 2, 'Thematic application: view', 1),
    (@type_id, 'app_cache-u', 'u', @priv_pos + 3, 'Thematic application: use', 1)
;


/*****************************************************************************/

-- Add privileges to owner role ...\
SET @role_id = (SELECT id FROM role WHERE identifier='owner');
INSERT INTO role_priv (id_role, id_priv) SELECT @role_id, id FROM priv WHERE identifier IN (
    'app_cached-v',
    'app_cached-s'
);
-- RESULT
 

-- Add privileges to starter role ...\
SET @role_id = (SELECT id FROM role WHERE identifier='starter');
INSERT INTO role_priv (id_role, id_priv) SELECT @role_id, id FROM priv WHERE identifier IN (
    'app_cached-v',
    'app_cached-s'
);
-- RESULT
 
-- Add privileges to explorer role ...\
SET @role_id = (SELECT id FROM role WHERE identifier='explorer');
INSERT INTO role_priv (id_role, id_priv) SELECT @role_id, id FROM priv WHERE identifier IN (
    'app_cached-v',
    'app_cached-s'
);
-- RESULT

/*****************************************************************************/
