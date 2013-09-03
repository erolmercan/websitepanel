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
using WebsitePanel.EnterpriseServer;
using WebsitePanel.Providers.ResultObjects;
using WebsitePanel.Providers.HostedSolution;


namespace WebsitePanel.Portal.HostedSolution
{
    public partial class OrganizationCreateUser : WebsitePanelModuleBase
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                password.SetPackagePolicy(PanelSecurity.PackageId, UserSettings.EXCHANGE_POLICY, "MailboxPasswordPolicy");

                PasswordPolicyResult passwordPolicy = ES.Services.Organizations.GetPasswordPolicy(PanelRequest.ItemID);
                if (passwordPolicy.IsSuccess)
                {
                    password.MinimumLength = passwordPolicy.Value.MinLength;
                    if (passwordPolicy.Value.IsComplexityEnable)
                    {
                        password.MinimumNumbers = 1;
                        password.MinimumSymbols = 1;
                        password.MinimumUppercase = 1;
                    }
                }
                else
                {
                    messageBox.ShowMessage(passwordPolicy, "CREATE_ORGANIZATION_USER", "HostedOrganization");
                }

                string instructions = ES.Services.Organizations.GetOrganizationUserSummuryLetter(PanelRequest.ItemID, PanelRequest.AccountID, false, false, false);
                if (!string.IsNullOrEmpty(instructions))
                {
                    chkSendInstructions.Checked = chkSendInstructions.Visible = sendInstructionEmail.Visible = true;
                    PackageInfo package = ES.Services.Packages.GetPackage(PanelSecurity.PackageId);
                    if (package != null)
                    {
                        UserInfo user = ES.Services.Users.GetUserById(package.UserId);
                        if (user != null)
                            sendInstructionEmail.Text = user.Email;
                    }
                }
                else
                {
                    chkSendInstructions.Checked = chkSendInstructions.Visible = sendInstructionEmail.Visible = false;
                }
            }


            PackageContext cntx = PackagesHelper.GetCachedPackageContext(PanelSecurity.PackageId);
            if (cntx.Quotas.ContainsKey(Quotas.EXCHANGE2007_ISCONSUMER))
            {
                if (cntx.Quotas[Quotas.EXCHANGE2007_ISCONSUMER].QuotaAllocatedValue != 1)
                {
                    locSubscriberNumber.Visible = txtSubscriberNumber.Visible = valRequireSubscriberNumber.Enabled = false;
                }
            }

        }

        protected void btnCreate_Click(object sender, EventArgs e)
        {
            CreateMailbox();
        }

        private void CreateMailbox()
        {
            if (!Page.IsValid)
                return;

            try
            {
                int accountId = ES.Services.Organizations.CreateUser(PanelRequest.ItemID, txtDisplayName.Text.Trim(),
                    email.AccountName.ToLower(),
                    email.DomainName.ToLower(),
                    password.Password,
                    txtSubscriberNumber.Text.Trim(),
                    chkSendInstructions.Checked,
                    sendInstructionEmail.Text);

                if (accountId < 0)
                {
                    messageBox.ShowResultMessage(accountId);
                    return;
                }
                else
                {
                    if ((!string.IsNullOrEmpty(txtFirstName.Text)) | (!string.IsNullOrEmpty(txtLastName.Text)) | (!string.IsNullOrEmpty(txtInitials.Text)))
                    {
                        SetUserAttributes(accountId);
                    }
                }

                Response.Redirect(EditUrl("AccountID", accountId.ToString(), "edit_user",
                    "SpaceID=" + PanelSecurity.PackageId,
                    "ItemID=" + PanelRequest.ItemID,
                    "Context=User"));
            }
            catch (Exception ex)
            {
                messageBox.ShowErrorMessage("ORGANIZATION_CREATE_USER", ex);
            }
        }

        private void SetUserAttributes(int accountId)
        {
            OrganizationUser user = ES.Services.Organizations.GetUserGeneralSettings(PanelRequest.ItemID, accountId);

            ES.Services.Organizations.SetUserGeneralSettings(
                    PanelRequest.ItemID, accountId,
                    txtDisplayName.Text,
                    null,
                    false,
                    user.Disabled,
                    user.Locked,

                    txtFirstName.Text,
                    txtInitials.Text,
                    txtLastName.Text,

                    null,
                    null,
                    null,
                    null,
                    null,

                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    user.ExternalEmail,
                    txtSubscriberNumber.Text);
        }
    }
}