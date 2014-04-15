// Copyright (c) 2014, Outercurve Foundation.
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

using WebsitePanel.Portal;
using WebsitePanel.Ecommerce.EnterpriseServer;

namespace WebsitePanel.Ecommerce.Portal.DesktopModules.Ecommerce
{
	public partial class HostingAddonsEditAddon : ecModuleBase
	{
		public const string VIEW_STATE_KEY = "__HostingAddon";

		protected HostingAddon EditingAddon
		{
			get { return GetViewStateObject<HostingAddon>(VIEW_STATE_KEY, new GetStateObjectDelegate(GetHostingAddon)); }
		}

		private object GetHostingAddon()
		{
			return StorehouseHelper.GetHostingAddon(ecPanelRequest.ProductId);
		}

		protected void Page_Load(object sender, EventArgs e)
		{
			if (!IsPostBack)
			{
				// go back
				if (EditingAddon == null)
					RedirectToBrowsePage();
				// load data
				LoadHostingAddonsAvailable();
				LoadHostingAddonData();
			}
		}

		protected void ctxValBillingCycles_EvaluatingContext(object sender, ManualValidationEventArgs e)
		{
			if (rblRecurringAddon.SelectedIndex == 1)
			{
				e.ContextIsValid = true;
				return;
			}
			// get addon cycles
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

		private void LoadHostingAddonsAvailable()
		{
			HostingPlansHelper plans = new HostingPlansHelper();

			string[] addonsTaken = Array.ConvertAll<int, string>(
				StorehouseHelper.GetHostingAddonsTaken(),
				new Converter<int, string>(Convert.ToString)
			);

			DataSet ds = plans.GetRawHostingAddons();
			// check empty dataset
			if (ds != null && ds.Tables.Count > 0)
			{
				// apply filter only if necessary
				if (addonsTaken.Length > 0)
				{
					// apply filter for plans already created exept editing one
					ds.Tables[0].DefaultView.RowFilter = "PlanID NOT IN (" + 
						String.Join(",", addonsTaken).Replace(EditingAddon.PlanId.ToString(), "0") + ")";
				}

				// bind default view
				ddlHostingPlans.DataSource = ds.Tables[0].DefaultView;
				ddlHostingPlans.DataBind();
			}
		}

		private void LoadHostingAddonData()
		{
			// toggle controls
			ecUtils.ToggleControls(!EditingAddon.DummyAddon, ddlHostingPlans);
			ecUtils.ToggleControls(EditingAddon.DummyAddon, txtAddonName);
            chkTaxInclusive.Checked = EditingAddon.TaxInclusive;
			//
			reqAddonName.ControlToValidate = EditingAddon.DummyAddon ? "txtAddonName" : "ddlHostingPlans";
			//
			if (EditingAddon.DummyAddon)
				txtAddonName.Text = EditingAddon.ProductName;
			else
				ecUtils.SelectListItem(ddlHostingPlans, EditingAddon.PlanId);
			//
			txtProductSku.Text = EditingAddon.ProductSku;
			//
			txtHostingAddonDesc.Text = EditingAddon.Description;
			//
			rblAddonStatus.SelectedIndex = (EditingAddon.Enabled) ? 0 : 1;
			//
			rblRecurringAddon.SelectedIndex = (EditingAddon.Recurring) ? 0 : 1;
			//
			rblDummyAddon.SelectedIndex = (EditingAddon.DummyAddon) ? 0 : 1;
			//
			rblShowQuantity.SelectedIndex = (EditingAddon.Countable) ? 0 : 1;
			//
			ctlPlanCycles.Visible = EditingAddon.Recurring;
			ctlOneTimeFee.Visible = !EditingAddon.Recurring;
			//
			if (EditingAddon.Recurring)
				ctlPlanCycles.LoadHostingPlanCycles(
					StorehouseHelper.GetHostingAddonCycles(ecPanelRequest.ProductId));
			else
				ctlOneTimeFee.SyncDataAssigned(EditingAddon.SetupFee, EditingAddon.OneTimeFee);
			//
			ctlAssignedProds.AssignedProducts = StorehouseHelper.GetAddonProductsIds(
				ecPanelRequest.ProductId);
		}

		protected void btnSaveAddon_Click(object sender, EventArgs e)
		{
			SaveHostingAddon();
		}

		protected void btnCancel_Click(object sender, EventArgs e)
		{
			RedirectToBrowsePage();
		}

		protected void btnDeleteAddon_Click(object sender, EventArgs e)
		{
			DeleteHostingAddon();
		}

		protected void rblRecurringAddon_SelectedIndexChanged(object sender, EventArgs e)
		{
			bool recurring = Convert.ToBoolean(rblRecurringAddon.SelectedValue);

			ctlPlanCycles.Visible = recurring;
			ctlOneTimeFee.Visible = !recurring;
		}

		protected void rblDummyAddon_SelectedIndexChanged(object sender, EventArgs e)
		{
			bool dummyAddon = Convert.ToBoolean(rblDummyAddon.SelectedValue);
			//
			reqAddonName.ControlToValidate = dummyAddon ? "txtAddonName" : "ddlHostingPlans";
			//
			txtAddonName.Visible = dummyAddon;
			//
			ddlHostingPlans.Visible = !dummyAddon;
		}

		private void DeleteHostingAddon()
		{
			try
			{
				int result = StorehouseHelper.DeleteProduct(ecPanelRequest.ProductId);

				if (result < 0)
				{
					ShowResultMessage(result);
					return;
				}
			}
			catch (Exception ex)
			{
				ShowErrorMessage("HOSTING_ADDON_DELETE", ex);
				return;
			}

			RedirectToBrowsePage();
		}

		private void SaveHostingAddon()
		{
			if (!Page.IsValid)
				return;

			try
			{
				string addonName = (ddlHostingPlans.Visible) ? ddlHostingPlans.SelectedItem.Text : txtAddonName.Text.Trim();
				string productSku = txtProductSku.Text.Trim();
				string description = txtHostingAddonDesc.Text.Trim();

                bool taxInclusive = chkTaxInclusive.Checked;
				bool enabled = Convert.ToBoolean(rblAddonStatus.SelectedValue);
				bool recurring = Convert.ToBoolean(rblRecurringAddon.SelectedValue);
				bool dummyAddon = Convert.ToBoolean(rblDummyAddon.SelectedValue);
				bool showQuantity = Convert.ToBoolean(rblShowQuantity.SelectedValue);
				//
				int planId = 0;
				//
				if (!dummyAddon)
					planId = Convert.ToInt32(ddlHostingPlans.SelectedValue);

				HostingPlanCycle[] addonCycles = null;
				if (recurring)
					addonCycles = ctlPlanCycles.GetHostingPlanCycles();
				else
					addonCycles = new HostingPlanCycle[] { ctlOneTimeFee.OneTimeFee };

				int[] addonProducts = ctlAssignedProds.AssignedProducts;

				// create hosting plan
				int result = StorehouseHelper.UpdateHostingAddon(
					ecPanelRequest.ProductId,
					addonName,
					productSku,
                    taxInclusive,
					planId,
					recurring,
					dummyAddon,
					showQuantity,
					enabled,
					description,
					addonCycles,
					addonProducts
				);

				if (result < 0)
				{
					ShowResultMessage(result);
					return;
				}
			}
			catch (Exception ex)
			{
				ShowErrorMessage("HOSTING_ADDON_SAVE", ex);
				return;
			}

			RedirectToBrowsePage();
		}
	}
}