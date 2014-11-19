﻿// Copyright (c) 2014, Outercurve Foundation.
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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Text;
using Microsoft.Win32;
using WebsitePanel.Providers.HostedSolution;
using WebsitePanel.Server.Utils;
using WebsitePanel.Providers.Utils;
using WebsitePanel.Providers.OS;
using WebsitePanel.Providers.Common;

using System.Management;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;


namespace WebsitePanel.Providers.RemoteDesktopServices
{
    public class Windows2012 : HostingServiceProviderBase, IRemoteDesktopServices
    {

        #region Constants

        private const string CapPath = @"RDS:\GatewayServer\CAP";
        private const string RapPath = @"RDS:\GatewayServer\RAP";
        private const string Computers = "Computers";
        private const string AdDcComputers = "Domain Controllers";
        private const string Users = "users";
        private const string RdsGroupFormat = "rds-{0}-{1}";
        private const string RdsModuleName = "RemoteDesktopServices";
        private const string AddNpsString = "netsh nps add np name=\"\"{0}\"\" policysource=\"1\" processingorder=\"{1}\" conditionid=\"0x3d\" conditiondata=\"^5$\" conditionid=\"0x1fb5\" conditiondata=\"{2}\" conditionid=\"0x1fb4\" conditiondata=\"{3}\" conditionid=\"0x1e\" conditiondata=\"UserAuthType:(PW|CA)\" profileid=\"0x1005\" profiledata=\"TRUE\" profileid=\"0x100f\" profiledata=\"TRUE\" profileid=\"0x1009\" profiledata=\"0x7\" profileid=\"0x1fe6\" profiledata=\"0x40000000\"";
        #endregion

        #region Properties

        internal string PrimaryDomainController
        {
            get
            {
                return ProviderSettings["PrimaryDomainController"];
            }
        }

        private string RootOU
        {
            get
            {
                return ProviderSettings["RootOU"];
            }
        }

        private string CentralNpsHost
        {
            get
            {
                return ProviderSettings["CentralNPS"];
            }
        }

        private IEnumerable<string> Gateways
        {
            get
            {
                return ProviderSettings["GWServrsList"].Split(';').Select(x => string.IsNullOrEmpty(x) ? x : x.Trim());
            }
        }

        private bool CentralNps
        {
            get
            {
                return Convert.ToBoolean(ProviderSettings["UseCentralNPS"]);
            }
        }

        private string RootDomain
        {
            get
            {
                return ServerSettings.ADRootDomain;
            }
        }

        private string ConnectionBroker
        {
            get
            {
                return ProviderSettings["ConnectionBroker"];
            }
        }

        #endregion

        #region HostingServiceProvider methods

        public override bool IsInstalled()
        {
            // TODO: Remove it.
            //return true;
            Server.Utils.OS.WindowsVersion version = WebsitePanel.Server.Utils.OS.GetVersion();
            return version == WebsitePanel.Server.Utils.OS.WindowsVersion.WindowsServer2012;
        }

        #endregion

        #region RDS Collections

        public bool CreateCollection(string organizationId, RdsCollection collection)
        {
            var result = true;

            Runspace runSpace = null;

            try
            {
                runSpace = OpenRunspace();

                foreach (var server in collection.Servers)
                {
                    //If server will restart it will not be added to collection
                    //Do not install feature here

                    if (!ExistRdsServerInDeployment(runSpace, server))
                    {
                        AddRdsServerToDeployment(runSpace, server);
                    }
                }

                Command cmd = new Command("New-RDSessionCollection");
                cmd.Parameters.Add("CollectionName", collection.Name);
                cmd.Parameters.Add("SessionHost", collection.Servers.Select(x => x.FqdName).ToArray());
                cmd.Parameters.Add("ConnectionBroker", ConnectionBroker);

                if (!string.IsNullOrEmpty(collection.Description))
                {
                    cmd.Parameters.Add("CollectionDescription", collection.Description);
                }

                var collectionPs = ExecuteShellCommand(runSpace, cmd, false).FirstOrDefault();

                if (collectionPs == null)
                {
                    throw new Exception("Collection not created");
                }

                var orgPath = GetOrganizationPath(organizationId);

                if (!ActiveDirectoryUtils.AdObjectExists(GetComputerGroupPath(organizationId, collection.Name)))
                {
                    //Create computer group
                    ActiveDirectoryUtils.CreateGroup(orgPath, GetComputersGroupName(collection.Name));

                    //todo Connection broker server must be added by default ???
                    //ActiveDirectoryUtils.AddObjectToGroup(GetComputerPath(ConnectionBroker), GetComputerGroupPath(organizationId, collection.Name));
                }

                if (!ActiveDirectoryUtils.AdObjectExists(GetUsersGroupPath(organizationId, collection.Name)))
                {
                    //Create user group
                    ActiveDirectoryUtils.CreateGroup(orgPath, GetUsersGroupName(collection.Name));
                }

                var policyName = GetPolicyName(organizationId, collection.Name);

                foreach (var gateway in Gateways)
                {
                    if (!CentralNps)
                    {
                        CreateRdCapForce(runSpace, gateway, policyName, collection.Name, new List<string> { GetUsersGroupName(collection.Name) });
                    }

                    CreateRdRapForce(runSpace, gateway, policyName, collection.Name, new List<string> { GetUsersGroupName(collection.Name) });
                }

                if (CentralNps)
                {
                    CreateCentralNpsPolicy(runSpace, CentralNpsHost, policyName, collection.Name, organizationId);
                }

                //add user group to collection
                AddUserGroupsToCollection(runSpace, collection.Name, new List<string> { GetUsersGroupName(collection.Name) });

                //add session servers to group
                foreach (var rdsServer in collection.Servers)
                {
                    AddComputerToCollectionAdComputerGroup(organizationId, collection.Name, rdsServer);
                }
            }
            catch (Exception e)
            {
                result = false;
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            return result;
        }

        public RdsCollection GetCollection(string collectionName)
        {
            RdsCollection collection =null;

            Runspace runSpace = null;

            try
            {
                runSpace = OpenRunspace();

                Command cmd = new Command("Get-RDSessionCollection");
                cmd.Parameters.Add("CollectionName", collectionName);
                cmd.Parameters.Add("ConnectionBroker", ConnectionBroker);

                var collectionPs = ExecuteShellCommand(runSpace, cmd, false).FirstOrDefault();

                if (collectionPs != null)
                {
                    collection = new RdsCollection();
                    collection.Name = Convert.ToString(GetPSObjectProperty(collectionPs, "CollectionName"));
                    collection.Description = Convert.ToString(GetPSObjectProperty(collectionPs, "CollectionDescription"));
                }
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            return collection;
        }

        public bool RemoveCollection(string organizationId, string collectionName)
        {
            var result = true;

            Runspace runSpace = null;

            try
            {
                runSpace = OpenRunspace();

                Command cmd = new Command("Remove-RDSessionCollection");
                cmd.Parameters.Add("CollectionName", collectionName);
                cmd.Parameters.Add("ConnectionBroker", ConnectionBroker);
                cmd.Parameters.Add("Force", true);

                ExecuteShellCommand(runSpace, cmd, false);

                var policyName = GetPolicyName(organizationId, collectionName);

                foreach (var gateway in Gateways)
                {
                    if (!CentralNps)
                    {
                        RemoveRdCap(runSpace, gateway, policyName);
                    }

                    RemoveRdRap(runSpace, gateway, policyName);
                }

                if (CentralNps)
                {
                    RemoveNpsPolicy(runSpace, CentralNpsHost, policyName);
                }

                //Remove security group

                ActiveDirectoryUtils.DeleteADObject(GetComputerGroupPath(organizationId, collectionName));

                ActiveDirectoryUtils.DeleteADObject(GetUsersGroupPath(organizationId, collectionName));
            }
            catch (Exception e)
            {
                result = false;
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            return result;
        }

        public List<string> GetCollectionUsers(string collectionName)
        {
            return GetUsersToCollectionAdGroup(collectionName);
        }

        public bool SetUsersInCollection(string organizationId, string collectionName, List<string> users)
        {
            var result = true;

            try
            {
                SetUsersToCollectionAdGroup(collectionName, organizationId, users);
            }
            catch (Exception e)
            {
                result = false;
            }

            return result;
        }

        public void AddSessionHostServerToCollection(string organizationId, string collectionName, RdsServer server)
        {
            Runspace runSpace = null;

            try
            {
                runSpace = OpenRunspace();

                if (!ExistRdsServerInDeployment(runSpace, server))
                {
                    AddRdsServerToDeployment(runSpace, server);
                }

                Command cmd = new Command("Add-RDSessionHost");
                cmd.Parameters.Add("CollectionName", collectionName);
                cmd.Parameters.Add("SessionHost", server.FqdName);
                cmd.Parameters.Add("ConnectionBroker", ConnectionBroker);

                ExecuteShellCommand(runSpace, cmd, false);

                AddComputerToCollectionAdComputerGroup(organizationId, collectionName, server);
            }
            catch (Exception e)
            {

            }
            finally
            {
                CloseRunspace(runSpace);
            }
        }

       public void AddSessionHostServersToCollection(string organizationId, string collectionName, List<RdsServer> servers)
        {
            foreach (var server in servers)
            {
                AddSessionHostServerToCollection(organizationId, collectionName, server);
            }
        }

        public void RemoveSessionHostServerFromCollection(string organizationId, string collectionName, RdsServer server)
        {
            Runspace runSpace = null;

            try
            {
                runSpace = OpenRunspace();

                Command cmd = new Command("Remove-RDSessionHost");
                cmd.Parameters.Add("ConnectionBroker", ConnectionBroker);
                cmd.Parameters.Add("SessionHost", server.FqdName);
                cmd.Parameters.Add("Force", true);

                ExecuteShellCommand(runSpace, cmd, false);

                RemoveComputerFromCollectionAdComputerGroup(organizationId, collectionName, server);
            }
            finally
            {
                CloseRunspace(runSpace);
            }
        }

        public void RemoveSessionHostServersFromCollection(string organizationId, string collectionName, List<RdsServer> servers)
        {
            foreach (var server in servers)
            {
                RemoveSessionHostServerFromCollection(organizationId, collectionName, server);
            }
        }

        #endregion

        #region Remote Applications

        public List<StartMenuApp> GetAvailableRemoteApplications(string collectionName)
        {
            var startApps = new List<StartMenuApp>();

            Runspace runSpace = null;

            try
            {
                runSpace = OpenRunspace();

                Command cmd = new Command("Get-RDAvailableApp");
                cmd.Parameters.Add("CollectionName", collectionName);
                cmd.Parameters.Add("ConnectionBroker", ConnectionBroker);

                var remoteApplicationsPS = ExecuteShellCommand(runSpace, cmd, false);

                if (remoteApplicationsPS != null)
                {
                    startApps.AddRange(remoteApplicationsPS.Select(CreateStartMenuAppFromPsObject));
                }
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            return startApps;
        }

        public List<RemoteApplication> GetCollectionRemoteApplications(string collectionName)
        {
            var remoteApps = new List<RemoteApplication>();

            Runspace runSpace = null;

            try
            {
                runSpace = OpenRunspace();

                Command cmd = new Command("Get-RDRemoteApp");
                cmd.Parameters.Add("CollectionName", collectionName);
                cmd.Parameters.Add("ConnectionBroker", ConnectionBroker);

                var remoteAppsPs = ExecuteShellCommand(runSpace, cmd, false);

                if (remoteAppsPs != null)
                {
                    remoteApps.AddRange(remoteAppsPs.Select(CreateRemoteApplicationFromPsObject));
                }
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            return remoteApps;
        }

        public bool AddRemoteApplication(string collectionName, RemoteApplication remoteApp)
        {
            var result = false;

            Runspace runSpace = null;

            try
            {
                runSpace = OpenRunspace();

                Command cmd = new Command("New-RDRemoteApp");
                cmd.Parameters.Add("CollectionName", collectionName);
                cmd.Parameters.Add("ConnectionBroker", ConnectionBroker);
                cmd.Parameters.Add("Alias", remoteApp.Alias);
                cmd.Parameters.Add("DisplayName", remoteApp.DisplayName);
                cmd.Parameters.Add("FilePath", remoteApp.FilePath);
                cmd.Parameters.Add("ShowInWebAccess", remoteApp.ShowInWebAccess);

                ExecuteShellCommand(runSpace, cmd, false);

                result = true;
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            return result;
        }

        public bool AddRemoteApplications(string collectionName, List<RemoteApplication> remoteApps)
        {
            var result = true;

            foreach (var remoteApp in remoteApps)
            {
                result = AddRemoteApplication(collectionName, remoteApp) && result;
            }

            return result;
        }

        public bool RemoveRemoteApplication(string collectionName, RemoteApplication remoteApp)
        {
            var result = false;

            Runspace runSpace = null;

            try
            {
                runSpace = OpenRunspace();

                Command cmd = new Command("Remove-RDRemoteApp");
                cmd.Parameters.Add("CollectionName", collectionName);
                cmd.Parameters.Add("ConnectionBroker", ConnectionBroker);
                cmd.Parameters.Add("Alias", remoteApp.Alias);
                cmd.Parameters.Add("Force", true);

                ExecuteShellCommand(runSpace, cmd, false);
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            return result;
        }
        
        #endregion

        #region Gateaway (RD CAP | RD RAP)

        internal void CreateCentralNpsPolicy(Runspace runSpace, string centralNpshost, string policyName, string collectionName, string organizationId)
        {
            var showCmd = new Command("netsh nps show np");

            var showResult = ExecuteRemoteShellCommand(runSpace, centralNpshost, showCmd);

            var count = showResult.Count(x => Convert.ToString(x).Contains("policy conf")) + 1001;

            var userGroupAd = ActiveDirectoryUtils.GetADObject(GetUsersGroupPath(organizationId, collectionName));

            var userGroupSid = (byte[])ActiveDirectoryUtils.GetADObjectProperty(userGroupAd, "objectSid");

            var computerGroupAd = ActiveDirectoryUtils.GetADObject(GetComputerGroupPath(organizationId, collectionName));

            var computerGroupSid = (byte[])ActiveDirectoryUtils.GetADObjectProperty(computerGroupAd, "objectSid");

            var addCmdString = string.Format(AddNpsString, policyName.Replace(" ", "_"), count, ConvertByteToStringSid(userGroupSid), ConvertByteToStringSid(computerGroupSid));

            Command addCmd = new Command(addCmdString);

            var result = ExecuteRemoteShellCommand(runSpace, centralNpshost, addCmd);
        }

        internal void RemoveNpsPolicy(Runspace runSpace, string centralNpshost, string policyName)
        {
            var removeCmd = new Command(string.Format("netsh nps delete np {0}", policyName.Replace(" ", "_")));

            var removeResult = ExecuteRemoteShellCommand(runSpace, centralNpshost, removeCmd);
        }

        internal void CreateRdCapForce(Runspace runSpace, string gatewayHost, string policyName, string collectionName, List<string> groups)
        {
            //New-Item -Path "RDS:\GatewayServer\CAP" -Name "Allow Admins" -UserGroups "Administrators@." -AuthMethod 1
            //Set-Item -Path "RDS:\GatewayServer\CAP\Allow Admins\SessionTimeout" -Value 480 -SessionTimeoutAction 0

            if (ItemExistsRemote(runSpace, gatewayHost, Path.Combine(CapPath, policyName)))
            {
                RemoveRdCap(runSpace, gatewayHost, policyName);
            }

            var userGroupParametr = string.Format("@({0})",string.Join(",", groups.Select(x => string.Format("\"{0}@{1}\"", x, RootDomain)).ToArray()));
            var computerGroupParameter = string.Format("\"{0}@{1}\"", GetComputersGroupName(collectionName), RootDomain);

            Command rdCapCommand = new Command("New-Item");
            rdCapCommand.Parameters.Add("Path", string.Format("\"{0}\"", CapPath));
            rdCapCommand.Parameters.Add("Name", string.Format("\"{0}\"", policyName));
            rdCapCommand.Parameters.Add("UserGroups", userGroupParametr);
            rdCapCommand.Parameters.Add("ComputerGroups", computerGroupParameter);
            rdCapCommand.Parameters.Add("AuthMethod", 1);

            ExecuteRemoteShellCommand(runSpace, gatewayHost, rdCapCommand, RdsModuleName);
        }

        internal void RemoveRdCap(Runspace runSpace, string gatewayHost, string name)
        {
            RemoveItemRemote(runSpace, gatewayHost, string.Format(@"{0}\{1}", CapPath, name), RdsModuleName);
        }

        internal void CreateRdRapForce(Runspace runSpace, string gatewayHost, string policyName, string collectionName, List<string> groups)
        {
            //New-Item -Path "RDS:\GatewayServer\RAP" -Name "Allow Connections To Everywhere" -UserGroups "Administrators@." -ComputerGroupType 1
            //Set-Item -Path "RDS:\GatewayServer\RAP\Allow Connections To Everywhere\PortNumbers" -Value 3389,3390

            if (ItemExistsRemote(runSpace, gatewayHost, Path.Combine(RapPath, policyName)))
            {
                RemoveRdRap(runSpace, gatewayHost, policyName);
            }

            var userGroupParametr = string.Format("@({0})", string.Join(",", groups.Select(x => string.Format("\"{0}@{1}\"", x, RootDomain)).ToArray()));
            var computerGroupParametr = string.Format("\"{0}@{1}\"", GetComputersGroupName(collectionName), RootDomain);

            Command rdRapCommand = new Command("New-Item");
            rdRapCommand.Parameters.Add("Path", string.Format("\"{0}\"", RapPath));
            rdRapCommand.Parameters.Add("Name", string.Format("\"{0}\"", policyName));
            rdRapCommand.Parameters.Add("UserGroups", userGroupParametr);
            rdRapCommand.Parameters.Add("ComputerGroupType", 1);
            rdRapCommand.Parameters.Add("ComputerGroup", computerGroupParametr);

            ExecuteRemoteShellCommand(runSpace, gatewayHost, rdRapCommand, RdsModuleName);
        }

        internal void RemoveRdRap(Runspace runSpace, string gatewayHost, string name)
        {
            RemoveItemRemote(runSpace, gatewayHost, string.Format(@"{0}\{1}", RapPath, name), RdsModuleName);
        }

        #endregion

        private void AddRdsServerToDeployment(Runspace runSpace, RdsServer server)
        {
            Command cmd = new Command("Add-RDserver");
            cmd.Parameters.Add("Server", server.FqdName);
            cmd.Parameters.Add("Role", "RDS-RD-SERVER");
            cmd.Parameters.Add("ConnectionBroker", ConnectionBroker);

            ExecuteShellCommand(runSpace, cmd, false);
        }


    
        private bool ExistRdsServerInDeployment(Runspace runSpace, RdsServer server)
        {
            Command cmd = new Command("Get-RDserver");
            cmd.Parameters.Add("Role", "RDS-RD-SERVER");
            cmd.Parameters.Add("ConnectionBroker", ConnectionBroker);

            var serversPs = ExecuteShellCommand(runSpace, cmd, false);

            if (serversPs != null)
            {
                foreach (var serverPs in serversPs)
                {
                    var serverName = Convert.ToString( GetPSObjectProperty(serverPs, "Server"));

                    if (string.Compare(serverName, server.FqdName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void SetUsersToCollectionAdGroup(string collectionName, string organizationId, IEnumerable<string> users)
        {
            var usersGroupName = GetUsersGroupName(collectionName);
            var usersGroupPath = GetUsersGroupPath(organizationId, collectionName);

            //remove all users from group
            foreach (string userPath in ActiveDirectoryUtils.GetGroupObjects(usersGroupName, "user"))
            {
                ActiveDirectoryUtils.RemoveObjectFromGroup(userPath, usersGroupPath);
            }

            //adding users to group
            foreach (var user in users)
            {
                var userPath = GetUserPath(organizationId, user);

                if (ActiveDirectoryUtils.AdObjectExists(userPath))
                {
                    var userObject = ActiveDirectoryUtils.GetADObject(userPath);
                    var samName = (string)ActiveDirectoryUtils.GetADObjectProperty(userObject, "sAMAccountName");

                    if (!ActiveDirectoryUtils.IsUserInGroup(samName, usersGroupName))
                    {
                        ActiveDirectoryUtils.AddObjectToGroup(userPath, GetUsersGroupPath(organizationId, collectionName));
                    }
                }
            }
        }

        private List<string> GetUsersToCollectionAdGroup(string collectionName)
        {
            var users = new List<string>();

            var usersGroupName = GetUsersGroupName(collectionName);

            foreach (string userPath in ActiveDirectoryUtils.GetGroupObjects(usersGroupName, "user"))
            {
                var userObject = ActiveDirectoryUtils.GetADObject(userPath);
                var samName = (string)ActiveDirectoryUtils.GetADObjectProperty(userObject, "sAMAccountName");

                users.Add(samName);
            }

            return users;
        }

        private void AddUserGroupsToCollection(Runspace runSpace, string collectionName, List<string> groups)
        {
            Command cmd = new Command("Set-RDSessionCollectionConfiguration");
            cmd.Parameters.Add("CollectionName", collectionName);
            cmd.Parameters.Add("UserGroup", groups);
            cmd.Parameters.Add("ConnectionBroker", ConnectionBroker);

            ExecuteShellCommand(runSpace, cmd, false).FirstOrDefault();
        }

        private void AddComputerToCollectionAdComputerGroup(string organizationId, string collectionName, RdsServer server)
        {
            var computerPath = GetComputerPath(server.Name, false);
            var computerGroupName = GetComputersGroupName( collectionName);

            if (!ActiveDirectoryUtils.AdObjectExists(computerPath))
            {
                computerPath = GetComputerPath(server.Name, true);
            }

            if (ActiveDirectoryUtils.AdObjectExists(computerPath))
            {
                var computerObject = ActiveDirectoryUtils.GetADObject(computerPath);
                var samName = (string)ActiveDirectoryUtils.GetADObjectProperty(computerObject, "sAMAccountName");

                if (!ActiveDirectoryUtils.IsComputerInGroup(samName, computerGroupName))
                {
                    ActiveDirectoryUtils.AddObjectToGroup(computerPath, GetComputerGroupPath(organizationId, collectionName));
                }
            }
        }

        private void RemoveComputerFromCollectionAdComputerGroup(string organizationId, string collectionName, RdsServer server)
        {
            var computerPath = GetComputerPath(server.Name, false);
            var computerGroupName = GetComputersGroupName(collectionName);

            if (!ActiveDirectoryUtils.AdObjectExists(computerPath))
            {
                computerPath = GetComputerPath(server.Name, true);
            }

            if (ActiveDirectoryUtils.AdObjectExists(computerPath))
            {
                var computerObject = ActiveDirectoryUtils.GetADObject(computerPath);
                var samName = (string)ActiveDirectoryUtils.GetADObjectProperty(computerObject, "sAMAccountName");

                if (ActiveDirectoryUtils.IsComputerInGroup(samName, computerGroupName))
                {
                    ActiveDirectoryUtils.RemoveObjectFromGroup(computerPath, GetComputerGroupPath(organizationId, collectionName));
                }
            }
        }

        public bool AddSessionHostFeatureToServer(string hostName)
        {
            bool installationResult = false;

            Runspace runSpace = null;

            try
            {
                runSpace = OpenRunspace();

                var feature = AddFeature(runSpace, hostName, "RDS-RD-Server", true, true);

                installationResult = (bool) GetPSObjectProperty(feature, "Success");
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            return installationResult;
        }

        public bool CheckSessionHostFeatureInstallation(string hostName)
        {
            bool isInstalled = false;

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Command cmd = new Command("Get-WindowsFeature");
                cmd.Parameters.Add("Name", "RDS-RD-Server");

                var feature = ExecuteRemoteShellCommand(runSpace, hostName, cmd).FirstOrDefault();

                if (feature != null)
                {
                    isInstalled = (bool) GetPSObjectProperty(feature, "Installed");
                }
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            return isInstalled;
        }

        public bool CheckServerAvailability(string hostName)
        {
            var ping = new Ping();

            var ipAddress = GetServerIp(hostName);

            var reply = ping.Send(ipAddress, 3000);

            return reply != null && reply.Status == IPStatus.Success;
        }
        
        #region Helpers

        private static string ConvertByteToStringSid(Byte[] sidBytes)
        {
            StringBuilder strSid = new StringBuilder();
            strSid.Append("S-");
            try
            {
                // Add SID revision.
                strSid.Append(sidBytes[0].ToString());
                // Next six bytes are SID authority value.
                if (sidBytes[6] != 0 || sidBytes[5] != 0)
                {
                    string strAuth = String.Format
                        ("0x{0:2x}{1:2x}{2:2x}{3:2x}{4:2x}{5:2x}",
                        (Int16)sidBytes[1],
                        (Int16)sidBytes[2],
                        (Int16)sidBytes[3],
                        (Int16)sidBytes[4],
                        (Int16)sidBytes[5],
                        (Int16)sidBytes[6]);
                    strSid.Append("-");
                    strSid.Append(strAuth);
                }
                else
                {
                    Int64 iVal = (Int32)(sidBytes[1]) +
                        (Int32)(sidBytes[2] << 8) +
                        (Int32)(sidBytes[3] << 16) +
                        (Int32)(sidBytes[4] << 24);
                    strSid.Append("-");
                    strSid.Append(iVal.ToString());
                }

                // Get sub authority count...
                int iSubCount = Convert.ToInt32(sidBytes[7]);
                int idxAuth = 0;
                for (int i = 0; i < iSubCount; i++)
                {
                    idxAuth = 8 + i * 4;
                    UInt32 iSubAuth = BitConverter.ToUInt32(sidBytes, idxAuth);
                    strSid.Append("-");
                    strSid.Append(iSubAuth.ToString());
                }
            }
            catch
            {
                return "";
            }
            return strSid.ToString();
        }

        private StartMenuApp CreateStartMenuAppFromPsObject(PSObject psObject)
        {
            var remoteApp = new StartMenuApp
            {
                DisplayName = Convert.ToString(GetPSObjectProperty(psObject, "DisplayName")),
                FilePath = Convert.ToString(GetPSObjectProperty(psObject, "FilePath")),
                FileVirtualPath = Convert.ToString(GetPSObjectProperty(psObject, "FileVirtualPath"))
            };

            return remoteApp;
        }

        private RemoteApplication CreateRemoteApplicationFromPsObject(PSObject psObject)
        {
            var remoteApp = new RemoteApplication
            {
                DisplayName = Convert.ToString(GetPSObjectProperty(psObject, "DisplayName")),
                FilePath = Convert.ToString(GetPSObjectProperty(psObject, "FilePath")),
                Alias = Convert.ToString(GetPSObjectProperty(psObject, "Alias")),
                ShowInWebAccess = Convert.ToBoolean(GetPSObjectProperty(psObject, "ShowInWebAccess"))
            };

            return remoteApp;
        }

        internal IPAddress GetServerIp(string hostname, AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            var address = GetServerIps(hostname);

            return address.FirstOrDefault(x => x.AddressFamily == addressFamily);
        }

        internal IEnumerable<IPAddress> GetServerIps(string hostname)
        {
            var address = Dns.GetHostAddresses(hostname);

            return address;
        }

        internal void RemoveItem(Runspace runSpace, string path)
        {
            Command rdRapCommand = new Command("Remove-Item");
            rdRapCommand.Parameters.Add("Path", path);
            rdRapCommand.Parameters.Add("Force", true);
            rdRapCommand.Parameters.Add("Recurse", true);

            ExecuteShellCommand(runSpace, rdRapCommand, false);
        }

        internal void RemoveItemRemote(Runspace runSpace, string hostname, string path, params string[] imports)
        {
            Command rdRapCommand = new Command("Remove-Item");
            rdRapCommand.Parameters.Add("Path", string.Format("\"{0}\"", path));
            rdRapCommand.Parameters.Add("Force", "");
            rdRapCommand.Parameters.Add("Recurse", "");

            ExecuteRemoteShellCommand(runSpace, hostname, rdRapCommand, imports);
        }

        private string GetPolicyName(string organizationId, string collectionName)
        {
            return string.Format("rds-{0}-{1}", organizationId, collectionName);
        }

        private string GetComputersGroupName(string collectionName)
        {
            return string.Format(RdsGroupFormat, collectionName, Computers.ToLowerInvariant());
        }

        private string GetUsersGroupName(string collectionName)
        {
            return string.Format(RdsGroupFormat, collectionName, Users.ToLowerInvariant());
        }

        internal string GetComputerGroupPath(string organizationId, string collection)
        {
            StringBuilder sb = new StringBuilder();
            // append provider
            AppendProtocol(sb);
            AppendDomainController(sb);
            AppendCNPath(sb, GetComputersGroupName(collection));
            AppendOUPath(sb, organizationId);
            AppendOUPath(sb, RootOU);
            AppendDomainPath(sb, RootDomain);

            return sb.ToString();
        }

        internal string GetUsersGroupPath(string organizationId, string collection)
        {
            StringBuilder sb = new StringBuilder();
            // append provider
            AppendProtocol(sb);
            AppendDomainController(sb);
            AppendCNPath(sb, GetUsersGroupName(collection));
            AppendOUPath(sb, organizationId);
            AppendOUPath(sb, RootOU);
            AppendDomainPath(sb, RootDomain);

            return sb.ToString();
        }

        private string GetUserPath(string organizationId, string loginName)
        {
            StringBuilder sb = new StringBuilder();
            // append provider
            AppendProtocol(sb);
            AppendDomainController(sb);
            AppendCNPath(sb, loginName);
            AppendOUPath(sb, organizationId);
            AppendOUPath(sb, RootOU);
            AppendDomainPath(sb, RootDomain);

            return sb.ToString();
        }

        private string GetOrganizationPath(string organizationId)
        {
            StringBuilder sb = new StringBuilder();
            // append provider
            AppendProtocol(sb);
            AppendDomainController(sb);
            AppendOUPath(sb, organizationId);
            AppendOUPath(sb, RootOU);
            AppendDomainPath(sb, RootDomain);

            return sb.ToString();
        }

        private string GetComputerPath(string objName, bool domainController)
        {
            StringBuilder sb = new StringBuilder();
            // append provider
            AppendProtocol(sb);
            AppendDomainController(sb);
            AppendCNPath(sb, objName);
            if (domainController)
            {
                AppendOUPath(sb, AdDcComputers);
            }
            else
            {
                AppendCNPath(sb, Computers);
                
            }
            AppendDomainPath(sb, RootDomain);

            return sb.ToString();
        }

        private static void AppendCNPath(StringBuilder sb, string organizationId)
        {
            if (string.IsNullOrEmpty(organizationId))
                return;

            sb.Append("CN=").Append(organizationId).Append(",");
        }

        private void AppendDomainController(StringBuilder sb)
        {
            sb.Append(PrimaryDomainController + "/");
        }

        private static void AppendProtocol(StringBuilder sb)
        {
            sb.Append("LDAP://");
        }

        private static void AppendOUPath(StringBuilder sb, string ou)
        {
            if (string.IsNullOrEmpty(ou))
                return;

            string path = ou.Replace("/", "\\");
            string[] parts = path.Split('\\');
            for (int i = parts.Length - 1; i != -1; i--)
                sb.Append("OU=").Append(parts[i]).Append(",");
        }

        private static void AppendDomainPath(StringBuilder sb, string domain)
        {
            if (string.IsNullOrEmpty(domain))
                return;

            string[] parts = domain.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                sb.Append("DC=").Append(parts[i]);

                if (i < (parts.Length - 1))
                    sb.Append(",");
            }
        }

        #endregion

        #region Windows Feature PowerShell

        internal bool IsFeatureInstalled(string hostName, string featureName)
        {
            bool isInstalled = false;

            Runspace runSpace = null;
            try
            {
                runSpace = OpenRunspace();

                Command cmd = new Command("Get-WindowsFeature");
                cmd.Parameters.Add("Name", featureName);

                var feature = ExecuteRemoteShellCommand(runSpace, hostName, cmd).FirstOrDefault();

                if (feature != null)
                {
                    isInstalled = (bool) GetPSObjectProperty(feature, "Installed");
                }
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            return isInstalled;
        }

        internal PSObject AddFeature(Runspace runSpace, string hostName, string featureName, bool includeAllSubFeature = true, bool restart = false)
        {
            PSObject feature;

            try
            {
                Command cmd = new Command("Add-WindowsFeature");
                cmd.Parameters.Add("Name", featureName);

                if (includeAllSubFeature)
                {
                    cmd.Parameters.Add("IncludeAllSubFeature", "");
                }

                if (restart)
                {
                    cmd.Parameters.Add("Restart", "");
                }

                feature = ExecuteRemoteShellCommand(runSpace, hostName, cmd).FirstOrDefault();
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            return feature;
        }

        #endregion

        #region PowerShell integration

        private static InitialSessionState session = null;

        internal virtual Runspace OpenRunspace()
        {
            Log.WriteStart("OpenRunspace");

            if (session == null)
            {
                session = InitialSessionState.CreateDefault();
                session.ImportPSModule(new string[] { "ServerManager", "RemoteDesktop", "RemoteDesktopServices" });
            }
            Runspace runSpace = RunspaceFactory.CreateRunspace(session);
            //
            runSpace.Open();
            //
            runSpace.SessionStateProxy.SetVariable("ConfirmPreference", "none");
            Log.WriteEnd("OpenRunspace");
            return runSpace;
        }

        internal void CloseRunspace(Runspace runspace)
        {
            try
            {
                if (runspace != null && runspace.RunspaceStateInfo.State == RunspaceState.Opened)
                {
                    runspace.Close();
                }
            }
            catch (Exception ex)
            {
                Log.WriteError("Runspace error", ex);
            }
        }

        internal Collection<PSObject> ExecuteRemoteShellCommand(Runspace runSpace, string hostName, Command cmd, params string[] moduleImports)
        {
            Command invokeCommand = new Command("Invoke-Command");
            invokeCommand.Parameters.Add("ComputerName", hostName);

            RunspaceInvoke invoke = new RunspaceInvoke();

            string commandString = moduleImports.Any() ? string.Format("import-module {0};", string.Join(",", moduleImports)) : string.Empty;

            commandString += cmd.CommandText;

            if (cmd.Parameters != null && cmd.Parameters.Any())
            {
                commandString += " " +
                                 string.Join(" ",
                                     cmd.Parameters.Select(x => string.Format("-{0} {1}", x.Name, x.Value)).ToArray());
            }

            ScriptBlock sb = invoke.Invoke(string.Format("{{{0}}}", commandString))[0].BaseObject as ScriptBlock;

            invokeCommand.Parameters.Add("ScriptBlock", sb);

            return ExecuteShellCommand(runSpace, invokeCommand, false);
        }

        internal Collection<PSObject> ExecuteShellCommand(Runspace runSpace, Command cmd)
        {
            return ExecuteShellCommand(runSpace, cmd, true);
        }

        internal Collection<PSObject> ExecuteShellCommand(Runspace runSpace, Command cmd, bool useDomainController)
        {
            object[] errors;
            return ExecuteShellCommand(runSpace, cmd, useDomainController, out errors);
        }

        internal Collection<PSObject> ExecuteShellCommand(Runspace runSpace, Command cmd, out object[] errors)
        {
            return ExecuteShellCommand(runSpace, cmd, true, out errors);
        }

        internal Collection<PSObject> ExecuteShellCommand(Runspace runSpace, Command cmd, bool useDomainController,
            out object[] errors)
        {
            Log.WriteStart("ExecuteShellCommand");
            List<object> errorList = new List<object>();

            if (useDomainController)
            {
                CommandParameter dc = new CommandParameter("DomainController", PrimaryDomainController);
                if (!cmd.Parameters.Contains(dc))
                {
                    cmd.Parameters.Add(dc);
                }
            }

            Collection<PSObject> results = null;
            // Create a pipeline
            Pipeline pipeLine = runSpace.CreatePipeline();
            using (pipeLine)
            {
                // Add the command
                pipeLine.Commands.Add(cmd);
                // Execute the pipeline and save the objects returned.
                results = pipeLine.Invoke();

                // Log out any errors in the pipeline execution
                // NOTE: These errors are NOT thrown as exceptions! 
                // Be sure to check this to ensure that no errors 
                // happened while executing the command.
                if (pipeLine.Error != null && pipeLine.Error.Count > 0)
                {
                    foreach (object item in pipeLine.Error.ReadToEnd())
                    {
                        errorList.Add(item);
                        string errorMessage = string.Format("Invoke error: {0}", item);
                        Log.WriteWarning(errorMessage);
                    }
                }
            }
            pipeLine = null;
            errors = errorList.ToArray();
            Log.WriteEnd("ExecuteShellCommand");
            return results;
        }

        internal object GetPSObjectProperty(PSObject obj, string name)
        {
            return obj.Members[name].Value;
        }

        /// <summary>
        /// Returns the identity of the object from the shell execution result
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        internal string GetResultObjectIdentity(Collection<PSObject> result)
        {
            Log.WriteStart("GetResultObjectIdentity");
            if (result == null)
                throw new ArgumentNullException("result", "Execution result is not specified");

            if (result.Count < 1)
                throw new ArgumentException("Execution result is empty", "result");

            if (result.Count > 1)
                throw new ArgumentException("Execution result contains more than one object", "result");

            PSMemberInfo info = result[0].Members["Identity"];
            if (info == null)
                throw new ArgumentException("Execution result does not contain Identity property", "result");

            string ret = info.Value.ToString();
            Log.WriteEnd("GetResultObjectIdentity");
            return ret;
        }

        internal string GetResultObjectDN(Collection<PSObject> result)
        {
            Log.WriteStart("GetResultObjectDN");
            if (result == null)
                throw new ArgumentNullException("result", "Execution result is not specified");

            if (result.Count < 1)
                throw new ArgumentException("Execution result does not contain any object");

            if (result.Count > 1)
                throw new ArgumentException("Execution result contains more than one object");

            PSMemberInfo info = result[0].Members["DistinguishedName"];
            if (info == null)
                throw new ArgumentException("Execution result does not contain DistinguishedName property", "result");

            string ret = info.Value.ToString();
            Log.WriteEnd("GetResultObjectDN");
            return ret;
        }

        internal bool ItemExists(Runspace runSpace, string path)
        {
            Command testPathCommand = new Command("Test-Path");
            testPathCommand.Parameters.Add("Path", path);

            var testPathResult = ExecuteShellCommand(runSpace, testPathCommand, false).First();

            var result = Convert.ToBoolean(testPathResult.ToString());

            return result;
        }

        internal bool ItemExistsRemote(Runspace runSpace, string hostname,string path)
        {
            Command testPathCommand = new Command("Test-Path");
            testPathCommand.Parameters.Add("Path", string.Format("\"{0}\"", path));

            var testPathResult = ExecuteRemoteShellCommand(runSpace, hostname, testPathCommand, RdsModuleName).First();

            var result = Convert.ToBoolean(testPathResult.ToString());

            return result;
        }

        #endregion
    }
}

