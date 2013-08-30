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
using System.Collections.Generic;
using System.DirectoryServices;
using System.Globalization;
using System.Text;
using WebsitePanel.Providers.Common;
using WebsitePanel.Providers.ResultObjects;

namespace WebsitePanel.Providers.HostedSolution
{
    public class OrganizationProvider : HostingServiceProviderBase, IOrganization
    {
        #region Properties

        private string RootOU
        {
            get { return ProviderSettings["RootOU"]; }
        }

        private string RootDomain
        {
            get { return ServerSettings.ADRootDomain; }
        }

        private string PrimaryDomainController
        {
            get { return ProviderSettings["PrimaryDomainController"]; }
        }

        #endregion



        #region Helpers

        private string GetOrganizationPath(string organizationId)
        {
            StringBuilder sb = new StringBuilder();
            // append provider
            AppendProtocol(sb);
            AppendDomainController(sb);
            AppendOUPath(sb, organizationId);
            AppendOUPath(sb, RootOU);
            AppendDomainPath(sb, RootDomain);

            return sb.ToString();
        }

        private string GetUserPath(string organizationId, string loginName)
        {
            StringBuilder sb = new StringBuilder();
            // append provider
            AppendProtocol(sb);
            AppendDomainController(sb);
            AppendCNPath(sb, loginName);
            AppendOUPath(sb, organizationId);
            AppendOUPath(sb, RootOU);
            AppendDomainPath(sb, RootDomain);

            return sb.ToString();
        }

        private string GetGroupPath(string organizationId)
        {
            StringBuilder sb = new StringBuilder();
            // append provider
            AppendProtocol(sb);
            AppendDomainController(sb);
            AppendCNPath(sb, organizationId);
            AppendOUPath(sb, organizationId);
            AppendOUPath(sb, RootOU);
            AppendDomainPath(sb, RootDomain);

            return sb.ToString();
        }

        private string GetObjectPath(string organizationId, string objName)
        {
            StringBuilder sb = new StringBuilder();
            // append provider
            AppendProtocol(sb);
            AppendDomainController(sb);
            AppendCNPath(sb, objName);
            AppendOUPath(sb, organizationId);
            AppendOUPath(sb, RootOU);
            AppendDomainPath(sb, RootDomain);

            return sb.ToString();
        }

        private string GetGroupPath(string organizationId, string groupName)
        {
            StringBuilder sb = new StringBuilder();
            // append provider
            AppendProtocol(sb);
            AppendDomainController(sb);
            AppendCNPath(sb, groupName);
            AppendOUPath(sb, organizationId);
            AppendOUPath(sb, RootOU);
            AppendDomainPath(sb, RootDomain);

            return sb.ToString();
        }

        private string GetRootOU()
        {
            StringBuilder sb = new StringBuilder();
            // append provider
            AppendProtocol(sb);
            AppendDomainController(sb);
            AppendOUPath(sb, RootOU);
            AppendDomainPath(sb, RootDomain);

            return sb.ToString();
        }

        private void AppendDomainController(StringBuilder sb)
        {
            sb.Append(PrimaryDomainController + "/");
        }

        private static void AppendCNPath(StringBuilder sb, string organizationId)
        {
            if (string.IsNullOrEmpty(organizationId))
                return;

            sb.Append("CN=").Append(organizationId).Append(",");
        }

        private static void AppendProtocol(StringBuilder sb)
        {
            sb.Append("LDAP://");
        }

        private static void AppendOUPath(StringBuilder sb, string ou)
        {
            if (string.IsNullOrEmpty(ou))
                return;

            string path = ou.Replace("/", "\\");
            string[] parts = path.Split('\\');
            for (int i = parts.Length - 1; i != -1; i--)
                sb.Append("OU=").Append(parts[i]).Append(",");
        }

        private static void AppendDomainPath(StringBuilder sb, string domain)
        {
            if (string.IsNullOrEmpty(domain))
                return;

            string[] parts = domain.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                sb.Append("DC=").Append(parts[i]);

                if (i < (parts.Length - 1))
                    sb.Append(",");
            }
        }

        #endregion



        #region Organizations

        public bool OrganizationExists(string organizationId)
        {
            return OrganizationExistsInternal(organizationId);
        }

        internal bool OrganizationExistsInternal(string organizationId)
        {
            if (string.IsNullOrEmpty(organizationId))
                throw new ArgumentNullException("organizationId");

            string orgPath = GetOrganizationPath(organizationId);
            return ActiveDirectoryUtils.AdObjectExists(orgPath);
        }

        public Organization CreateOrganization(string organizationId)
        {
            return CreateOrganizationInternal(organizationId);
        }

        internal Organization CreateOrganizationInternal(string organizationId)
        {
            HostedSolutionLog.LogStart("CreateOrganizationInternal");
            HostedSolutionLog.DebugInfo("OrganizationId : {0}", organizationId);

            if (string.IsNullOrEmpty(organizationId))
                throw new ArgumentNullException("organizationId");

            bool ouCreated = false;
            bool groupCreated = false;

            Organization org;
            try
            {
                string parentPath = GetRootOU();
                string orgPath = GetOrganizationPath(organizationId);

                //Create OU
                ActiveDirectoryUtils.CreateOrganizationalUnit(organizationId, parentPath);
                ouCreated = true;

                //Create security group
                ActiveDirectoryUtils.CreateGroup(orgPath, organizationId);
                groupCreated = true;


                org = new Organization();
                org.OrganizationId = organizationId;
                org.DistinguishedName = ActiveDirectoryUtils.RemoveADPrefix(orgPath);
                org.SecurityGroup = ActiveDirectoryUtils.RemoveADPrefix(GetGroupPath(organizationId));
                org.GroupName = organizationId;
            }
            catch (Exception ex)
            {
                HostedSolutionLog.LogError(ex);
                try
                {
                    if (groupCreated)
                    {
                        string groupPath = GetGroupPath(organizationId);
                        ActiveDirectoryUtils.DeleteADObject(groupPath);
                    }
                }
                catch (Exception e)
                {
                    HostedSolutionLog.LogError(e);
                }

                try
                {
                    if (ouCreated)
                    {
                        string orgPath = GetOrganizationPath(organizationId);
                        ActiveDirectoryUtils.DeleteADObject(orgPath);
                    }
                }
                catch (Exception e)
                {
                    HostedSolutionLog.LogError(e);
                }

                throw;
            }

            HostedSolutionLog.LogEnd("CreateOrganizationInternal");

            return org;
        }

        public override void ChangeServiceItemsState(ServiceProviderItem[] items, bool enabled)
        {

            foreach (ServiceProviderItem item in items)
            {
                try
                {
                    if (item is Organization)
                    {
                        Organization org = item as Organization;
                        ChangeOrganizationState(org, enabled);
                    }
                }
                catch (Exception ex)
                {
                    HostedSolutionLog.LogError(
                        String.Format("Error deleting '{0}' {1}", item.Name, item.GetType().Name), ex);
                }
            }
        }

        private void ChangeOrganizationState(Organization org, bool enabled)
        {
            string path = GetOrganizationPath(org.OrganizationId);
            DirectoryEntry entry = ActiveDirectoryUtils.GetADObject(path);

            string filter =
                string.Format(CultureInfo.InvariantCulture, "(&(objectClass=user)(!{0}=disabled))",
                              ADAttributes.CustomAttribute2);
            using (DirectorySearcher searcher = new DirectorySearcher(entry, filter))
            {
                SearchResultCollection resCollection = searcher.FindAll();
                foreach (SearchResult res in resCollection)
                {
                    DirectoryEntry de = res.GetDirectoryEntry();
                    de.InvokeSet("AccountDisabled", !enabled);
                    de.CommitChanges();
                }
            }
        }

        public override void DeleteServiceItems(ServiceProviderItem[] items)
        {
            foreach (ServiceProviderItem item in items)
            {
                try
                {
                    if (item is Organization)
                    {
                        Organization org = item as Organization;
                        DeleteOrganizationInternal(org.OrganizationId);
                    }

                }
                catch (Exception ex)
                {
                    HostedSolutionLog.LogError(String.Format("Error deleting '{0}' {1}", item.Name, item.GetType().Name), ex);
                }
            }

        }

        public void DeleteOrganization(string organizationId)
        {
            DeleteOrganizationInternal(organizationId);
        }

        internal void DeleteOrganizationInternal(string organizationId)
        {
            HostedSolutionLog.LogStart("DeleteOrganizationInternal");
            HostedSolutionLog.DebugInfo("OrganizationId : {0}", organizationId);

            if (string.IsNullOrEmpty(organizationId))
                throw new ArgumentNullException("organizationId");

            string groupPath = GetGroupPath(organizationId);
            ActiveDirectoryUtils.DeleteADObject(groupPath);

            string path = GetOrganizationPath(organizationId);
            ActiveDirectoryUtils.DeleteADObject(path, true);



            HostedSolutionLog.LogEnd("DeleteOrganizationInternal");
        }

        #endregion


        #region Users

        public int CreateUser(string organizationId, string loginName, string displayName, string upn, string password, bool enabled)
        {
            return CreateUserInternal(organizationId, loginName, displayName, upn, password, enabled);
        }

        internal int CreateUserInternal(string organizationId, string loginName, string displayName, string upn, string password, bool enabled)
        {
            HostedSolutionLog.LogStart("CreateUserInternal");
            HostedSolutionLog.DebugInfo("organizationId : {0}", organizationId);
            HostedSolutionLog.DebugInfo("loginName : {0}", loginName);
            HostedSolutionLog.DebugInfo("displayName : {0}", displayName);

            if (string.IsNullOrEmpty(organizationId))
                throw new ArgumentNullException("organizationId");

            if (string.IsNullOrEmpty(loginName))
                throw new ArgumentNullException("loginName");

            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            bool userCreated = false;
            string userPath = null;
            try
            {
                string path = GetOrganizationPath(organizationId);
                userPath = GetUserPath(organizationId, loginName);
                if (!ActiveDirectoryUtils.AdObjectExists(userPath))
                {
                    userPath = ActiveDirectoryUtils.CreateUser(path, null, loginName, displayName, password, enabled);
                    DirectoryEntry entry = new DirectoryEntry(userPath);
                    ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.UserPrincipalName, upn);
                    entry.CommitChanges();
                    userCreated = true;
                    HostedSolutionLog.DebugInfo("User created: {0}", displayName);
                }
                else
                {
                    HostedSolutionLog.DebugInfo("AD_OBJECT_ALREADY_EXISTS: {0}", userPath);
                    HostedSolutionLog.LogEnd("CreateUserInternal");
                    return Errors.AD_OBJECT_ALREADY_EXISTS;
                }

                string groupPath = GetGroupPath(organizationId);
                HostedSolutionLog.DebugInfo("Group retrieved: {0}", groupPath);


                ActiveDirectoryUtils.AddObjectToGroup(userPath, groupPath);
                HostedSolutionLog.DebugInfo("Added to group: {0}", groupPath);
            }
            catch (Exception e)
            {
                HostedSolutionLog.LogError(e);
                try
                {
                    if (userCreated)
                        ActiveDirectoryUtils.DeleteADObject(userPath);
                }
                catch (Exception ex)
                {
                    HostedSolutionLog.LogError(ex);
                }

                return Errors.AD_OBJECT_ALREADY_EXISTS;
            }

            HostedSolutionLog.LogEnd("CreateUserInternal");
            return Errors.OK;
        }

        public PasswordPolicyResult GetPasswordPolicy()
        {
            return GetPasswordPolicyInternal();
        }

        internal PasswordPolicyResult GetPasswordPolicyInternal()
        {
            HostedSolutionLog.LogStart("GetPasswordPolicyInternal");

            PasswordPolicyResult res = new PasswordPolicyResult { IsSuccess = true };

            string[] policyAttributes = new[] {"minPwdLength", 
                                               "pwdProperties", 
                                               "objectClass"};
            try
            {
                DirectoryEntry domainRoot = new DirectoryEntry(ActiveDirectoryUtils.ConvertDomainName(RootDomain));

                DirectorySearcher ds = new DirectorySearcher(
                    domainRoot,
                    "(objectClass=domainDNS)",
                    policyAttributes,
                    SearchScope.Base
                    );


                SearchResult result = ds.FindOne();

                PasswordPolicy ret = new PasswordPolicy
                {
                    MinLength = ((int)result.Properties["minPwdLength"][0]),
                    IsComplexityEnable = ((int)result.Properties["pwdProperties"][0] == 1)
                };
                res.Value = ret;
            }
            catch (Exception ex)
            {
                HostedSolutionLog.LogError(ex);
                res.IsSuccess = false;
                res.ErrorCodes.Add(ErrorCodes.CANNOT_GET_PASSWORD_COMPLEXITY);
            }

            HostedSolutionLog.LogEnd("GetPasswordPolicyInternal");
            return res;
        }


        public void DeleteUser(string loginName, string organizationId)
        {
            DeleteUserInternal(loginName, organizationId);
        }

        internal void DeleteUserInternal(string loginName, string organizationId)
        {
            HostedSolutionLog.LogStart("DeleteUserInternal");
            HostedSolutionLog.DebugInfo("loginName : {0}", loginName);
            HostedSolutionLog.DebugInfo("organizationId : {0}", organizationId);

            if (string.IsNullOrEmpty(loginName))
                throw new ArgumentNullException("loginName");

            if (string.IsNullOrEmpty(organizationId))
                throw new ArgumentNullException("organizationId");

            string path = GetUserPath(organizationId, loginName);
            if (ActiveDirectoryUtils.AdObjectExists(path))
                ActiveDirectoryUtils.DeleteADObject(path, true);

            HostedSolutionLog.LogEnd("DeleteUserInternal");
        }

        public OrganizationUser GetUserGeneralSettings(string loginName, string organizationId)
        {
            return GetUserGeneralSettingsInternal(loginName, organizationId);
        }

        internal OrganizationUser GetUserGeneralSettingsInternal(string loginName, string organizationId)
        {
            HostedSolutionLog.LogStart("GetUserGeneralSettingsInternal");
            HostedSolutionLog.DebugInfo("loginName : {0}", loginName);
            HostedSolutionLog.DebugInfo("organizationId : {0}", organizationId);

            if (string.IsNullOrEmpty(loginName))
                throw new ArgumentNullException("loginName");

            string path = GetUserPath(organizationId, loginName);

            OrganizationUser retUser = GetUser(path);

            HostedSolutionLog.LogEnd("GetUserGeneralSettingsInternal");
            return retUser;
        }

        private OrganizationUser GetUser(string path)
        {
            OrganizationUser retUser = new OrganizationUser();

            DirectoryEntry entry = ActiveDirectoryUtils.GetADObject(path);

            retUser.FirstName = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.FirstName);
            retUser.LastName = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.LastName);
            retUser.DisplayName = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.DisplayName);
            retUser.Initials = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.Initials);
            retUser.JobTitle = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.JobTitle);
            retUser.Company = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.Company);
            retUser.Department = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.Department);
            retUser.Office = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.Office);
            retUser.BusinessPhone = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.BusinessPhone);
            retUser.Fax = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.Fax);
            retUser.HomePhone = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.HomePhone);
            retUser.MobilePhone = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.MobilePhone);
            retUser.Pager = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.Pager);
            retUser.WebPage = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.WebPage);
            retUser.Address = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.Address);
            retUser.City = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.City);
            retUser.State = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.State);
            retUser.Zip = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.Zip);
            retUser.Country = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.Country);
            retUser.Notes = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.Notes);
            retUser.ExternalEmail = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.ExternalEmail);
            retUser.Disabled = (bool)entry.InvokeGet(ADAttributes.AccountDisabled);
            retUser.Manager = GetManager(entry, ADAttributes.Manager);
            retUser.SamAccountName = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.SAMAccountName);
            retUser.DomainUserName = GetDomainName(ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.SAMAccountName));
            retUser.DistinguishedName = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.DistinguishedName);
            retUser.Locked = (bool)entry.InvokeGet(ADAttributes.AccountLocked);
            retUser.UserPrincipalName = (string)entry.InvokeGet(ADAttributes.UserPrincipalName);

            return retUser;
        }

        private string GetDomainName(string username)
        {
            string domain = ActiveDirectoryUtils.GetNETBIOSDomainName(RootDomain);
            string ret = string.Format(@"{0}\{1}", domain, username);
            return ret;
        }

        private OrganizationUser GetManager(DirectoryEntry entry, string adAttribute)
        {
            OrganizationUser retUser = null;
            string path = ActiveDirectoryUtils.GetADObjectStringProperty(entry, adAttribute);
            if (!string.IsNullOrEmpty(path))
            {
                path = ActiveDirectoryUtils.AddADPrefix(path, PrimaryDomainController);
                if (ActiveDirectoryUtils.AdObjectExists(path))
                {
                    DirectoryEntry user = ActiveDirectoryUtils.GetADObject(path);
                    retUser = new OrganizationUser();
                    retUser.DisplayName = ActiveDirectoryUtils.GetADObjectStringProperty(user, ADAttributes.DisplayName);

                    retUser.AccountName = ActiveDirectoryUtils.GetADObjectStringProperty(user, ADAttributes.Name);
                }
            }

            return retUser;
        }

        public void SetUserGeneralSettings(string organizationId, string accountName, string displayName, string password,
            bool hideFromAddressBook, bool disabled, bool locked, string firstName, string initials, string lastName,
            string address, string city, string state, string zip, string country, string jobTitle,
            string company, string department, string office, string managerAccountName,
            string businessPhone, string fax, string homePhone, string mobilePhone, string pager,
            string webPage, string notes, string externalEmail)
        {
            SetUserGeneralSettingsInternal(organizationId, accountName, displayName, password, hideFromAddressBook,
                disabled, locked, firstName, initials, lastName, address, city, state, zip, country, jobTitle,
                company, department, office, managerAccountName, businessPhone, fax, homePhone,
                mobilePhone, pager, webPage, notes, externalEmail);
        }

        internal void SetUserGeneralSettingsInternal(string organizationId, string accountName, string displayName, string password,
            bool hideFromAddressBook, bool disabled, bool locked, string firstName, string initials, string lastName,
            string address, string city, string state, string zip, string country, string jobTitle,
            string company, string department, string office, string managerAccountName,
            string businessPhone, string fax, string homePhone, string mobilePhone, string pager,
            string webPage, string notes, string externalEmail)
        {
            string path = GetUserPath(organizationId, accountName);
            DirectoryEntry entry = ActiveDirectoryUtils.GetADObject(path);


            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.FirstName, firstName);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.LastName, lastName);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.DisplayName, displayName);

            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.Initials, initials);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.JobTitle, jobTitle);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.Company, company);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.Department, department);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.Office, office);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.BusinessPhone, businessPhone);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.Fax, fax);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.HomePhone, homePhone);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.MobilePhone, mobilePhone);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.Pager, pager);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.WebPage, webPage);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.Address, address);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.City, city);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.State, state);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.Zip, zip);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.Country, country);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.Notes, notes);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.ExternalEmail, externalEmail);
            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.CustomAttribute2, (disabled ? "disabled" : null));


            string manager = string.Empty;
            if (!string.IsNullOrEmpty(managerAccountName))
            {
                string managerPath = GetUserPath(organizationId, managerAccountName);
                manager = ActiveDirectoryUtils.AdObjectExists(managerPath) ? managerPath : string.Empty;
            }

            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.Manager, ActiveDirectoryUtils.RemoveADPrefix(manager));

            entry.InvokeSet(ADAttributes.AccountDisabled, disabled);
            if (!string.IsNullOrEmpty(password))
                entry.Invoke(ADAttributes.SetPassword, password);

            if (!locked)
            {
                bool isLoked = (bool)entry.InvokeGet(ADAttributes.AccountLocked);
                if (isLoked)
                    entry.InvokeSet(ADAttributes.AccountLocked, locked);

            }


            entry.CommitChanges();
        }

        public void SetUserPassword(string organizationId, string accountName, string password)
        {
            SetUserPasswordInternal(organizationId, accountName, password);
        }

        internal void SetUserPasswordInternal(string organizationId, string accountName, string password)
        {
            string path = GetUserPath(organizationId, accountName);
            DirectoryEntry entry = ActiveDirectoryUtils.GetADObject(path);

            if (!string.IsNullOrEmpty(password))
                entry.Invoke(ADAttributes.SetPassword, password);

            entry.CommitChanges();
        }


        public void SetUserPrincipalName(string organizationId, string accountName, string userPrincipalName)
        {
            SetUserPrincipalNameInternal(organizationId, accountName, userPrincipalName);
        }

        internal void SetUserPrincipalNameInternal(string organizationId, string accountName, string userPrincipalName)
        {
            string path = GetUserPath(organizationId, accountName);
            DirectoryEntry entry = ActiveDirectoryUtils.GetADObject(path);

            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.UserPrincipalName, userPrincipalName);

            entry.CommitChanges();
        }

        public string GetSamAccountNameByUserPrincipalName(string organizationId, string userPrincipalName)
        {
            return GetSamAccountNameByUserPrincipalNameInternal(organizationId, userPrincipalName);
        }

        private string GetSamAccountNameByUserPrincipalNameInternal(string organizationId, string userPrincipalName)
        {
            HostedSolutionLog.LogStart("GetSamAccountNameByUserPrincipalNameInternal");
            HostedSolutionLog.DebugInfo("userPrincipalName : {0}", userPrincipalName);
            HostedSolutionLog.DebugInfo("organizationId : {0}", organizationId);

            string accountName = string.Empty;

            try
            {

                string path = GetOrganizationPath(organizationId);
                DirectoryEntry entry = ActiveDirectoryUtils.GetADObject(path);

                DirectorySearcher searcher = new DirectorySearcher(entry);
                searcher.PropertiesToLoad.Add("userPrincipalName");
                searcher.PropertiesToLoad.Add("sAMAccountName");
                searcher.Filter = "(userPrincipalName=" + userPrincipalName + ")";
                searcher.SearchScope = SearchScope.Subtree;

                SearchResult resCollection = searcher.FindOne();
                if (resCollection != null)
                {
                    accountName = resCollection.Properties["samaccountname"][0].ToString();
                }

                HostedSolutionLog.LogEnd("GetSamAccountNameByUserPrincipalNameInternal");
            }
            catch (Exception e)
            {
                HostedSolutionLog.DebugInfo("Failed : {0}", e.Message);
            }

            return accountName;
        }


        public bool DoesSamAccountNameExist(string accountName)
        {
            return DoesSamAccountNameExistInternal(accountName);
        }


        private bool DoesSamAccountNameExistInternal(string accountName)
        {
            HostedSolutionLog.LogStart("DoesSamAccountNameExistInternal");
            HostedSolutionLog.DebugInfo("sAMAccountName : {0}", accountName);
            bool bFound = false;

            try
            {

                string path = GetRootOU();
                HostedSolutionLog.DebugInfo("Search path : {0}", path);
                DirectoryEntry entry = ActiveDirectoryUtils.GetADObject(path);

                DirectorySearcher searcher = new DirectorySearcher(entry);
                searcher.PropertiesToLoad.Add("sAMAccountName");
                searcher.Filter = "(sAMAccountName=" + accountName + ")";
                searcher.SearchScope = SearchScope.Subtree;

                SearchResult resCollection = searcher.FindOne();
                if (resCollection != null)
                {
                    if (resCollection.Properties["samaccountname"] != null)
                        bFound = true;
                }
            }
            catch (Exception e)
            {
                HostedSolutionLog.DebugInfo("Failed : {0}", e.Message);
            }

            HostedSolutionLog.DebugInfo("DoesSamAccountNameExistInternal Result: {0}", bFound);
            HostedSolutionLog.LogEnd("DoesSamAccountNameExistInternal");

            return bFound;
        }



        #endregion

        #region Domains

        public void CreateOrganizationDomain(string organizationDistinguishedName, string domain)
        {
            CreateOrganizationDomainInternal(organizationDistinguishedName, domain);
        }

        /// <summary>
        /// Creates organization domain
        /// </summary>
        /// <param name="organizationDistinguishedName"></param>
        /// <param name="domain"></param>
        private void CreateOrganizationDomainInternal(string organizationDistinguishedName, string domain)
        {
            HostedSolutionLog.LogStart("CreateOrganizationDomainInternal");

            string path = ActiveDirectoryUtils.AddADPrefix(organizationDistinguishedName, PrimaryDomainController);
            ActiveDirectoryUtils.AddUPNSuffix(path, domain);
            HostedSolutionLog.LogEnd("CreateOrganizationDomainInternal");
        }


        public void DeleteOrganizationDomain(string organizationDistinguishedName, string domain)
        {
            DeleteOrganizationDomainInternal(organizationDistinguishedName, domain);
        }

        /// <summary>
        /// Deletes organization domain
        /// </summary>
        /// <param name="organizationDistinguishedName"></param>
        /// <param name="domain"></param>
        private void DeleteOrganizationDomainInternal(string organizationDistinguishedName, string domain)
        {
            HostedSolutionLog.LogStart("DeleteOrganizationDomainInternal");

            //Remove UPN Suffix
            string path = ActiveDirectoryUtils.AddADPrefix(organizationDistinguishedName, PrimaryDomainController);
            ActiveDirectoryUtils.RemoveUPNSuffix(path, domain);

            HostedSolutionLog.LogEnd("DeleteOrganizationDomainInternal");
        }
        #endregion

        #region Security Groups

        public int CreateSecurityGroup(string organizationId, string groupName)
        {
            return CreateSecurityGroupInternal(organizationId, groupName);
        }

        internal int CreateSecurityGroupInternal(string organizationId, string groupName)
        {
            HostedSolutionLog.LogStart("CreateSecurityGroupInternal");
            HostedSolutionLog.DebugInfo("organizationId : {0}", organizationId);
            HostedSolutionLog.DebugInfo("groupName : {0}", groupName);

            if (string.IsNullOrEmpty(organizationId))
                throw new ArgumentNullException("organizationId");

            if (string.IsNullOrEmpty(groupName))
                throw new ArgumentNullException("groupName");

            bool groupCreated = false;
            string groupPath = null;
            try
            {
                string path = GetOrganizationPath(organizationId);
                groupPath = GetGroupPath(organizationId, groupName);

                if (!ActiveDirectoryUtils.AdObjectExists(groupPath))
                {
                    ActiveDirectoryUtils.CreateGroup(path, groupName);

                    groupCreated = true;

                    HostedSolutionLog.DebugInfo("Security Group created: {0}", groupName);
                }
                else
                {
                    HostedSolutionLog.DebugInfo("AD_OBJECT_ALREADY_EXISTS: {0}", groupPath);
                    HostedSolutionLog.LogEnd("CreateSecurityGroupInternal");

                    return Errors.AD_OBJECT_ALREADY_EXISTS;
                }
            }
            catch (Exception e)
            {
                HostedSolutionLog.LogError(e);
                try
                {
                    if (groupCreated)
                        ActiveDirectoryUtils.DeleteADObject(groupPath);
                }
                catch (Exception ex)
                {
                    HostedSolutionLog.LogError(ex);
                }

                return Errors.AD_OBJECT_ALREADY_EXISTS;
            }

            HostedSolutionLog.LogEnd("CreateSecurityGroupInternal");

            return Errors.OK;
        }

        public OrganizationSecurityGroup GetSecurityGroupGeneralSettings(string groupName, string organizationId)
        {
            return GetSecurityGroupGeneralSettingsInternal(groupName, organizationId);
        }

        internal OrganizationSecurityGroup GetSecurityGroupGeneralSettingsInternal(string groupName, string organizationId)
        {
            HostedSolutionLog.LogStart("GetSecurityGroupGeneralSettingsInternal");
            HostedSolutionLog.DebugInfo("groupName : {0}", groupName);
            HostedSolutionLog.DebugInfo("organizationId : {0}", organizationId);

            if (string.IsNullOrEmpty(organizationId))
                throw new ArgumentNullException("organizationId");

            if (string.IsNullOrEmpty(groupName))
                throw new ArgumentNullException("groupName");

            string path = GetGroupPath(organizationId, groupName);

            DirectoryEntry entry = ActiveDirectoryUtils.GetADObject(path);

            OrganizationSecurityGroup securityGroup = new OrganizationSecurityGroup();

            securityGroup.Notes = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.Notes);

            securityGroup.AccountName = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.SAMAccountName);
            securityGroup.SAMAccountName = ActiveDirectoryUtils.GetADObjectStringProperty(entry, ADAttributes.SAMAccountName);

            List<ExchangeAccount> members = new List<ExchangeAccount>();

            foreach (string userPath in ActiveDirectoryUtils.GetGroupObjects(groupName, "user"))
            {
                OrganizationUser tmpUser = GetUser(userPath);

                members.Add(new ExchangeAccount
                {
                    AccountName = tmpUser.AccountName,
                    SamAccountName = tmpUser.SamAccountName
                });
            }

            foreach (string groupPath in ActiveDirectoryUtils.GetGroupObjects(groupName, "group"))
            {
                DirectoryEntry groupEntry = ActiveDirectoryUtils.GetADObject(groupPath);

                members.Add(new ExchangeAccount
                {
                    AccountName = ActiveDirectoryUtils.GetADObjectStringProperty(groupEntry, ADAttributes.SAMAccountName),
                    SamAccountName = ActiveDirectoryUtils.GetADObjectStringProperty(groupEntry, ADAttributes.SAMAccountName)
                });
            }

            securityGroup.MembersAccounts = members.ToArray();

            HostedSolutionLog.LogEnd("GetSecurityGroupGeneralSettingsInternal");

            return securityGroup;
        }

        public void DeleteSecurityGroup(string groupName, string organizationId)
        {
            DeleteSecurityGroupInternal(groupName, organizationId);
        }

        internal void DeleteSecurityGroupInternal(string groupName, string organizationId)
        {
            HostedSolutionLog.LogStart("DeleteSecurityGroupInternal");
            HostedSolutionLog.DebugInfo("groupName : {0}", groupName);
            HostedSolutionLog.DebugInfo("organizationId : {0}", organizationId);

            if (string.IsNullOrEmpty(organizationId))
                throw new ArgumentNullException("organizationId");

            if (string.IsNullOrEmpty(groupName))
                throw new ArgumentNullException("groupName");

            string path = GetGroupPath(organizationId, groupName);

            if (ActiveDirectoryUtils.AdObjectExists(path))
                ActiveDirectoryUtils.DeleteADObject(path, true);

            HostedSolutionLog.LogEnd("DeleteSecurityGroupInternal");
        }

        public void SetSecurityGroupGeneralSettings(string organizationId, string groupName, string[] memberAccounts, string notes)
        {

            SetSecurityGroupGeneralSettingsInternal(organizationId, groupName, memberAccounts, notes);
        }

        internal void SetSecurityGroupGeneralSettingsInternal(string organizationId, string groupName, string[] memberAccounts, string notes)
        {
            HostedSolutionLog.LogStart("SetSecurityGroupGeneralSettingsInternal");
            HostedSolutionLog.DebugInfo("organizationId : {0}", organizationId);
            HostedSolutionLog.DebugInfo("groupName : {0}", groupName);

            if (string.IsNullOrEmpty(organizationId))
                throw new ArgumentNullException("organizationId");

            if (string.IsNullOrEmpty(groupName))
                throw new ArgumentNullException("groupName");

            string path = GetGroupPath(organizationId, groupName);

            DirectoryEntry entry = ActiveDirectoryUtils.GetADObject(path);

            ActiveDirectoryUtils.SetADObjectProperty(entry, ADAttributes.Notes, notes);

            foreach (string userPath in ActiveDirectoryUtils.GetGroupObjects(groupName, "user"))
            {
                ActiveDirectoryUtils.RemoveObjectFromGroup(userPath, path);
            }

            foreach (string groupPath in ActiveDirectoryUtils.GetGroupObjects(groupName, "group"))
            {
                ActiveDirectoryUtils.RemoveObjectFromGroup(groupPath, path);
            }

            foreach (string obj in memberAccounts)
            {
                string objPath = GetObjectPath(organizationId, obj);
                ActiveDirectoryUtils.AddObjectToGroup(objPath, path);
            }

            entry.CommitChanges();
        }

        public void AddObjectToSecurityGroup(string organizationId, string accountName, string groupName)
        {
            AddObjectToSecurityGroupInternal(organizationId, accountName, groupName);
        }

        internal void AddObjectToSecurityGroupInternal(string organizationId, string accountName, string groupName)
        {
            HostedSolutionLog.LogStart("AddUserToSecurityGroupInternal");
            HostedSolutionLog.DebugInfo("organizationId : {0}", organizationId);
            HostedSolutionLog.DebugInfo("accountName : {0}", accountName);
            HostedSolutionLog.DebugInfo("groupName : {0}", groupName);

            if (string.IsNullOrEmpty(organizationId))
                throw new ArgumentNullException("organizationId");

            if (string.IsNullOrEmpty(accountName))
                throw new ArgumentNullException("loginName");

            if (string.IsNullOrEmpty(groupName))
                throw new ArgumentNullException("groupName");

            string userPath = GetObjectPath(organizationId, accountName);

            string groupPath = GetGroupPath(organizationId, groupName);

            ActiveDirectoryUtils.AddObjectToGroup(userPath, groupPath);
        }

        public void DeleteObjectFromSecurityGroup(string organizationId, string accountName, string groupName)
        {
            DeleteObjectFromSecurityGroupInternal(organizationId, accountName, groupName);
        }

        internal void DeleteObjectFromSecurityGroupInternal(string organizationId, string accountName, string groupName)
        {
            HostedSolutionLog.LogStart("AddUserToSecurityGroupInternal");
            HostedSolutionLog.DebugInfo("organizationId : {0}", organizationId);
            HostedSolutionLog.DebugInfo("accountName : {0}", accountName);
            HostedSolutionLog.DebugInfo("groupName : {0}", groupName);

            if (string.IsNullOrEmpty(organizationId))
                throw new ArgumentNullException("organizationId");

            if (string.IsNullOrEmpty(accountName))
                throw new ArgumentNullException("loginName");

            if (string.IsNullOrEmpty(groupName))
                throw new ArgumentNullException("groupName");

            string userPath = GetObjectPath(organizationId, accountName);

            string groupPath = GetGroupPath(organizationId, groupName);

            ActiveDirectoryUtils.RemoveObjectFromGroup(userPath, groupPath);
        }

        #endregion

        public override bool IsInstalled()
        {
            return Environment.UserDomainName != Environment.MachineName;
        }


    }
}
