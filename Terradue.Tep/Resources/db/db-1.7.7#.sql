USE $MAIN$;

/*****************************************************************************/

-- Updatea asd table
ALTER TABLE asd ADD COLUMN `overspending` boolean NOT NULL default false;
-- RESULT