USE $MAIN$;

/*****************************************************************************/

-- Add service price
ALTER TABLE service_store ADD COLUMN `price` DOUBLE 0 DEFAULT 0;
ALTER TABLE service_store ADD COLUMN `price_input` DOUBLE 0 DEFAULT 0;
-- RESULT

-- Add table asd
CREATE TABLE asd (
    id int unsigned NOT NULL auto_increment,    
    id_usr int unsigned COMMENT 'FK: User',
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',    
    name varchar(200) NOT NULL COMMENT 'asd name',    
    startdate datetime NOT NULL COMMENT 'asd start date',
    enddate datetime NOT NULL COMMENT 'asd end date',
    credit int NOT NULL DEFAULT 0 COMMENT 'Total asd credit',
    used int NOT NULL DEFAULT 0 COMMENT 'asd credit used',
    CONSTRAINT pk_asd PRIMARY KEY (id),
    CONSTRAINT fk_asd_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'Application Subscription Description';
-- RESULT

-- Add table asd_perm
CREATE TABLE asd_perm (
    id_asd int unsigned NOT NULL COMMENT 'FK: asd set',
    id_usr int unsigned COMMENT 'FK: User',    
    CONSTRAINT fk_asd_perm_asd FOREIGN KEY (id_asd) REFERENCES asd(id) ON DELETE CASCADE,
    CONSTRAINT fk_asd_perm_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE    
) Engine=InnoDB COMMENT 'User permissions on asd';
-- RESULT