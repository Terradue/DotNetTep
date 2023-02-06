USE $MAIN$;

/*****************************************************************************/

-- ADD table service_store
CREATE TABLE IF NOT EXISTS service_store (
    id int unsigned NOT NULL auto_increment,
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    name varchar(100) NOT NULL COMMENT 'service name',
    wps_name varchar(100) NOT NULL COMMENT 'service wps name',
    description TEXT NULL DEFAULT NULL COMMENT 'service description',
    abstract varchar(500) NULL DEFAULT NULL COMMENT 'service description',
    pack varchar(100) NOT NULL COMMENT 'service pack',
    level varchar(200) NOT NULL COMMENT 'service level',
    link varchar(200) NULL DEFAULT NULL COMMENT 'service link',
    price DOUBLE NOT NULL DEFAULT 0 COMMENT 'service price',
    price_input DOUBLE NOT NULL DEFAULT 0 COMMENT 'service price per input',
    -- tag varchar(500) NULL DEFAULT NULL COMMENT 'service tag',
    icon_url varchar(300) NULL DEFAULT NULL COMMENT 'service icon url',
    apps varchar(400) NULL DEFAULT NULL COMMENT 'service apps',
    CONSTRAINT pk_service_store PRIMARY KEY (id),
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'service store';
-- RESULT

-- Add table asd
CREATE TABLE asd (
    id int unsigned NOT NULL auto_increment,    
    id_usr int unsigned COMMENT 'FK: User',
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',    
    name varchar(200) NOT NULL COMMENT 'asd name',    
    startdate datetime NOT NULL COMMENT 'asd start date',
    enddate datetime NOT NULL COMMENT 'asd end date',
    credit_total DOUBLE NOT NULL DEFAULT 0 COMMENT 'Total asd credit',
    status int NOT NULL DEFAULT 0 COMMENT 'asd credit used',
    credit_used DOUBLE NOT NULL DEFAULT 0 COMMENT 'asd credit used',
    CONSTRAINT pk_asd PRIMARY KEY (id),
    CONSTRAINT fk_asd_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'Application Subscription Description';
-- RESULT

-- Add table asd_perm
CREATE TABLE asd_perm (
    id_asd int unsigned NOT NULL COMMENT 'FK: asd set',
    id_usr int unsigned COMMENT 'FK: User',    
    id_grp int unsigned COMMENT 'FK: Group',    
    CONSTRAINT fk_asd_perm_asd FOREIGN KEY (id_asd) REFERENCES asd(id) ON DELETE CASCADE,
    CONSTRAINT fk_asd_perm_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    CONSTRAINT fk_asd_perm_grp FOREIGN KEY (id_grp) REFERENCES grp(id) ON DELETE CASCADE    
) Engine=InnoDB COMMENT 'User permissions on asd';
-- RESULT

-- Add config
INSERT IGNORE INTO config (name, type, caption, hint, value, optional) VALUES ('payperuse-enabled', 'boolean', 'payperuse-enabled', 'payperuse-enabled', "true", '0');
-- RESULT
