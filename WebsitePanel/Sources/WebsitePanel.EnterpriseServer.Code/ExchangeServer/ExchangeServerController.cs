// Copyright (c) 2012, Outercurve Foundation.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// - Redistributions of source code must  retain  the  above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form  must  reproduce the  above  copyright  notice,
//   this list of conditions  and  the  following  disclaimer in  the documentation
//   and/or other materials provided with the distribution.
//
// - Neither  the  name  of  the  Outercurve Foundation  nor   the   names  of  its
//   contributors may be used to endorse or  promote  products  derived  from  this
//   software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING,  BUT  NOT  LIMITED TO, THE IMPLIED
// WARRANTIES  OF  MERCHANTABILITY   AND  FITNESS  FOR  A  PARTICULAR  PURPOSE  ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL,  SPECIAL,  EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO,  PROCUREMENT  OF  SUBSTITUTE  GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)  HOWEVER  CAUSED AND ON
// ANY  THEORY  OF  LIABILITY,  WHETHER  IN  CONTRACT,  STRICT  LIABILITY,  OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE)  ARISING  IN  ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Net.Mail;
using System.Threading;
using WebsitePanel.EnterpriseServer.Code.HostedSolution;
using WebsitePanel.Providers;
using WebsitePanel.Providers.Common;
using WebsitePanel.Providers.Exchange;
using WebsitePanel.Providers.HostedSolution;
using WebsitePanel.Providers.OCS;
using WebsitePanel.Providers.ResultObjects;


namespace WebsitePanel.EnterpriseServer
{
    public class ExchangeServerController
    {
        #region Organizations
        public static DataSet GetRawExchangeOrganizationsPaged(int packageId, bool recursive,
            string filterColumn, string filterValue, string sortColumn, int startRow, int maximumRows)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                DataSet ds = new DataSet();

                // total records
                DataTable dtTotal = ds.Tables.Add();
                dtTotal.Columns.Add("Records", typeof(int));
                dtTotal.Rows.Add(3);

                // organizations
                DataTable dtItems = ds.Tables.Add();
                dtItems.Columns.Add("ItemID", typeof(int));
                dtItems.Columns.Add("OrganizationID", typeof(string));
                dtItems.Columns.Add("ItemName", typeof(string));
                dtItems.Columns.Add("PackageName", typeof(string));
                dtItems.Columns.Add("PackageID", typeof(int));
                dtItems.Columns.Add("Username", typeof(string));
                dtItems.Columns.Add("UserID", typeof(int));
                dtItems.Rows.Add(1, "fabrikam", "Fabrikam Inc", "Hosted Exchange", 1, "Customer", 1);
                dtItems.Rows.Add(1, "contoso", "Contoso", "Hosted Exchange", 1, "Customer", 1);
                dtItems.Rows.Add(1, "gencons", "General Consultants", "Hosted Exchange", 1, "Customer", 1);

                return ds;
            }
            #endregion

            return PackageController.GetRawPackageItemsPaged(
                packageId, ResourceGroups.Exchange, typeof(Organization),
                recursive, filterColumn, filterValue, sortColumn, startRow, maximumRows);
        }

        public static OrganizationsPaged GetExchangeOrganizationsPaged(int packageId, bool recursive,
            string filterColumn, string filterValue, string sortColumn, int startRow, int maximumRows)
        {
            ServiceItemsPaged items = PackageController.GetPackageItemsPaged(
                packageId, ResourceGroups.Exchange, typeof(Organization),
                recursive, filterColumn, filterValue, sortColumn, startRow, maximumRows);

            OrganizationsPaged orgs = new OrganizationsPaged();
            orgs.RecordsCount = items.RecordsCount;
            orgs.PageItems = new Organization[items.PageItems.Length];

            for (int i = 0; i < orgs.PageItems.Length; i++)
                orgs.PageItems[i] = (Organization)items.PageItems[i];

            return orgs;
        }

        public static List<Organization> GetExchangeOrganizations(int packageId, bool recursive)
        {
            List<ServiceProviderItem> items = PackageController.GetPackageItemsByType(
                packageId, typeof(Organization), recursive);

            return items.ConvertAll<Organization>(
                new Converter<ServiceProviderItem, Organization>(
                delegate(ServiceProviderItem item) { return (Organization)item; }));
        }

        public static List<Organization> GetExchangeOrganizationsInternal(int packageId, bool recursive)
        {
            List<ServiceProviderItem> items = PackageController.GetPackageItemsByTypeInternal(packageId, null, typeof(Organization), recursive);

            return items.ConvertAll<Organization>(
                new Converter<ServiceProviderItem, Organization>(
                delegate(ServiceProviderItem item) { return (Organization)item; }));
        }

        public static Organization GetOrganization(int itemId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                // load package by user
                Organization org = new Organization();
                org.PackageId = 0;
                org.Id = 1;
                org.OrganizationId = "fabrikam";
                org.Name = "Fabrikam Inc";
                org.KeepDeletedItemsDays = 14;
                return org;
            }
            #endregion

            return (Organization)PackageController.GetPackageItem(itemId);
        }

        public static OrganizationStatistics GetOrganizationStatistics(int itemId)
        {
            return GetOrganizationStatisticsInternal(itemId, false);
        }

        public static OrganizationStatistics GetOrganizationStatisticsByOrganization(int itemId)
        {
            return GetOrganizationStatisticsInternal(itemId, true);
        }


        private static OrganizationStatistics GetOrganizationStatisticsInternal(int itemId, bool byOrganization)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                OrganizationStatistics stats = new OrganizationStatistics();
                stats.AllocatedMailboxes = 10;
                stats.CreatedMailboxes = 4;
                stats.AllocatedContacts = 4;
                stats.CreatedContacts = 2;
                stats.AllocatedDistributionLists = 5;
                stats.CreatedDistributionLists = 1;
                stats.AllocatedPublicFolders = 40;
                stats.CreatedPublicFolders = 4;
                stats.AllocatedDomains = 5;
                stats.CreatedDomains = 2;
                stats.AllocatedDiskSpace = 200;
                stats.UsedDiskSpace = 70;
                return stats;
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_ORG_STATS", itemId);

            try
            {
                Organization org = (Organization)PackageController.GetPackageItem(itemId);
                if (org == null)
                    return null;

                OrganizationStatistics stats = new OrganizationStatistics();

                if (byOrganization)
                {
                    OrganizationStatistics tempStats = ObjectUtils.FillObjectFromDataReader<OrganizationStatistics>(DataProvider.GetExchangeOrganizationStatistics(org.Id));

                    stats.CreatedMailboxes = tempStats.CreatedMailboxes;
                    stats.CreatedContacts = tempStats.CreatedContacts;
                    stats.CreatedDistributionLists = tempStats.CreatedDistributionLists;
                    stats.CreatedDomains = tempStats.CreatedDomains;
                    stats.CreatedPublicFolders = tempStats.CreatedPublicFolders;
                    stats.UsedDiskSpace = tempStats.UsedDiskSpace;
                    stats.UsedLitigationHoldSpace = tempStats.UsedLitigationHoldSpace;
                }
                else
                {
                    UserInfo user = ObjectUtils.FillObjectFromDataReader<UserInfo>(DataProvider.GetUserByExchangeOrganizationIdInternally(org.Id));
                    List<PackageInfo> Packages = PackageController.GetPackages(user.UserId);

                    if ((Packages != null) & (Packages.Count > 0))
                    {
                        foreach (PackageInfo Package in Packages)
                        {
                            List<Organization> orgs = null;

                            orgs = GetExchangeOrganizations(Package.PackageId, false);

                            if ((orgs != null) & (orgs.Count > 0))
                            {
                                foreach (Organization o in orgs)
                                {
                                    OrganizationStatistics tempStats = ObjectUtils.FillObjectFromDataReader<OrganizationStatistics>(DataProvider.GetExchangeOrganizationStatistics(o.Id));

                                    stats.CreatedMailboxes += tempStats.CreatedMailboxes;
                                    stats.CreatedContacts += tempStats.CreatedContacts;
                                    stats.CreatedDistributionLists += tempStats.CreatedDistributionLists;
                                    stats.CreatedDomains += tempStats.CreatedDomains;
                                    stats.CreatedPublicFolders += tempStats.CreatedPublicFolders;
                                    stats.UsedDiskSpace += tempStats.UsedDiskSpace;
                                    stats.UsedLitigationHoldSpace += tempStats.UsedLitigationHoldSpace;
                                }
                            }
                        }
                    }
                }

                // disk space
                //stats.UsedDiskSpace = org.DiskSpace;


                // allocated quotas
                PackageContext cntx = PackageController.GetPackageContext(org.PackageId);
                stats.AllocatedMailboxes = cntx.Quotas[Quotas.EXCHANGE2007_MAILBOXES].QuotaAllocatedValue;
                stats.AllocatedContacts = cntx.Quotas[Quotas.EXCHANGE2007_CONTACTS].QuotaAllocatedValue;
                stats.AllocatedDistributionLists = cntx.Quotas[Quotas.EXCHANGE2007_DISTRIBUTIONLISTS].QuotaAllocatedValue;
                stats.AllocatedPublicFolders = cntx.Quotas[Quotas.EXCHANGE2007_PUBLICFOLDERS].QuotaAllocatedValue;
                stats.AllocatedDiskSpace = cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaAllocatedValue;
                stats.AllocatedLitigationHoldSpace = cntx.Quotas[Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE].QuotaAllocatedValue;

                return stats;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }



        public static int CalculateOrganizationDiskspace(int itemId)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "CALCULATE_DISKSPACE", itemId);

            try
            {
                // create thread parameters
                ThreadStartParameters prms = new ThreadStartParameters();
                prms.UserId = SecurityContext.User.UserId;
                prms.Parameters = new object[] { itemId };

                Thread t = new Thread(CalculateOrganizationDiskspaceAsync);
                t.Start(prms);
                return 0;

            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        private static void CalculateOrganizationDiskspaceAsync(object objPrms)
        {
            ThreadStartParameters prms = (ThreadStartParameters)objPrms;

            // impersonate thread
            SecurityContext.SetThreadPrincipal(prms.UserId);

            int itemId = (int)prms.Parameters[0];

            // calculate disk space
            CalculateOrganizationDiskspaceInternal(itemId);
        }

        internal static void CalculateOrganizationDiskspaceInternal(int itemId)
        {
            try
            {
                // calculate disk space
                Organization org = (Organization)PackageController.GetPackageItem(itemId);
                if (org == null)
                    return;

                SoapServiceProviderItem soapOrg = SoapServiceProviderItem.Wrap(org);


                int exchangeServiceId = PackageController.GetPackageServiceId(org.PackageId, ResourceGroups.Exchange);

                if (exchangeServiceId != 0)
                {
                    ServiceProvider exchange = GetServiceProvider(exchangeServiceId, org.ServiceId);

                    ServiceProviderItemDiskSpace[] itemsDiskspace = exchange.GetServiceItemsDiskSpace(new SoapServiceProviderItem[] { soapOrg });


                    if (itemsDiskspace != null && itemsDiskspace.Length > 0)
                    {
                        // set disk space
                        org.DiskSpace = (int)Math.Round(((float)itemsDiskspace[0].DiskSpace / 1024 / 1024));

                        // save organization
                        UpdateOrganization(org);
                    }
                }
            }
            catch (Exception ex)
            {
                // write to audit log
                TaskManager.WriteError(ex);
            }
        }

        private static bool OrganizationIdentifierExists(string organizationId)
        {
            return DataProvider.ExchangeOrganizationExists(organizationId);
        }





        private static int ExtendToExchangeOrganization(ref Organization org)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "CREATE_ORG", org.Name, new BackgroundTaskParameter("Organization ID", org.OrganizationId));

            try
            {
                // provision organization in Exchange
                int serviceId = GetExchangeServiceID(org.PackageId);
                int[] hubTransportServiceIds;
                int[] clientAccessServiceIds;

                GetExchangeServices(serviceId, out hubTransportServiceIds, out clientAccessServiceIds);


                ExchangeServer mailboxRole = GetExchangeServer(serviceId, org.ServiceId);


                bool authDomainCreated = false;
                int itemId = 0;
                bool organizationExtended = false;

                List<OrganizationDomainName> domains = null;
                try
                {
                    PackageContext cntx = PackageController.GetPackageContext(org.PackageId);

                    // 1) Create Organization (Mailbox)
                    // ================================
                    Organization exchangeOrganization = mailboxRole.ExtendToExchangeOrganization(org.OrganizationId,
                                                                                org.SecurityGroup,
                                                                                Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_ISCONSUMER].QuotaAllocatedValue));

                    organizationExtended = true;

                    exchangeOrganization.OrganizationId = org.OrganizationId;
                    exchangeOrganization.PackageId = org.PackageId;
                    exchangeOrganization.ServiceId = org.ServiceId;

                    exchangeOrganization.DefaultDomain = org.DefaultDomain;
                    exchangeOrganization.Name = org.Name;
                    exchangeOrganization.Id = org.Id;
                    exchangeOrganization.SecurityGroup = org.SecurityGroup;
                    exchangeOrganization.DistinguishedName = org.DistinguishedName;
                    exchangeOrganization.CrmAdministratorId = org.CrmAdministratorId;
                    exchangeOrganization.CrmCollation = org.CrmCollation;
                    exchangeOrganization.CrmCurrency = org.CrmCurrency;
                    exchangeOrganization.CrmLanguadgeCode = org.CrmLanguadgeCode;
                    exchangeOrganization.CrmOrganizationId = org.CrmOrganizationId;
                    exchangeOrganization.CrmOrgState = org.CrmOrgState;
                    exchangeOrganization.CrmUrl = org.CrmUrl;

                    org = exchangeOrganization;

                    // 2) Get OAB virtual directories from Client Access servers and
                    //    create Create Organization OAB (Mailbox)
                    // ==========================================
                    List<string> oabVirtualDirs = new List<string>();
                    foreach (int id in clientAccessServiceIds)
                    {
                        ExchangeServer clientAccessRole = null;
                        try
                        {
                            clientAccessRole = GetExchangeServer(id, org.ServiceId);
                        }
                        catch (Exception ex)
                        {
                            TaskManager.WriteError(ex);
                            continue;
                        }
                        oabVirtualDirs.Add(clientAccessRole.GetOABVirtualDirectory());
                    }

                    Organization orgOAB = mailboxRole.CreateOrganizationOfflineAddressBook(org.OrganizationId, org.SecurityGroup, string.Join(",", oabVirtualDirs.ToArray()));
                    org.OfflineAddressBook = orgOAB.OfflineAddressBook;


                    // 3) Add organization domains (Hub Transport)
                    domains = OrganizationController.GetOrganizationDomains(org.Id);

                    foreach (int id in hubTransportServiceIds)
                    {
                        ExchangeServer hubTransportRole = null;
                        try
                        {
                            hubTransportRole = GetExchangeServer(id, org.ServiceId);
                        }
                        catch (Exception ex)
                        {
                            TaskManager.WriteError(ex);
                            continue;
                        }

                        string[] existingDomains = hubTransportRole.GetAuthoritativeDomains();
                        if (existingDomains != null)
                            Array.Sort(existingDomains);

                        foreach (OrganizationDomainName domain in domains)
                        {
                            if (existingDomains == null || Array.BinarySearch(existingDomains, domain.DomainName) < 0)
                            {
                                hubTransportRole.AddAuthoritativeDomain(domain.DomainName);
                            }
                            if (domain.DomainType != ExchangeAcceptedDomainType.Authoritative)
                            {
                                hubTransportRole.ChangeAcceptedDomainType(domain.DomainName, domain.DomainType);
                            }
                        }
                        authDomainCreated = true;
                        break;
                    }

                    foreach (OrganizationDomainName d in domains)
                    {
                        DomainInfo domain = ServerController.GetDomain(d.DomainId);

                        //Add the service records
                        if (domain != null)
                        {
                            if (domain.ZoneItemId != 0)
                            {
                                ServerController.AddServiceDNSRecords(org.PackageId, ResourceGroups.Exchange, domain, "");
                                ServerController.AddServiceDNSRecords(org.PackageId, ResourceGroups.BlackBerry, domain, "");
                                ServerController.AddServiceDNSRecords(org.PackageId, ResourceGroups.OCS, domain, "");
                            }
                        }
                    }


                    // 4) Add the address book policy (Exchange 2010 SP2
                    //    
                    // ==========================================
                    Organization OrgTmp = mailboxRole.CreateOrganizationAddressBookPolicy(org.OrganizationId,
                                                                                        org.GlobalAddressList,
                                                                                        org.AddressList,
                                                                                        org.RoomsAddressList,
                                                                                        org.OfflineAddressBook);

                    org.AddressBookPolicy = OrgTmp.AddressBookPolicy;

                    StringDictionary settings = ServerController.GetServiceSettings(serviceId);
                    org.KeepDeletedItemsDays = Utils.ParseInt(settings["KeepDeletedItemsDays"], 14);

                }
                catch (Exception ex)
                {

                    // rollback organization creation
                    if (organizationExtended)
                        mailboxRole.DeleteOrganization(org.OrganizationId, org.DistinguishedName,
                            org.GlobalAddressList, org.AddressList, org.RoomsAddressList, org.OfflineAddressBook, org.SecurityGroup, org.AddressBookPolicy);

                    // rollback domain
                    if (authDomainCreated)
                        foreach (int id in hubTransportServiceIds)
                        {
                            ExchangeServer hubTransportRole = null;
                            try
                            {
                                hubTransportRole = GetExchangeServer(id, org.ServiceId);
                            }
                            catch (Exception exe)
                            {
                                TaskManager.WriteError(exe);
                                continue;
                            }

                            foreach (OrganizationDomainName domain in domains)
                            {
                                hubTransportRole.DeleteAuthoritativeDomain(domain.DomainName);

                            }

                            break;
                        }

                    throw TaskManager.WriteError(ex);
                }

                return itemId;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        private static int[] ParseMultiSetting(int mailboxServiceId, string settingName)
        {
            List<int> retIds = new List<int>();
            StringDictionary settings = ServerController.GetServiceSettings(mailboxServiceId);
            if (!String.IsNullOrEmpty(settings[settingName]))
            {
                string[] ids = settings[settingName].Split(',');

                int res;
                foreach (string id in ids)
                {
                    if (int.TryParse(id, out res))
                        retIds.Add(res);
                }
            }

            if (retIds.Count == 0)
                retIds.Add(mailboxServiceId);

            return retIds.ToArray();

        }

        private static void GetExchangeServices(int mailboxServiceId,
            out int[] hubTransportServiceIds, out int[] clientAccessServiceIds)
        {
            hubTransportServiceIds = ParseMultiSetting(mailboxServiceId, "HubTransportServiceID");

            clientAccessServiceIds = ParseMultiSetting(mailboxServiceId, "ClientAccessServiceID");
        }

        public static int DeleteOrganization(int itemId)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "DELETE_ORG", itemId);

            try
            {
                // delete organization in Exchange
                //System.Threading.Thread.Sleep(5000);
                Organization org = (Organization)PackageController.GetPackageItem(itemId);

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                bool successful = exchange.DeleteOrganization(
                    org.OrganizationId,
                    org.DistinguishedName,
                    org.GlobalAddressList,
                    org.AddressList,
                    org.RoomsAddressList,
                    org.OfflineAddressBook,
                    org.SecurityGroup,
                    org.AddressBookPolicy);


                return successful ? 0 : BusinessErrorCodes.ERROR_EXCHANGE_DELETE_SOME_PROBLEMS;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static Organization GetOrganizationStorageLimits(int itemId)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_ORG_LIMITS", itemId);

            try
            {
                return GetOrganization(itemId);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int SetOrganizationStorageLimits(int itemId, int issueWarningKB, int prohibitSendKB,
            int prohibitSendReceiveKB, int keepDeletedItemsDays, bool applyToMailboxes)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "SET_ORG_LIMITS", itemId);

            try
            {
                Organization org = (Organization)PackageController.GetPackageItem(itemId);
                if (org == null)
                    return 0;

                // load package context
                PackageContext cntx = PackageController.GetPackageContext(org.PackageId);

                int maxDiskSpace = 0;
                if (cntx.Quotas.ContainsKey(Quotas.EXCHANGE2007_DISKSPACE)
                    && cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaAllocatedValue > 0)
                    maxDiskSpace = cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaAllocatedValue * 1024;

                if (maxDiskSpace > 0 && (issueWarningKB > maxDiskSpace || prohibitSendKB > maxDiskSpace || prohibitSendReceiveKB > maxDiskSpace || issueWarningKB == -1 || prohibitSendKB == -1 || prohibitSendReceiveKB == -1))
                    return BusinessErrorCodes.ERROR_EXCHANGE_STORAGE_QUOTAS_EXCEED_HOST_VALUES;

                // set limits
                org.KeepDeletedItemsDays = keepDeletedItemsDays;

                // save organization
                UpdateOrganization(org);

                if (applyToMailboxes)
                {

                    int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                    ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                    exchange.SetOrganizationStorageLimits(org.DistinguishedName,
                        issueWarningKB,
                        prohibitSendKB,
                        prohibitSendReceiveKB,
                        keepDeletedItemsDays);
                }

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static ExchangeItemStatistics[] GetMailboxesStatistics(int itemId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                List<ExchangeItemStatistics> items = new List<ExchangeItemStatistics>();
                ExchangeItemStatistics item1 = new ExchangeItemStatistics();
                item1.ItemName = "John Smith";
                item1.TotalItems = 105;
                item1.TotalSizeMB = 14;
                item1.LastLogon = DateTime.Now;
                item1.LastLogoff = DateTime.Now;
                items.Add(item1);

                ExchangeItemStatistics item2 = new ExchangeItemStatistics();
                item2.ItemName = "Jack Brown";
                item2.TotalItems = 5;
                item2.TotalSizeMB = 2;
                item2.LastLogon = DateTime.Now;
                item2.LastLogoff = DateTime.Now;
                items.Add(item2);

                ExchangeItemStatistics item3 = new ExchangeItemStatistics();
                item3.ItemName = "Marry Smith";
                item3.TotalItems = 1302;
                item3.TotalSizeMB = 45;
                item3.LastLogon = DateTime.Now;
                item3.LastLogoff = DateTime.Now;
                items.Add(item3);

                return items.ToArray();
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_MAILBOXES_STATS", itemId);

            try
            {
                Organization org = (Organization)PackageController.GetPackageItem(itemId);
                if (org == null)
                    return null;


                // get stats
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                return exchange.GetMailboxesStatistics(org.DistinguishedName);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static ExchangeMailboxStatistics GetMailboxStatistics(int itemId, int accountId)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_MAILBOX_STATS", itemId);

            try
            {
                Organization org = (Organization)PackageController.GetPackageItem(itemId);
                if (org == null)
                    return null;


                // get stats
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                return exchange.GetMailboxStatistics(account.UserPrincipalName);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }


        public static ExchangeItemStatistics[] GetPublicFoldersStatistics(int itemId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                List<ExchangeItemStatistics> items = new List<ExchangeItemStatistics>();
                ExchangeItemStatistics item1 = new ExchangeItemStatistics();
                item1.ItemName = "\\fabrikam\\Documents";
                item1.TotalItems = 6;
                item1.TotalSizeMB = 56;
                item1.LastModificationTime = DateTime.Now;
                item1.LastAccessTime = DateTime.Now;
                items.Add(item1);

                ExchangeItemStatistics item2 = new ExchangeItemStatistics();
                item2.ItemName = "\\fabrikam\\Documents\\Legal";
                item2.TotalItems = 5;
                item2.TotalSizeMB = 4;
                item2.LastModificationTime = DateTime.Now;
                item2.LastAccessTime = DateTime.Now;
                items.Add(item2);

                ExchangeItemStatistics item3 = new ExchangeItemStatistics();
                item3.ItemName = "\\fabrikam\\Documents\\Contracts";
                item3.TotalItems = 8;
                item3.TotalSizeMB = 2;
                item3.LastModificationTime = DateTime.Now;
                item3.LastAccessTime = DateTime.Now;
                items.Add(item3);

                return items.ToArray();
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_FOLDERS_STATS", itemId);

            try
            {
                Organization org = (Organization)PackageController.GetPackageItem(itemId);
                if (org == null)
                    return null;

                // get the list of all public folders
                List<string> folderNames = new List<string>();
                List<ExchangeAccount> folders = GetAccounts(itemId, ExchangeAccountType.PublicFolder);
                foreach (ExchangeAccount folder in folders)
                    folderNames.Add(folder.DisplayName);

                // get stats
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);
                return exchange.GetPublicFoldersStatistics(org.OrganizationId, folderNames.ToArray());
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static ExchangeActiveSyncPolicy GetActiveSyncPolicy(int itemId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                ExchangeActiveSyncPolicy p = new ExchangeActiveSyncPolicy();
                p.MaxAttachmentSizeKB = -1;
                p.MaxPasswordFailedAttempts = -1;
                p.MinPasswordLength = 0;
                p.InactivityLockMin = -1;
                p.PasswordExpirationDays = -1;
                p.PasswordHistory = 0;
                return p;
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_ACTIVESYNC_POLICY", itemId);

            try
            {
                Organization org = (Organization)PackageController.GetPackageItem(itemId);
                if (org == null)
                    return null;

                // get policy
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                //Create Exchange Organization
                if (string.IsNullOrEmpty(org.GlobalAddressList))
                {
                    ExtendToExchangeOrganization(ref org);

                    PackageController.UpdatePackageItem(org);
                }
                ExchangeActiveSyncPolicy policy = exchange.GetActiveSyncPolicy(org.OrganizationId);

                // create policy if required
                if (policy == null)
                {
                    exchange.CreateOrganizationActiveSyncPolicy(org.OrganizationId);
                    return exchange.GetActiveSyncPolicy(org.OrganizationId);
                }

                return policy;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int SetActiveSyncPolicy(int itemId, bool allowNonProvisionableDevices,
                bool attachmentsEnabled, int maxAttachmentSizeKB, bool uncAccessEnabled, bool wssAccessEnabled,
                bool devicePasswordEnabled, bool alphanumericPasswordRequired, bool passwordRecoveryEnabled,
                bool deviceEncryptionEnabled, bool allowSimplePassword, int maxPasswordFailedAttempts, int minPasswordLength,
                int inactivityLockMin, int passwordExpirationDays, int passwordHistory, int refreshInterval)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "SET_ACTIVESYNC_POLICY", itemId);

            try
            {
                Organization org = (Organization)PackageController.GetPackageItem(itemId);
                if (org == null)
                    return 0;

                // get policy
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);
                exchange.SetActiveSyncPolicy(org.OrganizationId, allowNonProvisionableDevices, attachmentsEnabled,
                    maxAttachmentSizeKB, uncAccessEnabled, wssAccessEnabled, devicePasswordEnabled, alphanumericPasswordRequired,
                    passwordRecoveryEnabled, deviceEncryptionEnabled, allowSimplePassword, maxPasswordFailedAttempts,
                    minPasswordLength, inactivityLockMin, passwordExpirationDays, passwordHistory, refreshInterval);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        private static void UpdateOrganization(Organization organization)
        {
            PackageController.UpdatePackageItem(organization);
        }
        #endregion

        #region Accounts

        private static bool AccountExists(string accountName)
        {
            return DataProvider.ExchangeAccountExists(accountName);
        }

        public static ExchangeAccountsPaged GetAccountsPaged(int itemId, string accountTypes,
            string filterColumn, string filterValue, string sortColumn,
            int startRow, int maximumRows, bool archiving)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                string[] parseedAccountTypes = Utils.ParseDelimitedString(accountTypes, ',');

                ExchangeAccountsPaged res = new ExchangeAccountsPaged();
                res.PageItems = GetAccounts(itemId, (ExchangeAccountType)Utils.ParseInt(parseedAccountTypes[0], 1)).ToArray();
                res.RecordsCount = res.PageItems.Length;
                return res;
            }
            #endregion

            DataSet ds = DataProvider.GetExchangeAccountsPaged(SecurityContext.User.UserId, itemId,
                accountTypes, filterColumn, filterValue, sortColumn, startRow, maximumRows, archiving);

            ExchangeAccountsPaged result = new ExchangeAccountsPaged();
            result.RecordsCount = (int)ds.Tables[0].Rows[0][0];

            List<ExchangeAccount> accounts = new List<ExchangeAccount>();
            ObjectUtils.FillCollectionFromDataView(accounts, ds.Tables[1].DefaultView);
            result.PageItems = accounts.ToArray();
            return result;
        }

        public static List<ExchangeAccount> GetAccounts(int itemId, ExchangeAccountType accountType)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                if (accountType == ExchangeAccountType.Mailbox)
                    return SearchAccounts(0, true, false, false, true, true, false, "", "", "");
                else if (accountType == ExchangeAccountType.Contact)
                    return SearchAccounts(0, false, true, false, false, false, false, "", "", "");
                else if (accountType == ExchangeAccountType.DistributionList)
                    return SearchAccounts(0, false, false, true, false, false, false, "", "", "");
                else
                {
                    List<ExchangeAccount> demoAccounts = new List<ExchangeAccount>();
                    ExchangeAccount f1 = new ExchangeAccount();
                    f1.AccountId = 7;
                    f1.AccountName = "documents_fabrikam";
                    f1.AccountType = ExchangeAccountType.PublicFolder;
                    f1.DisplayName = "\\fabrikam\\Documents";
                    f1.PrimaryEmailAddress = "documents@fabrikam.net";
                    f1.MailEnabledPublicFolder = true;
                    demoAccounts.Add(f1);

                    ExchangeAccount f2 = new ExchangeAccount();
                    f2.AccountId = 8;
                    f2.AccountName = "documents_fabrikam";
                    f2.AccountType = ExchangeAccountType.PublicFolder;
                    f2.DisplayName = "\\fabrikam\\Documents\\Legal";
                    f2.PrimaryEmailAddress = "";
                    demoAccounts.Add(f2);

                    ExchangeAccount f3 = new ExchangeAccount();
                    f3.AccountId = 9;
                    f3.AccountName = "documents_fabrikam";
                    f3.AccountType = ExchangeAccountType.PublicFolder;
                    f3.DisplayName = "\\fabrikam\\Documents\\Contracts";
                    f3.PrimaryEmailAddress = "";
                    demoAccounts.Add(f3);
                    return demoAccounts;
                }
            }
            #endregion

            return ObjectUtils.CreateListFromDataReader<ExchangeAccount>(
                DataProvider.GetExchangeAccounts(itemId, (int)accountType));
        }


        public static List<ExchangeAccount> GetExchangeAccountByMailboxPlanId(int itemId, int mailboxPlanId)
        {
            return ObjectUtils.CreateListFromDataReader<ExchangeAccount>(DataProvider.GetExchangeAccountByMailboxPlanId(itemId, mailboxPlanId));
        }


        public static List<ExchangeAccount> GetExchangeMailboxes(int itemId)
        {
            return ObjectUtils.CreateListFromDataReader<ExchangeAccount>(DataProvider.GetExchangeMailboxes(itemId));
        }

        public static List<ExchangeAccount> SearchAccounts(int itemId,
            bool includeMailboxes, bool includeContacts, bool includeDistributionLists,
            bool includeRooms, bool includeEquipment, bool includeSecurityGroups,
            string filterColumn, string filterValue, string sortColumn)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                List<ExchangeAccount> demoAccounts = new List<ExchangeAccount>();

                if (includeMailboxes)
                {
                    ExchangeAccount m1 = new ExchangeAccount();
                    m1.AccountId = 1;
                    m1.AccountName = "john_fabrikam";
                    m1.AccountType = ExchangeAccountType.Mailbox;
                    m1.DisplayName = "John Smith";
                    m1.PrimaryEmailAddress = "john@fabrikam.net";
                    demoAccounts.Add(m1);



                    ExchangeAccount m3 = new ExchangeAccount();
                    m3.AccountId = 3;
                    m3.AccountName = "marry_fabrikam";
                    m3.AccountType = ExchangeAccountType.Mailbox;
                    m3.DisplayName = "Marry Smith";
                    m3.PrimaryEmailAddress = "marry@fabrikam.net";
                    demoAccounts.Add(m3);
                }

                if (includeRooms)
                {
                    ExchangeAccount r1 = new ExchangeAccount();
                    r1.AccountId = 20;
                    r1.AccountName = "room1_fabrikam";
                    r1.AccountType = ExchangeAccountType.Room;
                    r1.DisplayName = "Meeting Room 1";
                    r1.PrimaryEmailAddress = "room1@fabrikam.net";
                    demoAccounts.Add(r1);
                }

                if (includeEquipment)
                {
                    ExchangeAccount e1 = new ExchangeAccount();
                    e1.AccountId = 21;
                    e1.AccountName = "projector_fabrikam";
                    e1.AccountType = ExchangeAccountType.Equipment;
                    e1.DisplayName = "Projector 1";
                    e1.PrimaryEmailAddress = "projector@fabrikam.net";
                    demoAccounts.Add(e1);
                }

                if (includeContacts)
                {
                    ExchangeAccount c1 = new ExchangeAccount();
                    c1.AccountId = 4;
                    c1.AccountName = "pntr1_fabrikam";
                    c1.AccountType = ExchangeAccountType.Contact;
                    c1.DisplayName = "WebsitePanel Support";
                    c1.PrimaryEmailAddress = "support@websitepanel.net";
                    demoAccounts.Add(c1);

                    ExchangeAccount c2 = new ExchangeAccount();
                    c2.AccountId = 5;
                    c2.AccountName = "acc1_fabrikam";
                    c2.AccountType = ExchangeAccountType.Contact;
                    c2.DisplayName = "John Home Account";
                    c2.PrimaryEmailAddress = "john@yahoo.com";
                    demoAccounts.Add(c2);
                }

                if (includeDistributionLists)
                {
                    ExchangeAccount d1 = new ExchangeAccount();
                    d1.AccountId = 6;
                    d1.AccountName = "sales_fabrikam";
                    d1.AccountType = ExchangeAccountType.DistributionList;
                    d1.DisplayName = "Fabrikam Sales Dept";
                    d1.PrimaryEmailAddress = "sales@fabrikam.net";
                    demoAccounts.Add(d1);
                }

                if (includeSecurityGroups)
                {
                    ExchangeAccount g1 = new ExchangeAccount();
                    g1.AccountId = 7;
                    g1.AccountName = "group_fabrikam";
                    g1.AccountType = ExchangeAccountType.SecurityGroup;
                    g1.DisplayName = "Fabrikam Sales Dept";
                    demoAccounts.Add(g1);
                }

                return demoAccounts;
            }
            #endregion

            return ObjectUtils.CreateListFromDataReader<ExchangeAccount>(
                DataProvider.SearchExchangeAccounts(SecurityContext.User.UserId, itemId, includeMailboxes, includeContacts,
                includeDistributionLists, includeRooms, includeEquipment, includeSecurityGroups,
                filterColumn, filterValue, sortColumn));
        }



        public static ExchangeAccount GetAccount(int itemId, int accountId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                ExchangeAccount m1 = new ExchangeAccount();
                m1.AccountId = 1;
                m1.AccountName = "john_fabrikam";
                m1.AccountType = ExchangeAccountType.Mailbox;
                m1.DisplayName = "John Smith";
                m1.PrimaryEmailAddress = "john@fabrikam.net";
                return m1;
            }
            #endregion

            ExchangeAccount account = ObjectUtils.FillObjectFromDataReader<ExchangeAccount>(
                DataProvider.GetExchangeAccount(itemId, accountId));

            if (account == null)
                return null;

            // decrypt password
            account.AccountPassword = CryptoUtils.Decrypt(account.AccountPassword);

            return account;
        }

        public static bool CheckAccountCredentials(int itemId, string email, string password)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "AUTHENTICATE", email, itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return false;

                // check credentials
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);
                return exchange.CheckAccountCredentials(email, password);
            }
            catch (Exception ex)
            {
                TaskManager.WriteError(ex);
                return false;
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static ExchangeAccount SearchAccount(ExchangeAccountType accountType, string primaryEmailAddress)
        {
            ExchangeAccount account = ObjectUtils.FillObjectFromDataReader<ExchangeAccount>(
                DataProvider.SearchExchangeAccount(SecurityContext.User.UserId,
                (int)accountType, primaryEmailAddress));

            if (account == null)
                return null;

            // decrypt password
            account.AccountPassword = CryptoUtils.Decrypt(account.AccountPassword);

            return account;
        }

        private static int AddAccount(int itemId, ExchangeAccountType accountType,
            string accountName, string displayName, string primaryEmailAddress, bool mailEnabledPublicFolder,
            MailboxManagerActions mailboxManagerActions, string samAccountName, string accountPassword, int mailboxPlanId, string subscriberNumber)
        {
            return DataProvider.AddExchangeAccount(itemId, (int)accountType,
                accountName, displayName, primaryEmailAddress, mailEnabledPublicFolder,
                mailboxManagerActions.ToString(), samAccountName, CryptoUtils.Encrypt(accountPassword), mailboxPlanId, (string.IsNullOrEmpty(subscriberNumber) ? null : subscriberNumber.Trim()));
        }

        private static void UpdateAccount(ExchangeAccount account)
        {
            DataProvider.UpdateExchangeAccount(account.AccountId, account.AccountName, account.AccountType, account.DisplayName,
                account.PrimaryEmailAddress, account.MailEnabledPublicFolder,
                account.MailboxManagerActions.ToString(), account.SamAccountName, account.AccountPassword, account.MailboxPlanId, account.ArchivingMailboxPlanId,
                (string.IsNullOrEmpty(account.SubscriberNumber) ? null : account.SubscriberNumber.Trim()),
                account.EnableArchiving);
        }

        private static void DeleteAccount(int itemId, int accountId)
        {
            // try to get organization
            if (GetOrganization(itemId) == null)
                return;

            DataProvider.DeleteExchangeAccount(itemId, accountId);
        }
/*
        private static string BuildAccountName(string orgId, string name)
        {
            string accountName = name = name.Replace(" ", "");
            int counter = 0;
            bool bFound = false;

            if (!AccountExists(accountName)) return accountName;

            do
            {
                accountName = genSamLogin(name, counter.ToString("d5"));

                if (!AccountExists(accountName)) bFound = true;

                counter++;
            }
            while (!bFound);

            return accountName;
        }

        private static string genSamLogin(string login, string strCounter)
        {
            int maxLogin = 20;
            int fullLen = login.Length + strCounter.Length;
            if (fullLen <= maxLogin)
                return login + strCounter;
            else
            {
                if (login.Length - (fullLen - maxLogin) > 0)
                    return login.Substring(0, login.Length - (fullLen - maxLogin)) + strCounter;
                else return strCounter; // ????
            }

        }
*/

        #endregion

        #region Account Email Addresses
        private static bool EmailAddressExists(string emailAddress)
        {
            return DataProvider.ExchangeAccountEmailAddressExists(emailAddress);
        }


        private static ExchangeEmailAddress[] GetAccountEmailAddresses(int itemId, int accountId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                List<ExchangeEmailAddress> demoEmails = new List<ExchangeEmailAddress>();
                ExchangeEmailAddress e1 = new ExchangeEmailAddress();
                e1.EmailAddress = "john@fabrikam.net";
                e1.IsPrimary = true;
                demoEmails.Add(e1);

                ExchangeEmailAddress e2 = new ExchangeEmailAddress();
                e2.EmailAddress = "john.smith@fabrikam.net";
                demoEmails.Add(e2);

                ExchangeEmailAddress e3 = new ExchangeEmailAddress();
                e3.EmailAddress = "john@fabrikam.hosted-exchange.com";
                demoEmails.Add(e3);
                return demoEmails.ToArray();
            }
            #endregion

            List<ExchangeEmailAddress> emails = ObjectUtils.CreateListFromDataReader<ExchangeEmailAddress>(
                DataProvider.GetExchangeAccountEmailAddresses(accountId));

            // load account
            ExchangeAccount account = GetAccount(itemId, accountId);

            foreach (ExchangeEmailAddress email in emails)
            {
                if (String.Compare(account.PrimaryEmailAddress, email.EmailAddress, true) == 0)
                {
                    email.IsPrimary = true;
                }

                if (String.Compare(account.UserPrincipalName, email.EmailAddress, true) == 0)
                {
                    email.IsUserPrincipalName = true;
                }

            }

            return emails.ToArray();
        }

        private static void AddAccountEmailAddress(int accountId, string emailAddress)
        {
            DataProvider.AddExchangeAccountEmailAddress(accountId, emailAddress);
        }

        private static void DeleteAccountEmailAddresses(int accountId, string[] emailAddresses)
        {
            foreach (string emailAddress in emailAddresses)
                DataProvider.DeleteExchangeAccountEmailAddress(accountId, emailAddress);
        }

        #endregion

        #region Domains
        public static List<ExchangeDomainName> GetOrganizationDomains(int itemId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                List<ExchangeDomainName> demoDomains = new List<ExchangeDomainName>();
                ExchangeDomainName d1 = new ExchangeDomainName();
                d1.DomainId = 1;
                d1.DomainName = "fabrikam.hosted-exchange.com";
                d1.IsDefault = false;
                d1.IsHost = true;
                demoDomains.Add(d1);

                ExchangeDomainName d2 = new ExchangeDomainName();
                d2.DomainId = 2;
                d2.DomainName = "fabrikam.net";
                d2.IsDefault = true;
                d2.IsHost = false;
                demoDomains.Add(d2);

                return demoDomains;
            }
            #endregion

            // load organization
            Organization org = (Organization)PackageController.GetPackageItem(itemId);
            if (org == null)
                return null;

            // load all domains
            List<ExchangeDomainName> domains = ObjectUtils.CreateListFromDataReader<ExchangeDomainName>(
                DataProvider.GetExchangeOrganizationDomains(itemId));

            // set default domain
            foreach (ExchangeDomainName domain in domains)
            {
                if (String.Compare(domain.DomainName, org.DefaultDomain, true) == 0)
                {
                    domain.IsDefault = true;
                    break;
                }
            }

            return domains;
        }

        public static int AddAuthoritativeDomain(int itemId, int domainId)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "ADD_DOMAIN", itemId, new BackgroundTaskParameter("Domain ID", domainId));

            try
            {
                // load organization
                Organization org = (Organization)PackageController.GetPackageItem(itemId);
                if (org == null)
                    return -1;

                // load domain
                DomainInfo domain = ServerController.GetDomain(domainId);
                if (domain == null)
                    return -1;


                // delete domain on Exchange
                int[] hubTransportServiceIds;
                int[] clientAccessServiceIds;
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                GetExchangeServices(exchangeServiceId, out hubTransportServiceIds, out clientAccessServiceIds);

                foreach (int id in hubTransportServiceIds)
                {
                    ExchangeServer hubTransportRole = null;
                    try
                    {
                        hubTransportRole = GetExchangeServer(id, org.ServiceId);
                    }
                    catch (Exception ex)
                    {
                        TaskManager.WriteError(ex);
                        continue;
                    }

                    string[] domains = hubTransportRole.GetAuthoritativeDomains();
                    if (domains != null)
                        Array.Sort(domains);

                    if (domains == null || Array.BinarySearch(domains, domain.DomainName) < 0)
                        hubTransportRole.AddAuthoritativeDomain(domain.DomainName);
                    break;
                }

                //Add the service records
                if (domain != null)
                {
                    if (domain.ZoneItemId != 0)
                    {
                        ServerController.AddServiceDNSRecords(org.PackageId, ResourceGroups.Exchange, domain, "");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int ChangeAcceptedDomainType(int itemId, int domainId, ExchangeAcceptedDomainType domainType)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record

            List<BackgroundTaskParameter> parameters = new List<BackgroundTaskParameter>();
            parameters.Add(new BackgroundTaskParameter("Domain ID", domainId));
            parameters.Add(new BackgroundTaskParameter("Domain Type", domainType.ToString()));

            TaskManager.StartTask("EXCHANGE", "CHANGE_DOMAIN_TYPE", itemId, parameters);

            try
            {
                // load organization
                Organization org = (Organization)PackageController.GetPackageItem(itemId);
                if (org == null)
                    return -1;

                // load domain
                DomainInfo domain = ServerController.GetDomain(domainId);
                if (domain == null)
                    return -1;

                int[] hubTransportServiceIds;
                int[] clientAccessServiceIds;
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                GetExchangeServices(exchangeServiceId, out hubTransportServiceIds, out clientAccessServiceIds);

                foreach (int id in hubTransportServiceIds)
                {
                    ExchangeServer hubTransportRole = null;
                    try
                    {
                        hubTransportRole = GetExchangeServer(id, org.ServiceId);
                    }
                    catch (Exception ex)
                    {
                        TaskManager.WriteError(ex);
                        continue;
                    }

                    hubTransportRole.ChangeAcceptedDomainType(domain.DomainName, domainType);
                    break;

                }
                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int DeleteAuthoritativeDomain(int itemId, int domainId)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "DELETE_DOMAIN", itemId, new BackgroundTaskParameter("Domain ID", domainId));

            try
            {
                // load organization
                Organization org = (Organization)PackageController.GetPackageItem(itemId);
                if (org == null)
                    return -1;

                // load domain
                DomainInfo domain = ServerController.GetDomain(domainId);
                if (domain == null)
                    return -1;

                if (DataProvider.CheckDomainUsedByHostedOrganization(domain.DomainName) == 1)
                {
                    return -1;
                }

                // delete domain on Exchange
                int[] hubTransportServiceIds;
                int[] clientAccessServiceIds;
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                GetExchangeServices(exchangeServiceId, out hubTransportServiceIds, out clientAccessServiceIds);

                foreach (int id in hubTransportServiceIds)
                {
                    ExchangeServer hubTransportRole = null;
                    try
                    {
                        hubTransportRole = GetExchangeServer(id, org.ServiceId);
                    }
                    catch (Exception ex)
                    {
                        TaskManager.WriteError(ex);
                        continue;
                    }

                    hubTransportRole.DeleteAuthoritativeDomain(domain.DomainName);
                    break;

                }

                //Add the service records
                if (domain != null)
                {
                    if (domain.ZoneItemId != 0)
                    {
                        ServerController.RemoveServiceDNSRecords(org.PackageId, ResourceGroups.Exchange, domain, "", false);
                    }
                }



                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }


        #endregion

        #region Mailboxes

        private static void UpdateExchangeAccount(int accountId, string accountName, ExchangeAccountType accountType,
            string displayName, string primaryEmailAddress, bool mailEnabledPublicFolder,
            string mailboxManagerActions, string samAccountName, string accountPassword, int mailboxPlanId, int archivePlanId, string subscriberNumber,
            bool EnableArchiving)
        {
            DataProvider.UpdateExchangeAccount(accountId,
                accountName,
                accountType,
                displayName,
                primaryEmailAddress,
                mailEnabledPublicFolder,
                mailboxManagerActions,
                samAccountName,
                CryptoUtils.Encrypt(accountPassword),
                mailboxPlanId, archivePlanId,
                (string.IsNullOrEmpty(subscriberNumber) ? null : subscriberNumber.Trim()), EnableArchiving);
        }


        public static int CreateMailbox(int itemId, int accountId, ExchangeAccountType accountType, string accountName,
            string displayName, string name, string domain, string password, bool sendSetupInstructions, string setupInstructionMailAddress, int mailboxPlanId, int archivedPlanId, string subscriberNumber, bool EnableArchiving)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // check mailbox quota
            OrganizationStatistics orgStats = GetOrganizationStatistics(itemId);
            if ((orgStats.AllocatedMailboxes > -1) && (orgStats.CreatedMailboxes >= orgStats.AllocatedMailboxes))
                return BusinessErrorCodes.ERROR_EXCHANGE_MAILBOXES_QUOTA_LIMIT;


            // place log record
            TaskManager.StartTask("EXCHANGE", "CREATE_MAILBOX", itemId);

            bool userCreated = false;
            Organization org = null;
            try
            {
                accountName = accountName.Trim();
                displayName = displayName.Trim();
                name = name.Trim();
                domain = domain.Trim();


                // load organization
                org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // e-mail
                string email = name + "@" + domain;
                bool enabled = (accountType == ExchangeAccountType.Mailbox);


                //  string accountName = string.Empty;
                //Create AD user if needed
                if (accountId == 0)
                {
                    accountId = OrganizationController.CreateUser(org.Id, displayName, name, domain, password, subscriberNumber, enabled, false, string.Empty, out accountName);
                    if (accountId > 0)
                        userCreated = true;
                }
                if (accountId < 0)
                    return accountId;

                // get mailbox settings
                Organizations orgProxy = OrganizationController.GetOrganizationProxy(org.ServiceId);
                OrganizationUser retUser = orgProxy.GetUserGeneralSettings(accountName, org.OrganizationId);


                int exchangeServiceId = PackageController.GetPackageServiceId(org.PackageId, ResourceGroups.Exchange);

                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);


                //Create Exchange Organization
                if (string.IsNullOrEmpty(org.GlobalAddressList))
                {
                    ExtendToExchangeOrganization(ref org);

                    PackageController.UpdatePackageItem(org);
                }

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                //verify if the mailbox fits in the storage quota
                // load package context
                PackageContext cntx = PackageController.GetPackageContext(org.PackageId);

                int maxDiskSpace = -1;
                int quotaUsed = 0;
                if (cntx.Quotas.ContainsKey(Quotas.EXCHANGE2007_DISKSPACE)
                    && cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaAllocatedValue > 0)
                {
                    maxDiskSpace = cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaAllocatedValue;
                    quotaUsed = cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaUsedValue;
                }

                ExchangeMailboxPlan plan = GetExchangeMailboxPlan(itemId, mailboxPlanId);
                if (maxDiskSpace != -1)
                {
                    if (plan.MailboxSizeMB == -1)
                        return BusinessErrorCodes.ERROR_EXCHANGE_STORAGE_QUOTAS_EXCEED_HOST_VALUES;

                    if ((quotaUsed + plan.MailboxSizeMB) > (maxDiskSpace))
                        return BusinessErrorCodes.ERROR_EXCHANGE_STORAGE_QUOTAS_EXCEED_HOST_VALUES;
                }

                int maxRecoverableItemsSpace = -1;
                int quotaRecoverableItemsUsed = 0;
                if (cntx.Quotas.ContainsKey(Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE)
                    && cntx.Quotas[Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE].QuotaAllocatedValue > 0)
                {
                    maxRecoverableItemsSpace = cntx.Quotas[Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE].QuotaAllocatedValue;
                    quotaRecoverableItemsUsed = cntx.Quotas[Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE].QuotaUsedValue;
                }

                if (maxRecoverableItemsSpace != -1)
                {
                    if (plan.RecoverableItemsSpace == -1)
                        return BusinessErrorCodes.ERROR_EXCHANGE_STORAGE_QUOTAS_EXCEED_HOST_VALUES;

                    if ((quotaRecoverableItemsUsed + plan.RecoverableItemsSpace) > (maxRecoverableItemsSpace))
                        return BusinessErrorCodes.ERROR_EXCHANGE_STORAGE_QUOTAS_EXCEED_HOST_VALUES;
                }

                int maxArchivingStorage = -1;
                int quotaArchivingStorageUsed = 0;
                if (cntx.Quotas.ContainsKey(Quotas.EXCHANGE2013_ARCHIVINGSTORAGE)
                    && cntx.Quotas[Quotas.EXCHANGE2013_ARCHIVINGSTORAGE].QuotaAllocatedValue > 0)
                {
                    maxArchivingStorage = cntx.Quotas[Quotas.EXCHANGE2013_ARCHIVINGSTORAGE].QuotaAllocatedValue;
                    quotaArchivingStorageUsed = cntx.Quotas[Quotas.EXCHANGE2013_ARCHIVINGSTORAGE].QuotaUsedValue;
                }

                if (maxArchivingStorage != -1)
                {
                    if (plan.ArchiveSizeMB == -1)
                        return BusinessErrorCodes.ERROR_EXCHANGE_STORAGE_QUOTAS_EXCEED_HOST_VALUES;

                    if ((quotaArchivingStorageUsed + plan.ArchiveSizeMB) > (maxArchivingStorage))
                        return BusinessErrorCodes.ERROR_EXCHANGE_STORAGE_QUOTAS_EXCEED_HOST_VALUES;
                }


                //GetServiceSettings
                StringDictionary primSettings = ServerController.GetServiceSettings(exchangeServiceId);

                string samAccount = exchange.CreateMailEnableUser(email, org.OrganizationId, org.DistinguishedName, accountType, primSettings["mailboxdatabase"],
                                                org.OfflineAddressBook,
                                                org.AddressBookPolicy,
                                                retUser.SamAccountName,
                                                plan.EnablePOP,
                                                plan.EnableIMAP,
                                                plan.EnableOWA,
                                                plan.EnableMAPI,
                                                plan.EnableActiveSync,
                                                plan.MailboxSizeMB != -1 ? (((long)plan.IssueWarningPct * (long)plan.MailboxSizeMB * 1024) / 100) : -1,
                                                plan.MailboxSizeMB != -1 ? (((long)plan.ProhibitSendPct * (long)plan.MailboxSizeMB * 1024) / 100) : -1,
                                                plan.MailboxSizeMB != -1 ? (((long)plan.ProhibitSendReceivePct * (long)plan.MailboxSizeMB * 1024) / 100) : -1,
                                                plan.KeepDeletedItemsDays,
                                                plan.MaxRecipients,
                                                plan.MaxSendMessageSizeKB,
                                                plan.MaxReceiveMessageSizeKB,
                                                plan.HideFromAddressBook,
                                                Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_ISCONSUMER].QuotaAllocatedValue),
                                                plan.AllowLitigationHold,
                                                plan.RecoverableItemsSpace != -1 ? (plan.RecoverableItemsSpace * 1024) : -1,
                                                plan.RecoverableItemsSpace != -1 ? (((long)plan.RecoverableItemsWarningPct * (long)plan.RecoverableItemsSpace * 1024) / 100) : -1);

                MailboxManagerActions pmmActions = MailboxManagerActions.GeneralSettings
                    | MailboxManagerActions.MailFlowSettings
                    | MailboxManagerActions.AdvancedSettings
                    | MailboxManagerActions.EmailAddresses;


                UpdateExchangeAccount(accountId, accountName, accountType, displayName, email, false, pmmActions.ToString(), samAccount, password, mailboxPlanId, archivedPlanId, subscriberNumber, EnableArchiving);

                ResultObject resPolicy = new ResultObject() { IsSuccess = true };
                SetMailBoxRetentionPolicyAndArchiving(itemId, mailboxPlanId, archivedPlanId, accountName, exchange, org.OrganizationId, resPolicy, EnableArchiving);
                if (!resPolicy.IsSuccess)
                {
                    TaskManager.WriteError("Error SetMailBoxRetentionPolicy", resPolicy.ErrorCodes.ToArray());
                }
                

                // send setup instructions
                if (sendSetupInstructions)
                {
                    try
                    {
                        // send setup instructions
                        int sendResult = SendMailboxSetupInstructions(itemId, accountId, true, setupInstructionMailAddress, null);
                        if (sendResult < 0)
                            TaskManager.WriteWarning("Setup instructions were not sent. Error code: " + sendResult);
                    }
                    catch (Exception ex)
                    {
                        TaskManager.WriteError(ex);
                    }
                }

                try
                {
                    // update OAB
                    // check if this is the first mailbox within the organization
                    if (GetAccounts(itemId, ExchangeAccountType.Mailbox).Count == 1)
                        exchange.UpdateOrganizationOfflineAddressBook(org.OfflineAddressBook);
                }
                catch (Exception ex)
                {
                    TaskManager.WriteError(ex);
                }

                return accountId;
            }
            catch (Exception ex)
            {
                //rollback AD user
                if (userCreated)
                {
                    try
                    {
                        OrganizationController.DeleteUser(org.Id, accountId);
                    }
                    catch (Exception rollbackException)
                    {
                        TaskManager.WriteError(rollbackException);
                    }
                }
                throw TaskManager.WriteError(ex);

            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int DisableMailbox(int itemId, int accountId)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "DISABLE_MAILBOX", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                if (BlackBerryController.CheckBlackBerryUserExists(accountId))
                {
                    BlackBerryController.DeleteBlackBerryUser(itemId, accountId);
                }

                // delete mailbox
                int serviceExchangeId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(serviceExchangeId, org.ServiceId);
                exchange.DisableMailbox(account.UserPrincipalName);

                account.AccountType = ExchangeAccountType.User;
                account.MailEnabledPublicFolder = false;
                account.AccountPassword = null;
                UpdateAccount(account);
                DataProvider.DeleteUserEmailAddresses(account.AccountId, account.PrimaryEmailAddress);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }


        public static int DeleteMailbox(int itemId, int accountId)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "DELETE_MAILBOX", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                if (BlackBerryController.CheckBlackBerryUserExists(accountId))
                {
                    BlackBerryController.DeleteBlackBerryUser(itemId, accountId);
                }


                // delete mailbox
                int serviceExchangeId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(serviceExchangeId, org.ServiceId);
                exchange.DeleteMailbox(account.UserPrincipalName);



                // unregister account
                DeleteAccount(itemId, accountId);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        private static ExchangeMailbox GetDemoMailboxSettings()
        {
            ExchangeMailbox mb = new ExchangeMailbox();
            mb.DisplayName = "John Smith";
            mb.Domain = "HSTDEXCH1";
            mb.AccountName = "john_fabrikam";
            mb.EnableForwarding = true;
            mb.EnableIMAP = true;
            mb.EnableMAPI = true;
            mb.EnablePOP = true;
            mb.FirstName = "John";
            mb.LastName = "Smith";
            mb.ForwardingAccount = GetAccounts(0, ExchangeAccountType.Mailbox)[1];
            mb.EnableForwarding = true;
            mb.IssueWarningKB = 150000;
            mb.KeepDeletedItemsDays = 14;
            mb.LastLogoff = DateTime.Now;
            mb.LastLogon = DateTime.Now;
            mb.ManagerAccount = GetAccounts(0, ExchangeAccountType.Mailbox)[1];
            mb.MaxReceiveMessageSizeKB = 20000;
            mb.MaxRecipients = 30;
            mb.MaxSendMessageSizeKB = 10000;
            mb.ProhibitSendKB = 160000;
            mb.ProhibitSendReceiveKB = 170000;
            mb.TotalItems = 5;
            mb.TotalSizeMB = 4;
            return mb;
        }

        public static ExchangeMailbox GetMailboxGeneralSettings(int itemId, int accountId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                return GetDemoMailboxSettings();
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_MAILBOX_GENERAL", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return null;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);
                return exchange.GetMailboxGeneralSettings(account.UserPrincipalName);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int SetMailboxGeneralSettings(int itemId, int accountId, bool hideAddressBook, bool disabled)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "UPDATE_MAILBOX_GENERAL", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);


                PackageContext cntx = PackageController.GetPackageContext(org.PackageId);

                if (Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_ISCONSUMER].QuotaAllocatedValue))
                    hideAddressBook = true;

                exchange.SetMailboxGeneralSettings(
                    account.UserPrincipalName,
                    hideAddressBook,
                    disabled);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static ExchangeEmailAddress[] GetMailboxEmailAddresses(int itemId, int accountId)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_MAILBOX_ADDRESSES", itemId);

            try
            {
                return GetAccountEmailAddresses(itemId, accountId);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int AddMailboxEmailAddress(int itemId, int accountId, string emailAddress)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "ADD_MAILBOX_ADDRESS", itemId);

            try
            {
                // check
                if (EmailAddressExists(emailAddress))
                    return BusinessErrorCodes.ERROR_EXCHANGE_EMAIL_EXISTS;

                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // add e-mail
                AddAccountEmailAddress(accountId, emailAddress);

                // update e-mail addresses
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.SetMailboxEmailAddresses(
                    account.UserPrincipalName,
                    GetAccountSimpleEmailAddresses(itemId, accountId));

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        private static OCSServer GetOCSProxy(int itemId)
        {
            Organization org = OrganizationController.GetOrganization(itemId);
            int serviceId = PackageController.GetPackageServiceId(org.PackageId, ResourceGroups.OCS);

            OCSServer ocs = new OCSServer();
            ServiceProviderProxy.Init(ocs, serviceId);


            return ocs;
        }

        public static int SetMailboxPrimaryEmailAddress(int itemId, int accountId, string emailAddress)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "SET_PRIMARY_MAILBOX_ADDRESS", itemId);

            try
            {
                // get account
                ExchangeAccount account = GetAccount(itemId, accountId);
                account.PrimaryEmailAddress = emailAddress;

                // update exchange
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.SetMailboxPrimaryEmailAddress(
                    account.UserPrincipalName,
                    emailAddress);

                if (DataProvider.CheckOCSUserExists(account.AccountId))
                {
                    OCSServer ocs = GetOCSProxy(itemId);
                    string instanceId = DataProvider.GetOCSUserInstanceID(account.AccountId);
                    ocs.SetUserPrimaryUri(instanceId, emailAddress);
                }

                if (DataProvider.CheckLyncUserExists(account.AccountId))
                {
                    LyncController.SetLyncUserGeneralSettings(itemId, accountId, emailAddress, null);
                }

                // save account
                account.AccountPassword = null;
                UpdateAccount(account);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int DeleteMailboxEmailAddresses(int itemId, int accountId, string[] emailAddresses)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "DELETE_MAILBOX_ADDRESSES", itemId);

            try
            {
                // get account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // delete e-mail addresses
                List<string> toDelete = new List<string>();
                foreach (string emailAddress in emailAddresses)
                {
                    if ((String.Compare(account.PrimaryEmailAddress, emailAddress, true) != 0) &
                        (String.Compare(account.UserPrincipalName, emailAddress, true) != 0))
                        toDelete.Add(emailAddress);
                }

                // delete from meta-base
                DeleteAccountEmailAddresses(accountId, toDelete.ToArray());

                // delete from Exchange
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // update e-mail addresses
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.SetMailboxEmailAddresses(
                    account.UserPrincipalName,
                    GetAccountSimpleEmailAddresses(itemId, accountId));

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static ExchangeMailbox GetMailboxMailFlowSettings(int itemId, int accountId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                return GetDemoMailboxSettings();
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_MAILBOX_MAILFLOW", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return null;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);
                ExchangeMailbox mailbox = exchange.GetMailboxMailFlowSettings(account.UserPrincipalName);
                mailbox.DisplayName = account.DisplayName;
                return mailbox;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int SetMailboxMailFlowSettings(int itemId, int accountId,
            bool enableForwarding, string forwardingAccountName, bool forwardToBoth,
            string[] sendOnBehalfAccounts, string[] acceptAccounts, string[] rejectAccounts,
            bool requireSenderAuthentication)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "UPDATE_MAILBOX_MAILFLOW", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.SetMailboxMailFlowSettings(account.UserPrincipalName,
                    enableForwarding,
                    forwardingAccountName,
                    forwardToBoth,
                    sendOnBehalfAccounts,
                    acceptAccounts,
                    rejectAccounts,
                    requireSenderAuthentication);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }


        public static ExchangeMailbox GetMailboxAdvancedSettings(int itemId, int accountId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                return GetDemoMailboxSettings();
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_MAILBOX_ADVANCED", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return null;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);
                ExchangeMailbox mailbox = exchange.GetMailboxAdvancedSettings(account.UserPrincipalName);
                mailbox.DisplayName = account.DisplayName;
                return mailbox;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int SetMailboxManagerSettings(int itemId, int accountId, bool pmmAllowed, MailboxManagerActions action)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "UPDATE_MAILBOX_GENERAL", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // PMM settings
                if (pmmAllowed) account.MailboxManagerActions |= action;
                else account.MailboxManagerActions &= ~action;

                // update account
                account.AccountPassword = null;
                UpdateAccount(account);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static string GetMailboxSetupInstructions(int itemId, int accountId, bool pmm, bool emailMode, bool signup)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                return string.Empty;
            }
            #endregion

            // load organization
            Organization org = GetOrganization(itemId);
            if (org == null)
                return null;

            // load user info
            UserInfo user = PackageController.GetPackageOwner(org.PackageId);

            // get letter settings
            UserSettings settings = UserController.GetUserSettings(user.UserId, UserSettings.EXCHANGE_MAILBOX_SETUP_LETTER);

            string settingName = user.HtmlMail ? "HtmlBody" : "TextBody";
            string body = settings[settingName];
            if (String.IsNullOrEmpty(body))
                return null;

            string result = EvaluateMailboxTemplate(itemId, accountId, pmm, false, false, body);
            return user.HtmlMail ? result : result.Replace("\n", "<br/>");
        }

        private static string EvaluateMailboxTemplate(int itemId, int accountId,
            bool pmm, bool emailMode, bool signup, string template)
        {
            Hashtable items = new Hashtable();

            // load organization
            Organization org = GetOrganization(itemId);
            if (org == null)
                return null;

            // add organization
            items["Organization"] = org;

            // load account
            ExchangeAccount account = GetAccount(itemId, accountId);
            if (account == null)
                return null;

            // add account
            items["Account"] = account;
            items["AccountDomain"] = account.PrimaryEmailAddress.Substring(account.PrimaryEmailAddress.IndexOf("@") + 1);
            items["DefaultDomain"] = org.DefaultDomain;

            if (!String.IsNullOrEmpty(account.SamAccountName))
            {
                int idx = account.SamAccountName.IndexOf("\\");
                items["SamDomain"] = account.SamAccountName.Substring(0, idx);
                items["SamUsername"] = account.SamAccountName.Substring(idx + 1);
            }

            // name servers
            PackageSettings packageSettings = PackageController.GetPackageSettings(org.PackageId, PackageSettings.NAME_SERVERS);
            string[] nameServers = new string[] { };
            if (!String.IsNullOrEmpty(packageSettings["NameServers"]))
                nameServers = packageSettings["NameServers"].Split(';');

            items["NameServers"] = nameServers;

            // service settings
            int exchangeServiceId = GetExchangeServiceID(org.PackageId);
            StringDictionary exchangeSettings = ServerController.GetServiceSettings(exchangeServiceId);
            if (exchangeSettings != null)
            {
                items["TempDomain"] = exchangeSettings["TempDomain"];
                items["AutodiscoverIP"] = exchangeSettings["AutodiscoverIP"];
                items["AutodiscoverDomain"] = exchangeSettings["AutodiscoverDomain"];
                items["OwaUrl"] = exchangeSettings["OwaUrl"];
                items["ActiveSyncServer"] = exchangeSettings["ActiveSyncServer"];
                items["SmtpServers"] = Utils.ParseDelimitedString(exchangeSettings["SmtpServers"], '\n');
            }

            items["Email"] = emailMode;
            items["Signup"] = signup;
            items["PMM"] = pmm;

            // evaluate template
            return PackageController.EvaluateTemplate(template, items);
        }

        public static int SendMailboxSetupInstructions(int itemId, int accountId, bool signup, string to, string cc)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo);
            if (accountCheck < 0) return accountCheck;

            // load organization
            Organization org = GetOrganization(itemId);
            if (org == null)
                return -1;

            // load user info
            UserInfo user = PackageController.GetPackageOwner(org.PackageId);

            // get letter settings
            UserSettings settings = UserController.GetUserSettings(user.UserId, UserSettings.EXCHANGE_MAILBOX_SETUP_LETTER);

            string from = settings["From"];
            if (cc == null)
                cc = settings["CC"];
            string subject = settings["Subject"];
            string body = user.HtmlMail ? settings["HtmlBody"] : settings["TextBody"];
            bool isHtml = user.HtmlMail;

            MailPriority priority = MailPriority.Normal;
            if (!String.IsNullOrEmpty(settings["Priority"]))
                priority = (MailPriority)Enum.Parse(typeof(MailPriority), settings["Priority"], true);

            if (String.IsNullOrEmpty(body))
                return 0;// BusinessErrorCodes.ERROR_SETTINGS_ACCOUNT_LETTER_EMPTY_BODY;

            // load user info
            if (to == null)
                to = user.Email;

            subject = EvaluateMailboxTemplate(itemId, accountId, false, true, signup, subject);
            body = EvaluateMailboxTemplate(itemId, accountId, false, true, signup, body);

            // send message
            return MailHelper.SendMessage(from, to, cc, subject, body, priority, isHtml);
        }


        public static ExchangeMailbox GetMailboxPermissions(int itemId, int accountId)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_MAILBOX_PERMISSIONS", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return null;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);
                ExchangeMailbox mailbox = exchange.GetMailboxPermissions(org.OrganizationId, account.UserPrincipalName);
                mailbox.DisplayName = account.DisplayName;
                return mailbox;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int SetMailboxPermissions(int itemId, int accountId, string[] sendAsaccounts, string[] fullAccessAcounts)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "SET_MAILBOX_PERMISSIONS", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.SetMailboxPermissions(org.OrganizationId, account.UserPrincipalName, sendAsaccounts, fullAccessAcounts);


                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }

        }

        #endregion


        #region Mailbox plan
        public static int SetExchangeMailboxPlan(int itemId, int accountId, int mailboxPlanId, int archivePlanId, bool EnableArchiving)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "SET_MAILBOXPLAN", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // load package context
                PackageContext cntx = PackageController.GetPackageContext(org.PackageId);

                int maxDiskSpace = -1;
                int quotaUsed = 0;
                if (cntx.Quotas.ContainsKey(Quotas.EXCHANGE2007_DISKSPACE)
                    && cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaAllocatedValue > 0)
                {
                    maxDiskSpace = cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaAllocatedValue;
                    quotaUsed = cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaUsedValue;
                }

                ExchangeMailboxPlan plan = GetExchangeMailboxPlan(itemId, mailboxPlanId);

                if (maxDiskSpace != -1)
                {
                    if (plan.MailboxSizeMB == -1)
                        return BusinessErrorCodes.ERROR_EXCHANGE_STORAGE_QUOTAS_EXCEED_HOST_VALUES;

                    ExchangeAccount exchangeAccount = GetAccount(itemId, accountId);
                    if (exchangeAccount.MailboxPlanId > 0)
                    {
                        ExchangeMailboxPlan oldPlan = GetExchangeMailboxPlan(itemId, exchangeAccount.MailboxPlanId);

                        if (((quotaUsed - oldPlan.MailboxSizeMB) + plan.MailboxSizeMB) > (maxDiskSpace))
                            return BusinessErrorCodes.ERROR_EXCHANGE_STORAGE_QUOTAS_EXCEED_HOST_VALUES;
                    }
                    else
                    {
                        if ((quotaUsed + plan.MailboxSizeMB) > (maxDiskSpace))
                            return BusinessErrorCodes.ERROR_EXCHANGE_STORAGE_QUOTAS_EXCEED_HOST_VALUES;
                    }
                }

                int maxRecoverableItemsSpace = -1;
                int quotaRecoverableItemsUsed = 0;
                if (cntx.Quotas.ContainsKey(Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE)
                    && cntx.Quotas[Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE].QuotaAllocatedValue > 0)
                {
                    maxRecoverableItemsSpace = cntx.Quotas[Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE].QuotaAllocatedValue;
                    quotaRecoverableItemsUsed = cntx.Quotas[Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE].QuotaUsedValue;
                }

                if (maxRecoverableItemsSpace != -1)
                {
                    if (plan.RecoverableItemsSpace == -1)
                        return BusinessErrorCodes.ERROR_EXCHANGE_STORAGE_QUOTAS_EXCEED_HOST_VALUES;

                    if ((quotaRecoverableItemsUsed + plan.RecoverableItemsSpace) > (maxRecoverableItemsSpace))
                        return BusinessErrorCodes.ERROR_EXCHANGE_STORAGE_QUOTAS_EXCEED_HOST_VALUES;
                }

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                //TDMX
                exchange.SetMailboxAdvancedSettings(
                    org.OrganizationId,
                    account.UserPrincipalName,
                    plan.EnablePOP,
                    plan.EnableIMAP,
                    plan.EnableOWA,
                    plan.EnableMAPI,
                    plan.EnableActiveSync,
                    plan.MailboxSizeMB != -1 ? (((long)plan.IssueWarningPct * (long)plan.MailboxSizeMB * 1024) / 100) : -1,
                    plan.MailboxSizeMB != -1 ? (((long)plan.ProhibitSendPct * (long)plan.MailboxSizeMB * 1024) / 100) : -1,
                    plan.MailboxSizeMB != -1 ? (((long)plan.ProhibitSendReceivePct * (long)plan.MailboxSizeMB * 1024) / 100) : -1,
                    plan.KeepDeletedItemsDays,
                    plan.MaxRecipients,
                    plan.MaxSendMessageSizeKB,
                    plan.MaxReceiveMessageSizeKB,
                    plan.AllowLitigationHold,
                    plan.RecoverableItemsSpace != -1 ? (plan.RecoverableItemsSpace * 1024) : -1,
                    plan.RecoverableItemsSpace != -1 ? (((long)plan.RecoverableItemsWarningPct * (long)plan.RecoverableItemsSpace * 1024) / 100) : -1,
                    plan.LitigationHoldUrl,
                    plan.LitigationHoldMsg);

                ResultObject resPolicy = new ResultObject() { IsSuccess = true };
                SetMailBoxRetentionPolicyAndArchiving(itemId, mailboxPlanId, archivePlanId, account.UserPrincipalName, exchange, org.OrganizationId, resPolicy, EnableArchiving);
                if (!resPolicy.IsSuccess)
                {
                    TaskManager.WriteError("Error SetMailBoxRetentionPolicy", resPolicy.ErrorCodes.ToArray());
                }

                DataProvider.SetExchangeAccountMailboxPlan(accountId, mailboxPlanId, archivePlanId, EnableArchiving);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static List<ExchangeMailboxPlan> GetExchangeMailboxPlans(int itemId, bool archiving)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_EXCHANGE_MAILBOXPLANS", itemId);

            try
            {
                List<ExchangeMailboxPlan> mailboxPlans = new List<ExchangeMailboxPlan>();

                UserInfo user = ObjectUtils.FillObjectFromDataReader<UserInfo>(DataProvider.GetUserByExchangeOrganizationIdInternally(itemId));

                if (user.Role == UserRole.User)
                    ExchangeServerController.GetExchangeMailboxPlansByUser(itemId, user, ref mailboxPlans, archiving);
                else
                    ExchangeServerController.GetExchangeMailboxPlansByUser(0, user, ref mailboxPlans, archiving);


                ExchangeOrganization ExchangeOrg = ObjectUtils.FillObjectFromDataReader<ExchangeOrganization>(DataProvider.GetExchangeOrganization(itemId));

                if (ExchangeOrg != null)
                {
                    foreach (ExchangeMailboxPlan p in mailboxPlans)
                    {
                        p.IsDefault = (p.MailboxPlanId == ExchangeOrg.ExchangeMailboxPlanID);
                    }
                }

                return mailboxPlans;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        private static void GetExchangeMailboxPlansByUser(int itemId, UserInfo user, ref List<ExchangeMailboxPlan> mailboxPlans, bool archiving)
        {
            if ((user != null))
            {
                List<Organization> orgs = null;

                if (user.UserId != 1)
                {
                    List<PackageInfo> Packages = PackageController.GetPackages(user.UserId);

                    if ((Packages != null) & (Packages.Count > 0))
                    {
                        orgs = GetExchangeOrganizationsInternal(Packages[0].PackageId, false);
                    }
                }
                else
                {
                    orgs = GetExchangeOrganizationsInternal(1, false);
                }

                int OrgId = -1;
                if (itemId > 0) OrgId = itemId;
                else if ((orgs != null) & (orgs.Count > 0)) OrgId = orgs[0].Id;


                if (OrgId != -1)
                {
                    List<ExchangeMailboxPlan> Plans = ObjectUtils.CreateListFromDataReader<ExchangeMailboxPlan>(DataProvider.GetExchangeMailboxPlans(OrgId, archiving));

                    foreach (ExchangeMailboxPlan p in Plans)
                    {
                        mailboxPlans.Add(p);
                    }
                }

                UserInfo owner = UserController.GetUserInternally(user.OwnerId);

                GetExchangeMailboxPlansByUser(0, owner, ref mailboxPlans, archiving);
            }
        }


        public static ExchangeMailboxPlan GetExchangeMailboxPlan(int itemID, int mailboxPlanId)
        {

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_EXCHANGE_MAILBOXPLAN", mailboxPlanId);

            try
            {
                return ObjectUtils.FillObjectFromDataReader<ExchangeMailboxPlan>(
                    DataProvider.GetExchangeMailboxPlan(mailboxPlanId));
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int AddExchangeMailboxPlan(int itemID, ExchangeMailboxPlan mailboxPlan)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "ADD_EXCHANGE_MAILBOXPLAN", itemID);

            try
            {
                Organization org = GetOrganization(itemID);
                if (org == null)
                    return -1;

                // load package context
                PackageContext cntx = PackageController.GetPackageContext(org.PackageId);

                if (org.PackageId > 1)
                {
                    mailboxPlan.EnableActiveSync = mailboxPlan.EnableActiveSync & Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_ACTIVESYNCALLOWED].QuotaAllocatedValue);
                    mailboxPlan.EnableIMAP = mailboxPlan.EnableIMAP & Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_IMAPALLOWED].QuotaAllocatedValue);
                    mailboxPlan.EnableMAPI = mailboxPlan.EnableMAPI & Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_MAPIALLOWED].QuotaAllocatedValue);
                    mailboxPlan.EnableOWA = mailboxPlan.EnableOWA & Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_OWAALLOWED].QuotaAllocatedValue);
                    mailboxPlan.EnablePOP = mailboxPlan.EnablePOP & Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_POP3ALLOWED].QuotaAllocatedValue);

                    if (cntx.Quotas[Quotas.EXCHANGE2007_KEEPDELETEDITEMSDAYS].QuotaAllocatedValue != -1)
                        if (mailboxPlan.KeepDeletedItemsDays > cntx.Quotas[Quotas.EXCHANGE2007_KEEPDELETEDITEMSDAYS].QuotaAllocatedValue)
                            mailboxPlan.KeepDeletedItemsDays = cntx.Quotas[Quotas.EXCHANGE2007_KEEPDELETEDITEMSDAYS].QuotaAllocatedValue;

                    if (mailboxPlan.Archiving)
                    {
                        if (cntx.Quotas[Quotas.EXCHANGE2013_ARCHIVINGSTORAGE].QuotaAllocatedValue != -1)
                            if (mailboxPlan.MailboxSizeMB > cntx.Quotas[Quotas.EXCHANGE2013_ARCHIVINGSTORAGE].QuotaAllocatedValue)
                                mailboxPlan.MailboxSizeMB = cntx.Quotas[Quotas.EXCHANGE2013_ARCHIVINGSTORAGE].QuotaAllocatedValue;
                    }
                    else
                    {
                        if (cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaAllocatedValue != -1)
                            if (mailboxPlan.MailboxSizeMB > cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaAllocatedValue)
                                mailboxPlan.MailboxSizeMB = cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaAllocatedValue;
                    }

                    if (cntx.Quotas[Quotas.EXCHANGE2007_MAXRECEIVEMESSAGESIZEKB].QuotaAllocatedValue != -1)
                        if (mailboxPlan.MaxReceiveMessageSizeKB > cntx.Quotas[Quotas.EXCHANGE2007_MAXRECEIVEMESSAGESIZEKB].QuotaAllocatedValue)
                            mailboxPlan.MaxReceiveMessageSizeKB = cntx.Quotas[Quotas.EXCHANGE2007_MAXRECEIVEMESSAGESIZEKB].QuotaAllocatedValue;

                    if (cntx.Quotas[Quotas.EXCHANGE2007_MAXSENDMESSAGESIZEKB].QuotaAllocatedValue != -1)
                        if (mailboxPlan.MaxSendMessageSizeKB > cntx.Quotas[Quotas.EXCHANGE2007_MAXSENDMESSAGESIZEKB].QuotaAllocatedValue)
                            mailboxPlan.MaxSendMessageSizeKB = cntx.Quotas[Quotas.EXCHANGE2007_MAXSENDMESSAGESIZEKB].QuotaAllocatedValue;

                    if (cntx.Quotas[Quotas.EXCHANGE2007_MAXRECIPIENTS].QuotaAllocatedValue != -1)
                        if (mailboxPlan.MaxRecipients > cntx.Quotas[Quotas.EXCHANGE2007_MAXRECIPIENTS].QuotaAllocatedValue)
                            mailboxPlan.MaxRecipients = cntx.Quotas[Quotas.EXCHANGE2007_MAXRECIPIENTS].QuotaAllocatedValue;

                    if (Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_ISCONSUMER].QuotaAllocatedValue)) mailboxPlan.HideFromAddressBook = true;

                    mailboxPlan.AllowLitigationHold = mailboxPlan.AllowLitigationHold & Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_ALLOWLITIGATIONHOLD].QuotaAllocatedValue);

                    if (cntx.Quotas[Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE].QuotaAllocatedValue != -1)
                        if (mailboxPlan.RecoverableItemsSpace > cntx.Quotas[Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE].QuotaAllocatedValue)
                            mailboxPlan.RecoverableItemsSpace = cntx.Quotas[Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE].QuotaAllocatedValue;
                }

                return DataProvider.AddExchangeMailboxPlan(itemID, mailboxPlan.MailboxPlan, mailboxPlan.EnableActiveSync, mailboxPlan.EnableIMAP, mailboxPlan.EnableMAPI, mailboxPlan.EnableOWA, mailboxPlan.EnablePOP,
                                                        mailboxPlan.IsDefault, mailboxPlan.IssueWarningPct, mailboxPlan.KeepDeletedItemsDays, mailboxPlan.MailboxSizeMB, mailboxPlan.MaxReceiveMessageSizeKB, mailboxPlan.MaxRecipients,
                                                        mailboxPlan.MaxSendMessageSizeKB, mailboxPlan.ProhibitSendPct, mailboxPlan.ProhibitSendReceivePct, mailboxPlan.HideFromAddressBook, mailboxPlan.MailboxPlanType,
                                                        mailboxPlan.AllowLitigationHold, mailboxPlan.RecoverableItemsSpace, mailboxPlan.RecoverableItemsWarningPct,
                                                        mailboxPlan.LitigationHoldUrl, mailboxPlan.LitigationHoldMsg, mailboxPlan.Archiving, mailboxPlan.EnableArchiving,
                                                        mailboxPlan.ArchiveSizeMB, mailboxPlan.ArchiveWarningPct);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }

        }


        public static int UpdateExchangeMailboxPlan(int itemID, ExchangeMailboxPlan mailboxPlan)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "UPDATE_EXCHANGE_MAILBOXPLAN", itemID);

            try
            {
                Organization org = GetOrganization(itemID);
                if (org == null)
                    return -1;

                // load package context
                PackageContext cntx = PackageController.GetPackageContext(org.PackageId);

                if (org.PackageId > 1)
                {
                    mailboxPlan.EnableActiveSync = mailboxPlan.EnableActiveSync & Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_ACTIVESYNCALLOWED].QuotaAllocatedValue);
                    mailboxPlan.EnableIMAP = mailboxPlan.EnableIMAP & Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_IMAPALLOWED].QuotaAllocatedValue);
                    mailboxPlan.EnableMAPI = mailboxPlan.EnableMAPI & Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_MAPIALLOWED].QuotaAllocatedValue);
                    mailboxPlan.EnableOWA = mailboxPlan.EnableOWA & Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_OWAALLOWED].QuotaAllocatedValue);
                    mailboxPlan.EnablePOP = mailboxPlan.EnablePOP & Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_POP3ALLOWED].QuotaAllocatedValue);

                    if (cntx.Quotas[Quotas.EXCHANGE2007_KEEPDELETEDITEMSDAYS].QuotaAllocatedValue != -1)
                        if (mailboxPlan.KeepDeletedItemsDays > cntx.Quotas[Quotas.EXCHANGE2007_KEEPDELETEDITEMSDAYS].QuotaAllocatedValue)
                            mailboxPlan.KeepDeletedItemsDays = cntx.Quotas[Quotas.EXCHANGE2007_KEEPDELETEDITEMSDAYS].QuotaAllocatedValue;

                    if (cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaAllocatedValue != -1)
                        if (mailboxPlan.MailboxSizeMB > cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaAllocatedValue)
                            mailboxPlan.MailboxSizeMB = cntx.Quotas[Quotas.EXCHANGE2007_DISKSPACE].QuotaAllocatedValue;

                    if (cntx.Quotas[Quotas.EXCHANGE2007_MAXRECEIVEMESSAGESIZEKB].QuotaAllocatedValue != -1)
                        if (mailboxPlan.MaxReceiveMessageSizeKB > cntx.Quotas[Quotas.EXCHANGE2007_MAXRECEIVEMESSAGESIZEKB].QuotaAllocatedValue)
                            mailboxPlan.MaxReceiveMessageSizeKB = cntx.Quotas[Quotas.EXCHANGE2007_MAXRECEIVEMESSAGESIZEKB].QuotaAllocatedValue;

                    if (cntx.Quotas[Quotas.EXCHANGE2007_MAXSENDMESSAGESIZEKB].QuotaAllocatedValue != -1)
                        if (mailboxPlan.MaxSendMessageSizeKB > cntx.Quotas[Quotas.EXCHANGE2007_MAXSENDMESSAGESIZEKB].QuotaAllocatedValue)
                            mailboxPlan.MaxSendMessageSizeKB = cntx.Quotas[Quotas.EXCHANGE2007_MAXSENDMESSAGESIZEKB].QuotaAllocatedValue;

                    if (cntx.Quotas[Quotas.EXCHANGE2007_MAXRECIPIENTS].QuotaAllocatedValue != -1)
                        if (mailboxPlan.MaxRecipients > cntx.Quotas[Quotas.EXCHANGE2007_MAXRECIPIENTS].QuotaAllocatedValue)
                            mailboxPlan.MaxRecipients = cntx.Quotas[Quotas.EXCHANGE2007_MAXRECIPIENTS].QuotaAllocatedValue;

                    if (Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_ISCONSUMER].QuotaAllocatedValue)) mailboxPlan.HideFromAddressBook = true;

                    mailboxPlan.AllowLitigationHold = mailboxPlan.AllowLitigationHold & Convert.ToBoolean(cntx.Quotas[Quotas.EXCHANGE2007_ALLOWLITIGATIONHOLD].QuotaAllocatedValue);

                    if (cntx.Quotas[Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE].QuotaAllocatedValue != -1)
                        if (mailboxPlan.RecoverableItemsSpace > cntx.Quotas[Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE].QuotaAllocatedValue)
                            mailboxPlan.RecoverableItemsSpace = cntx.Quotas[Quotas.EXCHANGE2007_RECOVERABLEITEMSSPACE].QuotaAllocatedValue;

                }

                DataProvider.UpdateExchangeMailboxPlan(mailboxPlan.MailboxPlanId, mailboxPlan.MailboxPlan, mailboxPlan.EnableActiveSync, mailboxPlan.EnableIMAP, mailboxPlan.EnableMAPI, mailboxPlan.EnableOWA, mailboxPlan.EnablePOP,
                                                        mailboxPlan.IsDefault, mailboxPlan.IssueWarningPct, mailboxPlan.KeepDeletedItemsDays, mailboxPlan.MailboxSizeMB, mailboxPlan.MaxReceiveMessageSizeKB, mailboxPlan.MaxRecipients,
                                                        mailboxPlan.MaxSendMessageSizeKB, mailboxPlan.ProhibitSendPct, mailboxPlan.ProhibitSendReceivePct, mailboxPlan.HideFromAddressBook, mailboxPlan.MailboxPlanType,
                                                        mailboxPlan.AllowLitigationHold, mailboxPlan.RecoverableItemsSpace, mailboxPlan.RecoverableItemsWarningPct,
                                                        mailboxPlan.LitigationHoldUrl, mailboxPlan.LitigationHoldMsg,
                                                        mailboxPlan.Archiving, mailboxPlan.EnableArchiving,
                                                        mailboxPlan.ArchiveSizeMB, mailboxPlan.ArchiveWarningPct);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }


            return 0;
        }



        public static int DeleteExchangeMailboxPlan(int itemID, int mailboxPlanId)
        {
            TaskManager.StartTask("EXCHANGE", "DELETE_EXCHANGE_MAILBOXPLAN", itemID);

            try
            {
                DataProvider.DeleteExchangeMailboxPlan(mailboxPlanId);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }

        }

        public static void SetOrganizationDefaultExchangeMailboxPlan(int itemId, int mailboxPlanId)
        {
            TaskManager.StartTask("EXCHANGE", "SET_EXCHANGE_MAILBOXPLAN", itemId);

            try
            {
                DataProvider.SetOrganizationDefaultExchangeMailboxPlan(itemId, mailboxPlanId);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }

        }



        #endregion

        #region Exchange Retention Policy Tags

        private static void SetMailBoxRetentionPolicyAndArchiving(int itemId, int mailboxPlanId, int retentionPolicyId, string accountName, ExchangeServer exchange, string orgId, ResultObject result, bool EnableArchiving)
        {

            long archiveQuotaKB = 0;
            long archiveWarningQuotaKB = 0;
            string RetentionPolicy = "";
            if (retentionPolicyId > 0)
            {
                ExchangeMailboxPlan mailboxPlan = GetExchangeMailboxPlan(itemId, mailboxPlanId);
                if ( mailboxPlan != null)
                {
                    archiveQuotaKB = mailboxPlan.ArchiveSizeMB != -1 ? ((long)mailboxPlan.ArchiveSizeMB * 1024) : -1;
                    archiveWarningQuotaKB = mailboxPlan.ArchiveSizeMB != -1 ? (((long)mailboxPlan.ArchiveWarningPct * (long) mailboxPlan.ArchiveSizeMB * 1024) / 100) : -1;
                }


                ExchangeMailboxPlan retentionPolicy = GetExchangeMailboxPlan(itemId, retentionPolicyId);
                if (retentionPolicy != null)
                {
                    UpdateExchangeRetentionPolicy(itemId, retentionPolicyId, result);
                }

            }
            ResultObject res = exchange.SetMailBoxArchiving(orgId, accountName, EnableArchiving, archiveQuotaKB, archiveWarningQuotaKB, RetentionPolicy);
            if (res != null)
            {
                result.ErrorCodes.AddRange(res.ErrorCodes);
                result.IsSuccess = result.IsSuccess && res.IsSuccess;
            }
        }

        public static List<ExchangeRetentionPolicyTag> GetExchangeRetentionPolicyTags(int itemId)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_EXCHANGE_RETENTIONPOLICYTAGS", itemId);

            try
            {
                List<ExchangeRetentionPolicyTag> retentionPolicyTags = new List<ExchangeRetentionPolicyTag>();

                UserInfo user = ObjectUtils.FillObjectFromDataReader<UserInfo>(DataProvider.GetUserByExchangeOrganizationIdInternally(itemId));

                if (user.Role == UserRole.User)
                    ExchangeServerController.GetExchangeRetentionPolicyTagsByUser(itemId, user, ref retentionPolicyTags);
                else
                    ExchangeServerController.GetExchangeRetentionPolicyTagsByUser(0, user, ref retentionPolicyTags);

                return retentionPolicyTags;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        private static void GetExchangeRetentionPolicyTagsByUser(int itemId, UserInfo user, ref List<ExchangeRetentionPolicyTag> retentionPolicyTags)
        {
            if ((user != null))
            {
                List<Organization> orgs = null;

                if (user.UserId != 1)
                {
                    List<PackageInfo> Packages = PackageController.GetPackages(user.UserId);

                    if ((Packages != null) & (Packages.Count > 0))
                    {
                        orgs = GetExchangeOrganizationsInternal(Packages[0].PackageId, false);
                    }
                }
                else
                {
                    orgs = GetExchangeOrganizationsInternal(1, false);
                }

                int OrgId = -1;
                if (itemId > 0) OrgId = itemId;
                else if ((orgs != null) & (orgs.Count > 0)) OrgId = orgs[0].Id;


                if (OrgId != -1)
                {
                    List<ExchangeRetentionPolicyTag> RetentionPolicy = ObjectUtils.CreateListFromDataReader<ExchangeRetentionPolicyTag>(DataProvider.GetExchangeRetentionPolicyTags(OrgId));

                    foreach (ExchangeRetentionPolicyTag p in RetentionPolicy)
                    {
                        retentionPolicyTags.Add(p);
                    }
                }

                UserInfo owner = UserController.GetUserInternally(user.OwnerId);

                GetExchangeRetentionPolicyTagsByUser(0, owner, ref retentionPolicyTags);
            }
        }

        public static ExchangeRetentionPolicyTag GetExchangeRetentionPolicyTag(int itemID, int tagId)
        {

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_EXCHANGE_RETENTIONPOLICYTAG", tagId);

            try
            {
                return ObjectUtils.FillObjectFromDataReader<ExchangeRetentionPolicyTag>(
                    DataProvider.GetExchangeRetentionPolicyTag(tagId));
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static IntResult AddExchangeRetentionPolicyTag(int itemID, ExchangeRetentionPolicyTag tag)
        {
            // place log record
            IntResult res = TaskManager.StartResultTask<IntResult>("EXCHANGE", "ADD_EXCHANGE_RETENTIONPOLICYTAG", itemID);

            Organization org;
            try
            {
                org = GetOrganization(itemID);
                if (org == null)
                    throw new ApplicationException("Organization is null");
            }
            catch (Exception ex)
            {
                TaskManager.CompleteResultTask(res, ErrorCodes.CANNOT_GET_ORGANIZATION_BY_ITEM_ID, ex);
                return res;
            }

            try
            {
                // load package context
                PackageContext cntx = PackageController.GetPackageContext(org.PackageId);

                if (org.PackageId > 1)
                {
                    // quotas
                }

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);

                int tagId = DataProvider.AddExchangeRetentionPolicyTag(itemID, tag.TagName, tag.TagType, tag.AgeLimitForRetention, tag.RetentionAction );
                tag.TagID = tagId;

                if (exchangeServiceId > 0)
                {
                    ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                    ResultObject resTag = exchange.SetRetentionPolicyTag(tag.WSPUniqueName, (ExchangeRetentionPolicyTagType)tag.TagType, tag.AgeLimitForRetention, (ExchangeRetentionPolicyTagAction)tag.RetentionAction);
                    res.ErrorCodes.AddRange(resTag.ErrorCodes);
                    res.IsSuccess = res.IsSuccess && resTag.IsSuccess;
                }

                if (res.IsSuccess)
                    res.Value = tagId;
                else
                    DataProvider.DeleteExchangeRetentionPolicyTag(tagId);
            }
            catch (Exception ex)
            {
                TaskManager.WriteError(ex);
                TaskManager.CompleteResultTask(res);
                return res;
            }

            TaskManager.CompleteResultTask();
            return res;
        }

        public static ResultObject UpdateExchangeRetentionPolicyTag(int itemID, ExchangeRetentionPolicyTag tag)
        {
            // place log record
            ResultObject res = TaskManager.StartResultTask<ResultObject>("EXCHANGE", "UPDATE_EXCHANGE_RETENTIONPOLICYTAG", itemID);

            Organization org;
            try
            {
                org = GetOrganization(itemID);
                if (org == null)
                    throw new ApplicationException("Organization is null");
            }
            catch (Exception ex)
            {
                TaskManager.CompleteResultTask(res, ErrorCodes.CANNOT_GET_ORGANIZATION_BY_ITEM_ID, ex);
                return res;
            }

            try
            {
                // load package context
                PackageContext cntx = PackageController.GetPackageContext(org.PackageId);

                if (org.PackageId > 1)
                {
                    // quotas
                }

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);

                if (exchangeServiceId > 0)
                {
                    ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                    ResultObject resTag = exchange.SetRetentionPolicyTag(tag.WSPUniqueName, (ExchangeRetentionPolicyTagType)tag.TagType, tag.AgeLimitForRetention, (ExchangeRetentionPolicyTagAction)tag.RetentionAction);
                    res.ErrorCodes.AddRange(resTag.ErrorCodes);
                    res.IsSuccess = res.IsSuccess && resTag.IsSuccess;
                }

                if (res.IsSuccess)
                    DataProvider.UpdateExchangeRetentionPolicyTag(tag.TagID, tag.ItemID, tag.TagName, tag.TagType, tag.AgeLimitForRetention, tag.RetentionAction);
            }
            catch (Exception ex)
            {
                TaskManager.WriteError(ex);
                TaskManager.CompleteResultTask(res);
                return res;
            }

            TaskManager.CompleteResultTask();
            return res;
        }

        public static ResultObject DeleteExchangeRetentionPolicyTag(int itemID, int tagId)
        {
            ResultObject res = TaskManager.StartResultTask<ResultObject>("EXCHANGE", "DELETE_EXCHANGE_RETENTIONPOLICYTAG", itemID);

            Organization org;
            try
            {
                org = GetOrganization(itemID);
                if (org == null)
                    throw new ApplicationException("Organization is null");
            }
            catch (Exception ex)
            {
                TaskManager.CompleteResultTask(res, ErrorCodes.CANNOT_GET_ORGANIZATION_BY_ITEM_ID, ex);
                return res;
            }

            try
            {
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                if (exchangeServiceId > 0)
                {
                    // load package context
                    PackageContext cntx = PackageController.GetPackageContext(org.PackageId);

                    if (org.PackageId > 1)
                    {
                        // quotas
                    }

                    ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                    ExchangeRetentionPolicyTag tag = GetExchangeRetentionPolicyTag(itemID, tagId);
                    if (tag == null) throw new ApplicationException("Tag is null");

                    ResultObject resTag = exchange.RemoveRetentionPolicyTag(tag.WSPUniqueName);
                    res.ErrorCodes.AddRange(resTag.ErrorCodes);
                    res.IsSuccess = res.IsSuccess && resTag.IsSuccess;

                }
                
                if (res.IsSuccess)
                    DataProvider.DeleteExchangeRetentionPolicyTag(tagId);

            }
            catch (Exception ex)
            {
                TaskManager.WriteError(ex);
                TaskManager.CompleteResultTask(res);
                return res;
            }

            TaskManager.CompleteResultTask();
            return res;
        }


        public static List<ExchangeMailboxPlanRetentionPolicyTag> GetExchangeMailboxPlanRetentionPolicyTags(int policyId)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_EXCHANGE_RETENTIONPOLICYTAGS", policyId);

            try
            {
                List<ExchangeMailboxPlanRetentionPolicyTag> tags =
                    ObjectUtils.CreateListFromDataReader<ExchangeMailboxPlanRetentionPolicyTag>(DataProvider.GetExchangeMailboxPlanRetentionPolicyTags(policyId));

                return tags;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }


        private static void UpdateExchangeRetentionPolicy(int itemID, int policyId, ResultObject result)
        {
            Organization org = GetOrganization(itemID);
            if (org == null)
                return;

            int exchangeServiceId = GetExchangeServiceID(org.PackageId);

            if (exchangeServiceId > 0)
            {
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                ExchangeMailboxPlan policy = GetExchangeMailboxPlan(itemID, policyId);

                if (policy != null)
                {
                    List<ExchangeMailboxPlanRetentionPolicyTag> policytaglist = GetExchangeMailboxPlanRetentionPolicyTags(policyId);

                    List<string> tagLinks = new List<string>();

                    foreach (ExchangeMailboxPlanRetentionPolicyTag policytag in policytaglist)
                    {
                        ExchangeRetentionPolicyTag tag = GetExchangeRetentionPolicyTag(itemID, policytag.TagID);
                        tagLinks.Add(tag.WSPUniqueName);

                        // update PlanRetentionPolicyTags

                        ResultObject resItem = exchange.SetRetentionPolicyTag(tag.WSPUniqueName, (ExchangeRetentionPolicyTagType)tag.TagType, tag.AgeLimitForRetention, (ExchangeRetentionPolicyTagAction)tag.RetentionAction);
                        result.ErrorCodes.AddRange(resItem.ErrorCodes);
                        result.IsSuccess = result.IsSuccess && resItem.IsSuccess;
                    }

                    ResultObject res = exchange.SetRetentionPolicy(policy.WSPUniqueName, tagLinks.ToArray());
                    result.ErrorCodes.AddRange(res.ErrorCodes);
                    result.IsSuccess = result.IsSuccess && res.IsSuccess;
                }

            }
        }

        public static IntResult AddExchangeMailboxPlanRetentionPolicyTag(int itemID, ExchangeMailboxPlanRetentionPolicyTag planTag)
        {
            // place log record
            IntResult res = TaskManager.StartResultTask<IntResult>("EXCHANGE", "ADD_EXCHANGE_RETENTIONPOLICYTAG", itemID);

            Organization org;
            try
            {
                org = GetOrganization(itemID);
                if (org == null)
                    throw new ApplicationException("Organization is null");
            }
            catch (Exception ex)
            {
                TaskManager.CompleteResultTask(res, ErrorCodes.CANNOT_GET_ORGANIZATION_BY_ITEM_ID, ex);
                return res;
            }

            try
            {
                // load package context
                PackageContext cntx = PackageController.GetPackageContext(org.PackageId);

                if (org.PackageId > 1)
                {
                    // quotas
                }

                res.Value = DataProvider.AddExchangeMailboxPlanRetentionPolicyTag(planTag.TagID, planTag.MailboxPlanId);

                UpdateExchangeRetentionPolicy(itemID, planTag.MailboxPlanId, res);

            }
            catch (Exception ex)
            {
                TaskManager.WriteError(ex);
                TaskManager.CompleteResultTask(res);
                return res;
            }

            TaskManager.CompleteResultTask();
            return res;

        }

        public static ResultObject DeleteExchangeMailboxPlanRetentionPolicyTag(int itemID, int policyId, int planTagId)
        {
            ResultObject res = TaskManager.StartResultTask<ResultObject>("EXCHANGE", "DELETE_EXCHANGE_RETENTIONPOLICYTAG", itemID);

            Organization org;
            try
            {
                org = GetOrganization(itemID);
                if (org == null)
                    throw new ApplicationException("Organization is null");
            }
            catch (Exception ex)
            {
                TaskManager.CompleteResultTask(res, ErrorCodes.CANNOT_GET_ORGANIZATION_BY_ITEM_ID, ex);
                return res;
            }

            try
            {
                // load package context
                PackageContext cntx = PackageController.GetPackageContext(org.PackageId);

                if (org.PackageId > 1)
                {
                    // quotas
                }

                DataProvider.DeleteExchangeMailboxPlanRetentionPolicyTag(planTagId);

                UpdateExchangeRetentionPolicy(itemID, policyId,res);
            }
            catch (Exception ex)
            {
                TaskManager.WriteError(ex);
                TaskManager.CompleteResultTask(res);
                return res;
            }

            TaskManager.CompleteResultTask();
            return res;

        }

        #endregion

        #region Contacts
        public static int CreateContact(int itemId, string displayName, string email)
        {
            //if (EmailAddressExists(email))
            //  return BusinessErrorCodes.ERROR_EXCHANGE_EMAIL_EXISTS;


            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // check mailbox quota
            OrganizationStatistics orgStats = GetOrganizationStatistics(itemId);
            if (orgStats.AllocatedContacts > -1
                && orgStats.CreatedContacts >= orgStats.AllocatedContacts)
                return BusinessErrorCodes.ERROR_EXCHANGE_CONTACTS_QUOTA_LIMIT;

            // place log record
            TaskManager.StartTask("EXCHANGE", "CREATE_CONTACT", itemId);

            try
            {

                displayName = displayName.Trim();
                email = email.Trim();

                // load organization
                Organization org = GetOrganization(itemId);

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                string name = email;
                int idx = email.IndexOf("@");
                if (idx > -1)
                    name = email.Substring(0, idx);

                string accountName = OrganizationController.BuildAccountNameEx(org, name);

                // add contact
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                //Create Exchange Organization
                if (string.IsNullOrEmpty(org.GlobalAddressList))
                {
                    ExtendToExchangeOrganization(ref org);

                    PackageController.UpdatePackageItem(org);
                }

                exchange.CreateContact(
                    org.OrganizationId,
                    org.DistinguishedName,
                    displayName,
                    accountName,
                    email, org.DefaultDomain);

                ExchangeContact contact = exchange.GetContactGeneralSettings(accountName);

                // add meta-item
                int accountId = AddAccount(itemId, ExchangeAccountType.Contact, accountName,
                    displayName, email, false,
                    0, contact.SAMAccountName, null, 0, null);

                return accountId;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int DeleteContact(int itemId, int accountId)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "DELETE_CONTACT", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // delete contact
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.DeleteContact(account.AccountName);

                // remove meta-item
                DeleteAccount(itemId, accountId);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        private static ExchangeContact GetDemoContactSettings()
        {
            ExchangeContact c = new ExchangeContact();
            c.DisplayName = "WebsitePanel Support";
            c.AccountName = "wsp_fabrikam";
            c.FirstName = "WebsitePanel";
            c.LastName = "Support";
            c.EmailAddress = "support@websitepanel.net";
            c.AcceptAccounts = GetAccounts(0, ExchangeAccountType.Mailbox).ToArray();
            return c;
        }

        public static ExchangeContact GetContactGeneralSettings(int itemId, int accountId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                return GetDemoContactSettings();
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_CONTACT_GENERAL", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return null;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                return exchange.GetContactGeneralSettings(account.AccountName);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int SetContactGeneralSettings(int itemId, int accountId, string displayName, string emailAddress,
            bool hideAddressBook, string firstName, string initials,
            string lastName, string address, string city, string state, string zip, string country,
            string jobTitle, string company, string department, string office, string managerAccountName,
            string businessPhone, string fax, string homePhone, string mobilePhone, string pager,
            string webPage, string notes, int useMapiRichTextFormat)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "UPDATE_CONTACT_GENERAL", itemId);

            try
            {
                displayName = displayName.Trim();
                emailAddress = emailAddress.Trim();
                firstName = firstName.Trim();
                lastName = lastName.Trim();

                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.SetContactGeneralSettings(
                    account.AccountName,
                    displayName,
                    emailAddress,
                    hideAddressBook,
                    firstName,
                    initials,
                    lastName,
                    address,
                    city,
                    state,
                    zip,
                    country,
                    jobTitle,
                    company,
                    department,
                    office,
                    managerAccountName,
                    businessPhone,
                    fax,
                    homePhone,
                    mobilePhone,
                    pager,
                    webPage,
                    notes,
                    useMapiRichTextFormat, org.DefaultDomain);

                // update account
                account.DisplayName = displayName;
                account.PrimaryEmailAddress = emailAddress;
                account.AccountPassword = null;
                UpdateAccount(account);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static ExchangeContact GetContactMailFlowSettings(int itemId, int accountId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                return GetDemoContactSettings();
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_CONTACT_MAILFLOW", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return null;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                ExchangeContact contact = exchange.GetContactMailFlowSettings(account.AccountName);
                contact.DisplayName = account.DisplayName;
                return contact;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int SetContactMailFlowSettings(int itemId, int accountId,
            string[] acceptAccounts, string[] rejectAccounts, bool requireSenderAuthentication)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "UPDATE_CONTACT_MAILFLOW", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.SetContactMailFlowSettings(account.AccountName,
                    acceptAccounts,
                    rejectAccounts,
                    requireSenderAuthentication);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }
        #endregion

        #region Distribution Lists
        public static int CreateDistributionList(int itemId, string displayName, string name, string domain, int managerId)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // check mailbox quota
            OrganizationStatistics orgStats = GetOrganizationStatistics(itemId);
            if (orgStats.AllocatedDistributionLists > -1
                && orgStats.CreatedDistributionLists >= orgStats.AllocatedDistributionLists)
                return BusinessErrorCodes.ERROR_EXCHANGE_DLISTS_QUOTA_LIMIT;

            // place log record
            TaskManager.StartTask("EXCHANGE", "CREATE_DISTR_LIST", itemId);

            try
            {
                displayName = displayName.Trim();
                name = name.Trim();
                domain = domain.Trim();

                // e-mail
                string email = name + "@" + domain;

                // check e-mail
                if (EmailAddressExists(email))
                    return BusinessErrorCodes.ERROR_EXCHANGE_EMAIL_EXISTS;

                // load organization
                Organization org = GetOrganization(itemId);

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                string accountName = OrganizationController.BuildAccountNameEx(org, name);

                // add account
                // add contact
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                //Create Exchange Organization
                if (string.IsNullOrEmpty(org.GlobalAddressList))
                {
                    ExtendToExchangeOrganization(ref org);

                    PackageController.UpdatePackageItem(org);
                }

                OrganizationUser manager = OrganizationController.GetAccount(itemId, managerId);

                List<string> addressLists = new List<string>();
                addressLists.Add(org.GlobalAddressList);
                addressLists.Add(org.AddressList);

                exchange.CreateDistributionList(
                    org.OrganizationId,
                    org.DistinguishedName,
                    displayName,
                    accountName,
                    name,
                    domain, manager.SamAccountName, addressLists.ToArray());

                ExchangeDistributionList dl = exchange.GetDistributionListGeneralSettings(accountName);

                // add meta-item
                int accountId = AddAccount(itemId, ExchangeAccountType.DistributionList, accountName,
                    displayName, email, false,
                    0, dl.SAMAccountName, null, 0, null);

                // register email address
                AddAccountEmailAddress(accountId, email);

                return accountId;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int DeleteDistributionList(int itemId, int accountId)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "DELETE_DISTR_LIST", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // delete mailbox
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.DeleteDistributionList(account.AccountName);

                // unregister account
                DeleteAccount(itemId, accountId);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        private static ExchangeDistributionList GetDemoDistributionListSettings()
        {
            ExchangeDistributionList c = new ExchangeDistributionList();
            c.DisplayName = "Fabrikam Sales";
            c.AccountName = "sales_fabrikam";
            c.ManagerAccount = GetAccounts(0, ExchangeAccountType.Mailbox)[0];
            c.MembersAccounts = GetAccounts(0, ExchangeAccountType.Mailbox).ToArray();
            c.AcceptAccounts = GetAccounts(0, ExchangeAccountType.Mailbox).ToArray();
            return c;
        }

        public static ExchangeDistributionList GetDistributionListGeneralSettings(int itemId, int accountId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                return GetDemoDistributionListSettings();
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_DISTR_LIST_GENERAL", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return null;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                return exchange.GetDistributionListGeneralSettings(account.AccountName);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int SetDistributionListGeneralSettings(int itemId, int accountId, string displayName,
            bool hideAddressBook, string managerAccount, string[] memberAccounts,
            string notes)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "UPDATE_DISTR_LIST_GENERAL", itemId);

            try
            {
                displayName = displayName.Trim();

                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                List<string> addressLists = new List<string>();
                addressLists.Add(org.GlobalAddressList);
                addressLists.Add(org.AddressList);

                exchange.SetDistributionListGeneralSettings(
                    account.AccountName,
                    displayName,
                    hideAddressBook,
                    managerAccount,
                    memberAccounts,
                    notes,
                    addressLists.ToArray());

                // update account
                account.DisplayName = displayName;
                account.AccountPassword = null;
                UpdateAccount(account);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static ExchangeDistributionList GetDistributionListMailFlowSettings(int itemId, int accountId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                return GetDemoDistributionListSettings();
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_DISTR_LIST_MAILFLOW", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return null;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                ExchangeDistributionList list = exchange.GetDistributionListMailFlowSettings(account.AccountName);
                list.DisplayName = account.DisplayName;
                return list;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int SetDistributionListMailFlowSettings(int itemId, int accountId,
            string[] acceptAccounts, string[] rejectAccounts, bool requireSenderAuthentication)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "UPDATE_DISTR_LIST_MAILFLOW", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                List<string> addressLists = new List<string>();
                addressLists.Add(org.GlobalAddressList);
                addressLists.Add(org.AddressList);


                exchange.SetDistributionListMailFlowSettings(account.AccountName,
                    acceptAccounts,
                    rejectAccounts,
                    requireSenderAuthentication,
                    addressLists.ToArray());

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static ExchangeEmailAddress[] GetDistributionListEmailAddresses(int itemId, int accountId)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_DISTR_LIST_ADDRESSES", itemId);

            try
            {
                return GetAccountEmailAddresses(itemId, accountId);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int AddDistributionListEmailAddress(int itemId, int accountId, string emailAddress)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "ADD_DISTR_LIST_ADDRESS", itemId);

            try
            {
                // check
                if (EmailAddressExists(emailAddress))
                    return BusinessErrorCodes.ERROR_EXCHANGE_EMAIL_EXISTS;

                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // add e-mail
                AddAccountEmailAddress(accountId, emailAddress);

                // update e-mail addresses
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                List<string> addressLists = new List<string>();
                addressLists.Add(org.GlobalAddressList);
                addressLists.Add(org.AddressList);

                exchange.SetDistributionListEmailAddresses(
                    account.AccountName,
                    GetAccountSimpleEmailAddresses(itemId, accountId), addressLists.ToArray());

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int SetDistributionListPrimaryEmailAddress(int itemId, int accountId, string emailAddress)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "SET_PRIMARY_DISTR_LIST_ADDRESS", itemId);

            try
            {
                // get account
                ExchangeAccount account = GetAccount(itemId, accountId);
                account.PrimaryEmailAddress = emailAddress;

                // update exchange
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                List<string> addressLists = new List<string>();
                addressLists.Add(org.GlobalAddressList);
                addressLists.Add(org.AddressList);

                exchange.SetDistributionListPrimaryEmailAddress(
                    account.AccountName,
                    emailAddress,
                    addressLists.ToArray());

                // save account
                account.AccountPassword = null;
                UpdateAccount(account);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int DeleteDistributionListEmailAddresses(int itemId, int accountId, string[] emailAddresses)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "DELETE_DISTR_LIST_ADDRESSES", itemId);

            try
            {
                // get account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // delete e-mail addresses
                List<string> toDelete = new List<string>();
                foreach (string emailAddress in emailAddresses)
                {
                    if (String.Compare(account.PrimaryEmailAddress, emailAddress, true) != 0)
                        toDelete.Add(emailAddress);
                }

                // delete from meta-base
                DeleteAccountEmailAddresses(accountId, toDelete.ToArray());

                // delete from Exchange
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // update e-mail addresses
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                List<string> addressLists = new List<string>();
                addressLists.Add(org.GlobalAddressList);
                addressLists.Add(org.AddressList);

                exchange.SetDistributionListEmailAddresses(
                    account.AccountName,
                    GetAccountSimpleEmailAddresses(itemId, accountId), addressLists.ToArray());

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }


        public static ResultObject SetDistributionListPermissions(int itemId, int accountId, string[] sendAsAccounts, string[] sendOnBehalfAccounts)
        {
            ResultObject res = TaskManager.StartResultTask<ResultObject>("EXCHANGE", "SET_DISTRIBUTION_LIST_PERMISSINS");
            Organization org;
            try
            {
                org = GetOrganization(itemId);
                if (org == null)
                    throw new ApplicationException("Organization is null");
            }
            catch (Exception ex)
            {
                TaskManager.CompleteResultTask(res, ErrorCodes.CANNOT_GET_ORGANIZATION_BY_ITEM_ID, ex);
                return res;
            }

            ExchangeServer exchange;
            try
            {

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);
            }
            catch (Exception ex)
            {
                TaskManager.CompleteResultTask(res, ErrorCodes.CANNOT_GET_ORGANIZATION_PROXY, ex);
                return res;
            }

            ExchangeAccount account;

            try
            {
                account = GetAccount(itemId, accountId);
            }
            catch (Exception ex)
            {
                TaskManager.CompleteResultTask(res, ErrorCodes.CANNOT_GET_ACCOUNT, ex);
                return res;
            }

            try
            {
                List<string> addressLists = new List<string>();
                addressLists.Add(org.GlobalAddressList);
                addressLists.Add(org.AddressList);

                exchange.SetDistributionListPermissions(org.OrganizationId, account.AccountName, sendAsAccounts,
                                                        sendOnBehalfAccounts, addressLists.ToArray());
            }
            catch (Exception ex)
            {
                TaskManager.CompleteResultTask(res, ErrorCodes.CANNOT_SET_DISTRIBUTION_LIST_PERMISSIONS, ex);
                return res;
            }

            TaskManager.CompleteTask();
            return res;
        }

        public static ExchangeDistributionListResult GetDistributionListPermissions(int itemId, int accountId)
        {
            Organization org;
            ExchangeDistributionListResult res = TaskManager.StartResultTask<ExchangeDistributionListResult>("EXCHANGE", "GET_DISTRIBUTION_LIST_RESULT");

            try
            {
                org = GetOrganization(itemId);
                if (org == null)
                    throw new ApplicationException("Organization is null");
            }
            catch (Exception ex)
            {
                TaskManager.CompleteResultTask(res, ErrorCodes.CANNOT_GET_ORGANIZATION_BY_ITEM_ID, ex);
                return res;
            }

            ExchangeServer exchange;
            try
            {
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);
            }
            catch (Exception ex)
            {
                TaskManager.CompleteResultTask(res, ErrorCodes.CANNOT_GET_ORGANIZATION_PROXY, ex);
                return res;
            }

            ExchangeAccount account;
            try
            {
                account = GetAccount(itemId, accountId);
            }
            catch (Exception ex)
            {
                TaskManager.CompleteResultTask(res, ErrorCodes.CANNOT_GET_ACCOUNT, ex);
                return res;
            }

            try
            {
                res.Value = exchange.GetDistributionListPermissions(org.OrganizationId, account.AccountName);
                res.Value.DisplayName = account.DisplayName;
            }
            catch (Exception ex)
            {
                TaskManager.CompleteResultTask(res, ErrorCodes.CANNOT_GET_DISTRIBUTION_LIST_PERMISSIONS, ex);
                return res;
            }

            TaskManager.CompleteTask();
            return res;
        }

        public static ExchangeAccount[] GetDistributionListsByMember(int itemId, int accountId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                return null;
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_DISTR_LIST_BYMEMBER");
            TaskManager.ItemId = itemId;

            List<ExchangeAccount> ret = new List<ExchangeAccount>();

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return null;

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                List<ExchangeAccount> DistributionLists = GetAccounts(itemId, ExchangeAccountType.DistributionList);
                foreach (ExchangeAccount DistributionAccount in DistributionLists)
                {
                    ExchangeDistributionList DistributionList = exchange.GetDistributionListGeneralSettings(DistributionAccount.AccountName);

                    foreach (ExchangeAccount member in DistributionList.MembersAccounts)
                    {
                        if (member.AccountName == account.AccountName)
                        {
                            ret.Add(DistributionAccount);
                            break;
                        }

                    }
                }

                return ret.ToArray();
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int AddDistributionListMember(int itemId, string distributionListName, int memberId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                return 0;
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "ADD_DISTR_LIST_MEMBER");
            TaskManager.ItemId = itemId;

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return 0;

                // load account
                ExchangeAccount memberAccount = GetAccount(itemId, memberId);

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                ExchangeDistributionList distributionList = exchange.GetDistributionListGeneralSettings(distributionListName);

                List<string> members = new List<string>();
                foreach (ExchangeAccount member in distributionList.MembersAccounts)
                    members.Add(member.AccountName);
                members.Add(memberAccount.AccountName);

                List<string> addressLists = new List<string>();
                addressLists.Add(org.GlobalAddressList);
                addressLists.Add(org.AddressList);

                exchange.SetDistributionListGeneralSettings(distributionListName, distributionList.DisplayName, distributionList.HideFromAddressBook, distributionList.ManagerAccount.AccountName,
                    members.ToArray(),
                    distributionList.Notes, addressLists.ToArray());

            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }

            return 0;
        }

        public static int DeleteDistributionListMember(int itemId, string distributionListName, int memberId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                return 0;
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "DELETE_DISTR_LIST_MEMBER");
            TaskManager.ItemId = itemId;

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return 0;

                // load account
                ExchangeAccount memberAccount = GetAccount(itemId, memberId);

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                ExchangeDistributionList distributionList = exchange.GetDistributionListGeneralSettings(distributionListName);

                List<string> members = new List<string>();
                foreach (ExchangeAccount member in distributionList.MembersAccounts)
                    if (member.AccountName != memberAccount.AccountName) members.Add(member.AccountName);

                List<string> addressLists = new List<string>();
                addressLists.Add(org.GlobalAddressList);
                addressLists.Add(org.AddressList);

                exchange.SetDistributionListGeneralSettings(distributionListName, distributionList.DisplayName, distributionList.HideFromAddressBook, distributionList.ManagerAccount.AccountName,
                    members.ToArray(),
                    distributionList.Notes, addressLists.ToArray());

            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }

            return 0;
        }


        #endregion

        #region Public Folders
        public static int CreatePublicFolder(int itemId, string parentFolder, string folderName,
            bool mailEnabled, string name, string domain)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // check mailbox quota
            OrganizationStatistics orgStats = GetOrganizationStatistics(itemId);
            if (orgStats.AllocatedPublicFolders > -1
                && orgStats.CreatedPublicFolders >= orgStats.AllocatedPublicFolders)
                return BusinessErrorCodes.ERROR_EXCHANGE_PFOLDERS_QUOTA_LIMIT;

            // place log record
            TaskManager.StartTask("EXCHANGE", "CREATE_PUBLIC_FOLDER", itemId);

            try
            {
                // e-mail
                string email = "";
                if (mailEnabled && !String.IsNullOrEmpty(name))
                {
                    email = name + "@" + domain;

                    // check e-mail
                    if (EmailAddressExists(email))
                        return BusinessErrorCodes.ERROR_EXCHANGE_EMAIL_EXISTS;
                }

                // full folder name
                string normParent = parentFolder;
                if (!normParent.StartsWith("\\"))
                    normParent = "\\" + normParent;
                if (!normParent.EndsWith("\\"))
                    normParent = normParent + "\\";

                string folderPath = normParent + folderName;

                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                if (String.IsNullOrEmpty(name))
                    name = Utils.CleanIdentifier(folderName);

                string accountName = OrganizationController.BuildAccountNameEx(org, name);

                // add mailbox
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                //Create Exchange Organization
                if (string.IsNullOrEmpty(org.GlobalAddressList))
                {
                    ExtendToExchangeOrganization(ref org);

                    PackageController.UpdatePackageItem(org);
                }

                exchange.CreatePublicFolder(org.DistinguishedName,
                    org.OrganizationId,
                    org.SecurityGroup,
                    parentFolder,
                    folderName,
                    mailEnabled,
                    accountName,
                    name,
                    domain);


                ExchangePublicFolder folder = exchange.GetPublicFolderGeneralSettings(org.OrganizationId, parentFolder + "\\" + folderName);

                // add meta-item
                int accountId = AddAccount(itemId, ExchangeAccountType.PublicFolder, accountName,
                    folderPath, email, mailEnabled,
                    0, folder.NETBIOS + "\\" + accountName, null, 0, null);

                // register email address
                if (mailEnabled)
                    AddAccountEmailAddress(accountId, email);

                return accountId;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int DeletePublicFolders(int itemId, int[] accountIds)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            if (accountIds != null)
                foreach (int accountId in accountIds)
                {
                    int result = DeletePublicFolder(itemId, accountId);
                    if (result < 0)
                        return result;
                }
            return 0;
        }

        public static int DeletePublicFolder(int itemId, int accountId)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "DELETE_PUBLIC_FOLDER", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // delete folder
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.DeletePublicFolder(org.OrganizationId, account.DisplayName);

                // unregister account
                DeleteAccount(itemId, accountId);

                // delete all nested folder meta-items
                List<ExchangeAccount> folders = GetAccounts(itemId, ExchangeAccountType.PublicFolder);
                foreach (ExchangeAccount folder in folders)
                {
                    if (folder.DisplayName.ToLower().StartsWith(account.DisplayName.ToLower() + "\\"))
                        DeleteAccount(itemId, folder.AccountId);
                }

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int EnableMailPublicFolder(int itemId, int accountId,
            string name, string domain)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "ENABLE_MAIL_PUBLIC_FOLDER", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);
                if (account.MailEnabledPublicFolder)
                    return 0;

                // check email
                string email = name + "@" + domain;

                // check e-mail
                if (EmailAddressExists(email))
                    return BusinessErrorCodes.ERROR_EXCHANGE_EMAIL_EXISTS;

                string accountName = OrganizationController.BuildAccountNameEx(org, name);

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.EnableMailPublicFolder(
                    org.OrganizationId,
                    account.DisplayName,
                    account.AccountName,
                    name,
                    domain);

                // update and save account
                account.AccountName = accountName;
                account.MailEnabledPublicFolder = true;
                account.PrimaryEmailAddress = email;
                account.AccountPassword = null;
                UpdateAccount(account);

                // register e-mail
                AddAccountEmailAddress(accountId, email);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int DisableMailPublicFolder(int itemId, int accountId)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "DISABLE_MAIL_PUBLIC_FOLDER", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);
                if (!account.MailEnabledPublicFolder)
                    return 0;

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.DisableMailPublicFolder(org.OrganizationId, account.DisplayName);


                // update and save account
                account.MailEnabledPublicFolder = false;
                account.PrimaryEmailAddress = "";
                account.AccountPassword = null;
                UpdateAccount(account);


                // delete all mail accounts
                List<string> addrs = new List<string>();
                ExchangeEmailAddress[] emails = GetAccountEmailAddresses(itemId, accountId);
                foreach (ExchangeEmailAddress email in emails)
                    addrs.Add(email.EmailAddress);

                DeleteAccountEmailAddresses(accountId, addrs.ToArray());

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        private static ExchangePublicFolder GetDemoPublicFolderSettings()
        {
            ExchangePublicFolder c = new ExchangePublicFolder();
            c.DisplayName = "\\fabrikam\\Documents";
            c.MailEnabled = true;
            c.Name = "Documents";
            c.Accounts = GetAccounts(0, ExchangeAccountType.Mailbox).ToArray();
            c.AcceptAccounts = GetAccounts(0, ExchangeAccountType.Mailbox).ToArray();
            return c;
        }

        public static ExchangePublicFolder GetPublicFolderGeneralSettings(int itemId, int accountId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                return GetDemoPublicFolderSettings();
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_PUBLIC_FOLDER_GENERAL", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return null;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                ExchangePublicFolder folder = exchange.GetPublicFolderGeneralSettings(org.OrganizationId, account.DisplayName);
                folder.MailEnabled = account.MailEnabledPublicFolder;
                folder.DisplayName = account.DisplayName;
                return folder;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int SetPublicFolderGeneralSettings(int itemId, int accountId, string newName,
            bool hideAddressBook, ExchangeAccount[] accounts)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "UPDATE_PUBLIC_FOLDER_GENERAL", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.SetPublicFolderGeneralSettings(
                    org.OrganizationId,
                    account.DisplayName,
                    newName,
                    hideAddressBook,
                    accounts
                    );

                // update folder name
                string origName = account.DisplayName;
                string newFullName = origName.Substring(0, origName.LastIndexOf("\\") + 1) + newName;

                if (String.Compare(origName, newFullName, true) != 0)
                {
                    // rename original folder
                    account.DisplayName = newFullName;
                    account.AccountPassword = null;
                    UpdateAccount(account);

                    // rename nested folders
                    List<ExchangeAccount> folders = GetAccounts(itemId, ExchangeAccountType.PublicFolder);
                    foreach (ExchangeAccount folder in folders)
                    {
                        if (folder.DisplayName.ToLower().StartsWith(origName.ToLower() + "\\"))
                        {
                            folder.DisplayName = newFullName + folder.DisplayName.Substring(origName.Length);
                            UpdateAccount(folder);
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static ExchangePublicFolder GetPublicFolderMailFlowSettings(int itemId, int accountId)
        {
            #region Demo Mode
            if (IsDemoMode)
            {
                return GetDemoPublicFolderSettings();
            }
            #endregion

            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_PUBLIC_FOLDER_MAILFLOW", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return null;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                ExchangePublicFolder folder = exchange.GetPublicFolderMailFlowSettings(org.OrganizationId, account.DisplayName);
                folder.DisplayName = account.DisplayName;
                return folder;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int SetPublicFolderMailFlowSettings(int itemId, int accountId,
            string[] acceptAccounts, string[] rejectAccounts, bool requireSenderAuthentication)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "UPDATE_PUBLIC_FOLDER_MAILFLOW", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // get mailbox settings
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.SetPublicFolderMailFlowSettings(org.OrganizationId, account.DisplayName,
                    acceptAccounts,
                    rejectAccounts,
                    requireSenderAuthentication);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static ExchangeEmailAddress[] GetPublicFolderEmailAddresses(int itemId, int accountId)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_PUBLIC_FOLDER_ADDRESSES", itemId);

            try
            {
                return GetAccountEmailAddresses(itemId, accountId);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int AddPublicFolderEmailAddress(int itemId, int accountId, string emailAddress)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "ADD_PUBLIC_FOLDER_ADDRESS", itemId);

            try
            {
                // check
                if (EmailAddressExists(emailAddress))
                    return BusinessErrorCodes.ERROR_EXCHANGE_EMAIL_EXISTS;

                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // add e-mail
                AddAccountEmailAddress(accountId, emailAddress);

                // update e-mail addresses
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.SetPublicFolderEmailAddresses(
                    org.OrganizationId,
                    account.DisplayName,
                    GetAccountSimpleEmailAddresses(itemId, accountId));

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int SetPublicFolderPrimaryEmailAddress(int itemId, int accountId, string emailAddress)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "SET_PRIMARY_PUBLIC_FOLDER_ADDRESS", itemId);

            try
            {
                // get account
                ExchangeAccount account = GetAccount(itemId, accountId);
                account.PrimaryEmailAddress = emailAddress;

                // update exchange
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // check package
                int packageCheck = SecurityContext.CheckPackage(org.PackageId, DemandPackage.IsActive);
                if (packageCheck < 0) return packageCheck;

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.SetPublicFolderPrimaryEmailAddress(
                    org.OrganizationId,
                    account.DisplayName,
                    emailAddress);

                // save account
                account.AccountPassword = null;
                UpdateAccount(account);

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static int DeletePublicFolderEmailAddresses(int itemId, int accountId, string[] emailAddresses)
        {
            // check account
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "DELETE_PUBLIC_FOLDER_ADDRESSES", itemId);

            try
            {
                // get account
                ExchangeAccount account = GetAccount(itemId, accountId);

                // delete e-mail addresses
                List<string> toDelete = new List<string>();
                foreach (string emailAddress in emailAddresses)
                {
                    if (String.Compare(account.PrimaryEmailAddress, emailAddress, true) != 0)
                        toDelete.Add(emailAddress);
                }

                // delete from meta-base
                DeleteAccountEmailAddresses(accountId, toDelete.ToArray());

                // delete from Exchange
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return -1;

                // update e-mail addresses
                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.SetPublicFolderEmailAddresses(
                    org.OrganizationId,
                    account.DisplayName,
                    GetAccountSimpleEmailAddresses(itemId, accountId));

                return 0;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }
        #endregion

        #region Private Helpers


        private static string GetPrimaryDomainController(int organizationServiceId)
        {

            Organizations orgProxy = new Organizations();

            ServiceProviderProxy.Init(orgProxy, organizationServiceId);

            string[] organizationSettings = orgProxy.ServiceProviderSettingsSoapHeaderValue.Settings;



            string orgPrimaryDomainController = string.Empty;
            foreach (string str in organizationSettings)
            {
                string[] props = str.Split('=');
                if (props[0].ToLower() == "primarydomaincontroller")
                {
                    orgPrimaryDomainController = str;
                    break;
                }
            }

            return orgPrimaryDomainController;
        }

        private static void ExtendExchangeSettings(List<string> exchangeSettings, string primaryDomainController)
        {
            bool isAdded = false;
            for (int i = 0; i < exchangeSettings.Count; i++)
            {
                string[] props = exchangeSettings[i].Split('=');
                if (props[0].ToLower() == "primarydomaincontroller")
                {
                    exchangeSettings[i] = primaryDomainController;
                    isAdded = true;
                    break;
                }
            }

            if (!isAdded)
            {
                exchangeSettings.Add(primaryDomainController);
            }
        }

        internal static ServiceProvider GetServiceProvider(int exchangeServiceId, int organizationServiceId)
        {
            ServiceProvider ws = new ServiceProvider();

            ServiceProviderProxy.Init(ws, exchangeServiceId);

            string[] exchangeSettings = ws.ServiceProviderSettingsSoapHeaderValue.Settings;

            List<string> resSettings = new List<string>(exchangeSettings);

            string orgPrimaryDomainController = GetPrimaryDomainController(organizationServiceId);

            ExtendExchangeSettings(resSettings, orgPrimaryDomainController);
            ws.ServiceProviderSettingsSoapHeaderValue.Settings = resSettings.ToArray();
            return ws;
        }

        internal static ExchangeServer GetExchangeServer(int exchangeServiceId, int organizationServiceId)
        {
            ExchangeServer ws = new ExchangeServer();

            ServiceProviderProxy.Init(ws, exchangeServiceId);

            string[] exchangeSettings = ws.ServiceProviderSettingsSoapHeaderValue.Settings;

            List<string> resSettings = new List<string>(exchangeSettings);

            string orgPrimaryDomainController = GetPrimaryDomainController(organizationServiceId);

            ExtendExchangeSettings(resSettings, orgPrimaryDomainController);
            ws.ServiceProviderSettingsSoapHeaderValue.Settings = resSettings.ToArray();
            return ws;
        }

        internal static ServiceProvider GetExchangeServiceProvider(int exchangeServiceId, int organizationServiceId)
        {
            ServiceProvider ws = new ServiceProvider();

            ServiceProviderProxy.Init(ws, exchangeServiceId);

            string[] exchangeSettings = ws.ServiceProviderSettingsSoapHeaderValue.Settings;

            List<string> resSettings = new List<string>(exchangeSettings);

            string orgPrimaryDomainController = GetPrimaryDomainController(organizationServiceId);

            ExtendExchangeSettings(resSettings, orgPrimaryDomainController);
            ws.ServiceProviderSettingsSoapHeaderValue.Settings = resSettings.ToArray();
            return ws;
        }


        private static int GetExchangeServiceID(int packageId)
        {
            return PackageController.GetPackageServiceId(packageId, ResourceGroups.Exchange);
        }

        private static string[] GetAccountSimpleEmailAddresses(int itemId, int accountId)
        {
            ExchangeEmailAddress[] emails = GetAccountEmailAddresses(itemId, accountId);
            List<string> result = new List<string>();
            foreach (ExchangeEmailAddress email in emails)
            {
                string prefix = email.IsPrimary ? "SMTP:" : "smtp:";
                result.Add(prefix + email.EmailAddress);
            }

            return result.ToArray();
        }

        private static bool QuotaEnabled(PackageContext cntx, string quotaName)
        {
            return cntx.Quotas.ContainsKey(quotaName) && !cntx.Quotas[quotaName].QuotaExhausted;
        }

        private static bool IsDemoMode
        {
            get
            {
                return (SecurityContext.CheckAccount(DemandAccount.NotDemo) < 0);
            }
        }
        #endregion

        public static ExchangeMobileDevice[] GetMobileDevices(int itemId, int accountId)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_MOBILE_DEVICES", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return null;

                // load account
                ExchangeAccount account = GetAccount(itemId, accountId);

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                return exchange.GetMobileDevices(account.UserPrincipalName);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static ExchangeMobileDevice GetMobileDevice(int itemId, string deviceId)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "GET_MOBILE_DEVICE", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return null;

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                return exchange.GetMobileDevice(deviceId);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static void WipeDataFromDevice(int itemId, string deviceId)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "WIPE_DATA_FROM_DEVICE", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return;

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.WipeDataFromDevice(deviceId);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static void CancelRemoteWipeRequest(int itemId, string deviceId)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "CANCEL_REMOTE_WIPE_REQUEST", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return;

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.CancelRemoteWipeRequest(deviceId);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        public static void RemoveDevice(int itemId, string deviceId)
        {
            // place log record
            TaskManager.StartTask("EXCHANGE", "REMOVE_DEVICE", itemId);

            try
            {
                // load organization
                Organization org = GetOrganization(itemId);
                if (org == null)
                    return;

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                exchange.RemoveDevice(deviceId);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }
        }

        #region Disclaimers

        public static int AddExchangeDisclaimer(int itemID, ExchangeDisclaimer disclaimer)
        {
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "ADD_EXCHANGE_EXCHANGEDISCLAIMER", itemID);

            try
            {
                return DataProvider.AddExchangeDisclaimer(itemID, disclaimer);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }


        }

        public static int UpdateExchangeDisclaimer(int itemID, ExchangeDisclaimer disclaimer)
        {
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "UPDATE_EXCHANGE_EXCHANGEDISCLAIMER", itemID);

            try
            {
                DataProvider.UpdateExchangeDisclaimer(itemID, disclaimer);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }

            return 0;
        }

        public static int DeleteExchangeDisclaimer(int itemId, int exchangeDisclaimerId)
        {
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            TaskManager.StartTask("EXCHANGE", "DELETE_EXCHANGE_EXCHANGEDISCLAIMER", itemId);

            try
            {
                DataProvider.DeleteExchangeDisclaimer(exchangeDisclaimerId);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }

            return 0;
        }

        public static ExchangeDisclaimer GetExchangeDisclaimer(int itemId, int exchangeDisclaimerId)
        {

            TaskManager.StartTask("EXCHANGE", "GET_EXCHANGE_EXCHANGEDISCLAIMER", itemId);

            try
            {
                return ObjectUtils.FillObjectFromDataReader<ExchangeDisclaimer>(
                    DataProvider.GetExchangeDisclaimer(exchangeDisclaimerId));
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }

        }

        public static List<ExchangeDisclaimer> GetExchangeDisclaimers(int itemId)
        {
            TaskManager.StartTask("EXCHANGE", "GET_EXCHANGE_EXCHANGEDISCLAIMER", itemId);

            try
            {
                List<ExchangeDisclaimer> disclaimers = ObjectUtils.CreateListFromDataReader<ExchangeDisclaimer>(DataProvider.GetExchangeDisclaimers(itemId));
                return disclaimers;
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }

        }

        public static int SetExchangeAccountDisclaimerId(int itemId, int AccountID, int ExchangeDisclaimerId)
        {
            int accountCheck = SecurityContext.CheckAccount(DemandAccount.NotDemo | DemandAccount.IsActive);
            if (accountCheck < 0) return accountCheck;

            // place log record
            TaskManager.StartTask("EXCHANGE", "SET_EXCHANGE_ACCOUNTDISCLAIMERID", AccountID);

            try
            {
                ExchangeDisclaimer disclaimer = null;

                if (ExchangeDisclaimerId != -1)
                    disclaimer = GetExchangeDisclaimer(itemId, ExchangeDisclaimerId);

                // load account
                ExchangeAccount account = GetAccount(itemId, AccountID);

                Organization org = (Organization)PackageController.GetPackageItem(itemId);
                if (org == null)
                    return -1;

                int exchangeServiceId = GetExchangeServiceID(org.PackageId);
                ExchangeServer exchange = GetExchangeServer(exchangeServiceId, org.ServiceId);

                string transportRuleName = org.Name + "_" + account.PrimaryEmailAddress;

                exchange.RemoveTransportRule(transportRuleName);

                if (disclaimer != null)
                {
                    if (!string.IsNullOrEmpty(disclaimer.DisclaimerText))
                        exchange.NewDisclaimerTransportRule(transportRuleName, account.PrimaryEmailAddress, disclaimer.DisclaimerText);
                }

                DataProvider.SetExchangeAccountDisclaimerId(AccountID, ExchangeDisclaimerId);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }

            return 0;

        }

        public static int GetExchangeAccountDisclaimerId(int itemId, int AccountID)
        {
            TaskManager.StartTask("EXCHANGE", "GET_EXCHANGE_ACCOUNTDISCLAIMERID", AccountID);

            try
            {
                return DataProvider.GetExchangeAccountDisclaimerId(AccountID);
            }
            catch (Exception ex)
            {
                throw TaskManager.WriteError(ex);
            }
            finally
            {
                TaskManager.CompleteTask();
            }

        }
        #endregion

    }
}
