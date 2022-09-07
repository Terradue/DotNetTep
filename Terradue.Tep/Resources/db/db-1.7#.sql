USE $MAIN$;

/*****************************************************************************/

-- Add user credit
ALTER TABLE usr ADD COLUMN `credit` DOUBLE 0 DEFAULT 0;
-- RESULT

-- Add service price
ALTER TABLE service_store ADD COLUMN `price` DOUBLE 0 DEFAULT 0;
-- RESULT