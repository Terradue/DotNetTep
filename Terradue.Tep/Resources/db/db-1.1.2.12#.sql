USE $MAIN$;

/*****************************************************************************/



-- Adding unique index...\
ALTER TABLE resourceset 
CHANGE COLUMN `name` `name` VARCHAR(200) NULL DEFAULT NULL ;
ALTER TABLE resourceset 
ADD UNIQUE INDEX `uq_name_usr` (`id_usr` ASC, `name` ASC);
-- RESULT
