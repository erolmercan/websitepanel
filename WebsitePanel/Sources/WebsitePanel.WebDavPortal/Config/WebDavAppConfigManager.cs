﻿using System.Configuration;
using WebsitePanel.WebDavPortal.Config.Entities;
using WebsitePanel.WebDavPortal.WebConfigSections;

namespace WebsitePanel.WebDavPortal.Config
{
    public class WebDavAppConfigManager : IWebDavAppConfig
    {
        private static WebDavAppConfigManager _instance;
        private readonly WebDavExplorerConfigurationSettingsSection _configSection;

        private WebDavAppConfigManager()
        {
            _configSection = ((WebDavExplorerConfigurationSettingsSection) ConfigurationManager.GetSection(WebDavExplorerConfigurationSettingsSection.SectionName));
            Rfc2898CryptographyParameters = new Rfc2898CryptographyParameters();
            WebsitePanelConstantUserParameters = new WebsitePanelConstantUserParameters();
            ElementsRendering = new ElementsRendering();
            ConnectionStrings = new ConnectionStringsCollection();
            SessionKeys = new SessionKeysCollection();
            FileIcons = new FileIconsDictionary();
            HttpErrors = new HttpErrorsCollection();
            OfficeOnline = new OfficeOnlineCollection();
        }

        public static WebDavAppConfigManager Instance
        {
            get { return _instance ?? (_instance = new WebDavAppConfigManager()); }
        }

        public string UserDomain
        {
            get { return _configSection.UserDomain.Value; }
        }

        public string ApplicationName
        {
            get { return _configSection.ApplicationName.Value; }
        }

        public ElementsRendering ElementsRendering { get; private set; }
        public WebsitePanelConstantUserParameters WebsitePanelConstantUserParameters { get; private set; }
        public Rfc2898CryptographyParameters Rfc2898CryptographyParameters { get; private set; }
        public ConnectionStringsCollection ConnectionStrings { get; private set; }
        public SessionKeysCollection SessionKeys { get; private set; }
        public FileIconsDictionary FileIcons { get; private set; }
        public HttpErrorsCollection HttpErrors { get; private set; }
        public OfficeOnlineCollection OfficeOnline { get; private set; }
    }
}