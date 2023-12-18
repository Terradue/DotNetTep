USE $MAIN$;

/*****************************************************************************/

-- Add config
UPDATE config SET value = 'job_id,job_identifier,job_status,job_creation,job_store_url,job_nbinput,job_wps,job_shared,usr_username,usr_email,usr_affiliation,usr_level,usr_creation,usr_login,job_end,Prices,job_status_url,job_stack_name,job_app,job_title' WHERE (name = 'agent-jobreport-headerfile');
-- RESULT


