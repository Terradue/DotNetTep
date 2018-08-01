USE $MAIN$;

/*****************************************************************************/

-- Adding config...\
ALTER TABLE wpsjob 
ADD COLUMN status int NOT NULL DEFAULT 0 AFTER params;
-- RESULT
