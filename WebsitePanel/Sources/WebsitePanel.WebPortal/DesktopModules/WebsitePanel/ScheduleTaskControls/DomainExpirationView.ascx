﻿<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="DomainExpirationView.ascx.cs" Inherits="WebsitePanel.Portal.ScheduleTaskControls.DomainExpirationView" %>


<table cellspacing="0" cellpadding="4" width="100%">
    <tr>
        <td class="SubHead" nowrap>
            <asp:Label ID="Label1" runat="server" meta:resourcekey="cbEnableNotify" Text="Enable Client Notification:"></asp:Label>
        </td>
        <td>
            <asp:CheckBox runat="server" ID="cbEnableNotify" meta:resourcekey="cbEnableNotify" /><br/>
        </td>
    </tr>
    <tr>
        <td class="SubHead" nowrap>
            <asp:Label ID="lblMailTo" runat="server" meta:resourcekey="lblMailTo" Text="Mail To:"></asp:Label>
        </td>
        <td class="Normal" width="100%">
            <asp:TextBox ID="txtMailTo" runat="server" Width="95%" CssClass="NormalTextBox" MaxLength="1000"></asp:TextBox>
         </td>
    </tr>

    <tr>
        <td class="SubHead" nowrap>
            <asp:Label ID="lblDayBeforeNotify" runat="server" meta:resourcekey="lblDayBeforeNotify" Text="Notify before:"></asp:Label>
        </td>
        <td class="Normal" width="100%">
            <asp:TextBox ID="txtDaysBeforeNotify" runat="server" Width="95%" CssClass="NormalTextBox" MaxLength="1000"></asp:TextBox>
            <asp:Label ID="lblDayBeforeNotifyHint" runat="server" meta:resourcekey="lblDayBeforeNotifyHint" Text="Number of days before expiration date"></asp:Label>
        </td>
    </tr>
</table>