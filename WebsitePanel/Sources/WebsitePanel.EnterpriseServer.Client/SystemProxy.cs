//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.7905
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by wsdl, Version=2.0.50727.3038.
// 
namespace WebsitePanel.EnterpriseServer {
    using System.Xml.Serialization;
    using System.Web.Services;
    using System.ComponentModel;
    using System.Web.Services.Protocols;
    using System;
    using System.Diagnostics;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.3038")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name="esSystemSoap", Namespace="http://tempuri.org/")]
    public partial class esSystem : Microsoft.Web.Services3.WebServicesClientProtocol {
        
        private System.Threading.SendOrPostCallback GetSystemSettingsOperationCompleted;
        
        private System.Threading.SendOrPostCallback GetSystemSettingsActiveOperationCompleted;
        
        private System.Threading.SendOrPostCallback CheckIsTwilioEnabledOperationCompleted;
        
        private System.Threading.SendOrPostCallback SetSystemSettingsOperationCompleted;
        
        /// <remarks/>
        public esSystem() {
            this.Url = "http://localhost:9002/esSystem.asmx";
        }
        
        /// <remarks/>
        public event GetSystemSettingsCompletedEventHandler GetSystemSettingsCompleted;
        
        /// <remarks/>
        public event GetSystemSettingsActiveCompletedEventHandler GetSystemSettingsActiveCompleted;
        
        /// <remarks/>
        public event CheckIsTwilioEnabledCompletedEventHandler CheckIsTwilioEnabledCompleted;
        
        /// <remarks/>
        public event SetSystemSettingsCompletedEventHandler SetSystemSettingsCompleted;
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/GetSystemSettings", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public SystemSettings GetSystemSettings(string settingsName) {
            object[] results = this.Invoke("GetSystemSettings", new object[] {
                        settingsName});
            return ((SystemSettings)(results[0]));
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginGetSystemSettings(string settingsName, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("GetSystemSettings", new object[] {
                        settingsName}, callback, asyncState);
        }
        
        /// <remarks/>
        public SystemSettings EndGetSystemSettings(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((SystemSettings)(results[0]));
        }
        
        /// <remarks/>
        public void GetSystemSettingsAsync(string settingsName) {
            this.GetSystemSettingsAsync(settingsName, null);
        }
        
        /// <remarks/>
        public void GetSystemSettingsAsync(string settingsName, object userState) {
            if ((this.GetSystemSettingsOperationCompleted == null)) {
                this.GetSystemSettingsOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetSystemSettingsOperationCompleted);
            }
            this.InvokeAsync("GetSystemSettings", new object[] {
                        settingsName}, this.GetSystemSettingsOperationCompleted, userState);
        }
        
        private void OnGetSystemSettingsOperationCompleted(object arg) {
            if ((this.GetSystemSettingsCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetSystemSettingsCompleted(this, new GetSystemSettingsCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/GetSystemSettingsActive", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public SystemSettings GetSystemSettingsActive(string settingsName, bool decrypt) {
            object[] results = this.Invoke("GetSystemSettingsActive", new object[] {
                        settingsName,
                        decrypt});
            return ((SystemSettings)(results[0]));
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginGetSystemSettingsActive(string settingsName, bool decrypt, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("GetSystemSettingsActive", new object[] {
                        settingsName,
                        decrypt}, callback, asyncState);
        }
        
        /// <remarks/>
        public SystemSettings EndGetSystemSettingsActive(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((SystemSettings)(results[0]));
        }
        
        /// <remarks/>
        public void GetSystemSettingsActiveAsync(string settingsName, bool decrypt) {
            this.GetSystemSettingsActiveAsync(settingsName, decrypt, null);
        }
        
        /// <remarks/>
        public void GetSystemSettingsActiveAsync(string settingsName, bool decrypt, object userState) {
            if ((this.GetSystemSettingsActiveOperationCompleted == null)) {
                this.GetSystemSettingsActiveOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetSystemSettingsActiveOperationCompleted);
            }
            this.InvokeAsync("GetSystemSettingsActive", new object[] {
                        settingsName,
                        decrypt}, this.GetSystemSettingsActiveOperationCompleted, userState);
        }
        
        private void OnGetSystemSettingsActiveOperationCompleted(object arg) {
            if ((this.GetSystemSettingsActiveCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetSystemSettingsActiveCompleted(this, new GetSystemSettingsActiveCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/CheckIsTwilioEnabled", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public bool CheckIsTwilioEnabled() {
            object[] results = this.Invoke("CheckIsTwilioEnabled", new object[0]);
            return ((bool)(results[0]));
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginCheckIsTwilioEnabled(System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("CheckIsTwilioEnabled", new object[0], callback, asyncState);
        }
        
        /// <remarks/>
        public bool EndCheckIsTwilioEnabled(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((bool)(results[0]));
        }
        
        /// <remarks/>
        public void CheckIsTwilioEnabledAsync() {
            this.CheckIsTwilioEnabledAsync(null);
        }
        
        /// <remarks/>
        public void CheckIsTwilioEnabledAsync(object userState) {
            if ((this.CheckIsTwilioEnabledOperationCompleted == null)) {
                this.CheckIsTwilioEnabledOperationCompleted = new System.Threading.SendOrPostCallback(this.OnCheckIsTwilioEnabledOperationCompleted);
            }
            this.InvokeAsync("CheckIsTwilioEnabled", new object[0], this.CheckIsTwilioEnabledOperationCompleted, userState);
        }
        
        private void OnCheckIsTwilioEnabledOperationCompleted(object arg) {
            if ((this.CheckIsTwilioEnabledCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.CheckIsTwilioEnabledCompleted(this, new CheckIsTwilioEnabledCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/SetSystemSettings", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public int SetSystemSettings(string settingsName, SystemSettings settings) {
            object[] results = this.Invoke("SetSystemSettings", new object[] {
                        settingsName,
                        settings});
            return ((int)(results[0]));
        }
        
        /// <remarks/>
        public System.IAsyncResult BeginSetSystemSettings(string settingsName, SystemSettings settings, System.AsyncCallback callback, object asyncState) {
            return this.BeginInvoke("SetSystemSettings", new object[] {
                        settingsName,
                        settings}, callback, asyncState);
        }
        
        /// <remarks/>
        public int EndSetSystemSettings(System.IAsyncResult asyncResult) {
            object[] results = this.EndInvoke(asyncResult);
            return ((int)(results[0]));
        }
        
        /// <remarks/>
        public void SetSystemSettingsAsync(string settingsName, SystemSettings settings) {
            this.SetSystemSettingsAsync(settingsName, settings, null);
        }
        
        /// <remarks/>
        public void SetSystemSettingsAsync(string settingsName, SystemSettings settings, object userState) {
            if ((this.SetSystemSettingsOperationCompleted == null)) {
                this.SetSystemSettingsOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSetSystemSettingsOperationCompleted);
            }
            this.InvokeAsync("SetSystemSettings", new object[] {
                        settingsName,
                        settings}, this.SetSystemSettingsOperationCompleted, userState);
        }
        
        private void OnSetSystemSettingsOperationCompleted(object arg) {
            if ((this.SetSystemSettingsCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.SetSystemSettingsCompleted(this, new SetSystemSettingsCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        public new void CancelAsync(object userState) {
            base.CancelAsync(userState);
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.3038")]
    public delegate void GetSystemSettingsCompletedEventHandler(object sender, GetSystemSettingsCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.3038")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetSystemSettingsCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetSystemSettingsCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public SystemSettings Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((SystemSettings)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.3038")]
    public delegate void GetSystemSettingsActiveCompletedEventHandler(object sender, GetSystemSettingsActiveCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.3038")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetSystemSettingsActiveCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetSystemSettingsActiveCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public SystemSettings Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((SystemSettings)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.3038")]
    public delegate void CheckIsTwilioEnabledCompletedEventHandler(object sender, CheckIsTwilioEnabledCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.3038")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class CheckIsTwilioEnabledCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal CheckIsTwilioEnabledCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
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
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.3038")]
    public delegate void SetSystemSettingsCompletedEventHandler(object sender, SetSystemSettingsCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("wsdl", "2.0.50727.3038")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class SetSystemSettingsCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal SetSystemSettingsCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public int Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((int)(this.results[0]));
            }
        }
    }
}
