﻿using DynamicInterop;
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace RDotNet.NativeLibrary
{
    /// <summary>
    /// Collection of utility methods for operating systems.
    /// </summary>
    public static class NativeUtility
    {
        /// <summary>
        /// Gets the platform on which the current process runs.
        /// </summary>
        /// <remarks>
        /// <see cref="Environment.OSVersion"/>'s platform is not <see cref="PlatformID.MacOSX"/> even on Mac OS X.
        /// This method returns <see cref="PlatformID.MacOSX"/> when the current process runs on Mac OS X.
        /// This method uses UNIX's uname command to check the operating system,
        /// so this method cannot check the OS correctly if the PATH environment variable is changed (will returns <see cref="PlatformID.Unix"/>).
        /// </remarks>
        /// <returns>The current platform.</returns>
        public static PlatformID GetPlatform()
        {
            return PlatformUtility.GetPlatform();
        }

        /// <summary>
        /// Execute a command in a new process
        /// </summary>
        /// <param name="processName">Process name e.g. "uname"</param>
        /// <param name="arguments">Arguments e.g. "-s"</param>
        /// <returns>The output of the command to the standard output stream</returns>
        public static string ExecCommand(string processName, string arguments)
        {
            return PlatformUtility.ExecCommand(processName, arguments);
        }

        private static StringBuilder logSetEnvVar = new StringBuilder();

        /// <summary>
        /// Gets a log of the changes made to environment variables via the NativeUtility
        /// </summary>
        public static string SetEnvironmentVariablesLog { get { return logSetEnvVar.ToString(); } }

        /// <summary>
        /// Sets the PATH to the R binaries and R_HOME environment variables if needed.
        /// </summary>
        /// <param name="rPath">The path of the directory containing the R native library.
        /// If null (default), this function tries to locate the path via the Windows registry, or commonly used locations on MacOS and Linux</param>
        /// <param name="rHome">The path for R_HOME. If null (default), the function checks the R_HOME environment variable. If none is set,
        /// the function uses platform specific sensible default behaviors.</param>
        /// <remarks>
        /// This function has been designed to limit the tedium for users, while allowing custom settings for unusual installations.
        /// </remarks>
        public static void SetEnvironmentVariables(string rPath = null, string rHome = null)
        {
            /*
             * Changing the behavior in Oct 2014, following the report of
             * https://rdotnet.codeplex.com/workitem/140
             * Use rHome, whether from the method parameter or from the environment variable,
             * to deduce the path to the binaries, in preference to the registry key.
             */

            logSetEnvVar.Clear();

            var platform = GetPlatform();
            if (rPath != null)
                CheckDirExists(rPath);
            if (rHome != null)
                CheckDirExists(rHome);

            FindRPaths(ref rPath, ref rHome, logSetEnvVar);

            if (string.IsNullOrEmpty(rHome))
                throw new NotSupportedException("R_HOME was not provided and a suitable path could not be found by R.NET");
            SetenvPrepend(rPath);
            // It is highly recommended to use the 8.3 short path format on windows.
            // See the manual page of R.home function in R. Solves at least the issue R.NET 97.
            if (platform == PlatformID.Win32NT)
                rHome = GetShortPath(rHome);
            if (!Directory.Exists(rHome))
                throw new DirectoryNotFoundException(string.Format("Directory '{0}' does not exist - cannot set the environment variable R_HOME to that value", rHome));
            Environment.SetEnvironmentVariable("R_HOME", rHome);
            if (platform == PlatformID.Unix)
            {
                // Let's check that LD_LIBRARY_PATH is set if this is a custom installation of R.
                // Normally in an R session from a custom build/install we get something typically like:
                // > Sys.getenv('LD_LIBRARY_PATH')
                // [1] "/usr/local/lib/R/lib:/usr/local/lib:/usr/lib/jvm/java-7-openjdk-amd64/jre/lib/amd64/server"
                // The R script sets LD_LIBRARY_PATH before it starts the native executable under e.g. /usr/local/lib/R/bin/exec/R
                // This would be useless to set LD_LIBRARY_PATH in the current function:
                // it must be set as en env var BEFORE the process is started (see man page for dlopen)
                // so all we can do is an intelligible error message for the user, explaining he needs to set the LD_LIBRARY_PATH env variable
                // Let's delay the notification about a missing LD_LIBRARY_PATH till loading libR.so fails, if it does.
            }
        }

        /// <summary>
        /// A method to help diagnose the environment variable setup process. 
        /// This function does not change the environment, this is purely a "dry run"
        /// </summary>
        /// <param name="rPath">The path of the directory containing the R native library.
        /// If null (default), this function tries to locate the path via the Windows registry, or commonly used locations on MacOS and Linux</param>
        /// <param name="rHome">The path for R_HOME. If null (default), the function checks the R_HOME environment variable. If none is set,
        /// the function uses platform specific sensible default behaviors.</param>
        /// <returns>A console friendly output of the paths discovery process</returns>
        public static string FindRPaths(ref string rPath, ref string rHome)
        {
            StringBuilder logger = new StringBuilder();
            FindRPaths(ref rPath, ref rHome, logger);
            return logger.ToString();
        }

        private static void FindRPaths(ref string rPath, ref string rHome, StringBuilder logSetEnvVar)
        {
            doLogSetEnvVarInfo(string.Format("caller provided rPath={0}, rHome={1}",
               rPath == null ? "null" : rPath,
               rHome == null ? "null" : rHome), logSetEnvVar);

            if (string.IsNullOrEmpty(rHome))
            {
                rHome = GetRHomeEnvironmentVariable();
                doLogSetEnvVarInfo(string.Format("R.NET looked for preset R_HOME env. var. Found {0}",
                   rHome == null ? "null" : rHome), logSetEnvVar);
            }
            if (string.IsNullOrEmpty(rHome))
            {
                rHome = FindRHome(rPath: null, logger: logSetEnvVar);
                doLogSetEnvVarInfo(string.Format("R.NET looked for platform-specific way (e.g. win registry). Found {0}",
                   rHome == null ? "null" : rHome), logSetEnvVar);
                if (!string.IsNullOrEmpty(rHome))
                {
                    if (rPath == null)
                    {
                        rPath = FindRPath(rHome);
                        doLogSetEnvVarInfo(string.Format("R.NET trying to find rPath based on rHome; Deduced {0}",
                           rPath == null ? "null" : rPath), logSetEnvVar);
                    }
                    if (rPath == null)
                    {
                        rPath = FindRPath();
                        doLogSetEnvVarInfo(string.Format("R.NET trying to find rPath, independently of rHome; Deduced {0}",
                           rPath == null ? "null" : rPath), logSetEnvVar);
                    }
                }
                else
                {
                    rHome = FindRHome(rPath);
                    doLogSetEnvVarInfo(string.Format("R.NET trying to find rHome based on rPath; Deduced {0}",
                       rHome == null ? "null" : rHome), logSetEnvVar);
                }
            }
            if (string.IsNullOrEmpty(rHome))
                doLogSetEnvVar("Error", "R_HOME was not provided and a suitable path could not be found by R.NET", logSetEnvVar);
        }

        private static void doLogSetEnvVar(string level, string msg, StringBuilder logSetEnvVar)
        {
            if (logSetEnvVar != null)
            {
                logSetEnvVar.Append(level);
                logSetEnvVar.Append(": ");
                logSetEnvVar.AppendLine(msg);
            }
        }

        private static void doLogSetEnvVarWarn(string msg, StringBuilder logger)
        {
            doLogSetEnvVar("Warn", msg, logger);
        }

        private static void doLogSetEnvVarInfo(string msg, StringBuilder logger)
        {
            doLogSetEnvVar("Info", msg, logger);
        }

        private static void doFoundWinRegKey(RegistryKey rCore, StringBuilder logger)
        {
            doLogSetEnvVarInfo(string.Format("Found Windows registry key {0}", rCore.ToString()), logger);
        }


        private static string GetShortPath(string path)
        {
            var shortPath = new StringBuilder(MaxPathLength);
            GetShortPathName(path, shortPath, MaxPathLength);
            return shortPath.ToString();
        }

        private const int MaxPathLength = 248; //MaxPath is 248. MaxFileName is 260.

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetShortPathName([MarshalAs(UnmanagedType.LPTStr)] string path,
                                                  [MarshalAs(UnmanagedType.LPTStr)] StringBuilder shortPath,
                                                  int shortPathLength);

        /// <summary>
        /// Gets the value, if any, of the R_HOME environment variable of the current process
        /// </summary>
        /// <returns>The value, or null if not set</returns>
        public static string GetRHomeEnvironmentVariable()
        {
            return Environment.GetEnvironmentVariable("R_HOME");
        }

        /// <summary>
        /// Try to locate the directory path to use for the R_HOME environment variable. This is used by R.NET by default; users may want to use it to diagnose problematic behaviors.
        /// </summary>
        /// <param name="rPath">Optional path to the directory containing the R shared library. This is ignored unless on a Unix platform (i.e. ignored on Windows and MacOS)</param>
        /// <param name="logger">Optional logger for diagnosis</param>
        /// <returns>The path that R.NET found suitable as a candidate for the R_HOME environment</returns>
        public static string FindRHome(string rPath = null, StringBuilder logger = null)
        {
            var platform = GetPlatform();
            string rHome;
            switch (platform)
            {
                case PlatformID.Win32NT:
                    // We need here to guess, process and set R_HOME
                    // Rf_initialize_R for gnuwin calls get_R_HOME which scans the windows registry and figures out R_HOME; however for
                    // unknown reasons in R.NET we end up with long path names, whereas R.exe ends up with the short, 8.3 path format.
                    // Blanks in the R_HOME environment variable cause trouble (e.g. for Rcpp), so we really must make sure
                    // that rHome is a short path format. Here we retrieve the path possibly in long format, and process to short format later on
                    // to capture all possible sources of R_HOME specifications
                    // Behavior added to fix issue
                    rHome = GetRhomeWin32NT(logger);
                    break;

                case PlatformID.MacOSX:
                    rHome = "/Library/Frameworks/R.framework/Resources";
                    break;

                case PlatformID.Unix:
                    if (!string.IsNullOrEmpty(rPath))
                        // if rPath is e.g. /usr/local/lib/R/lib/ ,
                        rHome = Path.GetDirectoryName(rPath);
                    else
                        rHome = "/usr/lib/R";
                    if (!rHome.EndsWith("R"))
                        // if rPath is e.g. /usr/lib/ (symlink)  then default
                        rHome = "/usr/lib/R";
                    break;

                default:
                    throw new NotSupportedException(platform.ToString());
            }
            return rHome;
        }

        private static string GetRhomeWin32NT(StringBuilder logger)
        {
            RegistryKey rCoreKey = GetRCoreRegistryKeyWin32(logger);
            return GetRInstallPathFromRCoreKegKey(rCoreKey, logger);
        }

        private static void CheckDirExists(string rPath)
        {
            if (!Directory.Exists(rPath))
                throw new ArgumentException(string.Format("Specified directory not found: '{0}'", rPath));
        }

        private static string ConstructRPath(string rHome)
        {
            var shlibFilename = GetRLibraryFileName();
            var platform = GetPlatform();
            switch (platform)
            {
                case PlatformID.Win32NT:
                    var rPath = Path.Combine(rHome, "bin");
                    Version rVersion = GetRVersionFromRegistry();
                    if (rVersion.Major > 2 || (rVersion.Major == 2 && rVersion.Minor >= 12))
                    {
                        var bitness = Environment.Is64BitProcess ? "x64" : "i386";
                        rPath = Path.Combine(rPath, bitness);
                    }
                    return rPath;

                default:
                    throw new PlatformNotSupportedException();
            }
        }

        private static RegistryKey GetRCoreRegistryKey(StringBuilder logger)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return null;
            return GetRCoreRegistryKeyWin32(logger);
        }

        /// <summary>
        /// Gets the R version from the Windows R Registry (if available)
        /// </summary>
        /// <returns>a System.Version object</returns>
        public static Version GetRVersionFromRegistry(StringBuilder logger = null)
        {
            var rCoreKey = GetRCoreRegistryKey(logger);
            string version = GetRCurrentVersionStringFromRegistry(rCoreKey);
            if (string.IsNullOrEmpty(version))
            {
                var subKeyNames = rCoreKey.GetSubKeyNames();
                if (subKeyNames.Length > 0)
                    version = subKeyNames[0];
            }
            return new Version(version);
        }

        private static string GetRCurrentVersionStringFromRegistry(RegistryKey rCoreKey)
        {
            return rCoreKey.GetValue("Current Version") as string;
        }

        /// <summary>
        /// Attempt to find a suitable path to the R shared library. This is used by R.NET by default; users may want to use it to diagnose problematic behaviors.
        /// </summary>
        /// <returns>The path to the directory where the R shared library is expected to be</returns>
        public static string FindRPath(string rHome = null)
        {
            var platform = GetPlatform();
            switch (platform)
            {
                case PlatformID.Win32NT:
                    return FindRPathWindows(rHome);

                case PlatformID.MacOSX:
                    return FindRPathMacOS(rHome);

                case PlatformID.Unix:
                    return FindRPathUnix(rHome);

                default:
                    throw new PlatformNotSupportedException();
            }
        }

        private static string FindRPathUnix(string rHome)
        {
            // TODO: too many default strings here. R.NET should not try to overcome variance in Unix setups.
            var shlibFilename = GetRLibraryFileName();
            var rexepath = ExecCommand("which", "R"); // /usr/bin/R,  or /usr/local/bin/R
            if (string.IsNullOrEmpty(rexepath)) return "/usr/lib";
            var bindir = Path.GetDirectoryName(rexepath); //   /usr/local/bin
            // Trying to emulate the start of the R shell script
            // /usr/local/lib/R/lib/libR.so
            var libdir = Path.Combine(Path.GetDirectoryName(bindir), "lib", "R", "lib");
            if (File.Exists(Path.Combine(libdir, shlibFilename)))
                return libdir;
            libdir = Path.Combine(Path.GetDirectoryName(bindir), "lib64", "R", "lib");
            if (File.Exists(Path.Combine(libdir, shlibFilename)))
                return libdir;
            return "/usr/lib";
        }

        private static string FindRPathMacOS(string rHome)
        {
            // TODO: is there a way to detect installations on MacOS
            return "/Library/Frameworks/R.framework/Libraries";
        }

        private static string FindRPathWindows(string rHome)
        {
            if (!string.IsNullOrEmpty(rHome))
                return ConstructRPath(rHome);
            else
                return FindRPathFromRegistry();
        }

        private static void SetenvPrepend(string rPath, string envVarName = "PATH")
        {
            // this function results from a merge of PR https://rdotnet.codeplex.com/SourceControl/network/forks/skyguy94/PRFork/contribution/7684
            //  Not sure of the intent, and why a SetDllDirectory was used, where we moved away from. May need discussion with skyguy94
            //  relying on this too platform-specific way to specify the search path where
            //  Environment.SetEnvironmentVariable is multi-platform.

            Environment.SetEnvironmentVariable(envVarName, PrependToEnv(rPath, envVarName));
            /*
            var platform = GetPlatform();
            if (platform == PlatformID.Win32NT)
            {
               var result = WindowsLibraryLoader.SetDllDirectory(rPath);
               var buffer = new StringBuilder(100);
               WindowsLibraryLoader.GetDllDirectory(100, buffer);
               Console.WriteLine("DLLPath:" + buffer.ToString());
            }
            */
        }

        private static string PrependToEnv(string rPath, string envVarName = "PATH")
        {
            var currentPathEnv = Environment.GetEnvironmentVariable(envVarName);
            var paths = currentPathEnv.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
            if (paths[0] == rPath)
                return currentPathEnv;
            return rPath + Path.PathSeparator + currentPathEnv;
        }

        /// <summary>
        /// Windows-only function; finds in the Windows registry the path to the most recently installed R binaries.
        /// </summary>
        /// <returns>The path, such as</returns>
        public static string FindRPathFromRegistry(StringBuilder logger = null)
        {
            CheckPlatformWin32();
            bool is64Bit = Environment.Is64BitProcess;
            RegistryKey rCoreKey = GetRCoreRegistryKeyWin32(logger);
            var installPath = GetRInstallPathFromRCoreKegKey(rCoreKey, logger);
            var currentVersion = GetRVersionFromRegistry();
            var bin = Path.Combine(installPath, "bin");
            // Up to 2.11.x, DLLs are installed in R_HOME\bin.
            // From 2.12.0, DLLs are installed in the one level deeper directory.
            return currentVersion < new Version(2, 12) ? bin : Path.Combine(bin, is64Bit ? "x64" : "i386");
        }

        private static string GetRInstallPathFromRCoreKegKey(RegistryKey rCoreKey, StringBuilder logger)
        {
            string installPath = null;
            string[] subKeyNames = rCoreKey.GetSubKeyNames();
            string[] valueNames = rCoreKey.GetValueNames();
            if (valueNames.Length == 0)
            {
                doLogSetEnvVarWarn("Did not find any value names under " + rCoreKey, logger);
                return RecurseFirstSubkey(rCoreKey, logger);
            }
            else
            {
                const string installPathKey = "InstallPath";
                if (valueNames.Contains(installPathKey))
                {
                    doLogSetEnvVarInfo("Found sub-key InstallPath under " + rCoreKey, logger);
                    installPath = (string)rCoreKey.GetValue(installPathKey);
                }
                else
                {
                    doLogSetEnvVarInfo("Did not find sub-key InstallPath under " + rCoreKey, logger);
                    if (valueNames.Contains("Current Version"))
                    {
                        doLogSetEnvVarInfo("Found sub-key Current Version under " + rCoreKey, logger);
                        string currentVersion = GetRCurrentVersionStringFromRegistry(rCoreKey);
                        if (subKeyNames.Contains(currentVersion))
                        {
                            var rVersionCoreKey = rCoreKey.OpenSubKey(currentVersion);
                            return GetRInstallPathFromRCoreKegKey(rVersionCoreKey, logger);
                        }
                        else
                        {
                            doLogSetEnvVarWarn("Sub key "+ currentVersion + " not found in " + rCoreKey, logger);
                        }
                    }
                    else
                    {
                        doLogSetEnvVarInfo("Did not find sub-key Current Version under " + rCoreKey, logger);
                        return RecurseFirstSubkey(rCoreKey, logger);
                    }
                }
            }
            doLogSetEnvVarInfo(string.Format("InstallPath value of key " + rCoreKey.ToString() + ": {0}",
               installPath == null ? "null" : installPath), logger);
            return installPath;
        }

        private static string RecurseFirstSubkey(RegistryKey rCoreKey, StringBuilder logger )
        {
            string[] subKeyNames = rCoreKey.GetSubKeyNames();
            if (subKeyNames.Length > 0)
            {
                var versionNum = subKeyNames.First();
                var rVersionCoreKey = rCoreKey.OpenSubKey(versionNum);
                doLogSetEnvVarInfo("As a last resort, trying to recurse into " + rVersionCoreKey, logger);
                return GetRInstallPathFromRCoreKegKey(rVersionCoreKey, logger);
            }
            else
            {
                doLogSetEnvVarWarn("No sub-key found under " + rCoreKey, logger);
                return null;
            }
        }

        private static void CheckPlatformWin32()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                throw new NotSupportedException("This method is supported only on the Win32NT platform");
        }

        private static RegistryKey GetRCoreRegistryKeyWin32(StringBuilder logger)
        {
            CheckPlatformWin32();
            var rCore = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\R-core");
            if (rCore == null)
            {
                doLogSetEnvVarInfo(@"Local machine SOFTWARE\R-core not found - trying current user", logger);
                rCore = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\R-core");
                if (rCore == null)
                    throw new ApplicationException("Windows Registry key 'SOFTWARE\\R-core' not found in HKEY_LOCAL_MACHINE nor HKEY_CURRENT_USER");
            }
            doFoundWinRegKey(rCore, logger);
            bool is64Bit = Environment.Is64BitProcess;
            var subKey = is64Bit ? "R64" : "R";
            var r = rCore.OpenSubKey(subKey);
            if (r == null)
            {
                throw new ApplicationException(string.Format(
                   "Windows Registry sub-key '{0}' of key '{1}' was not found", subKey, rCore.ToString()));
            }
            doFoundWinRegKey(rCore, logger);
            return r;
        }

        /// <summary>
        /// Gets the default file name of the R library on the supported platforms.
        /// </summary>
        /// <returns>R dll file name</returns>
        public static string GetRLibraryFileName()
        {
            var p = GetPlatform();
            switch (p)
            {
                case PlatformID.Win32NT:
                    return "R.dll";

                case PlatformID.MacOSX:
                    return "libR.dylib";

                case PlatformID.Unix:
                    return "libR.so";

                default:
                    throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Is the platform a unix like (Unix or MacOX)
        /// </summary>
        public static bool IsUnix
        {
            get
            {
                var p = GetPlatform();
                return p == PlatformID.MacOSX || p == PlatformID.Unix;
            }
        }
    }
}