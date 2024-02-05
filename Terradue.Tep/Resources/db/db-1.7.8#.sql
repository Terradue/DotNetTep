USE $MAIN$;

/*****************************************************************************/

-- Updatea asd table
ALTER TABLE service_store
    ADD COLUMN `max_concurrent_inputs` INT NOT NULL DEFAULT 0,
    DROP COLUMN `price_input`,
    ADD COLUMN `price_type` INT NOT NULL DEFAULT 0,
    ADD COLUMN `wps_version` VARCHAR(30) NULL DEFAULT NULL;    
-- RESULT