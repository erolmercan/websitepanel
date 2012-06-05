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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

using WebsitePanel.Portal;
using WebsitePanel.Ecommerce.EnterpriseServer;

namespace WebsitePanel.Ecommerce.Portal
{
    public partial class HostingPlansAddPlan : ecModuleBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
			if (!IsPostBack)
				LoadHostingPlansAvailable();
        }

		private void LoadHostingPlansAvailable()
		{
			HostingPlansHelper plans = new HostingPlansHelper();

			string[] plansTaken = Array.ConvertAll<int, string>(
				StorehouseHelper.GetHostingPlansTaken(),
				new Converter<int, string>(Convert.ToString)
			);

			DataSet ds = plans.GetRawHostingPlans();
			// check empty dataset
			if (ds != null && ds.Tables.Count > 0)
			{
				// apply filter only if necessary
				if (plansTaken.Length > 0)
				{
					// apply filter for plans already created
					ds.Tables[0].DefaultView.RowFilter = "PlanID NOT IN (" + String.Join(",", plansTaken) + ")";
				}

				// bind default view
				ddlHostingPlans.DataSource = ds.Tables[0].DefaultView;
				ddlHostingPlans.DataBind();
			}
		}

		protected void btnCreatePlan_Click(object sender, EventArgs e)
		{
			CreateHostingPlan();
		}

		protected void btnCancel_Click(object sender, EventArgs e)
		{
			RedirectToBrowsePage();
		}

		protected void ctxValBillingCycles_EvaluatingContext(object sender, ManualValidationEventArgs e)
		{
			// get plan cycles
			HostingPlanCycle[] cycles = ctlPlanCycles.GetHostingPlanCycles();
			//
			if (cycles != null && cycles.Length > 0)
			{
				e.ContextIsValid = true;
				return;
			}
			//
			e.ContextIsValid = false;
		}

		private void CreateHostingPlan()
		{
			if (!Page.IsValid)
				return;

			try
			{
				string planName = ddlHostingPlans.SelectedItem.Text;
				string productSku = txtProductSku.Text.Trim();
                bool taxInlcusive = chkTaxInclusive.Checked;
				string planDescription = txtHostingPlanDesc.Text.Trim();
				bool enabled = Convert.ToBoolean(rblPlanStatus.SelectedValue);

				int planId = Convert.ToInt32(ddlHostingPlans.SelectedValue);
				int userRole = Convert.ToInt32(rblPlanIntendsFor.SelectedValue);
				int initialStatus = Convert.ToInt32(ddlInitialStatus.SelectedValue);
				int domainOption = Convert.ToInt32(ddlDomainOption.SelectedValue);

				HostingPlanCycle[] planCycles = ctlPlanCycles.GetHostingPlanCycles();
				int[] planCategories = ctlAssignedCats.AssignedCategories;
				string[] planHighlights = ctlPlanHighlights.HighlightedItems.ToArray();
				// create hosting plan
				int result = StorehouseHelper.AddHostingPlan(
					planName,
					productSku,
                    taxInlcusive,
					planId,
					userRole,
					initialStatus,
					domainOption,
					enabled,
					planDescription,
					planCycles,
					planHighlights,
					planCategories
				);

				if (result <= 0)
				{
					ShowResultMessage(result);
					return;
				}
			}
			catch (Exception ex)
			{
				ShowErrorMessage("HOSTING_PLAN_SAVE", ex);
				return;
			}

			RedirectToBrowsePage();
		}
    }
}