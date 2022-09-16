USE $MAIN$;

/*****************************************************************************/

-- Add wpsjob ows_url
ALTER TABLE wpsjob ADD COLUMN `stacitem_url` VARCHAR(400) NULL DEFAULT NULL AFTER `ows_url`;
-- RESULT


