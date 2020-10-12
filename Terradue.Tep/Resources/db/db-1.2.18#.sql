USE $MAIN$;

/*****************************************************************************/

-- Create Terms Conditions table...\
CREATE TABLE termsconditions (
  identifier VARCHAR(100) NOT NULL,
  id_usr INT UNSIGNED NOT NULL,
  INDEX tc_usr_idx (id_usr ASC),
  CONSTRAINT tc_usr FOREIGN KEY (id_usr) REFERENCES usr (id) ON DELETE CASCADE,
  UNIQUE INDEX `id_usrid_tc` (`identifier`, `id_usr`)
);
-- RESULT
