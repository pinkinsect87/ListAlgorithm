/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

:r .\Data\Data.Run_First.sql
:r .\Data\Data.dbo.Countries.sql
:r .\Data\Data.dbo.TemplateType.sql
:r .\Data\Data.dbo.ListAlgorithmTemplate.sql
:r .\Data\Data.dbo.Affiliates.sql
:r .\Data\Data.dbo.Status.sql
:r .\Data\Data.dbo.Segment.sql
:r .\Data\Data.Run_Last.sql
