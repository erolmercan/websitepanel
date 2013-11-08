﻿// Copyright (c) 2012, Outercurve Foundation.
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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

using WebsitePanel.Server.Utils;
using WebsitePanel.Providers.Utils;
using WebsitePanel.Providers.OS;
using WebsitePanel.Providers.Web;

namespace WebsitePanel.Providers.EnterpriseStorage
{
    public class Windows2012 : HostingServiceProviderBase, IEnterpriseStorage
    {
        #region Properties

        protected string UsersHome
        {
            get { return FileUtils.EvaluateSystemVariables(ProviderSettings["UsersHome"]); }
        }

        protected string LocationDrive
        {
            get { return FileUtils.EvaluateSystemVariables(ProviderSettings["LocationDrive"]); }
        }

        protected string UsersDomain
        {
            get { return FileUtils.EvaluateSystemVariables(ProviderSettings["UsersDomain"]); }
        }

        #endregion

        #region Folders
        public SystemFile[] GetFolders(string organizationId)
        {
            string rootPath = string.Format("{0}:\\{1}\\{2}", LocationDrive, UsersHome, organizationId);
            
            DirectoryInfo root = new DirectoryInfo(rootPath);
            IWebDav webdav = new Web.WebDav(UsersDomain);

            ArrayList items = new ArrayList();

            // get directories
            DirectoryInfo[] dirs = root.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                string fullName = System.IO.Path.Combine(rootPath, dir.Name);

                SystemFile folder = new SystemFile(dir.Name, fullName, true, 
                    FileUtils.BytesToMb(FileUtils.CalculateFolderSize(dir.FullName)), dir.CreationTime, dir.LastWriteTime);

                folder.Url = string.Format("https://{0}/{1}/{2}", UsersDomain, organizationId, dir.Name);
                folder.Rules = webdav.GetFolderWebDavRules(organizationId, dir.Name);

                items.Add(folder);

                // check if the directory is empty
                folder.IsEmpty = (Directory.GetFileSystemEntries(fullName).Length == 0);
            }

            return (SystemFile[])items.ToArray(typeof(SystemFile));
        }

        public SystemFile GetFolder(string organizationId, string folderName)
        {
            string fullName = string.Format("{0}:\\{1}\\{2}\\{3}", LocationDrive, UsersHome, organizationId, folderName);

            DirectoryInfo root = new DirectoryInfo(fullName);
            
            SystemFile folder = new SystemFile(root.Name, fullName, true,
                FileUtils.BytesToMb( FileUtils.CalculateFolderSize(root.FullName)), root.CreationTime, root.LastWriteTime);

            folder.Url = string.Format("https://{0}/{1}/{2}", UsersDomain, organizationId, folderName);
            folder.Rules = GetFolderWebDavRules(organizationId, folderName);

            return folder;
        }

        public void CreateFolder(string organizationId, string folder)
        {
            FileUtils.CreateDirectory(string.Format("{0}:\\{1}\\{2}\\{3}", LocationDrive, UsersHome, organizationId, folder));
        }

        public SystemFile RenameFolder(string organizationId, string originalFolder, string newFolder)
        {
            var oldPath = string.Format("{0}:\\{1}\\{2}\\{3}", LocationDrive, UsersHome, organizationId, originalFolder);
            var newPath = string.Format("{0}:\\{1}\\{2}\\{3}", LocationDrive, UsersHome, organizationId, newFolder);

            FileUtils.MoveFile(oldPath,newPath);

            IWebDav webdav = new WebDav(UsersDomain);

            //deleting old folder rules
            webdav.DeleteAllWebDavRules(organizationId, originalFolder);

            return GetFolder(organizationId, newFolder);
        }

        public void DeleteFolder(string organizationId, string folder)
        {
            string rootPath = string.Format("{0}:\\{1}\\{2}\\{3}", LocationDrive, UsersHome, organizationId, folder);

            DirectoryInfo treeRoot = new DirectoryInfo(rootPath);
            
            if (treeRoot.Exists)
            {
                DirectoryInfo[] dirs = treeRoot.GetDirectories();
                while (dirs.Length > 0)
                {
                    foreach (DirectoryInfo dir in dirs)
                        DeleteFolder(organizationId, folder != string.Empty ? string.Format("{0}\\{1}", folder, dir.Name) : dir.Name);

                    dirs = treeRoot.GetDirectories();
                }

                // DELETE THE FILES UNDER THE CURRENT ROOT
                string[] files = Directory.GetFiles(treeRoot.FullName);
                foreach (string file in files)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }

                IWebDav webdav = new WebDav(UsersDomain);
                
                webdav.DeleteAllWebDavRules(organizationId, folder);
                
                Directory.Delete(treeRoot.FullName, true);
            }
        }

        public bool SetFolderWebDavRules(string organizationId, string folder, WebDavFolderRule[] rules)
        {
            IWebDav webdav = new WebDav(UsersDomain);

            return webdav.SetFolderWebDavRules(organizationId, folder, rules);
        }

        public WebDavFolderRule[] GetFolderWebDavRules(string organizationId, string folder)
        {
            IWebDav webdav = new WebDav(UsersDomain);

            return webdav.GetFolderWebDavRules(organizationId, folder);
        }

        public bool CheckFileServicesInstallation()
        {
            return WebsitePanel.Server.Utils.OS.CheckFileServicesInstallation();
        }

        #endregion

        #region HostingServiceProvider methods
        
        public override string[] Install()
        {
            List<string> messages = new List<string>();

            // create folder if it not exists
            try
            {
                if (!FileUtils.DirectoryExists(UsersHome))
                {
                    FileUtils.CreateDirectory(UsersHome);
                }
            }
            catch (Exception ex)
            {
                messages.Add(String.Format("Folder '{0}' could not be created: {1}",
                    UsersHome, ex.Message));
            }
            return messages.ToArray();
        }

        public override void DeleteServiceItems(ServiceProviderItem[] items)
        {
            foreach (ServiceProviderItem item in items)
            {
                try
                {
                    if (item is HomeFolder)
                        // delete home folder
                        FileUtils.DeleteFile(item.Name);
                }
                catch (Exception ex)
                {
                    Log.WriteError(String.Format("Error deleting '{0}' {1}", item.Name, item.GetType().Name), ex);
                }
            }
        }

        public override ServiceProviderItemDiskSpace[] GetServiceItemsDiskSpace(ServiceProviderItem[] items)
        {
            List<ServiceProviderItemDiskSpace> itemsDiskspace = new List<ServiceProviderItemDiskSpace>();
            foreach (ServiceProviderItem item in items)
            {
                if (item is HomeFolder)
                {
                    try
                    {
                        string path = item.Name;

                        Log.WriteStart(String.Format("Calculating '{0}' folder size", path));

                        // calculate disk space
                        ServiceProviderItemDiskSpace diskspace = new ServiceProviderItemDiskSpace();
                        diskspace.ItemId = item.Id;
                        diskspace.DiskSpace = FileUtils.CalculateFolderSize(path);
                        itemsDiskspace.Add(diskspace);

                        Log.WriteEnd(String.Format("Calculating '{0}' folder size", path));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteError(ex);
                    }
                }
            }
            return itemsDiskspace.ToArray();
        }

        #endregion

        public override bool IsInstalled()
        {
            Server.Utils.OS.WindowsVersion version = WebsitePanel.Server.Utils.OS.GetVersion();
            return version == WebsitePanel.Server.Utils.OS.WindowsVersion.WindowsServer2012;
        }

    }
}
