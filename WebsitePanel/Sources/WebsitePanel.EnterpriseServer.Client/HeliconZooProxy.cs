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

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.5466
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using WebsitePanel.Providers.HeliconZoo;

// 
// This source code was auto-generated by wsdl, Version=2.0.50727.42.
// 
namespace WebsitePanel.EnterpriseServer {
    using System.Xml.Serialization;
    using System.Web.Services;
    using System.ComponentModel;
    using System.Web.Services.Protocols;
    using System;
    using System.Diagnostics;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name="esHeliconZooSoap", Namespace="http://smbsaas/websitepanel/enterpriseserver")]
    public partial class esHeliconZoo : Microsoft.Web.Services3.WebServicesClientProtocol {
        
        private System.Threading.SendOrPostCallback GetEnginesOperationCompleted;
        
        private System.Threading.SendOrPostCallback SetEnginesOperationCompleted;
        
        private System.Threading.SendOrPostCallback IsEnginesEnabledOperationCompleted;
        
        private System.Threading.SendOrPostCallback SwithEnginesEnabledOperationCompleted;
        
        private System.Threading.SendOrPostCallback GetAllowedHeliconZooQuotasForPackageOperationCompleted;
        
        private System.Threading.SendOrPostCallback GetEnabledEnginesForSiteOperationCompleted;
        
        private System.Threading.SendOrPostCallback SetEnabledEnginesForSiteOperationCompleted;
        
        private System.Threading.SendOrPostCallback IsWebCosoleEnabledOperationCompleted;
        
        private System.Threading.SendOrPostCallback SetWebCosoleEnabledOperationCompleted;
        
        /// <remarks/>
        public esHeliconZoo() {
            this.Url = "http://localhost:9002/esHeliconZoo.asmx";
        }
        
        /// <remarks/>
        public event GetEnginesCompletedEventHandler GetEnginesCompleted;
        
        /// <remarks/>
        public event SetEnginesCompletedEventHandler SetEnginesCompleted;
        
        /// <remarks/>
        public event IsEnginesEnabledCompletedEventHandler IsEnginesEnabledCompleted;
        
        /// <remarks/>
        public event SwithEnginesEnabledCompletedEventHandler SwithEnginesEnabledCompleted;
        
        /// <remarks/>
        public event GetAllowedHeliconZooQuotasForPackageCompletedEventHandler GetAllowedHeliconZooQuotasForPackageCompleted;
        
        /// <remarks/>
        public event GetEnabledEnginesForSiteCompletedEventHandler GetEnabledEnginesForSiteCompleted;
        
        /// <remarks/>
        public event SetEnabledEnginesForSiteCompletedEventHandler SetEnabledEnginesForSiteCompleted;
        
        /// <remarks/>
        public event IsWebCosoleEnabledCompletedEventHandler IsWebCosoleEnabledCompleted;
        
        /// <remarks/>
        public event SetWebCosoleEnabledCompletedEventHandler SetWebCosoleEnabledCompleted;
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://smbsaas/websitepanel/enterpriseserver/GetEngines", RequestNamespace="http://smbsaas/websitepanel/enterpriseserver", ResponseNamespace="http://smbsaas/websitepanel/enterpriseserver", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public HeliconZooEngine[] GetEngines(int serviceId) {
            object[] results = this.Invoke("GetEngines", new object[] {
                        serviceId});
            return ((HeliconZooEngine[])(results[0]));
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginGetEngines(int serviceId, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("GetEngines", new object[] {
                        serviceId}, callback, asyncState);
        }
        
        /// <remarks/>
        public HeliconZooEngine[] EndGetEngines(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((HeliconZooEngine[])(results[0]));
        }
        
        /// <remarks/>
        public void GetEnginesAsync(int serviceId) {
            this.GetEnginesAsync(serviceId, null);
        }
        
        /// <remarks/>
        public void GetEnginesAsync(int serviceId, object userState) {
            if ((this.GetEnginesOperationCompleted == null)) {
                this.GetEnginesOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetEnginesOperationCompleted);
            }
            this.InvokeAsync("GetEngines", new object[] {
                        serviceId}, this.GetEnginesOperationCompleted, userState);
        }
        
        private void OnGetEnginesOperationCompleted(object arg) {
            if ((this.GetEnginesCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetEnginesCompleted(this, new GetEnginesCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://smbsaas/websitepanel/enterpriseserver/SetEngines", RequestNamespace="http://smbsaas/websitepanel/enterpriseserver", ResponseNamespace="http://smbsaas/websitepanel/enterpriseserver", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void SetEngines(int serviceId, HeliconZooEngine[] userEngines) {
            this.Invoke("SetEngines", new object[] {
                        serviceId,
                        userEngines});
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginSetEngines(int serviceId, HeliconZooEngine[] userEngines, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("SetEngines", new object[] {
                        serviceId,
                        userEngines}, callback, asyncState);
        }
        
        /// <remarks/>
        public void EndSetEngines(System.IAsyncResult asyncResult) {
            this.EndInvoke(asyncResult);
        }
        
        /// <remarks/>
        public void SetEnginesAsync(int serviceId, HeliconZooEngine[] userEngines) {
            this.SetEnginesAsync(serviceId, userEngines, null);
        }
        
        /// <remarks/>
        public void SetEnginesAsync(int serviceId, HeliconZooEngine[] userEngines, object userState) {
            if ((this.SetEnginesOperationCompleted == null)) {
                this.SetEnginesOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSetEnginesOperationCompleted);
            }
            this.InvokeAsync("SetEngines", new object[] {
                        serviceId,
                        userEngines}, this.SetEnginesOperationCompleted, userState);
        }
        
        private void OnSetEnginesOperationCompleted(object arg) {
            if ((this.SetEnginesCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.SetEnginesCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://smbsaas/websitepanel/enterpriseserver/IsEnginesEnabled", RequestNamespace="http://smbsaas/websitepanel/enterpriseserver", ResponseNamespace="http://smbsaas/websitepanel/enterpriseserver", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public bool IsEnginesEnabled(int serviceId) {
            object[] results = this.Invoke("IsEnginesEnabled", new object[] {
                        serviceId});
            return ((bool)(results[0]));
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginIsEnginesEnabled(int serviceId, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("IsEnginesEnabled", new object[] {
                        serviceId}, callback, asyncState);
        }
        
        /// <remarks/>
        public bool EndIsEnginesEnabled(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((bool)(results[0]));
        }
        
        /// <remarks/>
        public void IsEnginesEnabledAsync(int serviceId) {
            this.IsEnginesEnabledAsync(serviceId, null);
        }
        
        /// <remarks/>
        public void IsEnginesEnabledAsync(int serviceId, object userState) {
            if ((this.IsEnginesEnabledOperationCompleted == null)) {
                this.IsEnginesEnabledOperationCompleted = new System.Threading.SendOrPostCallback(this.OnIsEnginesEnabledOperationCompleted);
            }
            this.InvokeAsync("IsEnginesEnabled", new object[] {
                        serviceId}, this.IsEnginesEnabledOperationCompleted, userState);
        }
        
        private void OnIsEnginesEnabledOperationCompleted(object arg) {
            if ((this.IsEnginesEnabledCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.IsEnginesEnabledCompleted(this, new IsEnginesEnabledCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://smbsaas/websitepanel/enterpriseserver/SwithEnginesEnabled", RequestNamespace="http://smbsaas/websitepanel/enterpriseserver", ResponseNamespace="http://smbsaas/websitepanel/enterpriseserver", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void SwithEnginesEnabled(int serviceId, bool enabled) {
            this.Invoke("SwithEnginesEnabled", new object[] {
                        serviceId,
                        enabled});
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginSwithEnginesEnabled(int serviceId, bool enabled, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("SwithEnginesEnabled", new object[] {
                        serviceId,
                        enabled}, callback, asyncState);
        }
        
        /// <remarks/>
        public void EndSwithEnginesEnabled(System.IAsyncResult asyncResult) {
            this.EndInvoke(asyncResult);
        }
        
        /// <remarks/>
        public void SwithEnginesEnabledAsync(int serviceId, bool enabled) {
            this.SwithEnginesEnabledAsync(serviceId, enabled, null);
        }
        
        /// <remarks/>
        public void SwithEnginesEnabledAsync(int serviceId, bool enabled, object userState) {
            if ((this.SwithEnginesEnabledOperationCompleted == null)) {
                this.SwithEnginesEnabledOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSwithEnginesEnabledOperationCompleted);
            }
            this.InvokeAsync("SwithEnginesEnabled", new object[] {
                        serviceId,
                        enabled}, this.SwithEnginesEnabledOperationCompleted, userState);
        }
        
        private void OnSwithEnginesEnabledOperationCompleted(object arg) {
            if ((this.SwithEnginesEnabledCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.SwithEnginesEnabledCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://smbsaas/websitepanel/enterpriseserver/GetAllowedHeliconZooQuotasForPackage" +
            "", RequestNamespace="http://smbsaas/websitepanel/enterpriseserver", ResponseNamespace="http://smbsaas/websitepanel/enterpriseserver", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public ShortHeliconZooEngine[] GetAllowedHeliconZooQuotasForPackage(int packageId) {
            object[] results = this.Invoke("GetAllowedHeliconZooQuotasForPackage", new object[] {
                        packageId});
            return ((ShortHeliconZooEngine[])(results[0]));
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginGetAllowedHeliconZooQuotasForPackage(int packageId, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("GetAllowedHeliconZooQuotasForPackage", new object[] {
                        packageId}, callback, asyncState);
        }
        
        /// <remarks/>
        public ShortHeliconZooEngine[] EndGetAllowedHeliconZooQuotasForPackage(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((ShortHeliconZooEngine[])(results[0]));
        }
        
        /// <remarks/>
        public void GetAllowedHeliconZooQuotasForPackageAsync(int packageId) {
            this.GetAllowedHeliconZooQuotasForPackageAsync(packageId, null);
        }
        
        /// <remarks/>
        public void GetAllowedHeliconZooQuotasForPackageAsync(int packageId, object userState) {
            if ((this.GetAllowedHeliconZooQuotasForPackageOperationCompleted == null)) {
                this.GetAllowedHeliconZooQuotasForPackageOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetAllowedHeliconZooQuotasForPackageOperationCompleted);
            }
            this.InvokeAsync("GetAllowedHeliconZooQuotasForPackage", new object[] {
                        packageId}, this.GetAllowedHeliconZooQuotasForPackageOperationCompleted, userState);
        }
        
        private void OnGetAllowedHeliconZooQuotasForPackageOperationCompleted(object arg) {
            if ((this.GetAllowedHeliconZooQuotasForPackageCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetAllowedHeliconZooQuotasForPackageCompleted(this, new GetAllowedHeliconZooQuotasForPackageCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://smbsaas/websitepanel/enterpriseserver/GetEnabledEnginesForSite", RequestNamespace="http://smbsaas/websitepanel/enterpriseserver", ResponseNamespace="http://smbsaas/websitepanel/enterpriseserver", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public string[] GetEnabledEnginesForSite(string siteId, int packageId) {
            object[] results = this.Invoke("GetEnabledEnginesForSite", new object[] {
                        siteId,
                        packageId});
            return ((string[])(results[0]));
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginGetEnabledEnginesForSite(string siteId, int packageId, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("GetEnabledEnginesForSite", new object[] {
                        siteId,
                        packageId}, callback, asyncState);
        }
        
        /// <remarks/>
        public string[] EndGetEnabledEnginesForSite(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((string[])(results[0]));
        }
        
        /// <remarks/>
        public void GetEnabledEnginesForSiteAsync(string siteId, int packageId) {
            this.GetEnabledEnginesForSiteAsync(siteId, packageId, null);
        }
        
        /// <remarks/>
        public void GetEnabledEnginesForSiteAsync(string siteId, int packageId, object userState) {
            if ((this.GetEnabledEnginesForSiteOperationCompleted == null)) {
                this.GetEnabledEnginesForSiteOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetEnabledEnginesForSiteOperationCompleted);
            }
            this.InvokeAsync("GetEnabledEnginesForSite", new object[] {
                        siteId,
                        packageId}, this.GetEnabledEnginesForSiteOperationCompleted, userState);
        }
        
        private void OnGetEnabledEnginesForSiteOperationCompleted(object arg) {
            if ((this.GetEnabledEnginesForSiteCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetEnabledEnginesForSiteCompleted(this, new GetEnabledEnginesForSiteCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://smbsaas/websitepanel/enterpriseserver/SetEnabledEnginesForSite", RequestNamespace="http://smbsaas/websitepanel/enterpriseserver", ResponseNamespace="http://smbsaas/websitepanel/enterpriseserver", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void SetEnabledEnginesForSite(string siteId, int packageId, string[] engines) {
            this.Invoke("SetEnabledEnginesForSite", new object[] {
                        siteId,
                        packageId,
                        engines});
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginSetEnabledEnginesForSite(string siteId, int packageId, string[] engines, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("SetEnabledEnginesForSite", new object[] {
                        siteId,
                        packageId,
                        engines}, callback, asyncState);
        }
        
        /// <remarks/>
        public void EndSetEnabledEnginesForSite(System.IAsyncResult asyncResult) {
            this.EndInvoke(asyncResult);
        }
        
        /// <remarks/>
        public void SetEnabledEnginesForSiteAsync(string siteId, int packageId, string[] engines) {
            this.SetEnabledEnginesForSiteAsync(siteId, packageId, engines, null);
        }
        
        /// <remarks/>
        public void SetEnabledEnginesForSiteAsync(string siteId, int packageId, string[] engines, object userState) {
            if ((this.SetEnabledEnginesForSiteOperationCompleted == null)) {
                this.SetEnabledEnginesForSiteOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSetEnabledEnginesForSiteOperationCompleted);
            }
            this.InvokeAsync("SetEnabledEnginesForSite", new object[] {
                        siteId,
                        packageId,
                        engines}, this.SetEnabledEnginesForSiteOperationCompleted, userState);
        }
        
        private void OnSetEnabledEnginesForSiteOperationCompleted(object arg) {
            if ((this.SetEnabledEnginesForSiteCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.SetEnabledEnginesForSiteCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://smbsaas/websitepanel/enterpriseserver/IsWebCosoleEnabled", RequestNamespace="http://smbsaas/websitepanel/enterpriseserver", ResponseNamespace="http://smbsaas/websitepanel/enterpriseserver", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public bool IsWebCosoleEnabled(int serviceId) {
            object[] results = this.Invoke("IsWebCosoleEnabled", new object[] {
                        serviceId});
            return ((bool)(results[0]));
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginIsWebCosoleEnabled(int serviceId, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("IsWebCosoleEnabled", new object[] {
                        serviceId}, callback, asyncState);
        }
        
        /// <remarks/>
        public bool EndIsWebCosoleEnabled(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((bool)(results[0]));
        }
        
        /// <remarks/>
        public void IsWebCosoleEnabledAsync(int serviceId) {
            this.IsWebCosoleEnabledAsync(serviceId, null);
        }
        
        /// <remarks/>
        public void IsWebCosoleEnabledAsync(int serviceId, object userState) {
            if ((this.IsWebCosoleEnabledOperationCompleted == null)) {
                this.IsWebCosoleEnabledOperationCompleted = new System.Threading.SendOrPostCallback(this.OnIsWebCosoleEnabledOperationCompleted);
            }
            this.InvokeAsync("IsWebCosoleEnabled", new object[] {
                        serviceId}, this.IsWebCosoleEnabledOperationCompleted, userState);
        }
        
        private void OnIsWebCosoleEnabledOperationCompleted(object arg) {
            if ((this.IsWebCosoleEnabledCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.IsWebCosoleEnabledCompleted(this, new IsWebCosoleEnabledCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://smbsaas/websitepanel/enterpriseserver/SetWebCosoleEnabled", RequestNamespace="http://smbsaas/websitepanel/enterpriseserver", ResponseNamespace="http://smbsaas/websitepanel/enterpriseserver", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void SetWebCosoleEnabled(int serviceId, bool enabled) {
            this.Invoke("SetWebCosoleEnabled", new object[] {
                        serviceId,
                        enabled});
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginSetWebCosoleEnabled(int serviceId, bool enabled, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("SetWebCosoleEnabled", new object[] {
                        serviceId,
                        enabled}, callback, asyncState);
        }
        
        /// <remarks/>
        public void EndSetWebCosoleEnabled(System.IAsyncResult asyncResult) {
            this.EndInvoke(asyncResult);
        }
        
        /// <remarks/>
        public void SetWebCosoleEnabledAsync(int serviceId, bool enabled) {
            this.SetWebCosoleEnabledAsync(serviceId, enabled, null);
        }
        
        /// <remarks/>
        public void SetWebCosoleEnabledAsync(int serviceId, bool enabled, object userState) {
            if ((this.SetWebCosoleEnabledOperationCompleted == null)) {
                this.SetWebCosoleEnabledOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSetWebCosoleEnabledOperationCompleted);
            }
            this.InvokeAsync("SetWebCosoleEnabled", new object[] {
                        serviceId,
                        enabled}, this.SetWebCosoleEnabledOperationCompleted, userState);
        }
        
        private void OnSetWebCosoleEnabledOperationCompleted(object arg) {
            if ((this.SetWebCosoleEnabledCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.SetWebCosoleEnabledCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        public new void CancelAsync(object userState) {
            base.CancelAsync(userState);
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    public delegate void GetEnginesCompletedEventHandler(object sender, GetEnginesCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetEnginesCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetEnginesCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public HeliconZooEngine[] Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((HeliconZooEngine[])(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    public delegate void SetEnginesCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    public delegate void IsEnginesEnabledCompletedEventHandler(object sender, IsEnginesEnabledCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class IsEnginesEnabledCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal IsEnginesEnabledCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public bool Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((bool)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    public delegate void SwithEnginesEnabledCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    public delegate void GetAllowedHeliconZooQuotasForPackageCompletedEventHandler(object sender, GetAllowedHeliconZooQuotasForPackageCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetAllowedHeliconZooQuotasForPackageCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetAllowedHeliconZooQuotasForPackageCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public ShortHeliconZooEngine[] Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((ShortHeliconZooEngine[])(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    public delegate void GetEnabledEnginesForSiteCompletedEventHandler(object sender, GetEnabledEnginesForSiteCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetEnabledEnginesForSiteCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetEnabledEnginesForSiteCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string[] Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string[])(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    public delegate void SetEnabledEnginesForSiteCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    public delegate void IsWebCosoleEnabledCompletedEventHandler(object sender, IsWebCosoleEnabledCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class IsWebCosoleEnabledCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal IsWebCosoleEnabledCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public bool Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((bool)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.42")]
    public delegate void SetWebCosoleEnabledCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
}
