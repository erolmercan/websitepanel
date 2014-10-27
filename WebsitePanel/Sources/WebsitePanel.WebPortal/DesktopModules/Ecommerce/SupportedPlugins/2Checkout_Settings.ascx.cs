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

using WebsitePanel.Ecommerce.EnterpriseServer;

namespace WebsitePanel.Ecommerce.Portal.SupportedPlugins
{
	public partial class ToCheckout_Settings : ecControlBase, IPluginProperties
	{
		#region IPluginProperties Members

		public KeyValueBunch Properties
		{
			get
			{
				return GetProviderProperties();
			}
			set
			{
				SetProviderProperties(value);
			}
		}

		#endregion

		protected void Page_Load(object sender, EventArgs e)
		{
			//
			litPaymentRoutine.Text = EcommerceSettings.AbsoluteAppPath + "/Services/2Checkout.ashx";
		}

		private void SetProviderProperties(KeyValueBunch props)
		{
			if (!props.IsEmpty)
				txtSecretWord.EnableDefaultPassword();

			txt2COAccount.Text = props[ToCheckoutSettings.ACCOUNT_SID];
			//
			chkLiveMode.Checked = ecUtils.ParseBoolean(props[ToCheckoutSettings.LIVE_MODE], false);
			//
			chkFixedCart.Checked = ecUtils.ParseBoolean(props[ToCheckoutSettings.FIXED_CART], false);
			//
			ecUtils.SelectListItem(ddl2CO_Currency, props[ToCheckoutSettings.CURRENCY]);
		}

		private KeyValueBunch GetProviderProperties()
		{
			KeyValueBunch props = new KeyValueBunch();
			// change secret word only if it was changed
			if (txtSecretWord.PasswordChanged)
				props[ToCheckoutSettings.SECRET_WORD] = txtSecretWord.Text.Trim();
			//
			props[ToCheckoutSettings.ACCOUNT_SID] = txt2COAccount.Text.Trim();
			//
			props[ToCheckoutSettings.CURRENCY] = ddl2CO_Currency.SelectedValue;
			//
			props[ToCheckoutSettings.LIVE_MODE] = chkLiveMode.Checked.ToString();
			//
			props[ToCheckoutSettings.FIXED_CART] = chkFixedCart.Checked.ToString();
			//
			props[ToCheckoutSettings.CONTINUE_SHOPPING_URL] = EcommerceSettings.StorefrontUrl; 
			//
			return props;
		}
	}
}