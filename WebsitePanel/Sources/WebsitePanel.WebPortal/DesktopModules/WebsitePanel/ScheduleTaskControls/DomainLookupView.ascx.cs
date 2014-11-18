﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using WebsitePanel.EnterpriseServer;
using WebsitePanel.Portal.UserControls.ScheduleTaskView;

namespace WebsitePanel.Portal.ScheduleTaskControls
{
    public partial class DomainLookupView : EmptyView
    {
        private static readonly string DnsServersParameter = "DNS_SERVERS";
        private static readonly string MailToParameter = "MAIL_TO";

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Sets scheduler task parameters on view.
        /// </summary>
        /// <param name="parameters">Parameters list to be set on view.</param>
        public override void SetParameters(ScheduleTaskParameterInfo[] parameters)
        {
            base.SetParameters(parameters);

            this.SetParameter(this.txtDnsServers, DnsServersParameter);
            this.SetParameter(this.txtMailTo, MailToParameter);
        }

        /// <summary>
        /// Gets scheduler task parameters from view.
        /// </summary>
        /// <returns>Parameters list filled  from view.</returns>
        public override ScheduleTaskParameterInfo[] GetParameters()
        {
            ScheduleTaskParameterInfo dnsServers = this.GetParameter(this.txtDnsServers, DnsServersParameter);
            ScheduleTaskParameterInfo mailTo = this.GetParameter(this.txtMailTo, MailToParameter);

            return new ScheduleTaskParameterInfo[2] { dnsServers, mailTo };
        }
    }
}