﻿using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace OVPN_user_pass_validation
{
    class Program
    {
        static internal string login, pass;
        
        static void Main(string[] args)
        {
            if (args.Length != 1 || !FileExistReadable(args[0]) || new FileInfo(args[0]).Length > 160)
                { Environment.ExitCode = 1; return; }

            //string xmlPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string xmlPath = @"D:\Documents\Visual Studio 2017\Projects\OVPN user-pass-validation\OVPN user-pass-validation\bin\x64\Release\OVPN user-pass-validation.exe";
            xmlPath = xmlPath.Substring(0, xmlPath.LastIndexOf('.')) + ".xml";

            XmlDocument conf = new XmlDocument();
            try
            {
                if (FileExistReadable(xmlPath))
                {
                    conf.Load(xmlPath);
                }
                else { throw new Exception("xml read/exist err"); }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.ExitCode = 1;
                return;
            }

            string[] cred;
            try { cred = File.ReadAllLines(args[0], Encoding.UTF8); }
            catch { Environment.ExitCode = 1; return; }
            if (cred.Length != 2) { Environment.ExitCode = 1; return; }
            login = cred[0];
            pass = cred[1];

            
            XmlNode confNode = conf.SelectSingleNode("/configuration");
            if (confNode.SelectSingleNode("PasswdFileAuth").Attributes["enabled"].Value == "true"
                && PasswdFileAuth.Check(confNode.SelectSingleNode("PasswdFileAuth")))
                { Environment.ExitCode = 0; return; }
            //if (confNode.SelectSingleNode("WindowsCredAuth").Attributes["enabled"].Value == "true"
            //    && PasswdFileAuth.Check(confNode.SelectSingleNode("WindowsCredAuth")))
            //    { Environment.ExitCode = 1; return; }
            Environment.ExitCode = 1;
            return;
        }
        

        internal static bool FileExistReadable(string filepath)
        {
            if (!System.IO.File.Exists(filepath)) return false;
            try { File.Open(filepath, FileMode.Open, FileAccess.Read).Dispose(); return true; }
                catch { return false; }
        }

        public static bool ValidateUsernameAndPassword(string userName, SecureString securePassword)
        {
            bool result = false;

            ContextType contextType = ContextType.Machine;

            if (InDomain())
            {
                contextType = ContextType.Domain;
            }



            try
            {
                using (PrincipalContext principalContext = new PrincipalContext(contextType))
                {
                    result = principalContext.ValidateCredentials(
                        userName,
                        new NetworkCredential(string.Empty, securePassword).Password
                    );
                }
            }
            catch (PrincipalOperationException)
            {
                // Account disabled? Considering as Login failed
                result = false;
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }

        /// <summary>
        ///     Validate: computer connected to domain?   
        /// </summary>
        /// <returns>
        ///     True -- computer is in domain
        ///     <para>False -- computer not in domain</para>
        /// </returns>
        public static bool InDomain()
        {
            bool result = true;

            try
            {
                Domain domain = Domain.GetComputerDomain();
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                result = false;
            }

            return result;
        }

    }

}
