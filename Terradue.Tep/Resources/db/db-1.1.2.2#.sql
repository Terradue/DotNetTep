USE $MAIN$;

/*****************************************************************************/

-- Add Rate table ... \
CREATE TABLE rate (
    id int unsigned NOT NULL auto_increment,
    identifier varchar(50) NOT NULL COMMENT 'Unique identifier',
    id_entity int unsigned COMMENT 'Entity associated to the activity',
    id_type int unsigned COMMENT 'Entity type',
    unit int unsigned COMMENT 'rate unit',
    cost int unsigned COMMENT 'rate cost',
    CONSTRAINT pk_rate PRIMARY KEY (id)
) Engine=InnoDB COMMENT 'Accounting rates';
-- RESULT

-- Add Transaction table ...\
CREATE TABLE transaction (
    id int unsigned NOT NULL auto_increment,
    id_usr int unsigned NOT NULL COMMENT 'FK: User',
    reference varchar(50) NULL COMMENT 'Unique identifier',
    id_entity int unsigned COMMENT 'Entity associated to the activity',
    id_type int unsigned COMMENT 'Entity type',    
    log_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Date/time of activity creation',
    id_provider int unsigned NULL COMMENT 'FK: User',
    balance int COMMENT 'transaction balance',
   	deposit boolean NOT NULL DEFAULT false COMMENT 'If true, transaction is a deposit',
    CONSTRAINT pk_transaction PRIMARY KEY (id),
    CONSTRAINT fk_transaction_usr FOREIGN KEY (id_usr) REFERENCES usr(id) ON DELETE CASCADE,
    UNIQUE INDEX (identifier)
) Engine=InnoDB COMMENT 'Accounting transactions';
INSERT INTO type (pos, class, caption_sg, caption_pl, keyword) VALUES (@'0', 'Terradue.Tep.Transaction, Terradue.Tep', 'Transaction', 'Transactions', 'transaction');
-- RESULT

