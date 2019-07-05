USE $MAIN$;

/*****************************************************************************/

-- Add wpsjob app identifier
ALTER TABLE wpsjob ADD COLUMN `app_identifier` VARCHAR(50) NULL DEFAULT NULL;
-- RESULT

-- Update wps process offering custom class
UPDATE type SET custom_class='Terradue.Tep.WpsProcessOfferingTep, Terradue.Tep' WHERE class='Terradue.Portal.WpsProcessOffering, Terradue.Portal';
-- RESULT