USE $MAIN$;

/*****************************************************************************/

-- Up domain table...\
ALTER TABLE domain 
ADD COLUMN `contributor` VARCHAR(100) NULL DEFAULT NULL;
ALTER TABLE domain 
ADD COLUMN `contributor_icon_url` VARCHAR(200) NULL DEFAULT NULL;
-- RESULT
