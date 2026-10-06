-- Execute interactively as the collector identity. All statements are reads.
SELECT DB_NAME() AS connected_database, ORIGINAL_LOGIN() AS connected_login,
       IS_SRVROLEMEMBER('sysadmin') AS is_sysadmin, IS_MEMBER('db_owner') AS is_db_owner,
       HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CONTROL') AS controls_database,
       HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'ALTER') AS can_alter_database;
SELECT * FROM sys.fn_my_permissions(NULL, 'DATABASE');
SELECT * FROM sys.fn_my_permissions(NULL, 'SERVER');
-- Expected: no sysadmin, db_owner, CONTROL or ALTER. Review inherited permissions,
-- memberships and object-specific grants with your DBA as well.
