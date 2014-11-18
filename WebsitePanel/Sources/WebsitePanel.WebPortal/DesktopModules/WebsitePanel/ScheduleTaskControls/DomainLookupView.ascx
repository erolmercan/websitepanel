﻿<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="DomainLookupView.ascx.cs" Inherits="WebsitePanel.Portal.ScheduleTaskControls.DomainLookupView" %>

<table cellspacing="0" cellpadding="4" width="100%">
    <tr>
        <td class="SubHead" nowrap>
            <asp:Label ID="lblDnsServers" runat="server" meta:resourcekey="lblDnsServers" Text="DNS Servers:"></asp:Label>
        </td>
        <td class="Normal" width="100%">
            <asp:TextBox ID="txtDnsServers" runat="server" Width="95%" CssClass="NormalTextBox" MaxLength="1000"></asp:TextBox>
        </td>
    </tr>
    <tr>
        <td class="SubHead" nowrap>
            <asp:Label ID="lblMailTo" runat="server" meta:resourcekey="lblMailTo" Text="Mail To:"></asp:Label>
        </td>
        <td class="Normal" width="100%">
            <asp:TextBox ID="txtMailTo" runat="server" Width="95%" CssClass="NormalTextBox" MaxLength="1000"></asp:TextBox>
    </tr>
</table>
