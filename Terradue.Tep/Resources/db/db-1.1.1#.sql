USE $MAIN$;

/*****************************************************************************/

-- Update domain kind ... \
UPDATE domain SET kind=1;
UPDATE type SET keyword='domain' WHERE class='Terradue.Portal.Domain, Terradue.Portal';
-- RESULT
