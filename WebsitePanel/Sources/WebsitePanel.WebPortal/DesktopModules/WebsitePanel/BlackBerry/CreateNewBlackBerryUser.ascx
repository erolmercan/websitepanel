﻿<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CreateNewBlackBerryUser.ascx.cs" Inherits="WebsitePanel.Portal.BlackBerry.CreateNewBlackBerryUser" %>
<%@ Register Src="../ExchangeServer/UserControls/UserSelector.ascx" TagName="UserSelector"
    TagPrefix="wsp" %>
<%@ Register Src="../UserControls/SimpleMessageBox.ascx" TagName="SimpleMessageBox"
    TagPrefix="wsp" %>
<%@ Register Src="../UserControls/EnableAsyncTasksSupport.ascx" TagName="EnableAsyncTasksSupport"
    TagPrefix="wsp" %>
<%@ Register Src="../UserControls/QuotaViewer.ascx" TagName="QuotaViewer" TagPrefix="wsp" %>
<%@ Register src="../ExchangeServer/UserControls/MailboxSelector.ascx" tagname="MailboxSelector" tagprefix="uc1" %>
<wsp:EnableAsyncTasksSupport id="asyncTasks" runat="server" />
<div id="ExchangeContainer">
    <div class="Module">
        <div class="Left">
        </div>
        <div class="Content">
            <div class="Center">
                <div class="Title">
                    <asp:Image ID="Image1" SkinID="BlackBerryUsersLogo" runat="server" />
                    <asp:Localize ID="locTitle" runat="server" meta:resourcekey="locTitle"></asp:Localize>
                </div>
                <div class="FormBody">
                    <wsp:SimpleMessageBox id="messageBox" runat="server" />
                    <table id="ExistingUserTable"   runat="server" width="100%"> 					    
					    <tr>
					        <td class="FormLabel150"><asp:Localize ID="Localize1" runat="server" meta:resourcekey="locDisplayName" Text="Display Name: *"></asp:Localize></td>
					        <td>
                                <uc1:MailboxSelector ID="mailboxSelector" ContactsEnabled="false" ShowOnlyMailboxes="true" MailboxesEnabled="true"  DistributionListsEnabled="false" runat="server" />
                            </td>
					    </tr>
					    	    					    					    
					</table>
					
					<div class="FormFooterClean">
					    <asp:Button id="btnCreate" runat="server" 
					    CssClass="Button1" meta:resourcekey="btnCreate" 
					     onclick="btnCreate_Click" ></asp:Button>					    
				    </div>			
                </div>
            </div>
        </div>
    </div>
</div>