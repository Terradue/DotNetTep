USE $MAIN$;

/*****************************************************************************/

-- Adding unique index...\
ALTER TABLE resourceset 
ADD UNIQUE INDEX `unique_name_usr` (`id_usr` ASC, `name` ASC);
-- RESULT
