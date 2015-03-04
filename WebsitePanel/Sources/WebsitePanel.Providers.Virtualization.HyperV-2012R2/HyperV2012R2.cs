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

﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

using System.Management;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

using System.Reflection;
using System.Globalization;

using System.Xml;
using WebsitePanel.Providers;
using WebsitePanel.Providers.HostedSolution;
using WebsitePanel.Providers.Utils;
using WebsitePanel.Server.Utils;

using Vds = Microsoft.Storage.Vds;
using System.Configuration;
﻿using System.Linq;

namespace WebsitePanel.Providers.Virtualization
{
    public class HyperV2012R2 : HostingServiceProviderBase, IVirtualizationServer
    {
        #region Constants
        private const string CONFIG_USE_DISKPART_TO_CLEAR_READONLY_FLAG = "WebsitePanel.HyperV.UseDiskPartClearReadOnlyFlag";
        private const string WMI_VIRTUALIZATION_NAMESPACE = @"root\virtualization";
        private const string WMI_CIMV2_NAMESPACE = @"root\cimv2";

        private const int SWITCH_PORTS_NUMBER = 1024;
        private const string LIBRARY_INDEX_FILE_NAME = "index.xml";
        private const string EXTERNAL_NETWORK_ADAPTER_NAME = "External Network Adapter";
        private const string PRIVATE_NETWORK_ADAPTER_NAME = "Private Network Adapter";
        private const string MANAGEMENT_NETWORK_ADAPTER_NAME = "Management Network Adapter";

        private const string KVP_RAM_SUMMARY_KEY = "VM-RAM-Summary";
        private const string KVP_HDD_SUMMARY_KEY = "VM-HDD-Summary";
        private const Int64 Size1G = 0x40000000;
        private const Int64 Size1M = 0x100000;

        #endregion

        #region Provider Settings
        protected string ServerNameSettings
        {
            get { return ProviderSettings["ServerName"]; }
        }

        public int AutomaticStartActionSettings
        {
            get { return ProviderSettings.GetInt("StartAction"); }
        }

        public int AutomaticStartupDelaySettings
        {
            get { return ProviderSettings.GetInt("StartupDelay"); }
        }

        public int AutomaticStopActionSettings
        {
            get { return ProviderSettings.GetInt("StopAction"); }
        }

        public int AutomaticRecoveryActionSettings
        {
            get { return 1 /* restart */; }
        }

        public int CpuReserveSettings
        {
            get { return ProviderSettings.GetInt("CpuReserve"); }
        }

        public int CpuLimitSettings
        {
            get { return ProviderSettings.GetInt("CpuLimit"); }
        }

        public int CpuWeightSettings
        {
            get { return ProviderSettings.GetInt("CpuWeight"); }
        }
        #endregion

        #region Fields
        private Wmi _wmi = null;

        private Wmi wmi
        {
            get
            {
                if (_wmi == null)
                    _wmi = new Wmi(ServerNameSettings, WMI_VIRTUALIZATION_NAMESPACE);
                return _wmi;
            }
        }
        #endregion

        #region Constructors
        public HyperV2012R2()
        {
        }
        #endregion

        #region Virtual Machines
        
        public VirtualMachine GetVirtualMachine(string vmId)
        {
            return GetVirtualMachineInternal( vmId, false);
        }
        
        public VirtualMachine GetVirtualMachineInternal(string vmId, bool extendedInfo)
        {

            HostedSolutionLog.LogStart("GetVirtualMachine");
            HostedSolutionLog.DebugInfo("Virtual Machine: {0}", vmId);

            Runspace runSpace = null;
            VirtualMachine vm = new VirtualMachine();

            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Get-VM");

                cmd.Parameters.Add("Id", vmId);
                
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd, false);
                if (result != null && result.Count > 0)
                {
                    vm.Name = GetPSObjectProperty(result[0], "Name").ToString();
                    vm.State = (VirtualMachineState)Enum.Parse(typeof(VirtualMachineState), GetPSObjectProperty(result[0], "State").ToString());
                    vm.CpuUsage = ConvertNullableToInt32(GetPSObjectProperty(result[0], "CpuUsage"));
                    vm.RamUsage = ConvertNullableToInt64(GetPSObjectProperty(result[0], "MemoryAssigned"));
                    vm.Uptime = TimeSpan.Parse(GetPSObjectProperty(result[0], "Uptime").ToString()).Ticks;
                    vm.Status = GetPSObjectProperty(result[0], "Status").ToString();
                    vm.ReplicationState = GetPSObjectProperty(result[0], "ReplicationState").ToString();
                    
                    vm.Heartbeat = GetVMHeartBeatStatus(runSpace, vm.Name);

                    vm.CreatedDate = DateTime.Now;

                    if (extendedInfo)
                    {
                        vm.CpuCores = GetVMProcessors(runSpace, vm.Name);

                        MemoryInfo memoryInfo = GetVMMemory(runSpace, vm.Name);
                        vm.RamSize = memoryInfo.Startup;

                        // BIOS 
                        BiosInfo biosInfo = GetVMBios(runSpace, vm.Name);
                        vm.NumLockEnabled = biosInfo.NumLockEnabled;

                        vm.BootFromCD = false;
                        if ((biosInfo.StartupOrder != null) && (biosInfo.StartupOrder.Length > 0))
                            vm.BootFromCD = (biosInfo.StartupOrder[0] == "CD");

                        // DVD drive
                        cmd = new Command("Get-VMDvdDrive");
                        cmd.Parameters.Add("VMName", vm.Name);

                        result = ExecuteShellCommand(runSpace, cmd, false);
                        vm.DvdDriveInstalled = (result != null && result.Count > 0);

                        // HDD
                        vm.Disks = GetVirtualHardDisks(runSpace, vm.Name);

                        if ((vm.Disks != null) & (vm.Disks.GetLength(0) > 0))
                        {
                            vm.VirtualHardDrivePath = vm.Disks[0].Path;
                            vm.HddSize = Convert.ToInt32(vm.Disks[0].FileSize);
                        }

                        // network adapters
                        vm.Adapters = GetNetworkAdapters(runSpace, vm.Name);
                        return vm;

                    }

                }
            }
            catch (Exception ex)
            {
                HostedSolutionLog.LogError("GetVirtualMachine", ex);
                throw;
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            HostedSolutionLog.LogEnd("GetVirtualMachine");
            return vm;
 
        }

        internal OperationalStatus GetVMHeartBeatStatus(Runspace runSpace, string name)
        {

            OperationalStatus status = OperationalStatus.None;

            Command cmd = new Command("Get-VMIntegrationService");

            cmd.Parameters.Add("VMName", name);
            cmd.Parameters.Add("Name", "HeartBeat");
                
            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd, false);
            if (result != null && result.Count > 0)
            {
                var statusString = GetPSObjectProperty(result[0], "PrimaryOperationalStatus");
               
                if (statusString != null)
                    status = (OperationalStatus)Enum.Parse(typeof(OperationalStatus), statusString.ToString());
            }
            return status;
        }

        public VirtualMachine GetVirtualMachineEx(string vmId)
        {
            return GetVirtualMachineInternal( vmId, true);
        }


        internal int GetVMProcessors(Runspace runSpace, string name)
        {

            int procs = 0;

            Command cmd = new Command("Get-VMProcessor");

            cmd.Parameters.Add("VMName", name);

            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd, false);
            if (result != null && result.Count > 0)
            {
                procs = Convert.ToInt32(GetPSObjectProperty(result[0], "Count"));

            }
            return procs;
        }

        internal MemoryInfo GetVMMemory(Runspace runSpace, string name)
        {
            MemoryInfo info =  new MemoryInfo();

            Command cmd = new Command("Get-VMMemory");

            cmd.Parameters.Add("VMName", name);

            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd, false);
            if (result != null && result.Count > 0)
            {
                info.DynamicMemoryEnabled = Convert.ToBoolean(GetPSObjectProperty(result[0], "DynamicMemoryEnabled"));
                info.Startup = Convert.ToInt64(GetPSObjectProperty(result[0], "Startup"));
                info.Minimum = Convert.ToInt64(GetPSObjectProperty(result[0], "Minimum"));
                info.Maximum = Convert.ToInt64(GetPSObjectProperty(result[0], "Maximum"));
                info.Buffer = Convert.ToInt32(GetPSObjectProperty(result[0], "Buffer"));
                info.Priority = Convert.ToInt32(GetPSObjectProperty(result[0], "Priority"));
            }
            return info;
        }

        internal BiosInfo GetVMBios(Runspace runSpace, string name)
        {
            BiosInfo info = new BiosInfo();

            Command cmd = new Command("Get-VMBios");

            cmd.Parameters.Add("VMName", name);

            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd, false);
            if (result != null && result.Count > 0)
            {
                info.NumLockEnabled = Convert.ToBoolean(GetPSObjectProperty(result[0], "NumLockEnabled"));

                List<string> startupOrders = new List<string>();

                foreach (var item in (IEnumerable)GetPSObjectProperty(result[0], "StartupOrder"))
                    startupOrders.Add(item.ToString());

                info.StartupOrder = startupOrders.ToArray();
            }
            return info;
        }

        internal VirtualHardDiskInfo[] GetVirtualHardDisks(Runspace runSpace, string name)
        {

            List<VirtualHardDiskInfo> disks = new List<VirtualHardDiskInfo>();

            Command cmd = new Command("Get-VMHardDiskDrive");
            cmd.Parameters.Add("VMName", name);

            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd, false);
            if (result != null && result.Count > 0)
            {
                foreach(PSObject d in result)
                {
                    VirtualHardDiskInfo disk = new VirtualHardDiskInfo();

                    disk.SupportPersistentReservations = Convert.ToBoolean(GetPSObjectProperty(d, "SupportPersistentReservations"));
                    disk.MaximumIOPS = Convert.ToUInt64(GetPSObjectProperty(d, "MaximumIOPS"));
                    disk.MinimumIOPS = Convert.ToUInt64(GetPSObjectProperty(d, "MinimumIOPS"));
                    disk.VHDControllerType = (ControllerType)Enum.Parse(typeof(ControllerType), GetPSObjectProperty(d, "ControllerType").ToString());
                    disk.ControllerNumber = Convert.ToInt32(GetPSObjectProperty(d, "ControllerNumber"));
                    disk.ControllerLocation = Convert.ToInt32(GetPSObjectProperty(d, "ControllerLocation"));
                    disk.Path = GetPSObjectProperty(d, "Path").ToString();
                    disk.Name = GetPSObjectProperty(d, "Name").ToString();

                    GetVirtualHardDiskDetail(runSpace, disk.Path, ref disk);

                    disks.Add(disk);
                }
            }
            return disks.ToArray();
        }

        internal void GetVirtualHardDiskDetail(Runspace runSpace, string path, ref VirtualHardDiskInfo disk)
        {
            if (!string.IsNullOrEmpty(path))
            {
                Command cmd = new Command("Get-VHD");
                cmd.Parameters.Add("Path", path);
                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd, false);
                if (result != null && result.Count > 0)
                {
                    disk.DiskFormat = (VirtualHardDiskFormat)Enum.Parse(typeof(VirtualHardDiskFormat), GetPSObjectProperty(result[0], "VhdFormat").ToString());
                    disk.DiskType = (VirtualHardDiskType)Enum.Parse(typeof(VirtualHardDiskType), GetPSObjectProperty(result[0], "VhdType").ToString());
                    disk.ParentPath = (string)GetPSObjectProperty(result[0], "ParentPath");
                    disk.MaxInternalSize = Convert.ToInt64(GetPSObjectProperty(result[0], "Size")) / Size1G;
                    disk.FileSize = Convert.ToInt64(GetPSObjectProperty(result[0], "FileSize")) / Size1G;
                    disk.Attached = Convert.ToBoolean(GetPSObjectProperty(result[0], "Attached"));
                }
            }
        }


        internal VirtualMachineNetworkAdapter[] GetNetworkAdapters(Runspace runSpace, string name)
        {
            List<VirtualMachineNetworkAdapter> adapters = new List<VirtualMachineNetworkAdapter>();

            Command cmd = new Command("Get-VMNetworkAdapter");
            cmd.Parameters.Add("VMName", name);

            Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd, false);
            if (result != null && result.Count > 0)
            {
                foreach(PSObject a in result)
                {
                    VirtualMachineNetworkAdapter adapter = new VirtualMachineNetworkAdapter();

                    adapter.Name = GetPSObjectProperty(a, "Name").ToString();
                    adapter.MacAddress = GetPSObjectProperty(a, "MacAddress").ToString();

                    adapters.Add(adapter);
                }
            }
            return adapters.ToArray();
        }

        public List<VirtualMachine> GetVirtualMachines()
        {
            HostedSolutionLog.LogStart("GetVirtualMachines");

            Runspace runSpace = null;
            List<VirtualMachine> vmachines = new List<VirtualMachine>();

            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Get-VM");

                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd, false);
                foreach (PSObject current in result)
                {
                    VirtualMachine vm = new VirtualMachine
                    {
                        VirtualMachineId = GetPSObjectProperty(current, "Id").ToString(),
                        Name = GetPSObjectProperty(current, "Name").ToString(),
                        State = (VirtualMachineState)Enum.Parse(typeof(VirtualMachineState), GetPSObjectProperty(current, "State").ToString()),
                        Uptime = GetPSObjectProperty<TimeSpan>(current, "UpTime").Ticks
                    };
                    vmachines.Add(vm);
                }
            }
            catch (Exception ex)
            {
                HostedSolutionLog.LogError("GetVirtualMachines", ex);
                throw;
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            HostedSolutionLog.LogEnd("GetVirtualMachines");
            return vmachines;

        }

        public byte[] GetVirtualMachineThumbnailImage(string vmId, ThumbnailSize size)
        {
            //ManagementBaseObject objSummary = GetVirtualMachineSummaryInformation(vmId, (SummaryInformationRequest)size);
            //wmi.Dump(objSummary);
            //return GetTumbnailFromSummaryInformation(objSummary, size);
            // TODO:
            return (byte[]) (new ImageConverter()).ConvertTo(new Bitmap(80, 60), typeof (byte[]));
        }

        private byte[] GetTumbnailFromSummaryInformation(ManagementBaseObject objSummary, ThumbnailSize size)
        {
            int width = 80;
            int height = 60;

            if (size == ThumbnailSize.Medium160x120)
            {
                width = 160;
                height = 120;
            }
            else if (size == ThumbnailSize.Large320x240)
            {
                width = 320;
                height = 240;
            }

            byte[] imgData = (byte[])objSummary["ThumbnailImage"];

            // create new bitmap
            Bitmap bmp = new Bitmap(width, height);

            if (imgData != null)
            {
                // lock bitmap
                Rectangle rect = new Rectangle(0, 0, width, height);
                BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb565);

                // get address of the first line
                IntPtr ptr = bmpData.Scan0;

                // coby thumbnail bytes into bitmap
                System.Runtime.InteropServices.Marshal.Copy(imgData, 0, ptr, imgData.Length);

                // unlock image
                bmp.UnlockBits(bmpData);
            }
            else
            {
                // fill grey rectangle
                Graphics g = Graphics.FromImage(bmp);
                SolidBrush brush = new SolidBrush(Color.LightGray);
                g.FillRectangle(brush, 0, 0, width, height);
            }

            MemoryStream stream = new MemoryStream();
            bmp.Save(stream, ImageFormat.Png);

            stream.Flush();
            byte[] buffer = stream.ToArray();

            bmp.Dispose();
            stream.Dispose();

            return buffer;
        }

        public VirtualMachine CreateVirtualMachine(VirtualMachine vm)
        {
            // evaluate paths
            vm.RootFolderPath = FileUtils.EvaluateSystemVariables(vm.RootFolderPath);
            vm.OperatingSystemTemplatePath = FileUtils.EvaluateSystemVariables(vm.OperatingSystemTemplatePath);
            vm.VirtualHardDrivePath = FileUtils.EvaluateSystemVariables(vm.VirtualHardDrivePath);

            string vmID = null;

            // request management service
            ManagementObject objVmsvc = GetVirtualSystemManagementService();

            // display name
            ManagementObject objGlobalSettings = wmi.GetWmiClass("msvm_VirtualSystemGlobalSettingData").CreateInstance();
            objGlobalSettings["ElementName"] = vm.Name;

            // VM folders
            objGlobalSettings["ExternalDataRoot"] = vm.RootFolderPath;
            objGlobalSettings["SnapshotDataRoot"] = vm.RootFolderPath;

            wmi.Dump(objGlobalSettings);

            // startup/shutdown actions
            if (AutomaticStartActionSettings != 100)
            {
                objGlobalSettings["AutomaticStartupAction"] = AutomaticStartActionSettings;
                objGlobalSettings["AutomaticStartupActionDelay"] = String.Format("000000000000{0:d2}.000000:000", AutomaticStartupDelaySettings);
            }

            if (AutomaticStopActionSettings != 100)
                objGlobalSettings["AutomaticShutdownAction"] = AutomaticStopActionSettings;

            if (AutomaticRecoveryActionSettings != 100)
                objGlobalSettings["AutomaticRecoveryAction"] = AutomaticRecoveryActionSettings;

            // create machine
            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("DefineVirtualSystem");
            inParams["SystemSettingData"] = objGlobalSettings.GetText(TextFormat.CimDtd20);
            inParams["ResourceSettingData"] = new string[] { };

            // invoke method
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("DefineVirtualSystem", inParams, null);
            ManagementObject objVM = wmi.GetWmiObjectByPath((string)outParams["DefinedSystem"]);

            // job
            JobResult job = CreateJobResultFromWmiMethodResults(outParams); ;

            // read VM id
            vmID = (string)objVM["Name"];

            // update general settings
            UpdateVirtualMachineGeneralSettings(vmID, objVM,
                vm.CpuCores,
                vm.RamSize,
                vm.BootFromCD,
                vm.NumLockEnabled);

            // hard disks
            // load IDE 0 controller
            ManagementObject objIDE0 = wmi.GetWmiObject(
                "Msvm_ResourceAllocationSettingData", "ResourceSubType = 'Microsoft Emulated IDE Controller'"
                    + " and InstanceID Like 'Microsoft:{0}%' and Address = 0", vmID);

            // load default hard disk drive
            ManagementObject objDefaultHdd = wmi.GetWmiObject(
                "Msvm_ResourceAllocationSettingData", "ResourceSubType = 'Microsoft Synthetic Disk Drive'"
                    + " and InstanceID like '%Default'");
            ManagementObject objHdd = (ManagementObject)objDefaultHdd.Clone();
            objHdd["Parent"] = objIDE0.Path;
            objHdd["Address"] = 0;

            // add HDD to VM resources
            ManagementObject objAddedHDD = AddVirtualMachineResources(objVM, objHdd);

            // attach VHD
            string fullVhdPath = vm.VirtualHardDrivePath;
            ManagementObject objDefaultVHD = wmi.GetWmiObject(
                "Msvm_ResourceAllocationSettingData", "ResourceSubType = 'Microsoft Virtual Hard Disk'"
                    + " and InstanceID like '%Default'");
            ManagementObject objVhd = (ManagementObject)objDefaultVHD.Clone();
            objVhd["Parent"] = objAddedHDD.Path.Path;
            objVhd["Connection"] = new string[] { fullVhdPath };

            // add VHD to the system
            AddVirtualMachineResources(objVM, objVhd);

            // DVD drive
            if (vm.DvdDriveInstalled)
            {
                AddVirtualMachineDvdDrive(vmID, objVM);
            }

            // add external adapter
            if (vm.ExternalNetworkEnabled && !String.IsNullOrEmpty(vm.ExternalSwitchId))
                AddNetworkAdapter(objVM, vm.ExternalSwitchId, vm.Name, vm.ExternalNicMacAddress, EXTERNAL_NETWORK_ADAPTER_NAME, vm.LegacyNetworkAdapter);

            // add private adapter
            if (vm.PrivateNetworkEnabled && !String.IsNullOrEmpty(vm.PrivateSwitchId))
                AddNetworkAdapter(objVM, vm.PrivateSwitchId, vm.Name, vm.PrivateNicMacAddress, PRIVATE_NETWORK_ADAPTER_NAME, vm.LegacyNetworkAdapter);

            // add management adapter
            if (vm.ManagementNetworkEnabled && !String.IsNullOrEmpty(vm.ManagementSwitchId))
                AddNetworkAdapter(objVM, vm.ManagementSwitchId, vm.Name, vm.ManagementNicMacAddress, MANAGEMENT_NETWORK_ADAPTER_NAME, vm.LegacyNetworkAdapter);

            vm.VirtualMachineId = vmID;
            return vm;
        }

        public VirtualMachine UpdateVirtualMachine(VirtualMachine vm)
        {
            string vmId = vm.VirtualMachineId;

            // get VM object
            ManagementObject objVM = GetVirtualMachineObject(vmId);

            // update general settings
            UpdateVirtualMachineGeneralSettings(vmId, objVM,
                vm.CpuCores,
                vm.RamSize,
                vm.BootFromCD,
                vm.NumLockEnabled);

            // check DVD drive
            ManagementObject objDvdDrive = wmi.GetWmiObject(
                "Msvm_ResourceAllocationSettingData", "ResourceSubType = 'Microsoft Synthetic DVD Drive'"
                    + " and InstanceID like 'Microsoft:{0}%' and Address = 0", vmId);

            if (vm.DvdDriveInstalled && objDvdDrive == null)
                AddVirtualMachineDvdDrive(vmId, objVM);
            else if (!vm.DvdDriveInstalled && objDvdDrive != null)
                RemoveVirtualMachineResources(objVM, objDvdDrive);

            // External NIC
            if (!vm.ExternalNetworkEnabled
                && !String.IsNullOrEmpty(vm.ExternalNicMacAddress))
            {
                // delete adapter
                DeleteNetworkAdapter(objVM, vm.ExternalNicMacAddress);

                // reset MAC
                vm.ExternalNicMacAddress = null;
            }
            else if (vm.ExternalNetworkEnabled
                && !String.IsNullOrEmpty(vm.ExternalNicMacAddress))
            {
                // add external adapter
                AddNetworkAdapter(objVM, vm.ExternalSwitchId, vm.Name, vm.ExternalNicMacAddress, EXTERNAL_NETWORK_ADAPTER_NAME, vm.LegacyNetworkAdapter);
            }


            // Private NIC
            if (!vm.PrivateNetworkEnabled
                && !String.IsNullOrEmpty(vm.PrivateNicMacAddress))
            {
                // delete adapter
                DeleteNetworkAdapter(objVM, vm.PrivateNicMacAddress);

                // reset MAC
                vm.PrivateNicMacAddress = null;
            }
            else if (vm.PrivateNetworkEnabled
                && !String.IsNullOrEmpty(vm.PrivateNicMacAddress))
            {
                // add private adapter
                AddNetworkAdapter(objVM, vm.PrivateSwitchId, vm.Name, vm.PrivateNicMacAddress, PRIVATE_NETWORK_ADAPTER_NAME, vm.LegacyNetworkAdapter);
            }

            return vm;
        }

        private void UpdateVirtualMachineGeneralSettings(string vmId, ManagementObject objVM, int cpuCores, long ramMB, bool bootFromCD, bool numLockEnabled)
        {
            // request management service
            ManagementObject objVmsvc = GetVirtualSystemManagementService();

            // VM resources
            List<string> vmConfig = new List<string>();

            // get system settings
            ManagementObject objSettings = GetVirtualMachineSettingsObject(vmId);

            // BIOS (num lock)
            objSettings["BIOSNumLock"] = numLockEnabled;

            // BIOS (boot order)
            // BootOrder = 0 - Boot from floppy, 1 - Boot from CD, 2 - Boot from disk, 3 - PXE Boot 
            objSettings["BootOrder"] = bootFromCD ? new int[] { 1, 2, 3, 0 } : new int[] { 2, 1, 3, 0 };

            // modify machine settings
            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("ModifyVirtualSystem");
            inParams["ComputerSystem"] = objVM;
            inParams["SystemSettingData"] = objSettings.GetText(TextFormat.CimDtd20);
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("ModifyVirtualSystem", inParams, null);
            JobResult job = CreateJobResultFromWmiMethodResults(outParams);

            // setup CPU
            ManagementObject objCpu = wmi.GetWmiObject("Msvm_ProcessorSettingData", "InstanceID Like 'Microsoft:{0}%'", vmId);
            objCpu["VirtualQuantity"] = cpuCores;
            objCpu["Limit"] = Convert.ToInt64(CpuLimitSettings * 1000);
            objCpu["Reservation"] = Convert.ToInt64(CpuReserveSettings * 1000);
            objCpu["Weight"] = CpuWeightSettings;
            vmConfig.Add(objCpu.GetText(TextFormat.CimDtd20));

            // setup RAM
            ManagementObject objRam = wmi.GetWmiObject("Msvm_MemorySettingData", "InstanceID Like 'Microsoft:{0}%'", vmId);
            objRam["VirtualQuantity"] = ramMB.ToString();
            objRam["Reservation"] = ramMB.ToString();
            objRam["Limit"] = ramMB.ToString();
            vmConfig.Add(objRam.GetText(TextFormat.CimDtd20));

            // modify machine resources
            inParams = objVmsvc.GetMethodParameters("ModifyVirtualSystemResources");
            inParams["ComputerSystem"] = objVM;
            inParams["ResourceSettingData"] = vmConfig.ToArray();
            outParams = objVmsvc.InvokeMethod("ModifyVirtualSystemResources", inParams, null);
            job = CreateJobResultFromWmiMethodResults(outParams);
        }

        private void AddVirtualMachineDvdDrive(string vmId, ManagementObject objVM)
        {
            // load IDE 1 controller
            ManagementObject objIDE1 = wmi.GetWmiObject(
                "Msvm_ResourceAllocationSettingData", "ResourceSubType = 'Microsoft Emulated IDE Controller'"
                + " and InstanceID Like 'Microsoft:{0}%' and Address = 1", vmId);

            // load default hard disk drive
            ManagementObject objDefaultDvd = wmi.GetWmiObject(
                "Msvm_ResourceAllocationSettingData", "ResourceSubType = 'Microsoft Synthetic DVD Drive'"
                    + " and InstanceID like '%Default'");
            ManagementObject objDvd = (ManagementObject)objDefaultDvd.Clone();
            objDvd["Parent"] = objIDE1.Path;
            objDvd["Address"] = 0;

            // add DVD drive to VM resources
            AddVirtualMachineResources(objVM, objDvd);
        }

        private void AddNetworkAdapter(ManagementObject objVm, string switchId, string portName, string macAddress, string adapterName, bool legacyAdapter)
        {
            string nicClassName = GetNetworkAdapterClassName(legacyAdapter);

            string vmId = (string)objVm["Name"];

            // check if already exists
            ManagementObject objNic = wmi.GetWmiObject(
                nicClassName, "InstanceID like 'Microsoft:{0}%' and Address = '{1}'", vmId, macAddress);

            if (objNic != null)
                return; // exists - exit

            portName = String.Format("{0} - {1}",
                portName, (adapterName == EXTERNAL_NETWORK_ADAPTER_NAME) ? "External" : "Private");

            // Network service
            ManagementObject objNetworkSvc = GetVirtualSwitchManagementService();

            // default NIC
            ManagementObject objDefaultNic = wmi.GetWmiObject(nicClassName, "InstanceID like '%Default'");

            // find switch
            ManagementObject objSwitch = wmi.GetWmiObject("msvm_VirtualSwitch", "Name = '{0}'", switchId);

            // create switch port
            ManagementBaseObject inParams = objNetworkSvc.GetMethodParameters("CreateSwitchPort");
            inParams["VirtualSwitch"] = objSwitch;
            inParams["Name"] = portName;
            inParams["FriendlyName"] = portName;
            inParams["ScopeOfResidence"] = "";

            // invoke method
            ManagementBaseObject outParams = objNetworkSvc.InvokeMethod("CreateSwitchPort", inParams, null);

            // process output parameters
            ReturnCode code = (ReturnCode)Convert.ToInt32(outParams["ReturnValue"]);
            if (code == ReturnCode.OK)
            {
                // created port
                ManagementObject objPort = wmi.GetWmiObjectByPath((string)outParams["CreatedSwitchPort"]);

                // create NIC
                ManagementObject objExtNic = (ManagementObject)objDefaultNic.Clone();
                objExtNic["Connection"] = new string[] { objPort.Path.Path };

                if (!String.IsNullOrEmpty(macAddress))
                {
                    objExtNic["StaticMacAddress"] = true;
                    objExtNic["Address"] = macAddress;
                }
                else
                {
                    objExtNic["StaticMacAddress"] = false;
                }
                objExtNic["ElementName"] = adapterName;

                if (!legacyAdapter)
                    objExtNic["VirtualSystemIdentifiers"] = new string[] { Guid.NewGuid().ToString("B") };

                // add NIC
                ManagementObject objCreatedExtNic = AddVirtualMachineResources(objVm, objExtNic);
            }
        }

        private string GetNetworkAdapterClassName(bool legacy)
        {
            return legacy ? "Msvm_EmulatedEthernetPortSettingData" : "Msvm_SyntheticEthernetPortSettingData";
        }

        private ManagementObject AddVirtualMachineResources(ManagementObject objVm, ManagementObject resource)
        {
            if (resource == null)
                return resource;

            // request management service
            ManagementObject objVmsvc = GetVirtualSystemManagementService();

            // add resources
            string txtResource = resource.GetText(TextFormat.CimDtd20);
            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("AddVirtualSystemResources");
            inParams["TargetSystem"] = objVm;
            inParams["ResourceSettingData"] = new string[] { txtResource };
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("AddVirtualSystemResources", inParams, null);
            JobResult result = CreateJobResultFromWmiMethodResults(outParams);

            if (result.ReturnValue == ReturnCode.OK)
            {
                string[] wmiPaths = (string[])outParams["NewResources"];
                return wmi.GetWmiObjectByPath(wmiPaths[0]);
            }
            else if (result.ReturnValue == ReturnCode.JobStarted)
            {
                if (JobCompleted(result.Job))
                {
                    string[] wmiPaths = (string[])outParams["NewResources"];
                    return wmi.GetWmiObjectByPath(wmiPaths[0]);
                }
                else
                {
                    throw new Exception("Cannot add virtual machine resources");
                }
            }
            else
            {
                throw new Exception("Cannot add virtual machine resources: " + txtResource);
            }
        }

        private JobResult RemoveVirtualMachineResources(ManagementObject objVm, ManagementObject resource)
        {
            if (resource == null)
                return null;

            // request management service
            ManagementObject objVmsvc = GetVirtualSystemManagementService();

            // remove resources
            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("RemoveVirtualSystemResources");
            inParams["TargetSystem"] = objVm;
            inParams["ResourceSettingData"] = new string[] { resource.Path.Path };
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("RemoveVirtualSystemResources", inParams, null);
            JobResult result = CreateJobResultFromWmiMethodResults(outParams);
            if (result.ReturnValue == ReturnCode.OK)
            {
                return result;
            }
            else if (result.ReturnValue == ReturnCode.JobStarted)
            {
                if (!JobCompleted(result.Job))
                {
                    throw new Exception("Cannot remove virtual machine resources");
                }
            }
            else
            {
                throw new Exception("Cannot remove virtual machine resources: " + resource.Path.Path);
            }

            return result;
        }

        public JobResult ChangeVirtualMachineState(string vmId, VirtualMachineRequestedState newState)
        {
            HostedSolutionLog.LogStart("ChangeVirtualMachineState");
            var jobResult = new JobResult();

            var vm = GetVirtualMachine(vmId);
            Runspace runSpace = null;

            try
            {
                runSpace = OpenRunspace();

                string cmdTxt;
                List<string> paramList = new List<string>();

                switch (newState)
                {
                    case VirtualMachineRequestedState.Start:
                        cmdTxt = "Start-VM";
                        break;
                    case VirtualMachineRequestedState.Pause:
                        cmdTxt = "Suspend-VM";
                        break;
                    case VirtualMachineRequestedState.Reset:
                        cmdTxt = "Restart-VM";
                        break;
                    case VirtualMachineRequestedState.Resume:
                        cmdTxt = "Resume-VM";
                        break;
                    case VirtualMachineRequestedState.ShutDown:
                        cmdTxt = "Stop-VM";
                        break;
                    case VirtualMachineRequestedState.TurnOff:
                        cmdTxt = "Stop-VM";
                        paramList.Add("TurnOff");
                        break;
                    case VirtualMachineRequestedState.Save:
                        cmdTxt = "Save-VM";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("newState");
                }

                Command cmd = new Command(cmdTxt);

                cmd.Parameters.Add("Name", vm.Name);
                //cmd.Parameters.Add("AsJob");
                paramList.ForEach(p => cmd.Parameters.Add(p));

                ExecuteShellCommand(runSpace, cmd, false);
                jobResult = CreateSuccessJobResult();
            }
            catch (Exception ex)
            {
                HostedSolutionLog.LogError("ChangeVirtualMachineState", ex);
                throw;
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            HostedSolutionLog.LogEnd("ChangeVirtualMachineState");

            return jobResult;
        }

        public ReturnCode ShutDownVirtualMachine(string vmId, bool force, string reason)
        {
            HostedSolutionLog.LogStart("ShutDownVirtualMachine");
            ReturnCode returnCode  = ReturnCode.OK;

            var vm = GetVirtualMachine(vmId);
            Runspace runSpace = null;

            try
            {
                runSpace = OpenRunspace();

                Command cmd = new Command("Stop-VM");

                cmd.Parameters.Add("Name", vm.Name);
                if (force) cmd.Parameters.Add("Force");

                ExecuteShellCommand(runSpace, cmd, false);
            }
            catch (Exception ex)
            {
                HostedSolutionLog.LogError("ShutDownVirtualMachine", ex);
                throw;
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            HostedSolutionLog.LogEnd("ShutDownVirtualMachine");

            return returnCode;
        }

        public List<ConcreteJob> GetVirtualMachineJobs(string vmId)
        {
            List<ConcreteJob> jobs = new List<ConcreteJob>();

            ManagementBaseObject objSummary = GetVirtualMachineSummaryInformation(
                vmId, SummaryInformationRequest.AsynchronousTasks);
            ManagementBaseObject[] objJobs = (ManagementBaseObject[])objSummary["AsynchronousTasks"];

            if (objJobs != null)
            {
                foreach (ManagementBaseObject objJob in objJobs)
                    jobs.Add(CreateJobFromWmiObject(objJob));
            }

            return jobs;
        }

        public JobResult RenameVirtualMachine(string vmId, string name)
        {
            // load virtual machine
            ManagementObject objVm = GetVirtualMachineObject(vmId);

            // load machine settings
            ManagementObject objVmSettings = GetVirtualMachineSettingsObject(vmId);

            // rename machine
            objVmSettings["ElementName"] = name;

            // save
            ManagementObject objVmsvc = GetVirtualSystemManagementService();
            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("ModifyVirtualSystem");
            inParams["ComputerSystem"] = objVm.Path.Path;
            inParams["SystemSettingData"] = objVmSettings.GetText(TextFormat.CimDtd20);
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("ModifyVirtualSystem", inParams, null);
            return CreateJobResultFromWmiMethodResults(outParams);
        }

        public JobResult DeleteVirtualMachine(string vmId)
        {
            // load virtual machine object
            ManagementObject objVm = GetVirtualMachineObject(vmId);

            // check state
            VirtualMachine vm = GetVirtualMachine(vmId);

            // The virtual computer system must be in the powered off or saved state prior to calling this method.
            if (vm.State == VirtualMachineState.Saved
                || vm.State == VirtualMachineState.Off)
            {
                // delete network adapters and ports
                DeleteNetworkAdapters(objVm);

                // destroy machine
                ManagementObject objVmsvc = GetVirtualSystemManagementService();

                // get method
                ManagementBaseObject inParams = objVmsvc.GetMethodParameters("DestroyVirtualSystem");
                inParams["ComputerSystem"] = objVm;

                // invoke method
                ManagementBaseObject outParams = objVmsvc.InvokeMethod("DestroyVirtualSystem", inParams, null);
                return CreateJobResultFromWmiMethodResults(outParams);
            }
            else
            {
                throw new Exception("The virtual computer system must be in the powered off or saved state prior to calling Destroy method.");
            }
        }

        private void DeleteNetworkAdapters(ManagementObject objVM)
        {
            string vmId = (string)objVM["Name"];

            // delete synthetic adapters
            foreach (ManagementObject objNic in wmi.GetWmiObjects("Msvm_SyntheticEthernetPortSettingData", "InstanceID like 'Microsoft:{0}%'", vmId))
                DeleteNetworkAdapter(objVM, objNic);

            // delete legacy adapters
            foreach (ManagementObject objNic in wmi.GetWmiObjects("Msvm_EmulatedEthernetPortSettingData", "InstanceID like 'Microsoft:{0}%'", vmId))
                DeleteNetworkAdapter(objVM, objNic);
        }

        private void DeleteNetworkAdapter(ManagementObject objVM, string macAddress)
        {
            // locate network adapter
            ManagementObject objNic = wmi.GetWmiObject("CIM_ResourceAllocationSettingData", "Address = '{0}'", macAddress);

            // delete adapter
            DeleteNetworkAdapter(objVM, objNic);
        }

        private void DeleteNetworkAdapter(ManagementObject objVM, ManagementObject objNic)
        {
            if (objNic == null)
                return;

            // delete corresponding switch port
            string[] conn = (string[])objNic["Connection"];
            if (conn != null && conn.Length > 0)
                DeleteSwitchPort(conn[0]);

            // delete adapter
            RemoveVirtualMachineResources(objVM, objNic);
        }

        private void DeleteSwitchPort(string portPath)
        {
            // Network service
            ManagementObject objNetworkSvc = GetVirtualSwitchManagementService();

            // create switch port
            ManagementBaseObject inParams = objNetworkSvc.GetMethodParameters("DeleteSwitchPort");
            inParams["SwitchPort"] = portPath;

            // invoke method
            objNetworkSvc.InvokeMethod("DeleteSwitchPort", inParams, null);
        }

        public JobResult ExportVirtualMachine(string vmId, string exportPath)
        {
            // load virtual machine object
            ManagementObject objVm = GetVirtualMachineObject(vmId);

            // check state
            VirtualMachine vm = GetVirtualMachine(vmId);

            // The virtual computer system must be in the powered off or saved state prior to calling this method.
            if (vm.State == VirtualMachineState.Off)
            {
                // export machine
                ManagementObject objVmsvc = GetVirtualSystemManagementService();

                // get method
                ManagementBaseObject inParams = objVmsvc.GetMethodParameters("ExportVirtualSystem");
                inParams["ComputerSystem"] = objVm;
                inParams["CopyVmState"] = true;
                inParams["ExportDirectory"] = FileUtils.EvaluateSystemVariables(exportPath);

                // invoke method
                ManagementBaseObject outParams = objVmsvc.InvokeMethod("ExportVirtualSystem", inParams, null);
                return CreateJobResultFromWmiMethodResults(outParams);
            }
            else
            {
                throw new Exception("The virtual computer system must be in the powered off or saved state prior to calling Export method.");
            }
        }
        #endregion

        #region Snapshots
        public List<VirtualMachineSnapshot> GetVirtualMachineSnapshots(string vmId)
        {
            // get all VM setting objects
            ManagementObject objVmSettings = GetVirtualMachineSettingsObject(vmId);
            VirtualMachineSnapshot runningSnapshot = CreateSnapshotFromWmiObject(objVmSettings);

            // load snapshots
            ManagementBaseObject objSummary = GetVirtualMachineSummaryInformation(vmId, SummaryInformationRequest.Snapshots);
            ManagementBaseObject[] objSnapshots = (ManagementBaseObject[])objSummary["Snapshots"];

            List<VirtualMachineSnapshot> snapshots = new List<VirtualMachineSnapshot>();

            if (objSnapshots != null)
            {
                foreach (ManagementBaseObject objSnapshot in objSnapshots)
                {
                    VirtualMachineSnapshot snapshot = CreateSnapshotFromWmiObject(objSnapshot);
                    snapshot.IsCurrent = (runningSnapshot.ParentId == snapshot.Id);
                    snapshots.Add(snapshot);
                }
            }

            return snapshots;
        }

        public VirtualMachineSnapshot GetSnapshot(string snapshotId)
        {
            // load snapshot
            ManagementObject objSnapshot = GetSnapshotObject(snapshotId);
            return CreateSnapshotFromWmiObject(objSnapshot);
        }

        public JobResult CreateSnapshot(string vmId)
        {
            // get VM management service
            ManagementObject objVmsvc = GetVirtualSystemManagementService();

            // load virtual machine
            ManagementObject objVm = GetVirtualMachineObject(vmId);

            // get method params
            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("CreateVirtualSystemSnapshot");
            inParams["SourceSystem"] = objVm;

            // invoke method
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("CreateVirtualSystemSnapshot", inParams, null);
            return CreateJobResultFromWmiMethodResults(outParams);
        }

        public JobResult RenameSnapshot(string vmId, string snapshotId, string name)
        {
            // load virtual machine
            ManagementObject objVm = GetVirtualMachineObject(vmId);

            // load snapshot
            ManagementObject objSnapshot = GetSnapshotObject(snapshotId);

            // rename snapshot
            objSnapshot["ElementName"] = name;

            // save
            ManagementObject objVmsvc = GetVirtualSystemManagementService();
            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("ModifyVirtualSystem");
            inParams["ComputerSystem"] = objVm.Path.Path;
            inParams["SystemSettingData"] = objSnapshot.GetText(TextFormat.CimDtd20);
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("ModifyVirtualSystem", inParams, null);
            return CreateJobResultFromWmiMethodResults(outParams);
        }

        public JobResult ApplySnapshot(string vmId, string snapshotId)
        {
            // get VM management service
            ManagementObject objVmsvc = GetVirtualSystemManagementService();

            // load virtual machine
            ManagementObject objVm = GetVirtualMachineObject(vmId);

            // load snapshot
            ManagementObject objSnapshot = GetSnapshotObject(snapshotId);

            ManagementObjectCollection objRelated = objVm.GetRelated("Msvm_SettingsDefineState");

            // get method params
            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("ApplyVirtualSystemSnapshot");
            inParams["ComputerSystem"] = objVm.Path.Path;
            inParams["SnapshotSettingData"] = objSnapshot.Path.Path;

            // invoke method
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("ApplyVirtualSystemSnapshot", inParams, null);
            return CreateJobResultFromWmiMethodResults(outParams);
        }

        public JobResult DeleteSnapshot(string snapshotId)
        {
            // get VM management service
            ManagementObject objVmsvc = GetVirtualSystemManagementService();

            // load snapshot object
            ManagementObject objSnapshot = GetSnapshotObject(snapshotId);

            // get method params
            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("RemoveVirtualSystemSnapshot");
            inParams["SnapshotSettingData"] = objSnapshot.Path.Path;

            // invoke method
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("RemoveVirtualSystemSnapshot", inParams, null);
            return CreateJobResultFromWmiMethodResults(outParams);
        }

        public JobResult DeleteSnapshotSubtree(string snapshotId)
        {
            // get VM management service
            ManagementObject objVmsvc = GetVirtualSystemManagementService();

            // load snapshot object
            ManagementObject objSnapshot = GetSnapshotObject(snapshotId);

            // get method params
            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("RemoveVirtualSystemSnapshotTree");
            inParams["SnapshotSettingData"] = objSnapshot.Path.Path;

            // invoke method
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("RemoveVirtualSystemSnapshotTree", inParams, null);
            return CreateJobResultFromWmiMethodResults(outParams);
        }

        public byte[] GetSnapshotThumbnailImage(string snapshotId, ThumbnailSize size)
        {
            ManagementBaseObject objSummary = GetSnapshotSummaryInformation(snapshotId, (SummaryInformationRequest)size);
            return GetTumbnailFromSummaryInformation(objSummary, size);
        }
        #endregion

        #region DVD operations
        public string GetInsertedDVD(string vmId)
        {
            // find CD/DVD disk
            ManagementObject objDvd = wmi.GetWmiObject(
                "Msvm_ResourceAllocationSettingData", "ResourceSubType = 'Microsoft Virtual CD/DVD Disk'"
                    + " and InstanceID Like 'Microsoft:{0}%'", vmId);

            if (objDvd == null)
                return null;

            string[] path = (string[])objDvd["Connection"];
            if (path != null && path.Length > 0)
                return path[0];

            return null;
        }

        public JobResult InsertDVD(string vmId, string isoPath)
        {
            isoPath = FileUtils.EvaluateSystemVariables(isoPath);

            // find DVD drive
            ManagementObject objDvdDrive = wmi.GetWmiObject(
                "Msvm_ResourceAllocationSettingData", "ResourceSubType = 'Microsoft Synthetic DVD Drive'"
                    + " and InstanceID Like 'Microsoft:{0}%'", vmId);

            // create CD/DVD disk
            ManagementObject objDefaultDVD = wmi.GetWmiObject(
                "Msvm_ResourceAllocationSettingData", "ResourceSubType = 'Microsoft Virtual CD/DVD Disk'"
                    + " and InstanceID like '%Default'");
            ManagementObject objDvd = (ManagementObject)objDefaultDVD.Clone();
            objDvd["Parent"] = objDvdDrive.Path;
            objDvd["Connection"] = new string[] { isoPath };

            // get VM service
            ManagementObject objVmsvc = GetVirtualSystemManagementService();

            // get method
            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("AddVirtualSystemResources");
            inParams["TargetSystem"] = GetVirtualMachineObject(vmId);
            inParams["ResourceSettingData"] = new string[] { objDvd.GetText(TextFormat.CimDtd20) };

            // execute method
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("AddVirtualSystemResources", inParams, null);
            return CreateJobResultFromWmiMethodResults(outParams);
        }

        public JobResult EjectDVD(string vmId)
        {
            // find CD/DVD disk
            ManagementObject objDvd = wmi.GetWmiObject(
                "Msvm_ResourceAllocationSettingData", "ResourceSubType = 'Microsoft Virtual CD/DVD Disk'"
                    + " and InstanceID Like 'Microsoft:{0}%'", vmId);

            // get VM service
            ManagementObject objVmsvc = GetVirtualSystemManagementService();

            // get method
            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("RemoveVirtualSystemResources");
            inParams["TargetSystem"] = GetVirtualMachineObject(vmId);
            inParams["ResourceSettingData"] = new object[] { objDvd.Path.Path };

            // execute method
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("RemoveVirtualSystemResources", inParams, null);
            return CreateJobResultFromWmiMethodResults(outParams);
        }
        #endregion

        #region Virtual Switches
        public List<VirtualSwitch> GetSwitches()
        {
            return GetSwitches(null, null);
        }

        public List<VirtualSwitch> GetExternalSwitches(string computerName)
        {
            return GetSwitches(computerName, "External");
        }

        private List<VirtualSwitch> GetSwitches(string computerName, string type)
        {
            HostedSolutionLog.LogStart("GetSwitches");
            HostedSolutionLog.DebugInfo("ComputerName: {0}", computerName);

            Runspace runSpace = null;
            List<VirtualSwitch> switches = new List<VirtualSwitch>();

            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Get-VMSwitch");

                if (!string.IsNullOrEmpty(computerName)) cmd.Parameters.Add("ComputerName", computerName);
                if (!string.IsNullOrEmpty(type)) cmd.Parameters.Add("SwitchType", type);

                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd,false);
                foreach (PSObject current in result)
                {
                    VirtualSwitch sw = new VirtualSwitch();
                    sw.SwitchId = GetPSObjectProperty(current, "Name").ToString();
                    sw.Name = GetPSObjectProperty(current, "Name").ToString();
                    sw.SwitchType = GetPSObjectProperty(current, "SwitchType").ToString();
                    switches.Add(sw);
                }
            }
            catch (Exception ex)
            {
                HostedSolutionLog.LogError("GetSwitches", ex);
                throw;
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            HostedSolutionLog.LogEnd("GetSwitches");
            return switches;

        }

        public bool SwitchExists(string switchId)
        {
            ManagementObject objSwitch = wmi.GetWmiObject("msvm_VirtualSwitch", "Name = '{0}'", switchId);
            return (objSwitch != null);
        }

        public VirtualSwitch CreateSwitch(string name)
        {
            // generate ID for new virtual switch
            string id = Guid.NewGuid().ToString();

            // get switch management object
            ManagementObject objNetworkSvc = GetVirtualSwitchManagementService();

            ManagementBaseObject inParams = objNetworkSvc.GetMethodParameters("CreateSwitch");
            inParams["Name"] = id;
            inParams["FriendlyName"] = name;
            inParams["NumLearnableAddresses"] = SWITCH_PORTS_NUMBER;

            // invoke method
            ManagementBaseObject outParams = objNetworkSvc.InvokeMethod("CreateSwitch", inParams, null);

            // process output parameters
            ManagementObject objSwitch = wmi.GetWmiObjectByPath((string)outParams["CreatedVirtualSwitch"]);
            return CreateSwitchFromWmiObject(objSwitch);
        }

        public ReturnCode DeleteSwitch(string switchId)
        {
            // find requested switch
            ManagementObject objSwitch = wmi.GetWmiObject("msvm_VirtualSwitch", "Name = '{0}'", switchId);

            if (objSwitch == null)
                throw new Exception("Virtual switch with the specified ID was not found.");

            // get switch management object
            ManagementObject objNetworkSvc = GetVirtualSwitchManagementService();

            // get method params
            ManagementBaseObject inParams = objNetworkSvc.GetMethodParameters("DeleteSwitch");
            inParams["VirtualSwitch"] = objSwitch.Path.Path;

            ManagementBaseObject outParams = (ManagementBaseObject)objNetworkSvc.InvokeMethod("DeleteSwitch", inParams, null);
            return (ReturnCode)Convert.ToInt32(outParams["ReturnValue"]);
        }
        #endregion

        #region Library
        public LibraryItem[] GetLibraryItems(string path)
        {
            path = Path.Combine(FileUtils.EvaluateSystemVariables(path), LIBRARY_INDEX_FILE_NAME);

            // convert to UNC if it is a remote computer
            path = ConvertToUNC(path);

            if (!File.Exists(path))
            {
                HostedSolutionLog.LogWarning("The folder does not contain 'index.xml' file: {0}", path);
                return null;
            }

            // create list
            List<LibraryItem> items = new List<LibraryItem>();

            // load xml
            XmlDocument xml = new XmlDocument();
            xml.Load(path);

            XmlNodeList nodeItems = xml.SelectNodes("/items/item");

            if (nodeItems.Count == 0)
                HostedSolutionLog.LogWarning("index.xml found, but contains 0 items: {0}", path);

            foreach (XmlNode nodeItem in nodeItems)
            {
                LibraryItem item = new LibraryItem();
                item.Path = nodeItem.Attributes["path"].Value;

                // optional attributes
                if (nodeItem.Attributes["diskSize"] != null)
                    item.DiskSize = Int32.Parse(nodeItem.Attributes["diskSize"].Value);

                if (nodeItem.Attributes["legacyNetworkAdapter"] != null)
                    item.LegacyNetworkAdapter = Boolean.Parse(nodeItem.Attributes["legacyNetworkAdapter"].Value);

                item.ProcessVolume = 0; // process (extend and sysprep) 1st volume by default
                if (nodeItem.Attributes["processVolume"] != null)
                    item.ProcessVolume = Int32.Parse(nodeItem.Attributes["processVolume"].Value);

                if (nodeItem.Attributes["remoteDesktop"] != null)
                    item.RemoteDesktop = Boolean.Parse(nodeItem.Attributes["remoteDesktop"].Value);

                // inner nodes
                item.Name = nodeItem.SelectSingleNode("name").InnerText;
                item.Description = nodeItem.SelectSingleNode("description").InnerText;

                // sysprep files
                XmlNodeList nodesSyspep = nodeItem.SelectNodes("provisioning/sysprep");
                List<string> sysprepFiles = new List<string>();
                foreach (XmlNode nodeSyspep in nodesSyspep)
                {
                    if (nodeSyspep.Attributes["file"] != null)
                        sysprepFiles.Add(nodeSyspep.Attributes["file"].Value);
                }
                item.SysprepFiles = sysprepFiles.ToArray();

                // vmconfig
                XmlNode nodeVmConfig = nodeItem.SelectSingleNode("provisioning/vmconfig");
                if (nodeVmConfig != null)
                {
                    if (nodeVmConfig.Attributes["computerName"] != null)
                        item.ProvisionComputerName = Boolean.Parse(nodeVmConfig.Attributes["computerName"].Value);

                    if (nodeVmConfig.Attributes["administratorPassword"] != null)
                        item.ProvisionAdministratorPassword = Boolean.Parse(nodeVmConfig.Attributes["administratorPassword"].Value);

                    if (nodeVmConfig.Attributes["networkAdapters"] != null)
                        item.ProvisionNetworkAdapters = Boolean.Parse(nodeVmConfig.Attributes["networkAdapters"].Value);
                }

                items.Add(item);
            }

            return items.ToArray();
        }

        private string ConvertToUNC(string path)
        {
            if (String.IsNullOrEmpty(ServerNameSettings)
                || path.StartsWith(@"\\"))
                return path;

            return String.Format(@"\\{0}\{1}", ServerNameSettings, path.Replace(":", "$"));
        }
        #endregion

        #region KVP
        public List<KvpExchangeDataItem> GetKVPItems(string vmId)
        {
            return GetKVPItems(vmId, "GuestExchangeItems");
        }

        public List<KvpExchangeDataItem> GetStandardKVPItems(string vmId)
        {
            return GetKVPItems(vmId, "GuestIntrinsicExchangeItems");
        }

        private List<KvpExchangeDataItem> GetKVPItems(string vmId, string exchangeItemsName)
        {
            List<KvpExchangeDataItem> pairs = new List<KvpExchangeDataItem>();

            // load VM
            ManagementObject objVm = GetVirtualMachineObject(vmId);

            ManagementObject objKvpExchange = null;

            try
            {
                objKvpExchange = wmi.GetRelatedWmiObject(objVm, "msvm_KvpExchangeComponent");
            }
            catch
            {
                // TODO
                // add logging...

                return pairs;
            }

            // return XML pairs
            string[] xmlPairs = (string[])objKvpExchange[exchangeItemsName];

            if (xmlPairs == null)
                return pairs;

            // join all pairs
            StringBuilder sb = new StringBuilder();
            sb.Append("<result>");
            foreach (string xmlPair in xmlPairs)
                sb.Append(xmlPair);
            sb.Append("</result>");

            // parse pairs
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(sb.ToString());

            foreach (XmlNode nodeName in doc.SelectNodes("/result/INSTANCE/PROPERTY[@NAME='Name']/VALUE"))
            {
                string name = nodeName.InnerText;
                string data = nodeName.ParentNode.ParentNode.SelectSingleNode("PROPERTY[@NAME='Data']/VALUE").InnerText;
                pairs.Add(new KvpExchangeDataItem(name, data));
            }

            return pairs;
        }

        public JobResult AddKVPItems(string vmId, KvpExchangeDataItem[] items)
        {
            // get KVP management object
            ManagementObject objVmsvc = GetVirtualSystemManagementService();

            // create KVP items array
            string[] wmiItems = new string[items.Length];

            for (int i = 0; i < items.Length; i++)
            {
                ManagementClass clsKvp = wmi.GetWmiClass("Msvm_KvpExchangeDataItem");
                ManagementObject objKvp = clsKvp.CreateInstance();
                objKvp["Name"] = items[i].Name;
                objKvp["Data"] = items[i].Data;
                objKvp["Source"] = 0;

                // convert to WMI format
                wmiItems[i] = objKvp.GetText(TextFormat.CimDtd20);
            }

            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("AddKvpItems");
            inParams["TargetSystem"] = GetVirtualMachineObject(vmId);
            inParams["DataItems"] = wmiItems;

            // invoke method
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("AddKvpItems", inParams, null);
            return CreateJobResultFromWmiMethodResults(outParams);
        }

        public JobResult RemoveKVPItems(string vmId, string[] itemNames)
        {
            // get KVP management object
            ManagementObject objVmsvc = GetVirtualSystemManagementService();

            // delete items one by one
            for (int i = 0; i < itemNames.Length; i++)
            {
                ManagementClass clsKvp = wmi.GetWmiClass("Msvm_KvpExchangeDataItem");
                ManagementObject objKvp = clsKvp.CreateInstance();
                objKvp["Name"] = itemNames[i];
                objKvp["Data"] = "";
                objKvp["Source"] = 0;

                // convert to WMI format
                string wmiItem = objKvp.GetText(TextFormat.CimDtd20);

                // call method
                ManagementBaseObject inParams = objVmsvc.GetMethodParameters("RemoveKvpItems");
                inParams["TargetSystem"] = GetVirtualMachineObject(vmId);
                inParams["DataItems"] = new string[] { wmiItem };

                // invoke method
                objVmsvc.InvokeMethod("RemoveKvpItems", inParams, null);
            }
            return null;
        }

        public JobResult ModifyKVPItems(string vmId, KvpExchangeDataItem[] items)
        {
            // get KVP management object
            ManagementObject objVmsvc = GetVirtualSystemManagementService();

            // create KVP items array
            string[] wmiItems = new string[items.Length];

            for (int i = 0; i < items.Length; i++)
            {
                ManagementClass clsKvp = wmi.GetWmiClass("Msvm_KvpExchangeDataItem");
                ManagementObject objKvp = clsKvp.CreateInstance();
                objKvp["Name"] = items[i].Name;
                objKvp["Data"] = items[i].Data;
                objKvp["Source"] = 0;

                // convert to WMI format
                wmiItems[i] = objKvp.GetText(TextFormat.CimDtd20);
            }

            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("ModifyKvpItems");
            inParams["TargetSystem"] = GetVirtualMachineObject(vmId);
            inParams["DataItems"] = wmiItems;

            // invoke method
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("ModifyKvpItems", inParams, null);
            return CreateJobResultFromWmiMethodResults(outParams);
        }
        #endregion

        #region Storage
        public VirtualHardDiskInfo GetVirtualHardDiskInfo(string vhdPath)
        {
            ManagementObject objImgSvc = GetImageManagementService();

            // get method params
            ManagementBaseObject inParams = objImgSvc.GetMethodParameters("GetVirtualHardDiskInfo");
            inParams["Path"] = FileUtils.EvaluateSystemVariables(vhdPath);

            // execute method
            ManagementBaseObject outParams = (ManagementBaseObject)objImgSvc.InvokeMethod("GetVirtualHardDiskInfo", inParams, null);
            ReturnCode result = (ReturnCode)Convert.ToInt32(outParams["ReturnValue"]);
            if (result == ReturnCode.OK)
            {
                // create XML
                string xml = (string)outParams["Info"];
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                // read properties
                VirtualHardDiskInfo vhd = new VirtualHardDiskInfo();
                vhd.DiskType = (VirtualHardDiskType)Enum.Parse(typeof(VirtualHardDiskType), GetPropertyValue("Type", doc), true);
                vhd.FileSize = Int64.Parse(GetPropertyValue("FileSize", doc));
                vhd.InSavedState = Boolean.Parse(GetPropertyValue("InSavedState", doc));
                vhd.InUse = Boolean.Parse(GetPropertyValue("InUse", doc));
                vhd.MaxInternalSize = Int64.Parse(GetPropertyValue("MaxInternalSize", doc));
                vhd.ParentPath = GetPropertyValue("ParentPath", doc);
                return vhd;
            }
            return null;
        }

        private string GetPropertyValue(string propertyName, XmlDocument doc)
        {
            string xpath = string.Format(@"//PROPERTY[@NAME = '{0}']/VALUE/child::text()", propertyName);
            XmlNode node = doc.SelectSingleNode(xpath);
            return node != null ? node.Value : null;
        }

        public MountedDiskInfo MountVirtualHardDisk(string vhdPath)
        {
            ManagementObject objImgSvc = GetImageManagementService();

            // get method params
            ManagementBaseObject inParams = objImgSvc.GetMethodParameters("Mount");
            inParams["Path"] = FileUtils.EvaluateSystemVariables(vhdPath);

            ManagementBaseObject outParams = (ManagementBaseObject)objImgSvc.InvokeMethod("Mount", inParams, null);
            JobResult result = CreateJobResultFromWmiMethodResults(outParams);

            // load storage job
            if (result.ReturnValue != ReturnCode.JobStarted)
                throw new Exception("Failed to start Mount job with the following error: " + result.ReturnValue); ;

            ManagementObject objJob = wmi.GetWmiObject("msvm_StorageJob", "InstanceID = '{0}'", result.Job.Id);

            if (!JobCompleted(result.Job))
                throw new Exception("Failed to complete Mount job with the following error: " + result.Job.ErrorDescription);

            try
            {
                List<string> volumes = new List<string>();

                // load output data
                ManagementObject objImage = wmi.GetRelatedWmiObject(objJob, "Msvm_MountedStorageImage");

                int pathId = Convert.ToInt32(objImage["PathId"]);
                int portNumber = Convert.ToInt32(objImage["PortNumber"]);
                int targetId = Convert.ToInt32(objImage["TargetId"]);
                int lun = Convert.ToInt32(objImage["Lun"]);

                string diskAddress = String.Format("Port{0}Path{1}Target{2}Lun{3}", portNumber, pathId, targetId, lun);

                HostedSolutionLog.LogInfo("Disk address: " + diskAddress);

                // find mounted disk using VDS
                Vds.Advanced.AdvancedDisk advancedDisk = null;
                Vds.Pack diskPack = null;

                // first attempt
                System.Threading.Thread.Sleep(3000);
                HostedSolutionLog.LogInfo("Trying to find mounted disk - first attempt");
                FindVdsDisk(diskAddress, out advancedDisk, out diskPack);

                // second attempt
                if (advancedDisk == null)
                {
                    System.Threading.Thread.Sleep(20000);
                    HostedSolutionLog.LogInfo("Trying to find mounted disk - second attempt");
                    FindVdsDisk(diskAddress, out advancedDisk, out diskPack);
                }

                if (advancedDisk == null)
                    throw new Exception("Could not find mounted disk");

                // check if DiskPart must be used to bring disk online and clear read-only flag
                bool useDiskPartToClearReadOnly = false;
                if (ConfigurationManager.AppSettings[CONFIG_USE_DISKPART_TO_CLEAR_READONLY_FLAG] != null)
                    useDiskPartToClearReadOnly = Boolean.Parse(ConfigurationManager.AppSettings[CONFIG_USE_DISKPART_TO_CLEAR_READONLY_FLAG]);

                // determine disk index for DiskPart
                Wmi cimv2 = new Wmi(ServerNameSettings, WMI_CIMV2_NAMESPACE);
                ManagementObject objDisk = cimv2.GetWmiObject("win32_diskdrive",
                    "Model='Msft Virtual Disk SCSI Disk Device' and ScsiTargetID={0} and ScsiLogicalUnit={1} and scsiPort={2}",
                    targetId, lun, portNumber);

                if (useDiskPartToClearReadOnly)
                {
                    // *** Clear Read-Only and bring disk online with DiskPart ***
                    HostedSolutionLog.LogInfo("Clearing disk Read-only flag and bringing disk online");

                    if (objDisk != null)
                    {
                        // disk found
                        // run DiskPart
                        string diskPartResult = RunDiskPart(String.Format(@"select disk {0}
attributes disk clear readonly
online disk
exit", Convert.ToInt32(objDisk["Index"])));

                        HostedSolutionLog.LogInfo("DiskPart Result: " + diskPartResult);
                    }
                }
                else
                {
                    // *** Clear Read-Only and bring disk online with VDS ***
                    // clear Read-Only
                    if ((advancedDisk.Flags & Vds.DiskFlags.ReadOnly) == Vds.DiskFlags.ReadOnly)
                    {
                        HostedSolutionLog.LogInfo("Clearing disk Read-only flag");
                        advancedDisk.ClearFlags(Vds.DiskFlags.ReadOnly);
                        while ((advancedDisk.Flags & Vds.DiskFlags.ReadOnly) == Vds.DiskFlags.ReadOnly)
                        {
                            System.Threading.Thread.Sleep(100);
                            advancedDisk.Refresh();
                        }
                    }

                    // bring disk ONLINE
                    if (advancedDisk.Status == Vds.DiskStatus.Offline)
                    {
                        HostedSolutionLog.LogInfo("Bringing disk online");
                        advancedDisk.Online();
                        while (advancedDisk.Status == Vds.DiskStatus.Offline)
                        {
                            System.Threading.Thread.Sleep(100);
                            advancedDisk.Refresh();
                        }
                    }
                }

                // small pause after getting disk online
                System.Threading.Thread.Sleep(3000);

                // get disk again
                FindVdsDisk(diskAddress, out advancedDisk, out diskPack);

                // find volumes using VDS
                HostedSolutionLog.LogInfo("Querying disk volumes with VDS");
                foreach (Vds.Volume volume in diskPack.Volumes)
                {
                    string letter = volume.DriveLetter.ToString();
                    if (letter != "")
                        volumes.Add(letter);
                }

                // find volumes using WMI
                if (volumes.Count == 0 && objDisk != null)
                {
                    HostedSolutionLog.LogInfo("Querying disk volumes with WMI");
                    foreach (ManagementObject objPartition in objDisk.GetRelated("Win32_DiskPartition"))
                    {
                        foreach (ManagementObject objVolume in objPartition.GetRelated("Win32_LogicalDisk"))
                        {
                            volumes.Add(objVolume["Name"].ToString().TrimEnd(':'));
                        }
                    }
                }

                HostedSolutionLog.LogInfo("Volumes found: " + volumes.Count);

                // info object
                MountedDiskInfo info = new MountedDiskInfo();
                info.DiskAddress = diskAddress;
                info.DiskVolumes = volumes.ToArray();
                return info;
            }
            catch (Exception ex)
            {
                // unmount disk
                UnmountVirtualHardDisk(vhdPath);

                // throw error
                throw ex;
            }
        }

        private void FindVdsDisk(string diskAddress, out Vds.Advanced.AdvancedDisk advancedDisk, out Vds.Pack diskPack)
        {
            advancedDisk = null;
            diskPack = null;

            Vds.ServiceLoader serviceLoader = new Vds.ServiceLoader();
            Vds.Service vds = serviceLoader.LoadService(ServerNameSettings);
            vds.WaitForServiceReady();

            foreach (Vds.Disk disk in vds.UnallocatedDisks)
            {
                if (disk.DiskAddress == diskAddress)
                {
                    advancedDisk = (Vds.Advanced.AdvancedDisk)disk;
                    break;
                }
            }

            if (advancedDisk == null)
            {
                vds.HardwareProvider = false;
                vds.SoftwareProvider = true;

                foreach (Vds.SoftwareProvider provider in vds.Providers)
                    foreach (Vds.Pack pack in provider.Packs)
                        foreach (Vds.Disk disk in pack.Disks)
                            if (disk.DiskAddress == diskAddress)
                            {
                                diskPack = pack;
                                advancedDisk = (Vds.Advanced.AdvancedDisk)disk;
                                break;
                            }
            }
        }

        public ReturnCode UnmountVirtualHardDisk(string vhdPath)
        {
            ManagementObject objImgSvc = GetImageManagementService();

            // get method params
            ManagementBaseObject inParams = objImgSvc.GetMethodParameters("Unmount");
            inParams["Path"] = FileUtils.EvaluateSystemVariables(vhdPath);

            ManagementBaseObject outParams = (ManagementBaseObject)objImgSvc.InvokeMethod("Unmount", inParams, null);
            return (ReturnCode)Convert.ToInt32(outParams["ReturnValue"]);
        }

        public JobResult ExpandVirtualHardDisk(string vhdPath, UInt64 sizeGB)
        {
            const UInt64 Size1G = 0x40000000;

            ManagementObject objImgSvc = GetImageManagementService();

            // get method params
            ManagementBaseObject inParams = objImgSvc.GetMethodParameters("ExpandVirtualHardDisk");
            inParams["Path"] = FileUtils.EvaluateSystemVariables(vhdPath);
            inParams["MaxInternalSize"] = sizeGB * Size1G;

            ManagementBaseObject outParams = (ManagementBaseObject)objImgSvc.InvokeMethod("ExpandVirtualHardDisk", inParams, null);
            return CreateJobResultFromWmiMethodResults(outParams);
        }

        public JobResult ConvertVirtualHardDisk(string sourcePath, string destinationPath, VirtualHardDiskType diskType)
        {
            sourcePath = FileUtils.EvaluateSystemVariables(sourcePath);
            destinationPath = FileUtils.EvaluateSystemVariables(destinationPath);

            // check source file
            if (!FileExists(sourcePath))
                throw new Exception("Source VHD cannot be found: " + sourcePath);

            // check destination folder
            string destFolder = Path.GetDirectoryName(destinationPath);
            if (!DirectoryExists(destFolder))
                CreateFolder(destFolder);

            ManagementObject objImgSvc = GetImageManagementService();

            // get method params
            ManagementBaseObject inParams = objImgSvc.GetMethodParameters("ConvertVirtualHardDisk");
            inParams["SourcePath"] = sourcePath;
            inParams["DestinationPath"] = destinationPath;
            inParams["Type"] = (UInt16)diskType;

            ManagementBaseObject outParams = (ManagementBaseObject)objImgSvc.InvokeMethod("ConvertVirtualHardDisk", inParams, null);
            return CreateJobResultFromWmiMethodResults(outParams);
        }

        public void DeleteRemoteFile(string path)
        {
            if (DirectoryExists(path))
                DeleteFolder(path); // WMI way
            else if (FileExists(path))
                DeleteFile(path); // WMI way
        }

        public void ExpandDiskVolume(string diskAddress, string volumeName)
        {
            // find mounted disk using VDS
            Vds.Advanced.AdvancedDisk advancedDisk = null;
            Vds.Pack diskPack = null;

            FindVdsDisk(diskAddress, out advancedDisk, out diskPack);

            if (advancedDisk == null)
                throw new Exception("Could not find mounted disk");

            // find volume
            Vds.Volume diskVolume = null;
            foreach (Vds.Volume volume in diskPack.Volumes)
            {
                if (volume.DriveLetter.ToString() == volumeName)
                {
                    diskVolume = volume;
                    break;
                }
            }

            if (diskVolume == null)
                throw new Exception("Could not find disk volume: " + volumeName);

            // determine maximum available space
            ulong oneMegabyte = 1048576;
            ulong freeSpace = 0;
            foreach (Vds.DiskExtent extent in advancedDisk.Extents)
            {
                if (extent.Type != Microsoft.Storage.Vds.DiskExtentType.Free)
                    continue;

                if (extent.Size > oneMegabyte)
                    freeSpace += extent.Size;
            }

            if (freeSpace == 0)
                return;

            // input disk
            Vds.InputDisk inputDisk = new Vds.InputDisk();
            foreach (Vds.VolumePlex plex in diskVolume.Plexes)
            {
                inputDisk.DiskId = advancedDisk.Id;
                inputDisk.Size = freeSpace;
                inputDisk.PlexId = plex.Id;

                foreach (Vds.DiskExtent extent in plex.Extents)
                    inputDisk.MemberIndex = extent.MemberIndex;

                break;
            }

            // extend volume
            Vds.Async extendEvent = diskVolume.BeginExtend(new Vds.InputDisk[] { inputDisk }, null, null);
            while (!extendEvent.IsCompleted)
                System.Threading.Thread.Sleep(100);
            diskVolume.EndExtend(extendEvent);
        }

        // obsolete and currently is not used
        private string RunDiskPart(string script)
        {
            // create temp script file name
            string localPath = Path.Combine(GetTempRemoteFolder(), Guid.NewGuid().ToString("N"));

            // save script to remote temp file
            string remotePath = ConvertToUNC(localPath);
            File.AppendAllText(remotePath, script);

            // run diskpart
            ExecuteRemoteProcess("DiskPart /s " + localPath);

            // delete temp script
            try
            {
                File.Delete(remotePath);
            }
            catch
            {
                // TODO
            }

            return "";
        }

        public string ReadRemoteFile(string path)
        {
            // temp file name on "system" drive available through hidden share
            string tempPath = Path.Combine(GetTempRemoteFolder(), Guid.NewGuid().ToString("N"));

            HostedSolutionLog.LogInfo("Read remote file: " + path);
            HostedSolutionLog.LogInfo("Local file temp path: " + tempPath);

            // copy remote file to temp file (WMI)
            if (!CopyFile(path, tempPath))
                return null;

            // read content of temp file
            string remoteTempPath = ConvertToUNC(tempPath);
            HostedSolutionLog.LogInfo("Remote file temp path: " + remoteTempPath);

            string content = File.ReadAllText(remoteTempPath);

            // delete temp file (WMI)
            DeleteFile(tempPath);

            return content;
        }

        public void WriteRemoteFile(string path, string content)
        {
            // temp file name on "system" drive available through hidden share
            string tempPath = Path.Combine(GetTempRemoteFolder(), Guid.NewGuid().ToString("N"));

            // write to temp file
            string remoteTempPath = ConvertToUNC(tempPath);
            File.WriteAllText(remoteTempPath, content);

            // delete file (WMI)
            if (FileExists(path))
                DeleteFile(path);

            // copy (WMI)
            CopyFile(tempPath, path);

            // delete temp file (WMI)
            DeleteFile(tempPath);
        }
        #endregion

        #region Jobs
        public ConcreteJob GetJob(string jobId)
        {
            HostedSolutionLog.LogStart("GetJob");
            HostedSolutionLog.DebugInfo("jobId: {0}", jobId);

            Runspace runSpace = null;
            ConcreteJob job;

            try
            {
                runSpace = OpenRunspace();
                Command cmd = new Command("Get-Job");

                if (!string.IsNullOrEmpty(jobId)) cmd.Parameters.Add("Id", jobId);

                Collection<PSObject> result = ExecuteShellCommand(runSpace, cmd, false);
                job = CreateJobFromPSObject(result);
            }
            catch (Exception ex)
            {
                HostedSolutionLog.LogError("GetJob", ex);
                throw;
            }
            finally
            {
                CloseRunspace(runSpace);
            }

            HostedSolutionLog.LogEnd("GetJob");
            return job;
        }

        public List<ConcreteJob> GetAllJobs()
        {
            List<ConcreteJob> jobs = new List<ConcreteJob>();

            ManagementObjectCollection objJobs = wmi.GetWmiObjects("CIM_ConcreteJob");
            foreach (ManagementObject objJob in objJobs)
                jobs.Add(CreateJobFromWmiObject(objJob));

            return jobs;
        }

        public ChangeJobStateReturnCode ChangeJobState(string jobId, ConcreteJobRequestedState newState)
        {
            ManagementObject objJob = GetJobWmiObject(jobId);

            // get method
            ManagementBaseObject inParams = objJob.GetMethodParameters("RequestStateChange");
            inParams["RequestedState"] = (Int32)newState;

            // invoke method
            ManagementBaseObject outParams = objJob.InvokeMethod("RequestStateChange", inParams, null);
            return (ChangeJobStateReturnCode)Convert.ToInt32(outParams["ReturnValue"]);
        }

        #endregion

        #region Configuration
        public int GetProcessorCoresNumber()
        {
            Wmi w = new Wmi(ServerNameSettings, @"root\cimv2");
            ManagementObject objCpu = w.GetWmiObject("win32_Processor");
            return Convert.ToInt32(objCpu["NumberOfCores"]);
        }
        #endregion

        #region IHostingServiceProvier methods
        public override string[] Install()
        {
            List<string> messages = new List<string>();

            // TODO

            return messages.ToArray();
        }

        public override bool IsInstalled()
        {
            // check if Hyper-V role is installed and available for management
            //Wmi root = new Wmi(ServerNameSettings, "root");
            //ManagementObject objNamespace = root.GetWmiObject("__NAMESPACE", "name = 'virtualization'");
            //return (objNamespace != null);
            return true;
        }

        public override void ChangeServiceItemsState(ServiceProviderItem[] items, bool enabled)
        {
            foreach (ServiceProviderItem item in items)
            {
                if (item is VirtualMachine)
                {
                    // start/stop virtual machine
                    VirtualMachine vm = item as VirtualMachine;
                    ChangeVirtualMachineServiceItemState(vm, enabled);
                }
            }
        }

        public override void DeleteServiceItems(ServiceProviderItem[] items)
        {
            foreach (ServiceProviderItem item in items)
            {
                if (item is VirtualMachine)
                {
                    // delete virtual machine
                    VirtualMachine vm = item as VirtualMachine;
                    DeleteVirtualMachineServiceItem(vm);
                }
                else if (item is VirtualSwitch)
                {
                    // delete switch
                    VirtualSwitch vs = item as VirtualSwitch;
                    DeleteVirtualSwitchServiceItem(vs);
                }
            }
        }

        private void ChangeVirtualMachineServiceItemState(VirtualMachine vm, bool started)
        {
            try
            {
                VirtualMachine vps = GetVirtualMachine(vm.VirtualMachineId);
                JobResult result = null;

                if (vps == null)
                {
                    HostedSolutionLog.LogWarning(String.Format("Virtual machine '{0}' object with ID '{1}' was not found. Change state operation aborted.",
                        vm.Name, vm.VirtualMachineId));
                    return;
                }

                #region Start
                if (started &&
                    (vps.State == VirtualMachineState.Off
                    || vps.State == VirtualMachineState.Paused
                    || vps.State == VirtualMachineState.Saved))
                {
                    VirtualMachineRequestedState state = VirtualMachineRequestedState.Start;
                    if (vps.State == VirtualMachineState.Paused)
                        state = VirtualMachineRequestedState.Resume;

                    result = ChangeVirtualMachineState(vm.VirtualMachineId, state);

                    // check result
                    if (result.ReturnValue != ReturnCode.JobStarted)
                    {
                        HostedSolutionLog.LogWarning(String.Format("Cannot {0} '{1}' virtual machine: {2}",
                            state, vm.Name, result.ReturnValue));
                        return;
                    }

                    // wait for completion
                    if (!JobCompleted(result.Job))
                    {
                        HostedSolutionLog.LogWarning(String.Format("Cannot complete {0} '{1}' of virtual machine: {1}",
                            state, vm.Name, result.Job.ErrorDescription));
                        return;
                    }
                }
                #endregion

                #region Stop
                else if (!started &&
                    (vps.State == VirtualMachineState.Running
                    || vps.State == VirtualMachineState.Paused))
                {
                    if (vps.State == VirtualMachineState.Running)
                    {
                        // try to shutdown the system
                        ReturnCode code = ShutDownVirtualMachine(vm.VirtualMachineId, true, "Virtual Machine has been suspended from WebsitePanel");
                        if (code == ReturnCode.OK)
                            return;
                    }

                    // turn off
                    VirtualMachineRequestedState state = VirtualMachineRequestedState.TurnOff;
                    result = ChangeVirtualMachineState(vm.VirtualMachineId, state);

                    // check result
                    if (result.ReturnValue != ReturnCode.JobStarted)
                    {
                        HostedSolutionLog.LogWarning(String.Format("Cannot {0} '{1}' virtual machine: {2}",
                            state, vm.Name, result.ReturnValue));
                        return;
                    }

                    // wait for completion
                    if (!JobCompleted(result.Job))
                    {
                        HostedSolutionLog.LogWarning(String.Format("Cannot complete {0} '{1}' of virtual machine: {1}",
                            state, vm.Name, result.Job.ErrorDescription));
                        return;
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                HostedSolutionLog.LogError(String.Format("Error {0} Virtual Machine '{1}'",
                    started ? "starting" : "turning off",
                    vm.Name), ex);
            }
        }

        private void DeleteVirtualMachineServiceItem(VirtualMachine vm)
        {
            try
            {
                JobResult result = null;
                VirtualMachine vps = GetVirtualMachine(vm.VirtualMachineId);

                if (vps == null)
                {
                    HostedSolutionLog.LogWarning(String.Format("Virtual machine '{0}' object with ID '{1}' was not found. Delete operation aborted.",
                        vm.Name, vm.VirtualMachineId));
                    return;
                }

                #region Turn off (if required)
                if (vps.State != VirtualMachineState.Off)
                {
                    result = ChangeVirtualMachineState(vm.VirtualMachineId, VirtualMachineRequestedState.TurnOff);
                    // check result
                    if (result.ReturnValue != ReturnCode.JobStarted)
                    {
                        HostedSolutionLog.LogWarning(String.Format("Cannot Turn off '{0}' virtual machine before deletion: {1}",
                            vm.Name, result.ReturnValue));
                        return;
                    }

                    // wait for completion
                    if (!JobCompleted(result.Job))
                    {
                        HostedSolutionLog.LogWarning(String.Format("Cannot complete Turn off '{0}' of virtual machine before deletion: {1}",
                            vm.Name, result.Job.ErrorDescription));
                        return;
                    }
                }
                #endregion

                #region Delete virtual machine
                result = DeleteVirtualMachine(vm.VirtualMachineId);

                // check result
                if (result.ReturnValue != ReturnCode.JobStarted)
                {
                    HostedSolutionLog.LogWarning(String.Format("Cannot delete '{0}' virtual machine: {1}",
                        vm.Name, result.ReturnValue));
                    return;
                }

                // wait for completion
                if (!JobCompleted(result.Job))
                {
                    HostedSolutionLog.LogWarning(String.Format("Cannot complete deletion of '{0}' virtual machine: {1}",
                        vm.Name, result.Job.ErrorDescription));
                    return;
                }
                #endregion

                #region Delete virtual machine
                try
                {
                    DeleteFile(vm.RootFolderPath);
                }
                catch (Exception ex)
                {
                    HostedSolutionLog.LogError(String.Format("Cannot delete virtual machine folder '{0}'",
                        vm.RootFolderPath), ex);
                }
                #endregion

            }
            catch (Exception ex)
            {
                HostedSolutionLog.LogError(String.Format("Error deleting Virtual Machine '{0}'", vm.Name), ex);
            }
        }

        private void DeleteVirtualSwitchServiceItem(VirtualSwitch vs)
        {
            try
            {
                // delete virtual switch
                DeleteSwitch(vs.SwitchId);
            }
            catch (Exception ex)
            {
                HostedSolutionLog.LogError(String.Format("Error deleting Virtual Switch '{0}'", vs.Name), ex);
            }
        }
        #endregion

        #region Private Methods
        protected JobResult CreateSuccessJobResult()
        {
            JobResult result = new JobResult();

            result.Job = new ConcreteJob(){JobState = ConcreteJobState.Completed};
            result.ReturnValue = ReturnCode.OK;

            return result;
        }
        protected JobResult CreateJobResultFromPSResults(Collection<PSObject> objJob)
        {
            if (objJob == null || objJob.Count == 0)
                return null;
            
            JobResult result = new JobResult();

            result.Job = CreateJobFromPSObject(objJob);
            
            result.ReturnValue = ReturnCode.JobStarted;
            switch (result.Job.JobState)
            {
                case ConcreteJobState.Failed:
                    result.ReturnValue = ReturnCode.Failed;
                    break;
            }

            return result;
        }

        protected JobResult CreateJobResultFromWmiMethodResults(ManagementBaseObject outParams)
        {
            JobResult result = new JobResult();

            // return value
            result.ReturnValue = (ReturnCode)Convert.ToInt32(outParams["ReturnValue"]);

            // try getting job details job
            try
            {
                ManagementBaseObject objJob = wmi.GetWmiObjectByPath((string)outParams["Job"]);
                if (objJob != null && objJob.Properties.Count > 0)
                {
                    result.Job = CreateJobFromWmiObject(objJob);
                }
            }
            catch { /* dumb */ }

            return result;
        }

        private ManagementObject GetJobWmiObject(string id)
        {
            return wmi.GetWmiObject("msvm_ConcreteJob", "InstanceID = '{0}'", id);
        }

        private ManagementObject GetVirtualSystemManagementService()
        {
            return wmi.GetWmiObject("msvm_VirtualSystemManagementService");
        }

        private ManagementObject GetVirtualSwitchManagementService()
        {
            return wmi.GetWmiObject("msvm_VirtualSwitchManagementService");
        }

        protected ManagementObject GetImageManagementService()
        {
            return wmi.GetWmiObject("msvm_ImageManagementService");
        }

        private ManagementObject GetVirtualMachineObject(string vmId)
        {
            return wmi.GetWmiObject("msvm_ComputerSystem", "Name = '{0}'", vmId);
        }

        private ManagementObject GetSnapshotObject(string snapshotId)
        {
            return wmi.GetWmiObject("Msvm_VirtualSystemSettingData", "InstanceID = '{0}'", snapshotId);
        }


        private VirtualMachineSnapshot CreateSnapshotFromWmiObject(ManagementBaseObject objSnapshot)
        {
            if (objSnapshot == null || objSnapshot.Properties.Count == 0)
                return null;

            VirtualMachineSnapshot snapshot = new VirtualMachineSnapshot();
            snapshot.Id = (string)objSnapshot["InstanceID"];
            snapshot.Name = (string)objSnapshot["ElementName"];

            string parentId = (string)objSnapshot["Parent"];
            if (!String.IsNullOrEmpty(parentId))
            {
                int idx = parentId.IndexOf("Microsoft:");
                snapshot.ParentId = parentId.Substring(idx, parentId.Length - idx - 1);
            }
            snapshot.Created = wmi.ToDateTime((string)objSnapshot["CreationTime"]);

            return snapshot;
        }

        private VirtualSwitch CreateSwitchFromWmiObject(ManagementObject objSwitch)
        {
            if (objSwitch == null || objSwitch.Properties.Count == 0)
                return null;

            VirtualSwitch sw = new VirtualSwitch();
            sw.SwitchId = (string)objSwitch["Name"];
            sw.Name = (string)objSwitch["ElementName"];
            return sw;
        }

        private ConcreteJob CreateJobFromPSObject(Collection<PSObject> objJob)
        {
            if (objJob == null || objJob.Count == 0)
                return null;

            ConcreteJob job = new ConcreteJob();
            job.Id = GetPSObjectProperty<int>(objJob[0], "Id").ToString();
            job.JobState = GetPSObjectPropertyEnum<ConcreteJobState>(objJob[0], "JobStateInfo");
            job.Caption = GetPSObjectProperty<string>(objJob[0], "Name");
            job.Description = GetPSObjectProperty<string>(objJob[0], "Command");
            job.StartTime = GetPSObjectProperty<DateTime>(objJob[0], "PSBeginTime");
            job.ElapsedTime = GetPSObjectProperty<DateTime?>(objJob[0], "PSEndTime") ?? DateTime.Now;

            // PercentComplete
            job.PercentComplete = 0;
            var progress = (PSDataCollection<ProgressRecord>)GetPSObjectProperty(objJob[0], "Progress");
            if (progress != null && progress.Count > 0)
                job.PercentComplete = progress[0].PercentComplete;
            
            // Errors
            var errors = (PSDataCollection<ErrorRecord>)GetPSObjectProperty(objJob[0], "Error");
            if (errors != null && errors.Count > 0)
            {
                job.ErrorDescription = errors[0].ErrorDetails.Message + ". " + errors[0].ErrorDetails.RecommendedAction;
                job.ErrorCode = errors[0].Exception != null ? -1 : 0;
            }

            return job;
        }
        
        private ConcreteJob CreateJobFromWmiObject(ManagementBaseObject objJob)
        {
            if (objJob == null || objJob.Properties.Count == 0)
                return null;

            ConcreteJob job = new ConcreteJob();
            job.Id = (string)objJob["InstanceID"];
            job.JobState = (ConcreteJobState)Convert.ToInt32(objJob["JobState"]);
            job.Caption = (string)objJob["Caption"];
            job.Description = (string)objJob["Description"];
            job.StartTime = wmi.ToDateTime((string)objJob["StartTime"]);
            // TODO proper parsing of WMI time spans, e.g. 00000000000001.325247:000
            job.ElapsedTime = DateTime.Now; //wmi.ToDateTime((string)objJob["ElapsedTime"]);
            job.ErrorCode = Convert.ToInt32(objJob["ErrorCode"]);
            job.ErrorDescription = (string)objJob["ErrorDescription"];
            job.PercentComplete = Convert.ToInt32(objJob["PercentComplete"]);
            return job;
        }

        private ManagementBaseObject GetSnapshotSummaryInformation(
            string snapshotId,
            SummaryInformationRequest requestedInformation)
        {
            // find VM settings object
            ManagementObject objVmSetting = GetSnapshotObject(snapshotId);

            // get summary
            return GetSummaryInformation(objVmSetting, requestedInformation);
        }

        private ManagementBaseObject GetVirtualMachineSummaryInformation(
            string vmId,
            params SummaryInformationRequest[] requestedInformation)
        {
            // find VM settings object
            ManagementObject objVmSetting = GetVirtualMachineSettingsObject(vmId);

            // get summary
            return GetSummaryInformation(objVmSetting, requestedInformation);
        }

        private ManagementBaseObject GetSummaryInformation(
            ManagementObject objVmSetting, params SummaryInformationRequest[] requestedInformation)
        {
            if (requestedInformation == null || requestedInformation.Length == 0)
                throw new ArgumentNullException("requestedInformation");

            // get management service
            ManagementObject objVmsvc = GetVirtualSystemManagementService();

            uint[] reqif = new uint[requestedInformation.Length];
            for (int i = 0; i < requestedInformation.Length; i++)
                reqif[i] = (uint)requestedInformation[i];

            // get method params
            ManagementBaseObject inParams = objVmsvc.GetMethodParameters("GetSummaryInformation");
            inParams["SettingData"] = new ManagementObject[] { objVmSetting };
            inParams["RequestedInformation"] = reqif;

            // invoke method
            ManagementBaseObject outParams = objVmsvc.InvokeMethod("GetSummaryInformation", inParams, null);
            return ((ManagementBaseObject[])outParams["SummaryInformation"])[0];
        }

        private ManagementObject GetVirtualMachineSettingsObject(string vmId)
        {
            return wmi.GetWmiObject("msvm_VirtualSystemSettingData", "InstanceID Like 'Microsoft:{0}%'", vmId);
        }

        private bool JobCompleted(ConcreteJob job)
        {
            bool jobCompleted = true;

            while (job.JobState == ConcreteJobState.Starting ||
                job.JobState == ConcreteJobState.Running)
            {
                System.Threading.Thread.Sleep(200);
                job = GetJob(job.Id);
            }

            if (job.JobState != ConcreteJobState.Completed)
            {
                jobCompleted = false;
            }

            return jobCompleted;
        }
        #endregion

        #region Remote File Methods
        public bool FileExists(string path)
        {
            HostedSolutionLog.LogInfo("Check remote file exists: " + path);

            if (path.StartsWith(@"\\")) // network share
                return File.Exists(path);
            else
            {
                Wmi cimv2 = new Wmi(ServerNameSettings, WMI_CIMV2_NAMESPACE);
                ManagementObject objFile = cimv2.GetWmiObject("CIM_Datafile", "Name='{0}'", path.Replace("\\", "\\\\"));
                return (objFile != null);
            }
        }

        public bool DirectoryExists(string path)
        {
            if (path.StartsWith(@"\\")) // network share
                return Directory.Exists(path);
            else
            {
                Wmi cimv2 = new Wmi(ServerNameSettings, WMI_CIMV2_NAMESPACE);
                ManagementObject objDir = cimv2.GetWmiObject("Win32_Directory", "Name='{0}'", path.Replace("\\", "\\\\"));
                return (objDir != null);
            }
        }

        public bool CopyFile(string sourceFileName, string destinationFileName)
        {
            HostedSolutionLog.LogInfo("Copy file - source: " + sourceFileName);
            HostedSolutionLog.LogInfo("Copy file - destination: " + destinationFileName);

            if (sourceFileName.StartsWith(@"\\")) // network share
            {
                if (!File.Exists(sourceFileName))
                    return false;

                File.Copy(sourceFileName, destinationFileName);
            }
            else
            {
                if (!FileExists(sourceFileName))
                    return false;

                // copy using WMI
                Wmi cimv2 = new Wmi(ServerNameSettings, WMI_CIMV2_NAMESPACE);
                ManagementObject objFile = cimv2.GetWmiObject("CIM_Datafile", "Name='{0}'", sourceFileName.Replace("\\", "\\\\"));
                if (objFile == null)
                    throw new Exception("Source file does not exists: " + sourceFileName);

                objFile.InvokeMethod("Copy", new object[] { destinationFileName });
            }
            return true;
        }

        public void DeleteFile(string path)
        {
            if (path.StartsWith(@"\\"))
            {
                // network share
                File.Delete(path);
            }
            else
            {
                // delete file using WMI
                Wmi cimv2 = new Wmi(ServerNameSettings, "root\\cimv2");
                ManagementObject objFile = cimv2.GetWmiObject("CIM_Datafile", "Name='{0}'", path.Replace("\\", "\\\\"));
                objFile.InvokeMethod("Delete", null);
            }
        }

        public void DeleteFolder(string path)
        {
            if (path.StartsWith(@"\\"))
            {
                // network share
                try
                {
                    FileUtils.DeleteFile(path);
                }
                catch { /* just skip */ }
                FileUtils.DeleteFile(path);
            }
            else
            {
                // local folder
                // delete sub folders first
                ManagementObjectCollection objSubFolders = GetSubFolders(path);
                foreach (ManagementObject objSubFolder in objSubFolders)
                    DeleteFolder(objSubFolder["Name"].ToString());

                // delete this folder itself
                Wmi cimv2 = new Wmi(ServerNameSettings, "root\\cimv2");
                ManagementObject objFolder = cimv2.GetWmiObject("Win32_Directory", "Name='{0}'", path.Replace("\\", "\\\\"));
                objFolder.InvokeMethod("Delete", null);
            }
        }

        private ManagementObjectCollection GetSubFolders(string path)
        {
            if (path.EndsWith("\\"))
                path = path.Substring(0, path.Length - 1);

            Wmi cimv2 = new Wmi(ServerNameSettings, "root\\cimv2");

            return cimv2.ExecuteWmiQuery("Associators of {Win32_Directory.Name='"
                + path + "'} "
                + "Where AssocClass = Win32_Subdirectory "
                + "ResultRole = PartComponent");
        }

        public void CreateFolder(string path)
        {
            ExecuteRemoteProcess(String.Format("cmd.exe /c md \"{0}\"", path));
        }

        public void ExecuteRemoteProcess(string command)
        {
            Wmi cimv2 = new Wmi(ServerNameSettings, "root\\cimv2");
            ManagementClass objProcess = cimv2.GetWmiClass("Win32_Process");

            // run process
            object[] methodArgs = { command, null, null, 0 };
            objProcess.InvokeMethod("Create", methodArgs);

            // process ID
            int processId = Convert.ToInt32(methodArgs[3]);

            // wait until finished
            // Create event query to be notified within 1 second of 
            // a change in a service
            WqlEventQuery query =
                new WqlEventQuery("__InstanceDeletionEvent",
                new TimeSpan(0, 0, 1),
                "TargetInstance isa \"Win32_Process\"");

            // Initialize an event watcher and subscribe to events 
            // that match this query
            ManagementEventWatcher watcher = new ManagementEventWatcher(cimv2.GetScope(), query);
            // times out watcher.WaitForNextEvent in 20 seconds
            watcher.Options.Timeout = new TimeSpan(0, 0, 20);

            // Block until the next event occurs 
            // Note: this can be done in a loop if waiting for 
            //        more than one occurrence
            while (true)
            {
                ManagementBaseObject e = null;

                try
                {
                    // wait untill next process finish
                    e = watcher.WaitForNextEvent();
                }
                catch
                {
                    // nothing has been finished in timeout period
                    return; // exit
                }

                // check process id
                int pid = Convert.ToInt32(((ManagementBaseObject)e["TargetInstance"])["ProcessID"]);
                if (pid == processId)
                {
                    //Cancel the subscription
                    watcher.Stop();

                    // exit
                    return;
                }
            }
        }

        public string GetTempRemoteFolder()
        {
            Wmi cimv2 = new Wmi(ServerNameSettings, "root\\cimv2");
            ManagementObject objOS = cimv2.GetWmiObject("win32_OperatingSystem");
            string sysPath = (string)objOS["SystemDirectory"];

            // remove trailing slash
            if (sysPath.EndsWith("\\"))
                sysPath = sysPath.Substring(0, sysPath.Length - 1);

            sysPath = sysPath.Substring(0, sysPath.LastIndexOf("\\") + 1) + "Temp";

            return sysPath;
        }
        #endregion

        #region Hyper-V Cloud
        public bool CheckServerState(string connString)
        {
            return !String.IsNullOrEmpty(connString);
        }
        #endregion Hyper-V Cloud

        #region PowerShell integration
        private static InitialSessionState session = null;

        internal virtual Runspace OpenRunspace()
        {
            HostedSolutionLog.LogStart("OpenRunspace");

            if (session == null)
            {
                session = InitialSessionState.CreateDefault();
                session.ImportPSModule(new string[] { "Hyper-V" });
            }
            Runspace runSpace = RunspaceFactory.CreateRunspace(session);
            //
            runSpace.Open();
            //
            runSpace.SessionStateProxy.SetVariable("ConfirmPreference", "none");
            HostedSolutionLog.LogEnd("OpenRunspace");
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
                HostedSolutionLog.LogError("Runspace error", ex);
            }
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

        internal Collection<PSObject> ExecuteShellCommand(Runspace runSpace, Command cmd, bool useDomainController, out object[] errors)
        {
            HostedSolutionLog.LogStart("ExecuteShellCommand");
            List<object> errorList = new List<object>();
                
            HostedSolutionLog.DebugCommand(cmd);
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
                        HostedSolutionLog.LogWarning(errorMessage);
                    }
                }
            }
            pipeLine = null;
            errors = errorList.ToArray();
            HostedSolutionLog.LogEnd("ExecuteShellCommand");
            return results;
        }

        internal object GetPSObjectProperty(PSObject obj, string name)
        {
            return obj.Members[name].Value;
        }
        internal T GetPSObjectProperty<T>(PSObject obj, string name)
        {
            return (T)obj.Members[name].Value;
        }
        internal T GetPSObjectPropertyEnum<T>(PSObject obj, string name) where T : struct 
        {
            return (T) Enum.Parse(typeof (T), GetPSObjectProperty(obj, name).ToString());
        }

        /// <summary>
        /// Returns the identity of the object from the shell execution result
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        internal string GetResultObjectIdentity(Collection<PSObject> result)
        {
            HostedSolutionLog.LogStart("GetResultObjectIdentity");
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
            HostedSolutionLog.LogEnd("GetResultObjectIdentity");
            return ret;
        }

        internal string GetResultObjectDN(Collection<PSObject> result)
        {
            HostedSolutionLog.LogStart("GetResultObjectDN");
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
            HostedSolutionLog.LogEnd("GetResultObjectDN");
            return ret;
        }
        #endregion


        internal int ConvertNullableToInt32(object value)
        {
            return value == null ? 0 : Convert.ToInt32(value);
        }

        internal long ConvertNullableToInt64(object value) 
        {
            return value == null ? 0 : Convert.ToInt64(value);
        }

    }
}
