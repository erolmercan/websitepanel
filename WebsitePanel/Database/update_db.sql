﻿USE [${install.database}]
GO

-- update database version
DECLARE @build_version nvarchar(10), @build_date datetime
SET @build_version = N'${release.version}'
SET @build_date = '${release.date}T00:00:00' -- ISO 8601 Format (YYYY-MM-DDTHH:MM:SS)

IF NOT EXISTS (SELECT * FROM [dbo].[Versions] WHERE [DatabaseVersion] = @build_version)
BEGIN
	INSERT [dbo].[Versions] ([DatabaseVersion], [BuildDate]) VALUES (@build_version, @build_date)
END
GO


--- Fix on version 2.0
DELETE FROM HostingPlanQuotas WHERE QuotaID = 340
GO
DELETE FROM HostingPlanQuotas WHERE QuotaID = 341
GO
DELETE FROM HostingPlanQuotas WHERE QuotaID = 342
GO
DELETE FROM HostingPlanQuotas WHERE QuotaID = 343
GO
DELETE FROM HostingPlanResources WHERE GroupID = 33
GO


-- Version 2.1 section
IF NOT EXISTS (SELECT * FROM [dbo].[Providers] WHERE [DisplayName] = 'Hosted Microsoft Exchange Server 2013')
BEGIN
INSERT [dbo].[Providers] ([ProviderId], [GroupId], [ProviderName], [DisplayName], [ProviderType], [EditorControl], [DisableAutoDiscovery]) VALUES(91, 12, N'Exchange2013', N'Hosted Microsoft Exchange Server 2013', N'WebsitePanel.Providers.HostedSolution.Exchange2013, WebsitePanel.Providers.HostedSolution.Exchange2013', N'Exchange',	1)
END
ELSE
BEGIN
UPDATE [dbo].[Providers] SET [DisableAutoDiscovery] = NULL WHERE [DisplayName] = 'Hosted Microsoft Exchange Server 2013'
END
GO


IF NOT EXISTS (SELECT * FROM [dbo].[Quotas] WHERE [QuotaName] = 'Exchange2007.AllowLitigationHold')
BEGIN
INSERT [dbo].[Quotas]  ([QuotaID], [GroupID],[QuotaOrder], [QuotaName], [QuotaDescription], [QuotaTypeID], [ServiceQuota], [ItemTypeID]) VALUES (420, 12, 24,N'Exchange2007.AllowLitigationHold',N'Allow Litigation Hold',1, 0 , NULL)
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Quotas] WHERE [QuotaName] = 'Exchange2007.RecoverableItemsSpace')
BEGIN
INSERT [dbo].[Quotas]  ([QuotaID], [GroupID],[QuotaOrder], [QuotaName], [QuotaDescription], [QuotaTypeID], [ServiceQuota], [ItemTypeID]) VALUES (421, 12, 25,N'Exchange2007.RecoverableItemsSpace',N'Recoverable Items Space',2, 0 , NULL)
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[ResourceGroups] WHERE [GroupName] = 'HeliconZoo')
BEGIN
INSERT [dbo].[ResourceGroups] ([GroupID], [GroupName], [GroupOrder], [GroupController], [ShowGroup]) VALUES (42, N'HeliconZoo', 2, N'WebsitePanel.EnterpriseServer.HeliconZooController', 1)
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Providers] WHERE [ProviderName] = 'HeliconZoo')
BEGIN
INSERT [dbo].[Providers] ([ProviderID], [GroupID], [ProviderName], [DisplayName], [ProviderType], [EditorControl], [DisableAutoDiscovery]) VALUES (135, 42, N'HeliconZoo', N'Web Application Engines', N'WebsitePanel.Providers.Web.HeliconZoo.HeliconZoo, WebsitePanel.Providers.Web.HeliconZoo', N'HeliconZoo', NULL)
END
GO

IF NOT EXISTS(select 1 from sys.columns COLS INNER JOIN sys.objects OBJS ON OBJS.object_id=COLS.object_id and OBJS.type='U' AND OBJS.name='ExchangeMailboxPlans' AND COLS.name='AllowLitigationHold')
BEGIN
ALTER TABLE [dbo].[ExchangeMailboxPlans] ADD
	[AllowLitigationHold] [bit] NULL,
	[RecoverableItemsWarningPct] [int] NULL,
	[RecoverableItemsSpace] [int] NULL
END
GO

IF NOT EXISTS(select 1 from sys.columns COLS INNER JOIN sys.objects OBJS ON OBJS.object_id=COLS.object_id and OBJS.type='U' AND OBJS.name='ExchangeMailboxPlans' AND COLS.name='LitigationHoldUrl')
BEGIN
ALTER TABLE [dbo].[ExchangeMailboxPlans] ADD
	[LitigationHoldUrl] [nvarchar] (256) COLLATE Latin1_General_CI_AS NULL,
	[LitigationHoldMsg] [nvarchar] (512) COLLATE Latin1_General_CI_AS NULL
END
GO




ALTER PROCEDURE [dbo].[AddExchangeMailboxPlan] 
(
	@MailboxPlanId int OUTPUT,
	@ItemID int,
	@MailboxPlan	nvarchar(300),
	@EnableActiveSync bit,
	@EnableIMAP bit,
	@EnableMAPI bit,
	@EnableOWA bit,
	@EnablePOP bit,
	@IsDefault bit,
	@IssueWarningPct int,
	@KeepDeletedItemsDays int,
	@MailboxSizeMB int,
	@MaxReceiveMessageSizeKB int,
	@MaxRecipients int,
	@MaxSendMessageSizeKB int,
	@ProhibitSendPct int,
	@ProhibitSendReceivePct int	,
	@HideFromAddressBook bit,
	@MailboxPlanType int,
	@AllowLitigationHold bit,
	@RecoverableItemsWarningPct int,
	@RecoverableItemsSpace int,
	@LitigationHoldUrl nvarchar(256),
	@LitigationHoldMsg nvarchar(512)
)
AS

IF (((SELECT Count(*) FROM ExchangeMailboxPlans WHERE ItemId = @ItemID) = 0) AND (@MailboxPlanType=0))
BEGIN
	SET @IsDefault = 1
END
ELSE
BEGIN
	IF ((@IsDefault = 1) AND (@MailboxPlanType=0))
	BEGIN
		UPDATE ExchangeMailboxPlans SET IsDefault = 0 WHERE ItemID = @ItemID
	END
END

INSERT INTO ExchangeMailboxPlans
(
	ItemID,
	MailboxPlan,
	EnableActiveSync,
	EnableIMAP,
	EnableMAPI,
	EnableOWA,
	EnablePOP,
	IsDefault,
	IssueWarningPct,
	KeepDeletedItemsDays,
	MailboxSizeMB,
	MaxReceiveMessageSizeKB,
	MaxRecipients,
	MaxSendMessageSizeKB,
	ProhibitSendPct,
	ProhibitSendReceivePct,
	HideFromAddressBook,
	MailboxPlanType,
	AllowLitigationHold,
	RecoverableItemsWarningPct,
	RecoverableItemsSpace,
	LitigationHoldUrl,
	LitigationHoldMsg

)
VALUES
(
	@ItemID,
	@MailboxPlan,
	@EnableActiveSync,
	@EnableIMAP,
	@EnableMAPI,
	@EnableOWA,
	@EnablePOP,
	@IsDefault,
	@IssueWarningPct,
	@KeepDeletedItemsDays,
	@MailboxSizeMB,
	@MaxReceiveMessageSizeKB,
	@MaxRecipients,
	@MaxSendMessageSizeKB,
	@ProhibitSendPct,
	@ProhibitSendReceivePct,
	@HideFromAddressBook,
	@MailboxPlanType,
	@AllowLitigationHold,
	@RecoverableItemsWarningPct,
	@RecoverableItemsSpace,
	@LitigationHoldUrl,
	@LitigationHoldMsg
)

SET @MailboxPlanId = SCOPE_IDENTITY()

RETURN

GO








ALTER PROCEDURE [dbo].[UpdateExchangeMailboxPlan] 
(
	@MailboxPlanId int,
	@MailboxPlan	nvarchar(300),
	@EnableActiveSync bit,
	@EnableIMAP bit,
	@EnableMAPI bit,
	@EnableOWA bit,
	@EnablePOP bit,
	@IsDefault bit,
	@IssueWarningPct int,
	@KeepDeletedItemsDays int,
	@MailboxSizeMB int,
	@MaxReceiveMessageSizeKB int,
	@MaxRecipients int,
	@MaxSendMessageSizeKB int,
	@ProhibitSendPct int,
	@ProhibitSendReceivePct int	,
	@HideFromAddressBook bit,
	@MailboxPlanType int,
	@AllowLitigationHold bit,
	@RecoverableItemsWarningPct int,
	@RecoverableItemsSpace int,
	@LitigationHoldUrl nvarchar(256),
	@LitigationHoldMsg nvarchar(512)
)
AS

UPDATE ExchangeMailboxPlans SET
	MailboxPlan = @MailboxPlan,
	EnableActiveSync = @EnableActiveSync,
	EnableIMAP = @EnableIMAP,
	EnableMAPI = @EnableMAPI,
	EnableOWA = @EnableOWA,
	EnablePOP = @EnablePOP,
	IsDefault = @IsDefault,
	IssueWarningPct= @IssueWarningPct,
	KeepDeletedItemsDays = @KeepDeletedItemsDays,
	MailboxSizeMB= @MailboxSizeMB,
	MaxReceiveMessageSizeKB= @MaxReceiveMessageSizeKB,
	MaxRecipients= @MaxRecipients,
	MaxSendMessageSizeKB= @MaxSendMessageSizeKB,
	ProhibitSendPct= @ProhibitSendPct,
	ProhibitSendReceivePct = @ProhibitSendReceivePct,
	HideFromAddressBook = @HideFromAddressBook,
	MailboxPlanType = @MailboxPlanType,
	AllowLitigationHold = @AllowLitigationHold,
	RecoverableItemsWarningPct = @RecoverableItemsWarningPct,
	RecoverableItemsSpace = @RecoverableItemsSpace, 
	LitigationHoldUrl = @LitigationHoldUrl,
	LitigationHoldMsg = @LitigationHoldMsg

WHERE MailboxPlanId = @MailboxPlanId

RETURN
	
GO



ALTER PROCEDURE [dbo].[GetExchangeMailboxPlan] 
(
	@MailboxPlanId int
)
AS
SELECT
	MailboxPlanId,
	ItemID,
	MailboxPlan,
	EnableActiveSync,
	EnableIMAP,
	EnableMAPI,
	EnableOWA,
	EnablePOP,
	IsDefault,
	IssueWarningPct,
	KeepDeletedItemsDays,
	MailboxSizeMB,
	MaxReceiveMessageSizeKB,
	MaxRecipients,
	MaxSendMessageSizeKB,
	ProhibitSendPct,
	ProhibitSendReceivePct,
	HideFromAddressBook,
	MailboxPlanType,
	AllowLitigationHold,
	RecoverableItemsWarningPct,
	RecoverableItemsSpace,
	LitigationHoldUrl,
	LitigationHoldMsg
FROM
	ExchangeMailboxPlans
WHERE
	MailboxPlanId = @MailboxPlanId
RETURN

GO



ALTER PROCEDURE [dbo].[GetExchangeOrganizationStatistics] 
(
	@ItemID int
)
AS

IF -1 IN (SELECT B.MailboxSizeMB FROM ExchangeAccounts AS A INNER JOIN ExchangeMailboxPlans AS B ON A.MailboxPlanId = B.MailboxPlanId WHERE A.ItemID=@ItemID)
BEGIN
SELECT
	(SELECT COUNT(*) FROM ExchangeAccounts WHERE (AccountType = 1 OR AccountType = 5 OR AccountType = 6) AND ItemID = @ItemID) AS CreatedMailboxes,
	(SELECT COUNT(*) FROM ExchangeAccounts WHERE AccountType = 2 AND ItemID = @ItemID) AS CreatedContacts,
	(SELECT COUNT(*) FROM ExchangeAccounts WHERE AccountType = 3 AND ItemID = @ItemID) AS CreatedDistributionLists,
	(SELECT COUNT(*) FROM ExchangeAccounts WHERE AccountType = 4 AND ItemID = @ItemID) AS CreatedPublicFolders,
	(SELECT COUNT(*) FROM ExchangeOrganizationDomains WHERE ItemID = @ItemID) AS CreatedDomains,
	(SELECT MIN(B.MailboxSizeMB) FROM ExchangeAccounts AS A INNER JOIN ExchangeMailboxPlans AS B ON A.MailboxPlanId = B.MailboxPlanId WHERE A.ItemID=@ItemID) AS UsedDiskSpace,
	(SELECT MIN(B.RecoverableItemsSpace) FROM ExchangeAccounts AS A INNER JOIN ExchangeMailboxPlans AS B ON A.MailboxPlanId = B.MailboxPlanId WHERE A.ItemID=@ItemID) AS UsedLitigationHoldSpace	
END
ELSE
BEGIN
SELECT
	(SELECT COUNT(*) FROM ExchangeAccounts WHERE (AccountType = 1 OR AccountType = 5 OR AccountType = 6) AND ItemID = @ItemID) AS CreatedMailboxes,
	(SELECT COUNT(*) FROM ExchangeAccounts WHERE AccountType = 2 AND ItemID = @ItemID) AS CreatedContacts,
	(SELECT COUNT(*) FROM ExchangeAccounts WHERE AccountType = 3 AND ItemID = @ItemID) AS CreatedDistributionLists,
	(SELECT COUNT(*) FROM ExchangeAccounts WHERE AccountType = 4 AND ItemID = @ItemID) AS CreatedPublicFolders,
	(SELECT COUNT(*) FROM ExchangeOrganizationDomains WHERE ItemID = @ItemID) AS CreatedDomains,
	(SELECT SUM(B.MailboxSizeMB) FROM ExchangeAccounts AS A INNER JOIN ExchangeMailboxPlans AS B ON A.MailboxPlanId = B.MailboxPlanId WHERE A.ItemID=@ItemID) AS UsedDiskSpace,
	(SELECT SUM(B.RecoverableItemsSpace) FROM ExchangeAccounts AS A INNER JOIN ExchangeMailboxPlans AS B ON A.MailboxPlanId = B.MailboxPlanId WHERE A.ItemID=@ItemID) AS UsedLitigationHoldSpace	
END


RETURN
GO


-- 	UPDATE Domains SET IsDomainPointer=0, DomainItemID=NULL WHERE MailDomainID IS NOT NULL AND isDomainPointer=1


ALTER PROCEDURE [dbo].[GetPackageQuotas]
(
	@ActorID int,
	@PackageID int
)
AS

-- check rights
IF dbo.CheckActorPackageRights(@ActorID, @PackageID) = 0
RAISERROR('You are not allowed to access this package', 16, 1)

DECLARE @PlanID int, @ParentPackageID int
SELECT @PlanID = PlanID, @ParentPackageID = ParentPackageID FROM Packages
WHERE PackageID = @PackageID

-- get resource groups
SELECT
	RG.GroupID,
	RG.GroupName,
	ISNULL(HPR.CalculateDiskSpace, 0) AS CalculateDiskSpace,
	ISNULL(HPR.CalculateBandwidth, 0) AS CalculateBandwidth,
	dbo.GetPackageAllocatedResource(@ParentPackageID, RG.GroupID, 0) AS ParentEnabled
FROM ResourceGroups AS RG
LEFT OUTER JOIN HostingPlanResources AS HPR ON RG.GroupID = HPR.GroupID AND HPR.PlanID = @PlanID
WHERE dbo.GetPackageAllocatedResource(@PackageID, RG.GroupID, 0) = 1
ORDER BY RG.GroupOrder


-- return quotas
SELECT
	Q.QuotaID,
	Q.GroupID,
	Q.QuotaName,
	Q.QuotaDescription,
	Q.QuotaTypeID,
	dbo.GetPackageAllocatedQuota(@PackageID, Q.QuotaID) AS QuotaValue,
	dbo.GetPackageAllocatedQuota(@ParentPackageID, Q.QuotaID) AS ParentQuotaValue,
	ISNULL(dbo.CalculateQuotaUsage(@PackageID, Q.QuotaID), 0) AS QuotaUsedValue
FROM Quotas AS Q
WHERE Q.HideQuota IS NULL OR Q.HideQuota = 0
ORDER BY Q.QuotaOrder
RETURN

GO







ALTER PROCEDURE [dbo].[UpdateServiceProperties]
(
	@ServiceID int,
	@Xml ntext
)
AS

-- delete old properties
BEGIN TRAN
DECLARE @idoc int
--Create an internal representation of the XML document.
EXEC sp_xml_preparedocument @idoc OUTPUT, @xml

-- Execute a SELECT statement that uses the OPENXML rowset provider.
DELETE FROM ServiceProperties
WHERE ServiceID = @ServiceID 
AND PropertyName COLLATE Latin1_General_CI_AS IN
(
	SELECT PropertyName
	FROM OPENXML(@idoc, '/properties/property', 1)
	WITH (PropertyName nvarchar(50) '@name')
)

INSERT INTO ServiceProperties
(
	ServiceID,
	PropertyName,
	PropertyValue
)
SELECT
	@ServiceID,
	PropertyName,
	PropertyValue
FROM OPENXML(@idoc, '/properties/property',1) WITH 
(
	PropertyName nvarchar(50) '@name',
	PropertyValue nvarchar(1000) '@value'
) as PV

-- remove document
exec sp_xml_removedocument @idoc

COMMIT TRAN
RETURN 
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Providers] WHERE [DisplayName] = 'Hosted MS CRM 2011')
BEGIN
INSERT [dbo].[Providers] ([ProviderID], [GroupID], [ProviderName], [DisplayName], [ProviderType], [EditorControl], [DisableAutoDiscovery]) VALUES (1201, 21, N'CRM', N'Hosted MS CRM 2011', N'WebsitePanel.Providers.HostedSolution.CRMProvider2011, WebsitePanel.Providers.HostedSolution.CRM2011', N'CRM', NULL)
END
GO


UPDATE Providers SET ProviderType = N'WebsitePanel.Providers.HostedSolution.CRMProvider2011, WebsitePanel.Providers.HostedSolution.CRM2011' WHERE ProviderID = 1201
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Providers] WHERE [DisplayName] = 'Hosted SharePoint Foundation 2013')
BEGIN
INSERT [dbo].[Providers] ([ProviderID], [GroupID], [ProviderName], [DisplayName], [ProviderType], [EditorControl], [DisableAutoDiscovery])
VALUES (1301, 20, N'HostedSharePoint2013', N'Hosted SharePoint Foundation 2013', N'WebsitePanel.Providers.HostedSolution.HostedSharePointServer2013, WebsitePanel.Providers.HostedSolution.SharePoint2013', N'HostedSharePoint30', NULL)
END
GO

UPDATE Providers SET ProviderType = N'WebsitePanel.Providers.HostedSolution.HostedSharePointServer2013, WebsitePanel.Providers.HostedSolution.SharePoint2013' WHERE ProviderID = 1301
GO

IF NOT EXISTS (SELECT * FROM [dbo].[Providers] WHERE [ProviderName] = 'Lync2013')
BEGIN
INSERT [dbo].[Providers] ([ProviderID], [GroupID], [ProviderName], [DisplayName], [ProviderType], [EditorControl], [DisableAutoDiscovery])
VALUES (1401, 41, N'Lync2013', N'Microsoft Lync Server 2013 Multitenant Hosting Pack', N'WebsitePanel.Providers.HostedSolution.Lync2013, WebsitePanel.Providers.HostedSolution.Lync2013', N'Lync', NULL)
END
GO

-- Scheduler Service
ALTER TABLE Schedule
ADD LastFinish DATETIME NULL
GO

UPDATE Schedule SET LastFinish = LastRun
GO

ALTER PROCEDURE [dbo].[GetSchedule]
(
	@ActorID int,
	@ScheduleID int
)
AS

-- select schedule
SELECT TOP 1
	S.ScheduleID,
	S.TaskID,
	S.PackageID,
	S.ScheduleName,
	S.ScheduleTypeID,
	S.Interval,
	S.FromTime,
	S.ToTime,
	S.StartTime,
	S.LastRun,
	S.LastFinish,
	S.NextRun,
	S.Enabled,
	S.HistoriesNumber,
	S.PriorityID,
	S.MaxExecutionTime,
	S.WeekMonthDay,
	1 AS StatusID
FROM Schedule AS S
WHERE
	S.ScheduleID = @ScheduleID
	AND dbo.CheckActorPackageRights(@ActorID, S.PackageID) = 1

-- select task
SELECT
	ST.TaskID,
	ST.TaskType,
	ST.RoleID
FROM Schedule AS S
INNER JOIN ScheduleTasks AS ST ON S.TaskID = ST.TaskID
WHERE
	S.ScheduleID = @ScheduleID
	AND dbo.CheckActorPackageRights(@ActorID, S.PackageID) = 1

-- select schedule parameters
SELECT
	S.ScheduleID,
	STP.ParameterID,
	STP.DataTypeID,
	ISNULL(SP.ParameterValue, STP.DefaultValue) AS ParameterValue
FROM Schedule AS S
INNER JOIN ScheduleTaskParameters AS STP ON S.TaskID = STP.TaskID
LEFT OUTER JOIN ScheduleParameters AS SP ON STP.ParameterID = SP.ParameterID AND SP.ScheduleID = S.ScheduleID
WHERE
	S.ScheduleID = @ScheduleID
	AND dbo.CheckActorPackageRights(@ActorID, S.PackageID) = 1

RETURN
GO

ALTER PROCEDURE [dbo].[GetScheduleInternal]
(
	@ScheduleID int
)
AS

-- select schedule
SELECT
	S.ScheduleID,
	S.TaskID,
	ST.TaskType,
	ST.RoleID,
	S.PackageID,
	S.ScheduleName,
	S.ScheduleTypeID,
	S.Interval,
	S.FromTime,
	S.ToTime,
	S.StartTime,
	S.LastRun,
	S.LastFinish,
	S.NextRun,
	S.Enabled,
	1 AS StatusID,
	S.PriorityID,
	S.HistoriesNumber,
	S.MaxExecutionTime,
	S.WeekMonthDay
FROM Schedule AS S
INNER JOIN ScheduleTasks AS ST ON S.TaskID = ST.TaskID
WHERE ScheduleID = @ScheduleID
RETURN
GO

ALTER PROCEDURE [dbo].[GetSchedules]
(
	@ActorID int,
	@PackageID int,
	@Recursive bit
)
AS

-- check rights
IF dbo.CheckActorPackageRights(@ActorID, @PackageID) = 0
RAISERROR('You are not allowed to access this package', 16, 1)

DECLARE @Schedules TABLE
(
	ScheduleID int
)

INSERT INTO @Schedules (ScheduleID)
SELECT
	S.ScheduleID
FROM Schedule AS S
INNER JOIN PackagesTree(@PackageID, @Recursive) AS PT ON S.PackageID = PT.PackageID
ORDER BY S.Enabled DESC, S.NextRun
	

-- select schedules
SELECT
	S.ScheduleID,
	S.TaskID,
	ST.TaskType,
	ST.RoleID,
	S.PackageID,
	S.ScheduleName,
	S.ScheduleTypeID,
	S.Interval,
	S.FromTime,
	S.ToTime,
	S.StartTime,
	S.LastRun,
	S.LastFinish,
	S.NextRun,
	S.Enabled,
	1 AS StatusID,
	S.PriorityID,
	S.MaxExecutionTime,
	S.WeekMonthDay,
	ISNULL(0, (SELECT TOP 1 SeverityID FROM AuditLog WHERE ItemID = S.ScheduleID AND SourceName = 'SCHEDULER' ORDER BY StartDate DESC)) AS LastResult,

	U.Username,
	U.FirstName,
	U.LastName,
	U.FullName,
	U.RoleID,
	U.Email
FROM @Schedules AS STEMP
INNER JOIN Schedule AS S ON STEMP.ScheduleID = S.ScheduleID
INNER JOIN Packages AS P ON S.PackageID = P.PackageID
INNER JOIN ScheduleTasks AS ST ON S.TaskID = ST.TaskID
INNER JOIN UsersDetailed AS U ON P.UserID = U.UserID

-- select schedule parameters
SELECT
	S.ScheduleID,
	STP.ParameterID,
	STP.DataTypeID,
	ISNULL(SP.ParameterValue, STP.DefaultValue) AS ParameterValue
FROM @Schedules AS STEMP
INNER JOIN Schedule AS S ON STEMP.ScheduleID = S.ScheduleID
INNER JOIN ScheduleTaskParameters AS STP ON S.TaskID = STP.TaskID
LEFT OUTER JOIN ScheduleParameters AS SP ON STP.ParameterID = SP.ParameterID AND SP.ScheduleID = S.ScheduleID
RETURN
GO

ALTER PROCEDURE [dbo].[GetSchedulesPaged]
(
	@ActorID int,
	@PackageID int,
	@Recursive bit,
	@FilterColumn nvarchar(50) = '',
	@FilterValue nvarchar(50) = '',
	@SortColumn nvarchar(50),
	@StartRow int,
	@MaximumRows int
)
AS
BEGIN

-- check rights
IF dbo.CheckActorPackageRights(@ActorID, @PackageID) = 0
RAISERROR('You are not allowed to access this package', 16, 1)

DECLARE @condition nvarchar(400)
SET @condition = ' 1 = 1 '

IF @FilterColumn <> '' AND @FilterColumn IS NOT NULL
AND @FilterValue <> '' AND @FilterValue IS NOT NULL
SET @condition = @condition + ' AND ' + @FilterColumn + ' LIKE ''' + @FilterValue + ''''

IF @SortColumn IS NULL OR @SortColumn = ''
SET @SortColumn = 'S.ScheduleName ASC'

DECLARE @sql nvarchar(3500)

set @sql = '
SELECT COUNT(S.ScheduleID) FROM Schedule AS S
INNER JOIN PackagesTree(@PackageID, @Recursive) AS PT ON S.PackageID = PT.PackageID
INNER JOIN Packages AS P ON S.PackageID = P.PackageID
INNER JOIN UsersDetailed AS U ON P.UserID = U.UserID
WHERE ' + @condition + '

DECLARE @Schedules AS TABLE
(
	ScheduleID int
);

WITH TempSchedules AS (
	SELECT ROW_NUMBER() OVER (ORDER BY ' + @SortColumn + ') as Row,
		S.ScheduleID
	FROM Schedule AS S
	INNER JOIN Packages AS P ON S.PackageID = P.PackageID
	INNER JOIN PackagesTree(@PackageID, @Recursive) AS PT ON S.PackageID = PT.PackageID
	INNER JOIN UsersDetailed AS U ON P.UserID = U.UserID
	WHERE ' + @condition + '
)

INSERT INTO @Schedules
SELECT ScheduleID FROM TempSchedules
WHERE TempSchedules.Row BETWEEN @StartRow and @StartRow + @MaximumRows - 1

SELECT
	S.ScheduleID,
	S.TaskID,
	ST.TaskType,
	ST.RoleID,
	S.ScheduleName,
	S.ScheduleTypeID,
	S.Interval,
	S.FromTime,
	S.ToTime,
	S.StartTime,
	S.LastRun,
	S.LastFinish,
	S.NextRun,
	S.Enabled,
	1 AS StatusID,
	S.PriorityID,
	S.MaxExecutionTime,
	S.WeekMonthDay,
	ISNULL(0, (SELECT TOP 1 SeverityID FROM AuditLog WHERE ItemID = S.ScheduleID AND SourceName = ''SCHEDULER'' ORDER BY StartDate DESC)) AS LastResult,

	-- packages
	P.PackageID,
	P.PackageName,
	
	-- user
	P.UserID,
	U.Username,
	U.FirstName,
	U.LastName,
	U.FullName,
	U.RoleID,
	U.Email
FROM @Schedules AS STEMP
INNER JOIN Schedule AS S ON STEMP.ScheduleID = S.ScheduleID
INNER JOIN ScheduleTasks AS ST ON S.TaskID = ST.TaskID
INNER JOIN Packages AS P ON S.PackageID = P.PackageID
INNER JOIN UsersDetailed AS U ON P.UserID = U.UserID'

exec sp_executesql @sql, N'@PackageID int, @StartRow int, @MaximumRows int, @Recursive bit',
@PackageID, @StartRow, @MaximumRows, @Recursive

END
RETURN
GO

ALTER PROCEDURE [dbo].[UpdateSchedule]
(
	@ActorID int,
	@ScheduleID int,
	@TaskID nvarchar(100),
	@ScheduleName nvarchar(100),
	@ScheduleTypeID nvarchar(50),
	@Interval int,
	@FromTime datetime,
	@ToTime datetime,
	@StartTime datetime,
	@LastRun datetime,
	@LastFinish datetime,
	@NextRun datetime,
	@Enabled bit,
	@PriorityID nvarchar(50),
	@HistoriesNumber int,
	@MaxExecutionTime int,
	@WeekMonthDay int,
	@XmlParameters ntext
)
AS

-- check rights
DECLARE @PackageID int
SELECT @PackageID = PackageID FROM Schedule
WHERE ScheduleID = @ScheduleID

IF dbo.CheckActorPackageRights(@ActorID, @PackageID) = 0
RAISERROR('You are not allowed to access this package', 16, 1)

BEGIN TRAN

UPDATE Schedule
SET
	TaskID = @TaskID,
	ScheduleName = @ScheduleName,
	ScheduleTypeID = @ScheduleTypeID,
	Interval = @Interval,
	FromTime = @FromTime,
	ToTime = @ToTime,
	StartTime = @StartTime,
	LastRun = @LastRun,
	LastFinish = @LastFinish,
	NextRun = @NextRun,
	Enabled = @Enabled,
	PriorityID = @PriorityID,
	HistoriesNumber = @HistoriesNumber,
	MaxExecutionTime = @MaxExecutionTime,
	WeekMonthDay = @WeekMonthDay
WHERE
	ScheduleID = @ScheduleID
	
DECLARE @idoc int
--Create an internal representation of the XML document.
EXEC sp_xml_preparedocument @idoc OUTPUT, @XmlParameters

-- Execute a SELECT statement that uses the OPENXML rowset provider.
DELETE FROM ScheduleParameters
WHERE ScheduleID = @ScheduleID

INSERT INTO ScheduleParameters
(
	ScheduleID,
	ParameterID,
	ParameterValue
)
SELECT
	@ScheduleID,
	ParameterID,
	ParameterValue
FROM OPENXML(@idoc, '/parameters/parameter',1) WITH 
(
	ParameterID nvarchar(50) '@id',
	ParameterValue nvarchar(3000) '@value'
) as PV

-- remove document
exec sp_xml_removedocument @idoc

COMMIT TRAN
RETURN
GO

UPDATE ScheduleTasks SET TaskType = RTRIM(TaskType) + '.Code'
WHERE SUBSTRING(RTRIM(TaskType), LEN(RTRIM(TaskType)) - 3, 4) <> 'Code'
GO

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GetRunningSchedules')
DROP PROCEDURE GetRunningSchedules
GO

CREATE PROCEDURE [dbo].[GetRunningSchedules]
(
	@ActorID int
)
AS

SELECT
	S.ScheduleID,
	S.TaskID,
	ST.TaskType,
	ST.RoleID,
	S.PackageID,
	S.ScheduleName,
	S.ScheduleTypeID,
	S.Interval,
	S.FromTime,
	S.ToTime,
	S.StartTime,
	S.LastRun,
	S.LastFinish,
	S.NextRun,
	S.Enabled,
	1 AS StatusID,
	S.PriorityID,
	S.MaxExecutionTime,
	S.WeekMonthDay,
	ISNULL(0, (SELECT TOP 1 SeverityID FROM AuditLog WHERE ItemID = S.ScheduleID AND SourceName = 'SCHEDULER' ORDER BY StartDate DESC)) AS LastResult,
	U.Username,
	U.FirstName,
	U.LastName,
	U.FullName,
	U.RoleID,
	U.Email
FROM Schedule AS S
INNER JOIN Packages AS P ON S.PackageID = P.PackageID
INNER JOIN ScheduleTasks AS ST ON S.TaskID = ST.TaskID
INNER JOIN UsersDetailed AS U ON P.UserID = U.UserID
WHERE (U.UserID = @ActorID OR U.OwnerID = @ActorID) AND S.LastRun > S.LastFinish
GO