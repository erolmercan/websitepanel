<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="EnterpriseStoragePermissions.ascx.cs" Inherits="WebsitePanel.Portal.ExchangeServer.UserControls.EnterpriseStoragePermissions" %>
<%@ Register Src="../../UserControls/PopupHeader.ascx" TagName="PopupHeader" TagPrefix="wsp" %>

<asp:UpdatePanel ID="PermissionsUpdatePanel" runat="server" UpdateMode="Conditional" ChildrenAsTriggers="true">
    <ContentTemplate>
	<div class="FormButtonsBarClean">
		<asp:Button ID="btnAdd" runat="server" Text="Add..." CssClass="Button1"  OnClick="btnAdd_Click" meta:resourcekey="btnAdd"  />
		<asp:Button ID="btnDelete" runat="server" Text="Delete" CssClass="Button1" OnClick="btnDelete_Click" meta:resourcekey="btnDelete"/>
	</div>
	<asp:GridView ID="gvPermissions" runat="server" meta:resourcekey="gvPermissions" AutoGenerateColumns="False"
		Width="600px" CssSelectorClass="NormalGridView"
		DataKeyNames="Account">
		<Columns>
			<asp:TemplateField>
				<HeaderTemplate>
					<asp:CheckBox ID="chkSelectAll" runat="server" onclick="javascript:SelectAllCheckboxes(this);" />
				</HeaderTemplate>
				<ItemTemplate>
					<asp:CheckBox ID="chkSelect" runat="server" />
				</ItemTemplate>
				<ItemStyle Width="10px" />
			</asp:TemplateField>
			<asp:TemplateField meta:resourcekey="gvPermissionsAccount" HeaderText="gvPermissionsAccount">
				<ItemStyle Width="60%" Wrap="false">
				</ItemStyle>
				<ItemTemplate>
                    <asp:Literal ID="litAccount" runat="server" Text='<%# Eval("DisplayName") %>'></asp:Literal>
				</ItemTemplate>
			</asp:TemplateField>
			<asp:TemplateField meta:resourcekey="gvPermissionsAccess" HeaderText="gvPermissionsAccess">
				<ItemStyle Width="40%" Wrap="false">
				</ItemStyle>
				<ItemTemplate>
					<asp:Literal ID="litAccess" runat="server" Text='<%# Eval("Access") %>'></asp:Literal>
				</ItemTemplate>
			</asp:TemplateField>
		</Columns>
	</asp:GridView>
    <br />
    <div class="FormButtonsBarClean">
		<asp:Button ID="btnSetReadOnly" runat="server" Text="Set Read-Only" CssClass="Button1"  OnClick="btn_UpdateAccess" CommandArgument="Read-Only" meta:resourcekey="btnSetReadOnly"  />
		<asp:Button ID="btnSetReadWrite" runat="server" Text="Set Read-Write" CssClass="Button1" OnClick="btn_UpdateAccess" CommandArgument="Read-Write" meta:resourcekey="btnSetReadWrite"/>
	</div>


<asp:Panel ID="AddAccountsPanel" runat="server" CssClass="Popup" style="display:none">
	<table class="Popup-Header" cellpadding="0" cellspacing="0">
		<tr>
			<td class="Popup-HeaderLeft"></td>
			<td class="Popup-HeaderTitle">
				<asp:Localize ID="headerAddAccounts" runat="server" meta:resourcekey="headerAddAccounts"></asp:Localize>
			</td>
			<td class="Popup-HeaderRight"></td>
		</tr>
	</table>
	<div class="Popup-Content">
		<div class="Popup-Body">
			<br />
<asp:UpdatePanel ID="AddAccountsUpdatePanel" runat="server" UpdateMode="Conditional" ChildrenAsTriggers="true">
    <ContentTemplate>
	            
                <div class="FormButtonsBarClean">
                    <div class="FormButtonsBarCleanRight">
                        <asp:Panel ID="SearchPanel" runat="server" DefaultButton="cmdSearch">
                            <asp:DropDownList ID="ddlSearchColumn" runat="server" CssClass="NormalTextBox">
                                <asp:ListItem Value="DisplayName" meta:resourcekey="ddlSearchColumnDisplayName">DisplayName</asp:ListItem>
                                <asp:ListItem Value="PrimaryEmailAddress" meta:resourcekey="ddlSearchColumnEmail">Email</asp:ListItem>
                            </asp:DropDownList><asp:TextBox ID="txtSearchValue" runat="server" CssClass="NormalTextBox" Width="100"></asp:TextBox><asp:ImageButton ID="cmdSearch" Runat="server" meta:resourcekey="cmdSearch" SkinID="SearchButton"
	                            CausesValidation="false" OnClick="cmdSearch_Click"/>
                        </asp:Panel>
                    </div>
                </div>
                <div class="Popup-Scroll">
					<asp:GridView ID="gvPopupAccounts" runat="server" meta:resourcekey="gvPopupAccounts" AutoGenerateColumns="False"
						Width="100%" CssSelectorClass="NormalGridView"
						DataKeyNames="AccountName">
						<Columns>
							<asp:TemplateField>
								<HeaderTemplate>
									<asp:CheckBox ID="chkSelectAll" runat="server" onclick="javascript:SelectAllCheckboxes(this);" />
								</HeaderTemplate>
								<ItemTemplate>
									<asp:CheckBox ID="chkSelect" runat="server" />
									<asp:Literal ID="litAccountType" runat="server" Visible="false" Text='<%# Eval("AccountType") %>'></asp:Literal>
								</ItemTemplate>
								<ItemStyle Width="10px" />
							</asp:TemplateField>
							<asp:TemplateField meta:resourcekey="gvAccountsDisplayName">
								<ItemStyle Width="50%"></ItemStyle>
								<ItemTemplate>
									<asp:Image ID="imgAccount" runat="server" ImageUrl='<%# GetAccountImage((int)Eval("AccountType")) %>' ImageAlign="AbsMiddle" />
									<asp:Literal ID="litDisplayName" runat="server" Text='<%# Eval("DisplayName") %>'></asp:Literal>
								</ItemTemplate>
							</asp:TemplateField>
							<asp:TemplateField meta:resourcekey="gvAccountsEmail">
								<ItemStyle Width="50%"></ItemStyle>
								<ItemTemplate>
									<asp:Literal ID="litPrimaryEmailAddress" runat="server" Text='<%# Eval("PrimaryEmailAddress") %>'></asp:Literal>
								</ItemTemplate>
							</asp:TemplateField>
						</Columns>
					</asp:GridView>
				</div>
	</ContentTemplate>
</asp:UpdatePanel>
			<br />
		</div>
		
		<div class="FormFooter">
			<asp:Button ID="btnAddSelected" runat="server" CssClass="Button1" meta:resourcekey="btnAddSelected" Text="Add Accounts" OnClick="btnAddSelected_Click" />
			<asp:Button ID="btnCancelAdd" runat="server" CssClass="Button1" meta:resourcekey="btnCancel" Text="Cancel" CausesValidation="false" />
		</div>
	</div>
</asp:Panel>

<asp:Button ID="btnAddAccountsFake" runat="server" style="display:none;" />
<ajaxToolkit:ModalPopupExtender ID="AddAccountsModal" runat="server"
	TargetControlID="btnAddAccountsFake" PopupControlID="AddAccountsPanel"
	BackgroundCssClass="modalBackground" DropShadow="false" CancelControlID="btnCancelAdd" />

	</ContentTemplate>
</asp:UpdatePanel>