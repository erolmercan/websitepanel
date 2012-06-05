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
	public partial class EcommerceSystemSettings : ecModuleBase
	{
		const string ITEM_DISABLED = "ITEM_DISABLED";
		const string ITEM_ENABLED = "ITEM_ENABLED";

		protected void Page_Load(object sender, EventArgs e)
		{
			SetupPaymentMethods();
            //
            SetupDomainRegistrars();
            //
            SetupEmailNotifications();
            //
            SetupMiscellaneous();
		}

        private void SetupMiscellaneous()
        {
            LinkTermsAndConds.NavigateUrl = EditUrl("UserID", PanelSecurity.SelectedUserId.ToString(), "terms_conds");
            //
			LinkProvisioningSts.NavigateUrl = EditUrl("UserID", PanelSecurity.SelectedUserId.ToString(), "prov_settings");
			//
			LinkWelcomeMsg.NavigateUrl = EditUrl("UserID", PanelSecurity.SelectedUserId.ToString(), "welcome_msg");
        }

        private void SetupEmailNotifications()
        {
            LinkNewInvoice.NavigateUrl = EditUrl("UserID", PanelSecurity.SelectedUserId.ToString(), "new_invoice");
			//
			LinkPaymentReceived.NavigateUrl = EditUrl("UserID", PanelSecurity.SelectedUserId.ToString(), "payment_rcvd");
			//
			LinkSvcActivate.NavigateUrl = EditUrl("UserID", PanelSecurity.SelectedUserId.ToString(), "svc_activated");
            //
            LinkSvcSuspend.NavigateUrl = EditUrl("UserID", PanelSecurity.SelectedUserId.ToString(), "svc_suspended");
            //
            LinkSvcCancel.NavigateUrl = EditUrl("UserID", PanelSecurity.SelectedUserId.ToString(), "svc_cancelled");
        }

        private void SetupDomainRegistrars()
        {
			LinkEnomRegistrar.NavigateUrl = EditUrl("UserID", PanelSecurity.SelectedUserId.ToString(), "enom");
            //
			LinkDirectiRegistrar.NavigateUrl = EditUrl("UserID", PanelSecurity.SelectedUserId.ToString(), "directi");
        }

		private void SetupPaymentMethods()
		{
			LinkCreditCard.NavigateUrl = EditUrl("UserID", PanelSecurity.SelectedUserId.ToString(), "credit_card");
			//
			Link2Checkout.NavigateUrl = EditUrl("UserID", PanelSecurity.SelectedUserId.ToString(), "2co");
			//
			LinkPayPalAccnt.NavigateUrl = EditUrl("UserID", PanelSecurity.SelectedUserId.ToString(), "pp_account");
			//
			LinkOffline.NavigateUrl = EditUrl("UserID", PanelSecurity.SelectedUserId.ToString(), "offline");
		}

        private void DomainRegistrars_PreRender()
        {
			// ENOM
			bool enomActive = StorehouseHelper.IsSupportedPluginActive(SupportedPlugin.ENOM);
            LinkEnomRegistrar.Text += " " + (enomActive ? "(" + GetSharedLocalizedString(Keys.ModuleName, ITEM_ENABLED) + ")"
				: "(" + GetSharedLocalizedString(Keys.ModuleName, ITEM_DISABLED) + ")");
			// DIRECTI
            bool directiActive = StorehouseHelper.IsSupportedPluginActive(SupportedPlugin.DIRECTI);
            LinkDirectiRegistrar.Text += " " + (directiActive ? "(" + GetSharedLocalizedString(Keys.ModuleName, ITEM_ENABLED) + ")"
                : "(" + GetSharedLocalizedString(Keys.ModuleName, ITEM_DISABLED) + ")");
        }

		private void PaymentMethods_PreRender()
		{
			// CREDIT CARD
			PaymentMethod method_cc = StorehouseHelper.GetPaymentMethod(PaymentMethod.CREDIT_CARD);
            LinkCreditCard.Text += " " + ((method_cc == null) ? "(" + GetSharedLocalizedString(Keys.ModuleName, ITEM_DISABLED) + ")"
				: "(" + GetSharedLocalizedString(Keys.ModuleName, ITEM_ENABLED) + ")");
			// 2CO
			PaymentMethod method_2co = StorehouseHelper.GetPaymentMethod(PaymentMethod.TCO);
            Link2Checkout.Text += " " + ((method_2co == null) ? "(" + GetSharedLocalizedString(Keys.ModuleName, ITEM_DISABLED) + ")"
				: "(" + GetSharedLocalizedString(Keys.ModuleName, ITEM_ENABLED) + ")");
			// PAYPAL STANDARD
			PaymentMethod method_pp = StorehouseHelper.GetPaymentMethod(PaymentMethod.PP_ACCOUNT);
            LinkPayPalAccnt.Text += " " + ((method_pp == null) ? "(" + GetSharedLocalizedString(Keys.ModuleName, ITEM_DISABLED) + ")"
				: "(" + GetSharedLocalizedString(Keys.ModuleName, ITEM_ENABLED) + ")");
			// OFFLINE
			PaymentMethod method_off = StorehouseHelper.GetPaymentMethod(PaymentMethod.OFFLINE);
            LinkOffline.Text += " " + ((method_off == null) ? "(" + GetSharedLocalizedString(Keys.ModuleName, ITEM_DISABLED) + ")"
				: "(" + GetSharedLocalizedString(Keys.ModuleName, ITEM_ENABLED) + ")");
		}

		protected override void OnPreRender(EventArgs e)
		{
			base.OnPreRender(e);
			// payment methods
			PaymentMethods_PreRender();
            //
            DomainRegistrars_PreRender();
		}
	}
}