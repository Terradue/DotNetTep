USE $MAIN$;

/*****************************************************************************/

-- Add privileges for thematic applications to Content Authority and End User roles ... \
INSERT INTO role_priv (id_role, id_priv) 
SELECT r.id, p.id FROM role AS r INNER JOIN priv AS p WHERE r.identifier IN ('enduser', 'manager') AND p.identifier IN ('app_cache-s', 'app_cache-v');
-- RESULT

/*****************************************************************************/
