#addin nuget:?package=Cake.AppleSimulator&version=0.2.0
#load "../cake/helpers.cake"
#load "../cake/dotnet.cake"
#load "./devices-shared.cake"

const string DefaultVersion = "17.2";

// Required arguments
string DEFAULT_IOS_PROJECT = "../../src/Controls/tests/TestCases.iOS.Tests/Controls.TestCases.iOS.Tests.csproj";
var projectPath = Argument("project", EnvironmentVariable("IOS_TEST_PROJECT") ?? DEFAULT_IOS_PROJECT);
var testDevice = Argument("device", EnvironmentVariable("IOS_TEST_DEVICE") ?? $"ios-simulator-64_{DefaultVersion}");
var targetFramework = Argument("tfm", EnvironmentVariable("TARGET_FRAMEWORK") ?? $"{DotnetVersion}-ios");
var binlogArg = Argument("binlog", EnvironmentVariable("IOS_TEST_BINLOG") ?? "");
var testApp = Argument("app", EnvironmentVariable("IOS_TEST_APP") ?? "");
var testAppProjectPath = Argument("appproject", EnvironmentVariable("IOS_TEST_APP_PROJECT") ?? DEFAULT_APP_PROJECT);
var testResultsPath = Argument("results", EnvironmentVariable("IOS_TEST_RESULTS") ?? GetTestResultsDirectory()?.FullPath);
var platform = testDevice.ToLower().Contains("simulator") ? "iPhoneSimulator" : "iPhone";
var runtimeIdentifier = Argument("rid", EnvironmentVariable("IOS_RUNTIME_IDENTIFIER") ?? GetDefaultRuntimeIdentifier(testDevice));
var deviceCleanupEnabled = Argument("cleanup", true);

// Test where clause
string testWhere = Argument("where", EnvironmentVariable("NUNIT_TEST_WHERE") ?? "");

// Device details
var udid = Argument("udid", EnvironmentVariable("IOS_SIMULATOR_UDID") ?? "");
var iosVersion = Argument("apiversion", EnvironmentVariable("IOS_PLATFORM_VERSION") ?? DefaultVersion);

// Directory setup
var binlogDirectory = DetermineBinlogDirectory(projectPath, binlogArg)?.FullPath;

string DEVICE_UDID = "";
string DEVICE_VERSION = "";
string DEVICE_NAME = "";
string DEVICE_OS = "";

Information($"Project File: {projectPath}");
Information($"Build Binary Log (binlog): {binlogDirectory}");
Information($"Build Platform: {platform}");
Information($"Build Configuration: {configuration}");
Information($"Build Runtime Identifier: {runtimeIdentifier}");
Information($"Build Target Framework: {targetFramework}");
Information($"Test Device: {testDevice}");
Information($"Test Results Path: {testResultsPath}");
Information("Runtime Variant: {0}", RUNTIME_VARIANT);

var dotnetToolPath = GetDotnetToolPath();

Setup(context =>
{
	LogSetupInfo(dotnetToolPath);
	PerformCleanupIfNeeded(deviceCleanupEnabled);

	// Device or simulator setup
	if (testDevice.Contains("device"))
	{
		GetDevices(iosVersion, dotnetToolPath);
	}
	else if (testDevice.IndexOf("_") != -1)
	{
		GetSimulators(testDevice, dotnetToolPath);
		ResetSimulators(testDevice, dotnetToolPath);
	}
});

Teardown(context => PerformCleanupIfNeeded(deviceCleanupEnabled));

Task("Cleanup");

Task("Build")
	.WithCriteria(!string.IsNullOrEmpty(projectPath))
	.Does(() =>
	{
		ExecuteBuild(projectPath, testDevice, binlogDirectory, configuration, runtimeIdentifier, targetFramework, dotnetToolPath);
	});

Task("Test")
	.IsDependentOn("Build")
	.Does(() =>
	{
		ExecuteTests(projectPath, testDevice, testResultsPath, configuration, targetFramework, runtimeIdentifier, dotnetToolPath);
	});

Task("uitest-build")
	.Does(() =>
	{
		ExecuteBuildUITestApp(testAppProjectPath, testDevice, binlogDirectory, configuration, targetFramework, runtimeIdentifier, dotnetToolPath);

	});

Task("uitest")
	.IsDependentOn("uitest-build")
	.Does(() =>
	{
		ExecuteUITests(projectPath, testAppProjectPath, testDevice, testResultsPath, binlogDirectory, configuration, targetFramework, runtimeIdentifier, iosVersion, dotnetToolPath);

	});

Task("cg-uitest")
	.Does(() =>
	{
		ExecuteCGLegacyUITests(projectPath, testAppProjectPath, testDevice, testResultsPath, configuration, targetFramework, runtimeIdentifier, iosVersion, dotnetToolPath);
	});

RunTarget(TARGET);

void ExecuteBuild(string project, string device, string binDir, string config, string rid, string tfm, string toolPath)
{
	var projectName = System.IO.Path.GetFileNameWithoutExtension(project);
	var binlog = $"{binDir}/{projectName}-{config}-ios.binlog";

	DotNetBuild(project, new DotNetBuildSettings
	{
		ToolPath = toolPath,
		Configuration = config,
		Framework = tfm,
		MSBuildSettings = new DotNetMSBuildSettings
		{
			MaxCpuCount = 0
		},
		ArgumentCustomization = args =>
		{
			args
				.Append("/p:BuildIpa=true")
				.Append($"/p:RuntimeIdentifier={rid}")
				.Append("/bl:" + binlog)
				.Append("/tl");

			return args;
		}
	});
}

void ExecuteTests(string project, string device, string resultsDir, string config, string tfm, string rid, string toolPath)
{
	CleanResults(resultsDir);

	var testApp = GetTestApplications(project, device, config, tfm, rid).FirstOrDefault();

	var XCODE_PATH = Argument("xcode_path", "");

	string xcode_args = "";
	if (!String.IsNullOrEmpty(XCODE_PATH))
	{
		Information($"Setting XCODE_PATH: {XCODE_PATH}");
		xcode_args = $"--xcode=\"{XCODE_PATH}\" ";
	}

	Information($"Testing App: {testApp}");

	var settings = new DotNetToolSettings
	{
		ToolPath = toolPath,
		DiagnosticOutput = true,
		ArgumentCustomization = args =>
		{
			args.Append("run xharness apple test " +
				$"--app=\"{testApp}\" " +
				$"--targets=\"{device}\" " +
				$"--output-directory=\"{resultsDir}\" " +
				$"--timeout=01:15:00 " +
				$"--launch-timeout=00:06:00 " +
				xcode_args +
				$"--verbosity=\"Debug\" ");

			if (device.Contains("device"))
			{
				if (string.IsNullOrEmpty(DEVICE_UDID))
				{
					throw new Exception("No device was found to install the app on. See the Setup method for more details.");
				}
				args.Append($"--device=\"{DEVICE_UDID}\" ");
			}
			return args;
		}
	};

	bool testsFailed = true;
	try
	{
		DotNetTool("tool", settings);
		testsFailed = false;
	}
	finally
	{
		StartProcess("xcrun", $"simctl spawn {sim.UDID} log collect --output {resultsDir}/simulator_logs.logarchive");
		HandleTestResults(resultsDir, testsFailed, true);
	}
	
	Information("Testing completed.");
}

void ExecuteUITests(string project, string app, string device, string resultsDir, string binDir, string config, string tfm, string rid, string ver, string toolPath)
{
	Information("Starting UI Tests...");
	
	string testApp = GetTestApplications(app, device, config, tfm, rid).FirstOrDefault();

	Information($"Testing Device: {device}");
	Information($"Testing App Project: {app}");
	Information($"Testing App: {testApp}");
	Information($"Results Directory: {resultsDir}");
	Information($"USE_NATIVE_AOT: {USE_NATIVE_AOT}");

	if (string.IsNullOrEmpty(testApp))
	{
		throw new Exception("UI Test application path not specified.");
	}

	InstallIpa(testApp, "", device, resultsDir, ver, toolPath);

	Information("Build UITests project {0}", project);

	var name = System.IO.Path.GetFileNameWithoutExtension(project);
	var binlog = $"{binDir}/{name}-{config}-ios.binlog";
	var appiumLog = $"{binDir}/appium_ios.log";
	var resultsFileName = $"{name}-{config}-ios";

	DotNetBuild(project, new DotNetBuildSettings
	{
		Configuration = config,
		ToolPath = toolPath,
		ArgumentCustomization = args => args
			.Append("/p:ExtraDefineConstants=IOSUITEST")
			.Append($"/p:_UseNativeAot={USE_NATIVE_AOT}")
			.Append("/bl:" + binlog)
	});

	SetEnvironmentVariable("APPIUM_LOG_FILE", appiumLog);

	Information("Run UITests project {0}", project);
	RunTestWithLocalDotNet(project, config, pathDotnet: toolPath, noBuild: true, resultsFileNameWithoutExtension: resultsFileName);
	Information("UI Tests completed.");
}

void ExecuteBuildUITestApp(string appProject, string device, string binDir, string config, string tfm, string rid, string toolPath)
{
	Information($"Building UI Test app: {appProject}");
	Information($"USE_NATIVE_AOT: {USE_NATIVE_AOT}");

	var projectName = System.IO.Path.GetFileNameWithoutExtension(appProject);
	var binlog = $"{binDir}/{projectName}-{config}-ios.binlog";

	if (USE_NATIVE_AOT && config.Equals("Debug", StringComparison.OrdinalIgnoreCase))
	{
		var errMsg = $"Error: Running UI tests with NativeAOT is only supported in Release configuration";
		Error(errMsg);
		throw new Exception(errMsg);
	}


	DotNetBuild(appProject, new DotNetBuildSettings
	{
		Configuration = config,
		Framework = tfm,
		ToolPath = toolPath,
		ArgumentCustomization = args =>
		{
			args
			.Append("/p:BuildIpa=true")
			.Append($"/p:_UseNativeAot={USE_NATIVE_AOT}")
			.Append($"/p:RuntimeIdentifier={rid}")
			.Append("/bl:" + binlog)
			.Append("/tl");

			return args;
		}
	});

	Information("UI Test app build completed.");
}

void ExecuteCGLegacyUITests(string project, string appProject, string device, string resultsDir, string config, string tfm, string rid, string iosVersion, string toolPath)
{
	Information("Starting Compatibility Gallery UI Tests...");

	CleanDirectories(resultsDir);

	Information("Starting Compatibility Gallery UI Tests...");
	
	var testApp = GetTestApplications(appProject, device, config, tfm, rid).FirstOrDefault();

	Information($"Testing Device: {device}");
	Information($"Testing App Project: {appProject}");
	Information($"Testing App: {testApp}");
	Information($"Results Directory: {resultsDir}");

	InstallIpa(testApp, "com.microsoft.mauicompatibilitygallery", device, $"{resultsDir}/ios", iosVersion, toolPath);

	// For non-CI builds we assume that this is configured correctly on your machine
	if (IsCIBuild())
	{
		// Install IDB (and prerequisites)
		StartProcess("brew", "tap facebook/fb");
		StartProcess("brew", "install idb-companion");
		StartProcess("pip3", "install --user fb-idb");

		// Create a temporary script file to hold the inline Bash script
		var makeSymLinkScript = "./temp_script.sh";
		// Below is an attempt to somewhat dynamically determine the path to idb and make a symlink to /usr/local/bin this is needed in order for Xamarin.UITest to find it
		System.IO.File.AppendAllLines(makeSymLinkScript, new[] { "sudo ln -s $(find /Users/$(whoami)/Library/Python/?.*/bin -name idb | head -1) /usr/local/bin" });

		StartProcess("bash", makeSymLinkScript);
		System.IO.File.Delete(makeSymLinkScript);
	}

	//set env var for the app path for Xamarin.UITest setup
	SetEnvironmentVariable("iOS_APP", $"{testApp}");

	var resultName = $"{System.IO.Path.GetFileNameWithoutExtension(project)}-{config}-{DateTime.UtcNow.ToFileTimeUtc()}";
	Information("Run UITests project {0}", resultName);
	RunTestWithLocalDotNet(
		project,
		config: config,
		pathDotnet: toolPath,
		noBuild: false,
		resultsFileNameWithoutExtension: resultName,
		filter: Argument("filter", ""));
}

// Helper methods

void PerformCleanupIfNeeded(bool cleanupEnabled)
{
	if (cleanupEnabled)
	{
		Information("Cleaning up...");
		Information("Deleting XHarness simulator if exists...");
		var sims = ListAppleSimulators().Where(s => s.Name.Contains("XHarness")).ToArray();
		foreach (var sim in sims)
		{
			Information($"Deleting XHarness simulator {sim.Name} ({sim.UDID})...");
			StartProcess("xcrun", $"simctl shutdown {sim.UDID}");
			ExecuteWithRetries(() => StartProcess("xcrun", $"simctl delete {sim.UDID}"), 3);
		}

	}
}

string GetDefaultRuntimeIdentifier(string testDeviceIdentifier)
{
	return testDeviceIdentifier.ToLower().Contains("simulator") ?
	 $"iossimulator-{System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString().ToLower()}"
   : $"ios-arm64";
}

void InstallIpa(string testApp, string testAppPackageName, string testDevice, string testResultsDirectory, string version, string toolPath)
{
	Information("Install with xharness: {0}", testApp);
	var settings = new DotNetToolSettings
	{
		ToolPath = toolPath,
		DiagnosticOutput = true,
		ArgumentCustomization = args =>
		{
			args.Append("run xharness apple install " +
							$"--app=\"{testApp}\" " +
							$"--targets=\"{testDevice}\" " +
							$"--output-directory=\"{testResultsDirectory}\" " +
							$"--verbosity=\"Debug\" ");
			if (testDevice.Contains("device"))
			{
				if (string.IsNullOrEmpty(DEVICE_UDID))
				{
					throw new Exception("No device was found to install the app on. See the Setup method for more details.");
				}
				args.Append($"--device=\"{DEVICE_UDID}\" ");
			}
			return args;
		}
	};

	try
	{
		DotNetTool("tool", settings);
	}
	finally
	{
		string iosVersionToRun = version;
		string deviceToRun = "";

		if (testDevice.Contains("device"))
		{
			if (!string.IsNullOrEmpty(DEVICE_UDID))
			{
				deviceToRun = DEVICE_UDID;
			}
			else
			{
				throw new Exception("No device was found to run tests on.");
			}

			iosVersionToRun = DEVICE_VERSION;

			Information("The device to run tests: {0} {1}", DEVICE_NAME, iosVersionToRun);
		}
		else
		{
			var simulatorName = "XHarness";
			Information("Looking for simulator: {0} iosversion {1}", simulatorName, iosVersionToRun);
			var sims = ListAppleSimulators();
			var simXH = sims.Where(s => s.Name.Contains(simulatorName) && s.Name.Contains(iosVersionToRun)).FirstOrDefault();
			if (simXH == null)
				throw new Exception("No simulator was found to run tests on.");

			deviceToRun = simXH.UDID;
			DEVICE_NAME = simXH.Name;
			Information("The emulator to run tests: {0} {1}", simXH.Name, simXH.UDID);
		}

		Information("The platform version to run tests: {0}", iosVersionToRun);
		SetEnvironmentVariable("DEVICE_UDID", deviceToRun);
		SetEnvironmentVariable("DEVICE_NAME", DEVICE_NAME);
		SetEnvironmentVariable("PLATFORM_VERSION", iosVersionToRun);
	}
}

void GetSimulators(string version, string tool)
{
	Information("Getting simulators for version {0}", version);
	DotNetTool("tool", new DotNetToolSettings
	{
		ToolPath = tool,
		DiagnosticOutput = true,
		ArgumentCustomization = args => args.Append("run xharness apple simulators install " +
			$"\"{version}\" " +
			$"--verbosity=\"Debug\" ")
	});
}

void ResetSimulators(string version, string tool)
{
	Information("Getting simulators for version {0}", version);
	var logDirectory = GetLogDirectory();
	DotNetTool("tool", new DotNetToolSettings
	{
		ToolPath = tool,
		DiagnosticOutput = true,
		ArgumentCustomization = args => args.Append("run xharness apple simulators reset-simulator " +
			$"--output-directory=\"{logDirectory}\" " +
			$"--target=\"{version}\" " +
			$"--verbosity=\"Debug\" ")
	});
}

void GetDevices(string version, string tool)
{
	var deviceUdid = "";
	var deviceName = "";
	var deviceVersion = "";
	var deviceOS = "";

	var list = new List<string>();
	bool isDevice = false;
	// print the apple state of the machine
	// this will print the connected devices
	DotNetTool("tool", new DotNetToolSettings
	{
		ToolPath = tool,
		DiagnosticOutput = true,
		ArgumentCustomization = args => args.Append("run xharness apple state --verbosity=\"Debug\" "),
		SetupProcessSettings = processSettings =>
			{
				processSettings.RedirectStandardOutput = true;
				processSettings.RedirectStandardError = true;
				processSettings.RedirectedStandardOutputHandler = (output) =>
				{
					Information("Apple State: {0}", output);
					if (output == "Connected Devices:")
					{
						isDevice = true;
					}
					else if (isDevice)
					{
						list.Add(output);
					}
					return output;
				};
			}
	});
	Information("List count: {0}", list.Count);
	//this removes the extra lines from output
	if (list.Count == 0)
	{
		throw new Exception($"No devices found");
		return;
	}
	list.Remove(list.Last());
	list.Remove(list.Last());
	foreach (var item in list)
	{
		var stringToTest = $"Device: {item.Trim()}";
		Information(stringToTest);
		var regex = new System.Text.RegularExpressions.Regex(@"Device:\s+((?:[^\s]+(?:\s+[^\s]+)*)?)\s+([0-9A-Fa-f-]+)\s+([\d.]+)\s+(iPhone|iPad)\s+iOS");
		var match = regex.Match(stringToTest);
		if (match.Success)
		{
			deviceName = match.Groups[1].Value;
			deviceUdid = match.Groups[2].Value;
			deviceVersion = match.Groups[3].Value;
			deviceOS = match.Groups[4].Value;
			Information("DeviceName:{0} udid:{1} version:{2} os:{3}", deviceName, deviceUdid, deviceVersion, deviceOS);
			if (version.Contains(deviceVersion.Split(".")[0]))
			{
				Information("We want this device: {0} {1} because it matches {2}", deviceName, deviceVersion, version);
				DEVICE_UDID = deviceUdid;
				DEVICE_VERSION = deviceVersion;
				DEVICE_NAME = deviceName;
				DEVICE_OS = deviceOS;
				break;
			}
		}
		else
		{
			Information("No match found for {0}", stringToTest);
		}
	}
	if (string.IsNullOrEmpty(DEVICE_UDID))
	{
		throw new Exception($"No devices found for version {version}");
	}
}
