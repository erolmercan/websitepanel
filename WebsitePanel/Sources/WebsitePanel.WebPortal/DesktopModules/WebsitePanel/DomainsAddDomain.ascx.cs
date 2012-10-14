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
using System.Web;
using WebsitePanel.EnterpriseServer;
using System.Collections.Generic;

namespace WebsitePanel.Portal
{
	public partial class DomainsAddDomain : WebsitePanelModuleBase
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			try
			{
				// bind controls
				BindControls();

                PackageContext cntx = PackagesHelper.GetCachedPackageContext(PanelSecurity.PackageId);

                if (Utils.CheckQouta(Quotas.WEB_ENABLEHOSTNAMESUPPORT, cntx))
                {
                    lblHostName.Visible = txtHostName.Visible = true;
                    UserSettings settings = ES.Services.Users.GetUserSettings(PanelSecurity.LoggedUserId, UserSettings.WEB_POLICY);
                    txtHostName.Text = String.IsNullOrEmpty(settings["HostName"]) ? "" : settings["HostName"];
                }
                else
                {
                    lblHostName.Visible = txtHostName.Visible = false;
                    txtHostName.Text = "";
                }



			}
			catch (Exception ex)
			{
				ShowErrorMessage("DOMAIN_GET_DOMAIN", ex);
			}

            if (PanelSecurity.LoggedUser.Role == UserRole.User)
            {
                if (!PackagesHelper.CheckGroupQuotaEnabled(PanelSecurity.PackageId, ResourceGroups.Dns, Quotas.DNS_EDITOR))
                    this.DisableControls = true;
            }
		}

		private void BindControls()
		{
			// get domain type
			DomainType type = GetDomainType(Request["DomainType"]);

			// enable domain/sub-domain fields
			if (type == DomainType.Domain || type == DomainType.DomainPointer)
			{
				// domains
				DomainPanel.Visible = true;
			}
			else
			{
				// sub-domains
				SubDomainPanel.Visible = true;

				// fill sub-domains
				if (!IsPostBack)
				{
					if (type == DomainType.SubDomain)
						BindUserDomains();
					else
						BindResellerDomains();
				}
			}
			// load package context
			PackageContext cntx = PackagesHelper.GetCachedPackageContext(PanelSecurity.PackageId);

			if ((type == DomainType.DomainPointer || (type == DomainType.Domain && cntx.Quotas[Quotas.OS_DOMAINPOINTERS].QuotaAllocatedValue == 0)) && !IsPostBack)
			{
				// bind mail domains
				MailDomainsList.DataSource = ES.Services.MailServers.GetMailDomains(PanelSecurity.PackageId, false);
				MailDomainsList.DataBind();
			}

			// create web site option
			CreateWebSitePanel.Visible = (type == DomainType.Domain || type == DomainType.SubDomain)
				&& cntx.Groups.ContainsKey(ResourceGroups.Web);

            CreateWebSite.Enabled = true;
			CreateWebSite.Checked &= CreateWebSitePanel.Visible;

			// point mail domain
			PointMailDomainPanel.Visible = (type == DomainType.DomainPointer || (type == DomainType.Domain && cntx.Quotas[Quotas.OS_DOMAINPOINTERS].QuotaAllocatedValue == 0))
				&& cntx.Groups.ContainsKey(ResourceGroups.Mail) && MailDomainsList.Items.Count > 0;
			MailDomainsList.Enabled = PointMailDomain.Checked;

			// DNS option
			EnableDnsPanel.Visible = cntx.Groups.ContainsKey(ResourceGroups.Dns);
			EnableDns.Checked &= EnableDnsPanel.Visible;

			// instant alias
			// check if instant alias was setup
			bool instantAliasAllowed = false;
			PackageSettings settings = ES.Services.Packages.GetPackageSettings(PanelSecurity.PackageId, PackageSettings.INSTANT_ALIAS);
			instantAliasAllowed = (settings != null && !String.IsNullOrEmpty(settings["InstantAlias"]));

			InstantAliasPanel.Visible = instantAliasAllowed && (type != DomainType.DomainPointer) /*&& EnableDnsPanel.Visible*/;
			CreateInstantAlias.Checked &= InstantAliasPanel.Visible;

			// allow sub-domains
			AllowSubDomainsPanel.Visible = (type == DomainType.Domain) && PanelSecurity.EffectiveUser.Role != UserRole.User;
		}

		private DomainType GetDomainType(string typeName)
		{
			DomainType type = DomainType.Domain;

			if (!String.IsNullOrEmpty(typeName))
				type = (DomainType)Enum.Parse(typeof(DomainType), typeName, true);

			return type;
		}

		private void BindUserDomains()
		{
			DomainInfo[] allDomains = ES.Services.Servers.GetMyDomains(PanelSecurity.PackageId);

			// filter domains
			List<DomainInfo> domains = new List<DomainInfo>();
			foreach (DomainInfo domain in allDomains)
				if (!domain.IsDomainPointer && !domain.IsSubDomain && !domain.IsInstantAlias)
					domains.Add(domain);

			DomainsList.DataSource = domains;
			DomainsList.DataBind();
		}

		private void BindResellerDomains()
		{
			DomainsList.DataSource = ES.Services.Servers.GetResellerDomains(PanelSecurity.PackageId);
			DomainsList.DataBind();
		}

		private void AddDomain()
		{
			if (!Page.IsValid)
				return;

			// get domain type
			DomainType type = GetDomainType(Request["DomainType"]);

			// get domain name
			string domainName = DomainName.Text.Trim();
			if (type == DomainType.SubDomain || type == DomainType.ProviderSubDomain)
				domainName = SubDomainName.Text.Trim() + "." + DomainsList.SelectedValue;

			int pointWebSiteId = 0;
			int pointMailDomainId = 0;

			// load package context
			PackageContext cntx = PackagesHelper.GetCachedPackageContext(PanelSecurity.PackageId);

			if (type == DomainType.DomainPointer || (type == DomainType.Domain && cntx.Quotas[Quotas.OS_DOMAINPOINTERS].QuotaAllocatedValue == 0))
			{
				if (PointMailDomain.Checked && MailDomainsList.Items.Count > 0)
					pointMailDomainId = Utils.ParseInt(MailDomainsList.SelectedValue, 0);
			}

			// add domain
			int domainId = 0;
			try
			{
				domainId = ES.Services.Servers.AddDomainWithProvisioning(PanelSecurity.PackageId,
					domainName.ToLower(), type, CreateWebSite.Checked, pointWebSiteId, pointMailDomainId,
                    EnableDns.Checked, CreateInstantAlias.Checked, AllowSubDomains.Checked, txtHostName.Text);

				if (domainId < 0)
				{
					ShowResultMessage(domainId);
					return;
				}
			}
			catch (Exception ex)
			{
				ShowErrorMessage("DOMAIN_ADD_DOMAIN", ex);
				return;
			}

			// put created domain to the cookie
			HttpCookie domainCookie = new HttpCookie("CreatedDomainId", domainId.ToString());
			Response.Cookies.Add(domainCookie);

			// return
			RedirectBack();
		}

		private void RedirectBack()
		{
			RedirectSpaceHomePage();
		}

		protected void btnCancel_Click(object sender, EventArgs e)
		{
			// return
			RedirectBack();
		}
		protected void btnAdd_Click(object sender, EventArgs e)
		{
			AddDomain();
		}
	}
}