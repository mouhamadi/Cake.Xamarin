using System;
using System.IO;
using System.Linq;
using Cake.Common.Tools;
using Cake.Common.Tools.NUnit;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Build.BuildEngine;

namespace Cake.Xamarin
{
	/// <summary>
	/// Xamarin related cake aliases.
	/// </summary>
	[CakeAliasCategory ("Xamarin")]
	public static class XamarinAliases
	{
		internal const string DEFAULT_MDTOOL_PATH = "/Applications/Xamarin Studio.app/Contents/MacOS/mdtool";

		/// <summary>
		/// Creates an android .APK package file
		/// </summary>
		/// <returns>The file path of the .APK which was created (all subfolders of the project file specified are searched for .apk files and the newest one found is returned).</returns>
		/// <param name="context">The context.</param>
		/// <param name="projectFile">The .CSPROJ file to build from.</param>
		/// <param name="sign">Will create a signed .APK file if set to <c>true</c> based on the signing settings in the .CSPROJ, otherwise the .APK will be unsigned.</param>
		/// <param name="configurator">The settings configurator.</param>
		[CakeMethodAlias]
		public static FilePath AndroidPackage (this ICakeContext context, FilePath projectFile, bool sign = false, Action<DotNetBuildSettings> configurator = null)
		{
			var target = sign ? "SignAndroidPackage" : "PackageForAndroid";

			if (!context.FileSystem.Exist (projectFile))
				throw new CakeException ("Project File Not Found: " + projectFile.FullPath);
            
			context.DotNetBuild (projectFile, c => {
				c.Configuration = "Release";        
				c.Targets.Add (target);

				// Pass along configuration to user for further changes
				if (configurator != null)
					configurator (c);
			});

			var searchPattern = projectFile.GetDirectory () + (sign ? "/**/*-Signed.apk" : "/**/*.apk");

			// Use the globber to find any .apk files within the tree
			return context.Globber
                .GetFiles (searchPattern)
                .OrderBy (f => new FileInfo (f.FullPath).LastWriteTimeUtc)
                .FirstOrDefault ();            
		}

		/// <summary>
		/// Creates an archive of the Xamarin.iOS app
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="solutionFile">The solution file.</param>
		/// <param name="projectName">The name of the project within the solution to archive.</param>
		[CakeMethodAlias]
		public static void iOSArchive (this ICakeContext context, FilePath solutionFile, string projectName)
		{
			iOSArchive (context, solutionFile, projectName, new MDToolSettings {
				Configuration = "Release|iPhone"
			});
		}


		/// <summary>
		/// Creates an archive of an app with MDTool
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="solutionFile">The solution file.</param>
		/// <param name="projectName">The name of the project within the solution to archive.</param>
		/// <param name="settings">The mdtool settings.</param>
		[CakeMethodAlias]
		public static void MDToolArchive (this ICakeContext context, FilePath solutionFile, string projectName, Action<MDToolSettings> settings)
		{
			var mds = new MDToolSettings ();

			if (settings != null)
				settings (mds);

			iOSArchive (context, solutionFile, projectName, mds);
		}

		/// <summary>
		/// Creates an archive of the Xamarin.iOS app
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="solutionFile">The solution file.</param>
		/// <param name="projectName">The name of the project within the solution to archive.</param>
		/// <param name="settings">The mdtool settings.</param>
		[CakeMethodAlias]
		public static void iOSArchive (this ICakeContext context, FilePath solutionFile, string projectName, Action<MDToolSettings> settings)
		{
			var mds = new MDToolSettings ();

			if (settings != null)
				settings (mds);

			iOSArchive (context, solutionFile, projectName, mds);
		}

		/// <summary>
		/// Creates an archive of the Xamarin.iOS app
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="solutionFile">The solution file.</param>
		/// <param name="projectName">The name of the project within the solution to archive.</param>
		/// <param name="settings">The mdtool settings.</param>
		[CakeMethodAlias]
		public static void iOSArchive (this ICakeContext context, FilePath solutionFile, string projectName, MDToolSettings settings)
		{
			if (!context.Environment.IsUnix ())
				throw new CakeException ("iOSArchive alias only runs on Mac OSX");

			var runner = new MDToolRunner (context.FileSystem, context.Environment, context.ProcessRunner, context.Globber);
			runner.Archive (solutionFile, projectName, settings);
		}

		/// <summary>
		/// Builds a Xamarin.iOS project
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="projectOrSolutionFile">The project or solution file.</param>
		[CakeMethodAlias]
		public static void iOSBuild (this ICakeContext context, FilePath projectOrSolutionFile)
		{
			iOSBuild (context, projectOrSolutionFile, new MDToolSettings ());
		}


		/// <summary>
		/// Builds a project with MDTool
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="projectOrSolutionFile">The project or solution file.</param>
		/// <param name="settings">The mdtool settings.</param>
		[CakeMethodAlias]
		public static void MDToolBuild (this ICakeContext context, FilePath projectOrSolutionFile, Action<MDToolSettings> settings)
		{
			var mds = new MDToolSettings ();

			if (settings != null)
				settings (mds);

			iOSBuild (context, projectOrSolutionFile, mds);
		}

		/// <summary>
		/// Builds a Xamarin.iOS project
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="projectOrSolutionFile">The project or solution file.</param>
		/// <param name="settings">The mdtool settings.</param>
		[CakeMethodAlias]
		public static void iOSBuild (this ICakeContext context, FilePath projectOrSolutionFile, Action<MDToolSettings> settings)
		{
			var mds = new MDToolSettings ();

			if (settings != null)
				settings (mds);
            
			iOSBuild (context, projectOrSolutionFile, mds);
		}

		/// <summary>
		/// Builds a Xamarin.iOS project
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="projectOrSolutionFile">The project or solution file.</param>
		/// <param name="settings">The mdtool settings.</param>
		[CakeMethodAlias]
		public static void iOSBuild (this ICakeContext context, FilePath projectOrSolutionFile, MDToolSettings settings)
		{
			if (!context.Environment.IsUnix ())
				throw new CakeException ("iOSBuild alias only runs on Mac OSX");
            
			var runner = new MDToolRunner (context.FileSystem, context.Environment, context.ProcessRunner, context.Globber);
			runner.Build (projectOrSolutionFile, settings);
		}

		/// <summary>
		/// Builds a Xamarin.iOS project
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="projectOrSolutionFile">The project file.</param>
		[CakeMethodAlias]
		public static void iOSMSBuild (this ICakeContext context, FilePath projectOrSolutionFile)
		{
			iOSMSBuild (context, projectOrSolutionFile, null);
		}

		/// <summary>
		/// Builds a Xamarin.iOS project
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="projectOrSolutionFile">The project file.</param>
		/// <param name="configurator">The configurator.</param>
		[CakeMethodAlias]
		public static void iOSMSBuild (this ICakeContext context, FilePath projectOrSolutionFile, Action<DotNetBuildSettings> configurator)
		{
			if (configurator != null)
				context.DotNetBuild (projectOrSolutionFile, configurator);
			else
				context.DotNetBuild (projectOrSolutionFile);
		}

		/// <summary>
		/// Restores Xamarin Components for a given project
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="solutionFile">The project file.</param>
		[CakeMethodAlias]
		public static void RestoreComponents (this ICakeContext context, FilePath solutionFile)
		{
			RestoreComponents (context, solutionFile, new XamarinComponentSettings ());
		}

		/// <summary>
		/// Restores Xamarin Components for a given project
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="solutionFile">The project file.</param>
		/// <param name="settings">The xamarin-component.exe tool settings.</param>
		[CakeMethodAlias]
		public static void RestoreComponents (this ICakeContext context, FilePath solutionFile, XamarinComponentSettings settings)
		{
			var runner = new XamarinComponentRunner (context.FileSystem, context.Environment, context.ProcessRunner, context.Globber);
			runner.Restore (solutionFile, settings);
		}


		/// <summary>
		/// Packages the component for a given component YAML configuration file
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="componentYamlDirectory">The directory containing the component.yaml file.</param>
		[CakeMethodAlias]
		public static void PackageComponent (this ICakeContext context, DirectoryPath componentYamlDirectory)
		{
			var runner = new XamarinComponentRunner (context.FileSystem, context.Environment, context.ProcessRunner, context.Globber);
			runner.Package (componentYamlDirectory, new XamarinComponentSettings ());
		}

		/// <summary>
		/// Packages the component for a given component YAML configuration file
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="componentYamlDirectory">The directory containing the component.yaml file.</param>
		/// <param name="settings">The settings.</param>
		[CakeMethodAlias]
		public static void PackageComponent (this ICakeContext context, DirectoryPath componentYamlDirectory, XamarinComponentSettings settings)
		{
			var runner = new XamarinComponentRunner (context.FileSystem, context.Environment, context.ProcessRunner, context.Globber);
			runner.Package (componentYamlDirectory, settings);
		}

		/// <summary>
		/// Runs UITests in a given assembly using NUnit
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="testsAssembly">The assembly containing NUnit UITests.</param>
		/// <param name="nunitSettings">The NUnit settings.</param>
		[CakeMethodAlias]
		public static void UITest (this ICakeContext context, FilePath testsAssembly, NUnitSettings nunitSettings = null)
		{            
			// Run UITests via NUnit
			context.NUnit (new [] { testsAssembly }, nunitSettings ?? new NUnitSettings ());
		}


		/// <summary>
		/// Uploads an android .APK package to TestCloud and runs UITests
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="apkFile">The .APK file.</param>
		/// <param name="apiKey">The TestCloud API key.</param>
		/// <param name="devicesHash">The hash of the set of devices to run on.</param>
		/// <param name="userEmail">The user account email address.</param>
		/// <param name="uitestsAssemblies">The directory containing the UITests assemblies.</param>
		[CakeMethodAlias]
		public static void TestCloud (this ICakeContext context, FilePath apkFile, string apiKey, string devicesHash, string userEmail, DirectoryPath uitestsAssemblies)
		{
			TestCloud (context, apkFile, apiKey, devicesHash, userEmail, uitestsAssemblies, new TestCloudSettings ());
		}

		/// <summary>
		/// Uploads an android .APK package to TestCloud and runs UITests
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="apkFile">The .APK file.</param>
		/// <param name="apiKey">The TestCloud API key.</param>
		/// <param name="devicesHash">The hash of the set of devices to run on.</param>
		/// <param name="userEmail">The user account email address.</param>
		/// <param name="uitestsAssemblies">The directory containing the UITests assemblies.</param>
		/// <param name="settings">The settings.</param>
		[CakeMethodAlias]
		public static void TestCloud (this ICakeContext context, FilePath apkFile, string apiKey, string devicesHash, string userEmail, DirectoryPath uitestsAssemblies, TestCloudSettings settings)
		{
			var runner = new TestCloudRunner (context.FileSystem, context.Environment, context.ProcessRunner, context.Globber);
			runner.Run (apkFile, apiKey, devicesHash, userEmail, uitestsAssemblies, settings);
		}

		/// <summary>
		/// Androids the manifest verion name and number.
		/// </summary>
		/// <param name="context">Context.</param>
		/// <param name="androidManifestPath">Android manifest path.</param>
		/// <param name="version">Version.</param>
		/// <param name="versionNumber">Version number.</param>
		[CakeMethodAlias]
		public static void AndroidManifestVerionNameAndNumber (this ICakeContext context, FilePath androidManifestPath, Version version, int versionNumber)
		{
			var path = androidManifestPath.FullPath;
			if (!File.Exists (path)) {
				throw new CakeException ("the AndroidManifest file provided must exist");
			}

			string androidNS = "http://schemas.android.com/apk/res/android";

			XName versionCodeAttributeName = XName.Get ("versionCode", androidNS);
			XName versionNameAttributeName = XName.Get ("versionName", androidNS);

			XDocument doc = XDocument.Load (path);

			doc.Root.SetAttributeValue (versionNameAttributeName, version);
			doc.Root.SetAttributeValue (versionCodeAttributeName, versionNumber);
			doc.Save (path);

			Console.WriteLine (doc);
		}

		/// <summary>
		/// Androids the name of the manifest verion.
		/// </summary>
		/// <param name="context">Context.</param>
		/// <param name="androidManifestPath">Android manifest path.</param>
		/// <param name="version">Version.</param>
		[CakeMethodAlias]
		public static void AndroidManifestVerionName (this ICakeContext context, FilePath androidManifestPath, Version version)
		{
			var path = androidManifestPath.FullPath;
			if (!File.Exists (path)) {
				throw new CakeException ("the AndroidManifest file provided must exist");
			}

			string androidNS = "http://schemas.android.com/apk/res/android";

			XName versionNameAttributeName = XName.Get ("versionName", androidNS);

			XDocument doc = XDocument.Load (path);

			doc.Root.SetAttributeValue (versionNameAttributeName, version);
			doc.Save (path);
			Console.WriteLine (doc);
		}
		/// <summary>
		/// Androids the manifest verion number.
		/// </summary>
		/// <param name="context">Context.</param>
		/// <param name="androidManifestPath">Android manifest path.</param>
		/// <param name="versionNumber">Version number.</param>
		[CakeMethodAlias]
		public static void AndroidManifestVerionNumber (this ICakeContext context, FilePath androidManifestPath, int versionNumber)
		{
			var path = androidManifestPath.FullPath;
			if (!File.Exists (path)) {
				throw new CakeException ("the AndroidManifest file provided must exist");
			}

			string androidNS = "http://schemas.android.com/apk/res/android";

			XName versionCodeAttributeName = XName.Get ("versionCode", androidNS);

			XDocument doc = XDocument.Load (path);

			doc.Root.SetAttributeValue (versionCodeAttributeName, versionNumber);
			doc.Save (path);

			Console.WriteLine (doc);
		}



		/// <summary>
		/// Infos the plist bundle version.
		/// </summary>
		/// <param name="context">Context.</param>
		/// <param name="infoPlistPath">Info plist path.</param>
		/// <param name="version">Version.</param>
		[CakeMethodAlias]
		public static void iOSInfoPlistBundleVersion (this ICakeContext context, FilePath infoPlistPath, Version version)
		{
			var path = infoPlistPath.FullPath;
			if (!File.Exists (path)) {
				throw new CakeException ("the Info.plist file provided must exist");
			}

			XDocument doc = XDocument.Load (path);

			var bundleVersionElement = doc.XPathSelectElement ("plist/dict/key[string()='CFBundleVersion']");
			var versionElement = bundleVersionElement.NextNode as XElement;
			versionElement.Value = version.ToString ();
			doc.Save (path);
		}

		/// <summary>
		/// Infos the plist short version string.
		/// </summary>
		/// <returns><c>true</c>, if plist short version string was infoed, <c>false</c> otherwise.</returns>
		/// <param name="context">Context.</param>
		/// <param name="infoPlistPath">Info plist path.</param>
		/// <param name="version">Version.</param>
		[CakeMethodAlias]
		public static bool iOSInfoPlistShortVersionString (this ICakeContext context, FilePath infoPlistPath, Version version)
		{
			var path = infoPlistPath.FullPath;
			if (!File.Exists (path)) {
				throw new CakeException ("the Info.plist file provided must exist");
			}

			XDocument doc = XDocument.Load (path);

			var bundleShortVersionElement = doc.XPathSelectElement ("plist/dict/key[string()='CFBundleShortVersionString']");
			if (bundleShortVersionElement != null) {
				var shortVersionElement = bundleShortVersionElement.NextNode as XElement;         
				shortVersionElement.Value = version.Build.ToString ();
				doc.Save (path);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Infos the plist bundle identifier.
		/// </summary>
		/// <returns><c>true</c>, if plist bundle identifier was infoed, <c>false</c> otherwise.</returns>
		/// <param name="context">Context.</param>
		/// <param name="infoPlistPath">Info plist path.</param>
		/// <param name="bundleIdentifier">Bundle identifier.</param>
		[CakeMethodAlias]
		public static bool iOSInfoPlistBundleIdentifier (this ICakeContext context, FilePath infoPlistPath, string bundleIdentifier)
		{
			var path = infoPlistPath.FullPath;
			if (!File.Exists (path)) {
				throw new CakeException ("the Info.plist file provided must exist");
			}

			XDocument doc = XDocument.Load (path);

			var bundleIdentifierElement = doc.XPathSelectElement ("plist/dict/key[string()='CFBundleIdentifier']");
			if (bundleIdentifierElement != null) {
				var identifierElement = bundleIdentifierElement.NextNode as XElement;
				identifierElement.Value = bundleIdentifier;
				doc.Save (path);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Androids the generate keystore.
		/// </summary>
		/// <returns>The generate keystore.</returns>
		/// <param name="context">Context.</param>
		/// <param name="releaseKeyStorePath">Release key store path.</param>
		/// <param name="name">Name.</param>
		/// <param name="aliasName">Alias name.</param>
		/// <param name="storepass">Storepass.</param>
		/// <param name="keypass">Keypass.</param>
		/// <param name="validityDays">Validity days.</param>
		[CakeMethodAlias]
		public static IProcess AndroidGenerateKeystore (this ICakeContext context, FilePath releaseKeyStorePath, string name, string aliasName, string  storepass, string keypass, int validityDays)
		{
			var command = "keytool";
			var generatePrivateKeyArgument = "-genkey -v -keystore " + releaseKeyStorePath + " -dname " + name + " -alias " + aliasName + " -storepass " + storepass + " -keypass " + keypass + " -keyalg RSA -keysize 2048 -validity " + validityDays;

			ProcessSettings settings = new ProcessSettings { Arguments = generatePrivateKeyArgument };
			// Get the working directory.
			var workingDirectory = settings.WorkingDirectory ?? context.Environment.WorkingDirectory;
			settings.WorkingDirectory = workingDirectory.MakeAbsolute (context.Environment);

			var process = context.ProcessRunner.Start (command, settings);
			if (process == null) {
				throw new CakeException ("Could not start process.");
			}

			process.WaitForExit ();

			return process;
		}

		/// <summary>
		/// Androids the sign apk.
		/// </summary>
		/// <param name="context">Context.</param>
		/// <param name="releaseKeyStorePath">Release key store path.</param>
		/// <param name="apkFilePath">Apk file path.</param>
		/// <param name="aliasName">Alias name.</param>
		/// <param name="storepass">Storepass.</param>
		/// <param name="keypass">Keypass.</param>
		[CakeMethodAlias]
		public static IProcess AndroidSignApk (this ICakeContext context, FilePath releaseKeyStorePath, FilePath apkFilePath, string aliasName, string  storepass, string keypass)
		{
			var command = "jarsigner";
			var signAppArgument = "-verbose -sigalg SHA1withRSA -digestalg SHA1 -keystore " + releaseKeyStorePath.FullPath + " " + apkFilePath.FullPath + " " + aliasName + " -storepass " + storepass + " -keypass " + keypass;

			ProcessSettings settings = new ProcessSettings { Arguments = signAppArgument };
			// Get the working directory.
			var workingDirectory = settings.WorkingDirectory ?? context.Environment.WorkingDirectory;
			settings.WorkingDirectory = workingDirectory.MakeAbsolute (context.Environment);

			var process = context.ProcessRunner.Start (command, settings);
			if (process == null) {
				throw new CakeException ("Could not start process.");
			}

			process.WaitForExit ();

			return process;
		}

		/// <summary>
		/// Androids the sign apk verify.
		/// </summary>
		/// <param name="context">Context.</param>
		/// <param name="apkFilePath">Apk file path.</param>
		[CakeMethodAlias]
		public static IProcess AndroidSignApkVerify (this ICakeContext context, FilePath apkFilePath)
		{
			var command = "jarsigner ";
			var verifySignArgument = "-verify -verbose -certs " + apkFilePath.FullPath; 


			ProcessSettings settings = new ProcessSettings { Arguments = verifySignArgument };
			// Get the working directory.
			var workingDirectory = settings.WorkingDirectory ?? context.Environment.WorkingDirectory;
			settings.WorkingDirectory = workingDirectory.MakeAbsolute (context.Environment);

			var process = context.ProcessRunner.Start (command, settings);
			if (process == null) {
				throw new CakeException ("Could not start process.");
			}

			process.WaitForExit ();

			return process;
		}

		/// <summary>
		/// Androids the zip align.
		/// </summary>
		/// <returns>The zip align.</returns>
		/// <param name="context">Context.</param>
		/// <param name="zipAlignCommandPath">Zip align command path.</param>
		/// <param name="alignment">Alignment.</param>
		/// <param name="inputApkPath">Input apk path.</param>
		/// <param name="outputApkPath">Output apk path.</param>
		[CakeMethodAlias]
		public static IProcess AndroidZipAlign (this ICakeContext context, string zipAlignCommandPath, int alignment, string inputApkPath, string outputApkPath)
		{
			var command = zipAlignCommandPath;
			var vzipAlignArgument = "-f -v " + alignment + " " + inputApkPath + " " + outputApkPath;


			ProcessSettings settings = new ProcessSettings { Arguments = vzipAlignArgument };
			// Get the working directory.
			var workingDirectory = settings.WorkingDirectory ?? context.Environment.WorkingDirectory;
			settings.WorkingDirectory = workingDirectory.MakeAbsolute (context.Environment);

			var process = context.ProcessRunner.Start (command, settings);

			if (process == null) {
				throw new CakeException ("Could not start process.");
			}

			process.WaitForExit ();

			return process;
		}

	}
}
