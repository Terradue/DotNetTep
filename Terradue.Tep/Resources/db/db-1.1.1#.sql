USE $MAIN$;

/*****************************************************************************/

-- Update domain kind ... \
UPDATE domain SET kind=1;
UPDATE type SET keyword='domain' WHERE class='Terradue.Portal.Domain, Terradue.Portal';
-- RESULT

CREATE TABLE rolegrant_pending (
    id_usr int unsigned COMMENT 'FK: User (id_usr or id_grp must be set)',
    id_grp int unsigned COMMENT 'FK: Group (id_usr or id_grp must be set)',
    id_role int unsigned NOT NULL COMMENT 'FK: Role to which the user/group is assigned',
    id_domain int unsigned COMMENT 'FK: Domain for which the user/group has the role',
    key varchar(50) COMMENT 'Access key',
    CONSTRAINT fk_rolegrant_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_rolegrant_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE,
    CONSTRAINT fk_rolegrant_role FOREIGN KEY (id_role) REFERENCES role(id) ON DELETE CASCADE,
    CONSTRAINT fk_rolegrant_domain FOREIGN KEY (id_domain) REFERENCES domain(id) ON DELETE CASCADE
) Engine=InnoDB COMMENT 'Assignments of users/groups to roles for domains';