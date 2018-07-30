USE $MAIN$;

/*****************************************************************************/

-- Add app_cache table...\
CREATE TABLE app_cache (
    id int unsigned NOT NULL auto_increment,
	uid varchar(50) NOT NULL COMMENT 'Unique identifier',
    id_domain int unsigned COMMENT 'FK: Owning domain',
    feed TEXT NOT NULL COMMENT 'app feed',
    last_update datetime COMMENT 'Last update time',
    CONSTRAINT pk_appcache PRIMARY KEY (id),
    CONSTRAINT fk_appcache_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE CASCADE,
    UNIQUE INDEX `uq_uid_domain` (`uid` ASC, `id_domain` ASC)
) Engine=InnoDB COMMENT 'Thematic Apps cache';

INSERT INTO type (class, caption_sg, caption_pl, keyword) VALUES ('Terradue.Tep.ThematicApplicationCached, Terradue.Tep', 'Thematic Apps cached', 'Thematic Apps cached', 'apps');
-- RESULT

-- Adding action...\
INSERT INTO action (`identifier`, `name`, `description`, `class`, `method`, `enabled`, `time_interval`) VALUES ('RefreshThematicAppsCache', 'Refresh thematic apps cached', 'This action refresh the cached thematic apps', 'Terradue.Tep.Actions, Terradue.Tep', 'RefreshThematicAppsCache',1,'1D');
-- RESULT
