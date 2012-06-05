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

using WebsitePanel.Ecommerce.EnterpriseServer;

namespace WebsitePanel.Ecommerce.Portal.SupportedPlugins
{
	public partial class PayPalStandard_Settings : ecControlBase, IPluginProperties
	{
		#region IPluginProperties Members

		public KeyValueBunch Properties
		{
			get
			{
				return GetProviderSettings();
			}
			set
			{
				SetProviderSettings(value);
			}
		}

		#endregion

		protected void Page_Load(object sender, EventArgs e)
		{

		}

		private void SetProviderSettings(KeyValueBunch props)
		{
			// set business
			txtBusiness.Text = props[PayPalStdSettings.BUSINESS];
			// set live mode
			chkLiveMode.Checked = ecUtils.ParseBoolean(props[PayPalStdSettings.LIVE_MODE], false);
		}

		private KeyValueBunch GetProviderSettings()
		{
			KeyValueBunch props = new KeyValueBunch();
			// copy business
			props[PayPalStdSettings.BUSINESS] = txtBusiness.Text.Trim();
			// copy live mode
			props[PayPalStdSettings.LIVE_MODE] = chkLiveMode.Checked.ToString();
			// copy return url
			props[PayPalStdSettings.RETURN_URL] = EcommerceSettings.AbsoluteAppPath + "/DesktopModules/Ecommerce/Plugins/PayPalStd/PP_Routine.aspx";
			// copy cancel return url
			props[PayPalStdSettings.CANCEL_RETURN_URL] = EcommerceSettings.StorefrontUrl;
			//
			return props;
		}
	}
}