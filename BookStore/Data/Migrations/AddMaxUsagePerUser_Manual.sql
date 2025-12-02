-- Manual SQL script to add MaxUsagePerUser column to Coupons table
-- Run this script in SQL Server Management Studio or through your database connection

ALTER TABLE [Coupons]
ADD [MaxUsagePerUser] int NULL;

GO

