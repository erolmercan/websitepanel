// Copyright (c) 2015, Outercurve Foundation.
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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

using WebsitePanel.Ecommerce.EnterpriseServer;

namespace WebsitePanel.Ecommerce.Portal
{
    public partial class DomainNamesAddDomain : ecModuleBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
			if (!IsPostBack)
				BindDomainRegistrars();
        }

		protected void btnCreateTLD_Click(object sender, EventArgs e)
		{
			CreateDomainProduct();
		}

		protected void btnCancel_Click(object sender, EventArgs e)
		{
			RedirectToBrowsePage();
		}

		protected void ctxValBillingCycles_EvaluatingContext(object sender, ManualValidationEventArgs e)
		{
			// get tld cycles
			DomainNameCycle[] cycles = ctlBillingCycles.GetDomainNameCycles();
			//
			if (cycles != null && cycles.Length > 0)
			{
				e.ContextIsValid = true;
				return;
			}
			//
			e.ContextIsValid = false;
		}

		private void BindDomainRegistrars()
		{
			ddlTLDRegistrar.DataSource = StorehouseHelper.GetSupportedPluginsByGroup(SupportedPlugin.DOMAIN_REGISTRAR_GROUP);
			ddlTLDRegistrar.DataBind();
		}

		private void CreateDomainProduct()
		{
			if (!Page.IsValid)
				return;

			try
			{
				string tld = txtDomainTLD.Text.Trim();
				//
				int pluginId = Convert.ToInt32(ddlTLDRegistrar.SelectedValue);
				//
				string productSku = txtProductSku.Text.Trim();
				//
                bool taxInclusive = chkTaxInclusive.Checked;
                //
				bool whoisEnabled = chkWhoisEnabled.Checked;
				//
				bool enabled = Convert.ToBoolean(rblTLDStatus.SelectedValue);
				//
				DomainNameCycle[] cycles = ctlBillingCycles.GetDomainNameCycles();
				
				int result = StorehouseHelper.AddTopLevelDomain(tld, productSku, taxInclusive, pluginId, 
					enabled, whoisEnabled, cycles);

				if (result <= 0)
				{
					ShowResultMessage(result);
					return;
				}
			}
			catch (Exception ex)
			{
				ShowErrorMessage("DOMAIN_PRODUCT_SAVE", ex);
				return;
			}

			RedirectToBrowsePage();
		}
    }
}
