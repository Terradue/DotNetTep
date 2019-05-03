USE $MAIN$;

/*****************************************************************************/

-- Add app-cache privs...\
SET @type_id = (SELECT id FROM type WHERE class='Terradue.Tep.ThematicApplicationCached, Terradue.Tep');
SET @priv_pos = (SELECT MAX(pos) FROM priv);
INSERT IGNORE INTO priv (id_type, identifier, operation, pos, name, enable_log) VALUES
    (@type_id, 'appcache-p','p', @priv_pos + 1, 'ThematicApplicationCached: make public', 1),
	(@type_id, 'appcache-v','v', @priv_pos + 2, 'ThematicApplicationCached: view', 1),
	(@type_id, 'appcache-c','c', @priv_pos + 3, 'ThematicApplicationCached: create', 1),
	(@type_id, 'appcache-m','m', @priv_pos + 4, 'ThematicApplicationCached: update', 1),
	(@type_id, 'appcache-M','M', @priv_pos + 5, 'ThematicApplicationCached: manage', 1),
	(@type_id, 'appcache-d','d', @priv_pos + 6, 'ThematicApplicationCached: delete', 1),
	(@type_id, 'appcache-s','s', @priv_pos + 7, 'ThematicApplicationCached: search', 1)
;
-- RESULT


