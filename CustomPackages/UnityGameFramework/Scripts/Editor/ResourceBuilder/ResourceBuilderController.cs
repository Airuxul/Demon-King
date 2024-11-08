//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.FileSystem;
using GameFramework.Resource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UnityGameFramework.Editor.ResourceTools
{
    public sealed partial class ResourceBuilderController
    {
        private const string RemoteVersionListFileName = "GameFrameworkVersion.dat";
        private const string LocalVersionListFileName = "GameFrameworkList.dat";
        private const string DefaultExtension = "dat";
        private const string NoneOptionName = "<None>";
        private static readonly int AssetsStringLength = "Assets".Length;

        private readonly string _ConfigurationPath;
        private readonly ResourceCollection _ResourceCollection;
        private readonly ResourceAnalyzerController _ResourceAnalyzerController;
        private readonly SortedDictionary<string, ResourceData> _ResourceDatas;
        private readonly Dictionary<string, IFileSystem> _OutputPackageFileSystems;
        private readonly Dictionary<string, IFileSystem> _OutputPackedFileSystems;
        private readonly BuildReport _BuildReport;
        private readonly List<string> _CompressionHelperTypeNames;
        private readonly List<string> _BuildEventHandlerTypeNames;
        private IBuildEventHandler _BuildEventHandler;
        private IFileSystemManager _FileSystemManager;

        public ResourceBuilderController()
        {
            _ConfigurationPath = Type.GetConfigurationPath<ResourceBuilderConfigPathAttribute>() ?? Utility.Path.GetRegularPath(Path.Combine(Application.dataPath, "GameFramework/Configs/ResourceBuilder.xml"));

            _ResourceCollection = new ResourceCollection();
            _ResourceCollection.OnLoadingResource += delegate (int index, int count)
            {
                if (OnLoadingResource != null)
                {
                    OnLoadingResource(index, count);
                }
            };

            _ResourceCollection.OnLoadingAsset += delegate (int index, int count)
            {
                if (OnLoadingAsset != null)
                {
                    OnLoadingAsset(index, count);
                }
            };

            _ResourceCollection.OnLoadCompleted += delegate ()
            {
                if (OnLoadCompleted != null)
                {
                    OnLoadCompleted();
                }
            };

            _ResourceAnalyzerController = new ResourceAnalyzerController(_ResourceCollection);

            _ResourceAnalyzerController.OnAnalyzingAsset += delegate (int index, int count)
            {
                if (OnAnalyzingAsset != null)
                {
                    OnAnalyzingAsset(index, count);
                }
            };

            _ResourceAnalyzerController.OnAnalyzeCompleted += delegate ()
            {
                if (OnAnalyzeCompleted != null)
                {
                    OnAnalyzeCompleted();
                }
            };

            _ResourceDatas = new SortedDictionary<string, ResourceData>(StringComparer.Ordinal);
            _OutputPackageFileSystems = new Dictionary<string, IFileSystem>(StringComparer.Ordinal);
            _OutputPackedFileSystems = new Dictionary<string, IFileSystem>(StringComparer.Ordinal);
            _BuildReport = new BuildReport();

            _CompressionHelperTypeNames = new List<string>
            {
                NoneOptionName
            };

            _BuildEventHandlerTypeNames = new List<string>
            {
                NoneOptionName
            };

            _CompressionHelperTypeNames.AddRange(Type.GetRuntimeOrEditorTypeNames(typeof(Utility.Compression.ICompressionHelper)));
            _BuildEventHandlerTypeNames.AddRange(Type.GetRuntimeOrEditorTypeNames(typeof(IBuildEventHandler)));
            _BuildEventHandler = null;
            _FileSystemManager = null;

            Platforms = Platform.Undefined;
            AssetBundleCompression = AssetBundleCompressionType.LZ4;
            CompressionHelperTypeName = string.Empty;
            AdditionalCompressionSelected = false;
            ForceRebuildAssetBundleSelected = false;
            BuildEventHandlerTypeName = string.Empty;
            OutputDirectory = string.Empty;
            OutputPackageSelected = OutputFullSelected = OutputPackedSelected = true;
        }

        public string ProductName
        {
            get
            {
                return PlayerSettings.productName;
            }
        }

        public string CompanyName
        {
            get
            {
                return PlayerSettings.companyName;
            }
        }

        public string GameIdentifier
        {
            get
            {
#if UNITY_5_6_OR_NEWER
                return PlayerSettings.applicationIdentifier;
#else
                return PlayerSettings.bundleIdentifier;
#endif
            }
        }

        public string GameFrameworkVersion
        {
            get
            {
                return GameFramework.Version.GameFrameworkVersion;
            }
        }

        public string UnityVersion
        {
            get
            {
                return Application.unityVersion;
            }
        }

        public string ApplicableGameVersion
        {
            get
            {
                return Application.version;
            }
        }

        public int InternalResourceVersion
        {
            get;
            set;
        }

        public Platform Platforms
        {
            get;
            set;
        }

        public AssetBundleCompressionType AssetBundleCompression
        {
            get;
            set;
        }

        public string CompressionHelperTypeName
        {
            get;
            set;
        }

        public bool AdditionalCompressionSelected
        {
            get;
            set;
        }

        public bool ForceRebuildAssetBundleSelected
        {
            get;
            set;
        }

        public string BuildEventHandlerTypeName
        {
            get;
            set;
        }

        public string OutputDirectory
        {
            get;
            set;
        }

        public bool OutputPackageSelected
        {
            get;
            set;
        }

        public bool OutputFullSelected
        {
            get;
            set;
        }

        public bool OutputPackedSelected
        {
            get;
            set;
        }

        public bool IsValidOutputDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(OutputDirectory))
                {
                    return false;
                }

                if (!Directory.Exists(OutputDirectory))
                {
                    return false;
                }

                return true;
            }
        }

        public string WorkingPath
        {
            get
            {
                if (!IsValidOutputDirectory)
                {
                    return string.Empty;
                }

                return Utility.Path.GetRegularPath(new DirectoryInfo(Utility.Text.Format("{0}/Working/", OutputDirectory)).FullName);
            }
        }

        public string OutputPackagePath
        {
            get
            {
                if (!IsValidOutputDirectory)
                {
                    return string.Empty;
                }

                return Utility.Path.GetRegularPath(new DirectoryInfo(Utility.Text.Format("{0}/Package/{1}_{2}/", OutputDirectory, ApplicableGameVersion.Replace('.', '_'), InternalResourceVersion)).FullName);
            }
        }

        public string OutputFullPath
        {
            get
            {
                if (!IsValidOutputDirectory)
                {
                    return string.Empty;
                }

                return Utility.Path.GetRegularPath(new DirectoryInfo(Utility.Text.Format("{0}/Full/{1}_{2}/", OutputDirectory, ApplicableGameVersion.Replace('.', '_'), InternalResourceVersion)).FullName);
            }
        }

        public string OutputPackedPath
        {
            get
            {
                if (!IsValidOutputDirectory)
                {
                    return string.Empty;
                }

                return Utility.Path.GetRegularPath(new DirectoryInfo(Utility.Text.Format("{0}/Packed/{1}_{2}/", OutputDirectory, ApplicableGameVersion.Replace('.', '_'), InternalResourceVersion)).FullName);
            }
        }

        public string BuildReportPath
        {
            get
            {
                if (!IsValidOutputDirectory)
                {
                    return string.Empty;
                }

                return Utility.Path.GetRegularPath(new DirectoryInfo(Utility.Text.Format("{0}/BuildReport/{1}_{2}/", OutputDirectory, ApplicableGameVersion.Replace('.', '_'), InternalResourceVersion)).FullName);
            }
        }

        public event GameFrameworkAction<int, int> OnLoadingResource = null;

        public event GameFrameworkAction<int, int> OnLoadingAsset = null;

        public event GameFrameworkAction OnLoadCompleted = null;

        public event GameFrameworkAction<int, int> OnAnalyzingAsset = null;

        public event GameFrameworkAction OnAnalyzeCompleted = null;

        public event GameFrameworkFunc<string, float, bool> ProcessingAssetBundle = null;

        public event GameFrameworkFunc<string, float, bool> ProcessingBinary = null;

        public event GameFrameworkAction<Platform> ProcessResourceComplete = null;

        public event GameFrameworkAction<string> BuildResourceError = null;

        public bool Load()
        {
            if (!File.Exists(_ConfigurationPath))
            {
                return false;
            }

            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(_ConfigurationPath);
                XmlNode xmlRoot = xmlDocument.SelectSingleNode("UnityGameFramework");
                XmlNode xmlEditor = xmlRoot.SelectSingleNode("ResourceBuilder");
                XmlNode xmlSettings = xmlEditor.SelectSingleNode("Settings");

                XmlNodeList xmlNodeList = null;
                XmlNode xmlNode = null;

                xmlNodeList = xmlSettings.ChildNodes;
                for (int i = 0; i < xmlNodeList.Count; i++)
                {
                    xmlNode = xmlNodeList.Item(i);
                    switch (xmlNode.Name)
                    {
                        case "InternalResourceVersion":
                            InternalResourceVersion = int.Parse(xmlNode.InnerText) + 1;
                            break;

                        case "Platforms":
                            Platforms = (Platform)int.Parse(xmlNode.InnerText);
                            break;

                        case "AssetBundleCompression":
                            AssetBundleCompression = (AssetBundleCompressionType)byte.Parse(xmlNode.InnerText);
                            break;

                        case "CompressionHelperTypeName":
                            CompressionHelperTypeName = xmlNode.InnerText;
                            break;

                        case "AdditionalCompressionSelected":
                            AdditionalCompressionSelected = bool.Parse(xmlNode.InnerText);
                            break;

                        case "ForceRebuildAssetBundleSelected":
                            ForceRebuildAssetBundleSelected = bool.Parse(xmlNode.InnerText);
                            break;

                        case "BuildEventHandlerTypeName":
                            BuildEventHandlerTypeName = xmlNode.InnerText;
                            break;

                        case "OutputDirectory":
                            OutputDirectory = xmlNode.InnerText;
                            break;

                        case "OutputPackageSelected":
                            OutputPackageSelected = bool.Parse(xmlNode.InnerText);
                            break;

                        case "OutputFullSelected":
                            OutputFullSelected = bool.Parse(xmlNode.InnerText);
                            break;

                        case "OutputPackedSelected":
                            OutputPackedSelected = bool.Parse(xmlNode.InnerText);
                            break;
                    }
                }
            }
            catch
            {
                File.Delete(_ConfigurationPath);
                return false;
            }

            return true;
        }

        public bool Save()
        {
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null));

                XmlElement xmlRoot = xmlDocument.CreateElement("UnityGameFramework");
                xmlDocument.AppendChild(xmlRoot);

                XmlElement xmlBuilder = xmlDocument.CreateElement("ResourceBuilder");
                xmlRoot.AppendChild(xmlBuilder);

                XmlElement xmlSettings = xmlDocument.CreateElement("Settings");
                xmlBuilder.AppendChild(xmlSettings);

                XmlElement xmlElement = null;

                xmlElement = xmlDocument.CreateElement("InternalResourceVersion");
                xmlElement.InnerText = InternalResourceVersion.ToString();
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("Platforms");
                xmlElement.InnerText = ((int)Platforms).ToString();
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("AssetBundleCompression");
                xmlElement.InnerText = ((byte)AssetBundleCompression).ToString();
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("CompressionHelperTypeName");
                xmlElement.InnerText = CompressionHelperTypeName;
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("AdditionalCompressionSelected");
                xmlElement.InnerText = AdditionalCompressionSelected.ToString();
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("ForceRebuildAssetBundleSelected");
                xmlElement.InnerText = ForceRebuildAssetBundleSelected.ToString();
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("BuildEventHandlerTypeName");
                xmlElement.InnerText = BuildEventHandlerTypeName;
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("OutputDirectory");
                xmlElement.InnerText = OutputDirectory;
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("OutputPackageSelected");
                xmlElement.InnerText = OutputPackageSelected.ToString();
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("OutputFullSelected");
                xmlElement.InnerText = OutputFullSelected.ToString();
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("OutputPackedSelected");
                xmlElement.InnerText = OutputPackedSelected.ToString();
                xmlSettings.AppendChild(xmlElement);

                string configurationDirectoryName = Path.GetDirectoryName(_ConfigurationPath);
                if (!Directory.Exists(configurationDirectoryName))
                {
                    Directory.CreateDirectory(configurationDirectoryName);
                }

                xmlDocument.Save(_ConfigurationPath);
                AssetDatabase.Refresh();
                return true;
            }
            catch
            {
                if (File.Exists(_ConfigurationPath))
                {
                    File.Delete(_ConfigurationPath);
                }

                return false;
            }
        }

        public string[] GetCompressionHelperTypeNames()
        {
            return _CompressionHelperTypeNames.ToArray();
        }

        public string[] GetBuildEventHandlerTypeNames()
        {
            return _BuildEventHandlerTypeNames.ToArray();
        }

        public bool IsPlatformSelected(Platform platform)
        {
            return (Platforms & platform) != 0;
        }

        public void SelectPlatform(Platform platform, bool selected)
        {
            if (selected)
            {
                Platforms |= platform;
            }
            else
            {
                Platforms &= ~platform;
            }
        }

        public bool RefreshCompressionHelper()
        {
            bool retVal = false;
            if (!string.IsNullOrEmpty(CompressionHelperTypeName) && _CompressionHelperTypeNames.Contains(CompressionHelperTypeName))
            {
                System.Type compressionHelperType = Utility.Assembly.GetType(CompressionHelperTypeName);
                if (compressionHelperType != null)
                {
                    Utility.Compression.ICompressionHelper compressionHelper = (Utility.Compression.ICompressionHelper)Activator.CreateInstance(compressionHelperType);
                    if (compressionHelper != null)
                    {
                        Utility.Compression.SetCompressionHelper(compressionHelper);
                        return true;
                    }
                }
            }
            else
            {
                retVal = true;
            }

            CompressionHelperTypeName = string.Empty;
            Utility.Compression.SetCompressionHelper(null);
            return retVal;
        }

        public bool RefreshBuildEventHandler()
        {
            bool retVal = false;
            if (!string.IsNullOrEmpty(BuildEventHandlerTypeName) && _BuildEventHandlerTypeNames.Contains(BuildEventHandlerTypeName))
            {
                System.Type buildEventHandlerType = Utility.Assembly.GetType(BuildEventHandlerTypeName);
                if (buildEventHandlerType != null)
                {
                    IBuildEventHandler buildEventHandler = (IBuildEventHandler)Activator.CreateInstance(buildEventHandlerType);
                    if (buildEventHandler != null)
                    {
                        _BuildEventHandler = buildEventHandler;
                        return true;
                    }
                }
            }
            else
            {
                retVal = true;
            }

            BuildEventHandlerTypeName = string.Empty;
            _BuildEventHandler = null;
            return retVal;
        }

        public bool BuildResources()
        {
            if (!IsValidOutputDirectory)
            {
                return false;
            }

            if (Directory.Exists(OutputPackagePath))
            {
                Directory.Delete(OutputPackagePath, true);
            }

            Directory.CreateDirectory(OutputPackagePath);

            if (Directory.Exists(OutputFullPath))
            {
                Directory.Delete(OutputFullPath, true);
            }

            Directory.CreateDirectory(OutputFullPath);

            if (Directory.Exists(OutputPackedPath))
            {
                Directory.Delete(OutputPackedPath, true);
            }

            Directory.CreateDirectory(OutputPackedPath);

            if (Directory.Exists(BuildReportPath))
            {
                Directory.Delete(BuildReportPath, true);
            }

            Directory.CreateDirectory(BuildReportPath);

            BuildAssetBundleOptions buildAssetBundleOptions = GetBuildAssetBundleOptions();
            _BuildReport.Initialize(BuildReportPath, ProductName, CompanyName, GameIdentifier, GameFrameworkVersion, UnityVersion, ApplicableGameVersion, InternalResourceVersion,
                Platforms, AssetBundleCompression, CompressionHelperTypeName, AdditionalCompressionSelected, ForceRebuildAssetBundleSelected, BuildEventHandlerTypeName, OutputDirectory, buildAssetBundleOptions, _ResourceDatas);

            try
            {
                _BuildReport.LogInfo("Build Start Time: {0:yyyy-MM-dd HH:mm:ss.fff}", DateTime.UtcNow.ToLocalTime());

                if (_BuildEventHandler != null)
                {
                    _BuildReport.LogInfo("Execute build event handler 'OnPreprocessAllPlatforms'...");
                    _BuildEventHandler.OnPreprocessAllPlatforms(ProductName, CompanyName, GameIdentifier, GameFrameworkVersion, UnityVersion, ApplicableGameVersion, InternalResourceVersion,
                        Platforms, AssetBundleCompression, CompressionHelperTypeName, AdditionalCompressionSelected, ForceRebuildAssetBundleSelected, BuildEventHandlerTypeName, OutputDirectory, buildAssetBundleOptions,
                        WorkingPath, OutputPackageSelected, OutputPackagePath, OutputFullSelected, OutputFullPath, OutputPackedSelected, OutputPackedPath, BuildReportPath);
                }

                _BuildReport.LogInfo("Start prepare resource collection...");
                if (!_ResourceCollection.Load())
                {
                    _BuildReport.LogError("Can not parse 'ResourceCollection.xml', please use 'Resource Editor' tool first.");

                    if (_BuildEventHandler != null)
                    {
                        _BuildReport.LogInfo("Execute build event handler 'OnPostprocessAllPlatforms'...");
                        _BuildEventHandler.OnPostprocessAllPlatforms(ProductName, CompanyName, GameIdentifier, GameFrameworkVersion, UnityVersion, ApplicableGameVersion, InternalResourceVersion,
                            Platforms, AssetBundleCompression, CompressionHelperTypeName, AdditionalCompressionSelected, ForceRebuildAssetBundleSelected, BuildEventHandlerTypeName, OutputDirectory, buildAssetBundleOptions,
                            WorkingPath, OutputPackageSelected, OutputPackagePath, OutputFullSelected, OutputFullPath, OutputPackedSelected, OutputPackedPath, BuildReportPath);
                    }

                    _BuildReport.SaveReport();
                    return false;
                }

                if (Platforms == Platform.Undefined)
                {
                    _BuildReport.LogError("Platform undefined.");

                    if (_BuildEventHandler != null)
                    {
                        _BuildReport.LogInfo("Execute build event handler 'OnPostprocessAllPlatforms'...");
                        _BuildEventHandler.OnPostprocessAllPlatforms(ProductName, CompanyName, GameIdentifier, GameFrameworkVersion, UnityVersion, ApplicableGameVersion, InternalResourceVersion,
                            Platforms, AssetBundleCompression, CompressionHelperTypeName, AdditionalCompressionSelected, ForceRebuildAssetBundleSelected, BuildEventHandlerTypeName, OutputDirectory, buildAssetBundleOptions,
                            WorkingPath, OutputPackageSelected, OutputPackagePath, OutputFullSelected, OutputFullPath, OutputPackedSelected, OutputPackedPath, BuildReportPath);
                    }

                    _BuildReport.SaveReport();
                    return false;
                }

                _BuildReport.LogInfo("Prepare resource collection complete.");
                _BuildReport.LogInfo("Start analyze assets dependency...");

                _ResourceAnalyzerController.Analyze();

                _BuildReport.LogInfo("Analyze assets dependency complete.");
                _BuildReport.LogInfo("Start prepare build data...");

                AssetBundleBuild[] assetBundleBuildDatas = null;
                ResourceData[] assetBundleResourceDatas = null;
                ResourceData[] binaryResourceDatas = null;
                if (!PrepareBuildData(out assetBundleBuildDatas, out assetBundleResourceDatas, out binaryResourceDatas))
                {
                    _BuildReport.LogError("Prepare resource build data failure.");

                    if (_BuildEventHandler != null)
                    {
                        _BuildReport.LogInfo("Execute build event handler 'OnPostprocessAllPlatforms'...");
                        _BuildEventHandler.OnPostprocessAllPlatforms(ProductName, CompanyName, GameIdentifier, GameFrameworkVersion, UnityVersion, ApplicableGameVersion, InternalResourceVersion,
                            Platforms, AssetBundleCompression, CompressionHelperTypeName, AdditionalCompressionSelected, ForceRebuildAssetBundleSelected, BuildEventHandlerTypeName, OutputDirectory, buildAssetBundleOptions,
                            WorkingPath, OutputPackageSelected, OutputPackagePath, OutputFullSelected, OutputFullPath, OutputPackedSelected, OutputPackedPath, BuildReportPath);
                    }

                    _BuildReport.SaveReport();
                    return false;
                }

                _BuildReport.LogInfo("Prepare resource build data complete.");
                _BuildReport.LogInfo("Start build resources for selected platforms...");

                bool watchResult = _BuildEventHandler == null || !_BuildEventHandler.ContinueOnFailure;
                bool isSuccess = false;
                isSuccess = BuildResources(Platform.Windows, assetBundleBuildDatas, buildAssetBundleOptions, assetBundleResourceDatas, binaryResourceDatas);

                if (!watchResult || isSuccess)
                {
                    isSuccess = BuildResources(Platform.Windows64, assetBundleBuildDatas, buildAssetBundleOptions, assetBundleResourceDatas, binaryResourceDatas);
                }

                if (!watchResult || isSuccess)
                {
                    isSuccess = BuildResources(Platform.MacOS, assetBundleBuildDatas, buildAssetBundleOptions, assetBundleResourceDatas, binaryResourceDatas);
                }

                if (!watchResult || isSuccess)
                {
                    isSuccess = BuildResources(Platform.Linux, assetBundleBuildDatas, buildAssetBundleOptions, assetBundleResourceDatas, binaryResourceDatas);
                }

                if (!watchResult || isSuccess)
                {
                    isSuccess = BuildResources(Platform.IOS, assetBundleBuildDatas, buildAssetBundleOptions, assetBundleResourceDatas, binaryResourceDatas);
                }

                if (!watchResult || isSuccess)
                {
                    isSuccess = BuildResources(Platform.Android, assetBundleBuildDatas, buildAssetBundleOptions, assetBundleResourceDatas, binaryResourceDatas);
                }

                if (!watchResult || isSuccess)
                {
                    isSuccess = BuildResources(Platform.WindowsStore, assetBundleBuildDatas, buildAssetBundleOptions, assetBundleResourceDatas, binaryResourceDatas);
                }

                if (!watchResult || isSuccess)
                {
                    isSuccess = BuildResources(Platform.WebGL, assetBundleBuildDatas, buildAssetBundleOptions, assetBundleResourceDatas, binaryResourceDatas);
                }

                if (_BuildEventHandler != null)
                {
                    _BuildReport.LogInfo("Execute build event handler 'OnPostprocessAllPlatforms'...");
                    _BuildEventHandler.OnPostprocessAllPlatforms(ProductName, CompanyName, GameIdentifier, GameFrameworkVersion, UnityVersion, ApplicableGameVersion, InternalResourceVersion,
                        Platforms, AssetBundleCompression, CompressionHelperTypeName, AdditionalCompressionSelected, ForceRebuildAssetBundleSelected, BuildEventHandlerTypeName, OutputDirectory, buildAssetBundleOptions,
                        WorkingPath, OutputPackageSelected, OutputPackagePath, OutputFullSelected, OutputFullPath, OutputPackedSelected, OutputPackedPath, BuildReportPath);
                }

                _BuildReport.LogInfo("Build resources for selected platforms complete.");
                _BuildReport.SaveReport();

                return true;
            }
            catch (Exception exception)
            {
                string errorMessage = exception.ToString();
                _BuildReport.LogFatal(errorMessage);
                _BuildReport.SaveReport();
                if (BuildResourceError != null)
                {
                    BuildResourceError(errorMessage);
                }

                return false;
            }
            finally
            {
                _OutputPackageFileSystems.Clear();
                _OutputPackedFileSystems.Clear();
                if (_FileSystemManager != null)
                {
                    GameFrameworkEntry.Shutdown();
                    _FileSystemManager = null;
                }
            }
        }

        private bool BuildResources(Platform platform, AssetBundleBuild[] assetBundleBuildDatas, BuildAssetBundleOptions buildAssetBundleOptions, ResourceData[] assetBundleResourceDatas, ResourceData[] binaryResourceDatas)
        {
            if (!IsPlatformSelected(platform))
            {
                return true;
            }

            string platformName = platform.ToString();
            _BuildReport.LogInfo("Start build resources for '{0}'...", platformName);

            string workingPath = Utility.Text.Format("{0}{1}/", WorkingPath, platformName);
            _BuildReport.LogInfo("Working path is '{0}'.", workingPath);

            string outputPackagePath = Utility.Text.Format("{0}{1}/", OutputPackagePath, platformName);
            if (OutputPackageSelected)
            {
                Directory.CreateDirectory(outputPackagePath);
                _BuildReport.LogInfo("Output package is selected, path is '{0}'.", outputPackagePath);
            }
            else
            {
                _BuildReport.LogInfo("Output package is not selected.");
            }

            string outputFullPath = Utility.Text.Format("{0}{1}/", OutputFullPath, platformName);
            if (OutputFullSelected)
            {
                Directory.CreateDirectory(outputFullPath);
                _BuildReport.LogInfo("Output full is selected, path is '{0}'.", outputFullPath);
            }
            else
            {
                _BuildReport.LogInfo("Output full is not selected.");
            }

            string outputPackedPath = Utility.Text.Format("{0}{1}/", OutputPackedPath, platformName);
            if (OutputPackedSelected)
            {
                Directory.CreateDirectory(outputPackedPath);
                _BuildReport.LogInfo("Output packed is selected, path is '{0}'.", outputPackedPath);
            }
            else
            {
                _BuildReport.LogInfo("Output packed is not selected.");
            }

            // Clean working path
            List<string> validNames = new List<string>();
            foreach (ResourceData assetBundleResourceData in assetBundleResourceDatas)
            {
                validNames.Add(GetResourceFullName(assetBundleResourceData.Name, assetBundleResourceData.Variant).ToLowerInvariant());
            }

            if (Directory.Exists(workingPath))
            {
                Uri workingUri = new Uri(workingPath, UriKind.Absolute);
                string[] fileNames = Directory.GetFiles(workingPath, "*", SearchOption.AllDirectories);
                foreach (string fileName in fileNames)
                {
                    if (fileName.EndsWith(".manifest", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string relativeName = workingUri.MakeRelativeUri(new Uri(fileName, UriKind.Absolute)).ToString();
                    if (!validNames.Contains(relativeName))
                    {
                        File.Delete(fileName);
                    }
                }

                string[] manifestNames = Directory.GetFiles(workingPath, "*.manifest", SearchOption.AllDirectories);
                foreach (string manifestName in manifestNames)
                {
                    if (!File.Exists(manifestName.Substring(0, manifestName.LastIndexOf('.'))))
                    {
                        File.Delete(manifestName);
                    }
                }

                Utility.Path.RemoveEmptyDirectory(workingPath);
            }

            if (!Directory.Exists(workingPath))
            {
                Directory.CreateDirectory(workingPath);
            }

            if (_BuildEventHandler != null)
            {
                _BuildReport.LogInfo("Execute build event handler 'OnPreprocessPlatform' for '{0}'...", platformName);
                _BuildEventHandler.OnPreprocessPlatform(platform, workingPath, OutputPackageSelected, outputPackagePath, OutputFullSelected, outputFullPath, OutputPackedSelected, outputPackedPath);
            }

            // Build AssetBundles
            _BuildReport.LogInfo("Unity start build asset bundles for '{0}'...", platformName);
            AssetBundleManifest assetBundleManifest = BuildPipeline.BuildAssetBundles(workingPath, assetBundleBuildDatas, buildAssetBundleOptions, GetBuildTarget(platform));
            if (assetBundleManifest == null)
            {
                _BuildReport.LogError("Build asset bundles for '{0}' failure.", platformName);

                if (_BuildEventHandler != null)
                {
                    _BuildReport.LogInfo("Execute build event handler 'OnPostprocessPlatform' for '{0}'...", platformName);
                    _BuildEventHandler.OnPostprocessPlatform(platform, workingPath, OutputPackageSelected, outputPackagePath, OutputFullSelected, outputFullPath, OutputPackedSelected, outputPackedPath, false);
                }

                return false;
            }

            if (_BuildEventHandler != null)
            {
                _BuildReport.LogInfo("Execute build event handler 'OnBuildAssetBundlesComplete' for '{0}'...", platformName);
                _BuildEventHandler.OnBuildAssetBundlesComplete(platform, workingPath, OutputPackageSelected, outputPackagePath, OutputFullSelected, outputFullPath, OutputPackedSelected, outputPackedPath, assetBundleManifest);
            }

            _BuildReport.LogInfo("Unity build asset bundles for '{0}' complete.", platformName);

            // Create FileSystems
            _BuildReport.LogInfo("Start create file system for '{0}'...", platformName);

            if (OutputPackageSelected)
            {
                CreateFileSystems(_ResourceDatas.Values, outputPackagePath, _OutputPackageFileSystems);
            }

            if (OutputPackedSelected)
            {
                CreateFileSystems(GetPackedResourceDatas(), outputPackedPath, _OutputPackedFileSystems);
            }

            _BuildReport.LogInfo("Create file system for '{0}' complete.", platformName);

            // Process AssetBundles
            for (int i = 0; i < assetBundleResourceDatas.Length; i++)
            {
                string fullName = GetResourceFullName(assetBundleResourceDatas[i].Name, assetBundleResourceDatas[i].Variant);
                if (ProcessingAssetBundle != null)
                {
                    if (ProcessingAssetBundle(fullName, (float)(i + 1) / assetBundleResourceDatas.Length))
                    {
                        _BuildReport.LogWarning("The build has been canceled by user.");

                        if (_BuildEventHandler != null)
                        {
                            _BuildReport.LogInfo("Execute build event handler 'OnPostprocessPlatform' for '{0}'...", platformName);
                            _BuildEventHandler.OnPostprocessPlatform(platform, workingPath, OutputPackageSelected, outputPackagePath, OutputFullSelected, outputFullPath, OutputPackedSelected, outputPackedPath, false);
                        }

                        return false;
                    }
                }

                _BuildReport.LogInfo("Start process asset bundle '{0}' for '{1}'...", fullName, platformName);

                if (!ProcessAssetBundle(platform, workingPath, outputPackagePath, outputFullPath, outputPackedPath, AdditionalCompressionSelected, assetBundleResourceDatas[i].Name, assetBundleResourceDatas[i].Variant, assetBundleResourceDatas[i].FileSystem))
                {
                    return false;
                }

                _BuildReport.LogInfo("Process asset bundle '{0}' for '{1}' complete.", fullName, platformName);
            }

            // Process Binaries
            for (int i = 0; i < binaryResourceDatas.Length; i++)
            {
                string fullName = GetResourceFullName(binaryResourceDatas[i].Name, binaryResourceDatas[i].Variant);
                if (ProcessingBinary != null)
                {
                    if (ProcessingBinary(fullName, (float)(i + 1) / binaryResourceDatas.Length))
                    {
                        _BuildReport.LogWarning("The build has been canceled by user.");

                        if (_BuildEventHandler != null)
                        {
                            _BuildReport.LogInfo("Execute build event handler 'OnPostprocessPlatform' for '{0}'...", platformName);
                            _BuildEventHandler.OnPostprocessPlatform(platform, workingPath, OutputPackageSelected, outputPackagePath, OutputFullSelected, outputFullPath, OutputPackedSelected, outputPackedPath, false);
                        }

                        return false;
                    }
                }

                _BuildReport.LogInfo("Start process binary '{0}' for '{1}'...", fullName, platformName);

                if (!ProcessBinary(platform, workingPath, outputPackagePath, outputFullPath, outputPackedPath, AdditionalCompressionSelected, binaryResourceDatas[i].Name, binaryResourceDatas[i].Variant, binaryResourceDatas[i].FileSystem))
                {
                    return false;
                }

                _BuildReport.LogInfo("Process binary '{0}' for '{1}' complete.", fullName, platformName);
            }

            if (OutputPackageSelected)
            {
                ProcessPackageVersionList(outputPackagePath, platform);
                _BuildReport.LogInfo("Process package version list for '{0}' complete.", platformName);
            }

            if (OutputFullSelected)
            {
                VersionListData versionListData = ProcessUpdatableVersionList(outputFullPath, platform);
                _BuildReport.LogInfo("Process updatable version list for '{0}' complete, updatable version list path is '{1}', length is '{2}', hash code is '{3}[0x{3:X8}]', compressed length is '{4}', compressed hash code is '{5}[0x{5:X8}]'.", platformName, versionListData.Path, versionListData.Length, versionListData.HashCode, versionListData.CompressedLength, versionListData.CompressedHashCode);
                if (_BuildEventHandler != null)
                {
                    _BuildReport.LogInfo("Execute build event handler 'OnOutputUpdatableVersionListData' for '{0}'...", platformName);
                    _BuildEventHandler.OnOutputUpdatableVersionListData(platform, versionListData.Path, versionListData.Length, versionListData.HashCode, versionListData.CompressedLength, versionListData.CompressedHashCode);
                }
            }

            if (OutputPackedSelected)
            {
                ProcessReadOnlyVersionList(outputPackedPath, platform);
                _BuildReport.LogInfo("Process read-only version list for '{0}' complete.", platformName);
            }

            if (_BuildEventHandler != null)
            {
                _BuildReport.LogInfo("Execute build event handler 'OnPostprocessPlatform' for '{0}'...", platformName);
                _BuildEventHandler.OnPostprocessPlatform(platform, workingPath, OutputPackageSelected, outputPackagePath, OutputFullSelected, outputFullPath, OutputPackedSelected, outputPackedPath, true);
            }

            if (ProcessResourceComplete != null)
            {
                ProcessResourceComplete(platform);
            }

            _BuildReport.LogInfo("Build resources for '{0}' success.", platformName);
            return true;
        }

        private bool ProcessAssetBundle(Platform platform, string workingPath, string outputPackagePath, string outputFullPath, string outputPackedPath, bool additionalCompressionSelected, string name, string variant, string fileSystem)
        {
            string fullName = GetResourceFullName(name, variant);
            ResourceData resourceData = _ResourceDatas[fullName];
            string workingName = Utility.Path.GetRegularPath(Path.Combine(workingPath, fullName.ToLowerInvariant()));

            byte[] bytes = File.ReadAllBytes(workingName);
            int length = bytes.Length;
            int hashCode = Utility.Verifier.GetCrc32(bytes);
            int compressedLength = length;
            int compressedHashCode = hashCode;

            byte[] hashBytes = Utility.Converter.GetBytes(hashCode);
            if (resourceData.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt)
            {
                bytes = Utility.Encryption.GetQuickXorBytes(bytes, hashBytes);
            }
            else if (resourceData.LoadType == LoadType.LoadFromMemoryAndDecrypt)
            {
                bytes = Utility.Encryption.GetXorBytes(bytes, hashBytes);
            }

            return ProcessOutput(platform, outputPackagePath, outputFullPath, outputPackedPath, additionalCompressionSelected, name, variant, fileSystem, resourceData, bytes, length, hashCode, compressedLength, compressedHashCode);
        }

        private bool ProcessBinary(Platform platform, string workingPath, string outputPackagePath, string outputFullPath, string outputPackedPath, bool additionalCompressionSelected, string name, string variant, string fileSystem)
        {
            string fullName = GetResourceFullName(name, variant);
            ResourceData resourceData = _ResourceDatas[fullName];
            string assetName = resourceData.GetAssetNames()[0];
            string assetPath = Utility.Path.GetRegularPath(Application.dataPath.Substring(0, Application.dataPath.Length - AssetsStringLength) + assetName);

            byte[] bytes = File.ReadAllBytes(assetPath);
            int length = bytes.Length;
            int hashCode = Utility.Verifier.GetCrc32(bytes);
            int compressedLength = length;
            int compressedHashCode = hashCode;

            byte[] hashBytes = Utility.Converter.GetBytes(hashCode);
            if (resourceData.LoadType == LoadType.LoadFromBinaryAndQuickDecrypt)
            {
                bytes = Utility.Encryption.GetQuickXorBytes(bytes, hashBytes);
            }
            else if (resourceData.LoadType == LoadType.LoadFromBinaryAndDecrypt)
            {
                bytes = Utility.Encryption.GetXorBytes(bytes, hashBytes);
            }

            return ProcessOutput(platform, outputPackagePath, outputFullPath, outputPackedPath, additionalCompressionSelected, name, variant, fileSystem, resourceData, bytes, length, hashCode, compressedLength, compressedHashCode);
        }

        private void ProcessPackageVersionList(string outputPackagePath, Platform platform)
        {
            Asset[] originalAssets = _ResourceCollection.GetAssets();
            PackageVersionList.Asset[] assets = new PackageVersionList.Asset[originalAssets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                Asset originalAsset = originalAssets[i];
                assets[i] = new PackageVersionList.Asset(originalAsset.Name, GetDependencyAssetIndexes(originalAsset.Name));
            }

            SortedDictionary<string, ResourceData>.ValueCollection resourceDatas = _ResourceDatas.Values;

            int index = 0;
            PackageVersionList.Resource[] resources = new PackageVersionList.Resource[_ResourceCollection.ResourceCount];
            foreach (ResourceData resourceData in resourceDatas)
            {
                ResourceCode resourceCode = resourceData.GetCode(platform);
                resources[index++] = new PackageVersionList.Resource(resourceData.Name, resourceData.Variant, GetExtension(resourceData), (byte)resourceData.LoadType, resourceCode.Length, resourceCode.HashCode, GetAssetIndexes(resourceData));
            }

            string[] fileSystemNames = GetFileSystemNames(resourceDatas);
            PackageVersionList.FileSystem[] fileSystems = new PackageVersionList.FileSystem[fileSystemNames.Length];
            for (int i = 0; i < fileSystems.Length; i++)
            {
                fileSystems[i] = new PackageVersionList.FileSystem(fileSystemNames[i], GetResourceIndexesFromFileSystem(resourceDatas, fileSystemNames[i]));
            }

            string[] resourceGroupNames = GetResourceGroupNames(resourceDatas);
            PackageVersionList.ResourceGroup[] resourceGroups = new PackageVersionList.ResourceGroup[resourceGroupNames.Length];
            for (int i = 0; i < resourceGroups.Length; i++)
            {
                resourceGroups[i] = new PackageVersionList.ResourceGroup(resourceGroupNames[i], GetResourceIndexesFromResourceGroup(resourceDatas, resourceGroupNames[i]));
            }

            PackageVersionList versionList = new PackageVersionList(ApplicableGameVersion, InternalResourceVersion, assets, resources, fileSystems, resourceGroups);
            PackageVersionListSerializer serializer = new PackageVersionListSerializer();
            serializer.RegisterSerializeCallback(0, BuiltinVersionListSerializer.PackageVersionListSerializeCallback_V0);
            serializer.RegisterSerializeCallback(1, BuiltinVersionListSerializer.PackageVersionListSerializeCallback_V1);
            serializer.RegisterSerializeCallback(2, BuiltinVersionListSerializer.PackageVersionListSerializeCallback_V2);
            string packageVersionListPath = Utility.Path.GetRegularPath(Path.Combine(outputPackagePath, RemoteVersionListFileName));
            using (FileStream fileStream = new FileStream(packageVersionListPath, FileMode.Create, FileAccess.Write))
            {
                if (!serializer.Serialize(fileStream, versionList))
                {
                    throw new GameFrameworkException("Serialize package version list failure.");
                }
            }
        }

        private VersionListData ProcessUpdatableVersionList(string outputFullPath, Platform platform)
        {
            Asset[] originalAssets = _ResourceCollection.GetAssets();
            UpdatableVersionList.Asset[] assets = new UpdatableVersionList.Asset[originalAssets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                Asset originalAsset = originalAssets[i];
                assets[i] = new UpdatableVersionList.Asset(originalAsset.Name, GetDependencyAssetIndexes(originalAsset.Name));
            }

            SortedDictionary<string, ResourceData>.ValueCollection resourceDatas = _ResourceDatas.Values;

            int index = 0;
            UpdatableVersionList.Resource[] resources = new UpdatableVersionList.Resource[_ResourceCollection.ResourceCount];
            foreach (ResourceData resourceData in resourceDatas)
            {
                ResourceCode resourceCode = resourceData.GetCode(platform);
                resources[index++] = new UpdatableVersionList.Resource(resourceData.Name, resourceData.Variant, GetExtension(resourceData), (byte)resourceData.LoadType, resourceCode.Length, resourceCode.HashCode, resourceCode.CompressedLength, resourceCode.CompressedHashCode, GetAssetIndexes(resourceData));
            }

            string[] fileSystemNames = GetFileSystemNames(resourceDatas);
            UpdatableVersionList.FileSystem[] fileSystems = new UpdatableVersionList.FileSystem[fileSystemNames.Length];
            for (int i = 0; i < fileSystems.Length; i++)
            {
                fileSystems[i] = new UpdatableVersionList.FileSystem(fileSystemNames[i], GetResourceIndexesFromFileSystem(resourceDatas, fileSystemNames[i]));
            }

            string[] resourceGroupNames = GetResourceGroupNames(resourceDatas);
            UpdatableVersionList.ResourceGroup[] resourceGroups = new UpdatableVersionList.ResourceGroup[resourceGroupNames.Length];
            for (int i = 0; i < resourceGroups.Length; i++)
            {
                resourceGroups[i] = new UpdatableVersionList.ResourceGroup(resourceGroupNames[i], GetResourceIndexesFromResourceGroup(resourceDatas, resourceGroupNames[i]));
            }

            UpdatableVersionList versionList = new UpdatableVersionList(ApplicableGameVersion, InternalResourceVersion, assets, resources, fileSystems, resourceGroups);
            UpdatableVersionListSerializer serializer = new UpdatableVersionListSerializer();
            serializer.RegisterSerializeCallback(0, BuiltinVersionListSerializer.UpdatableVersionListSerializeCallback_V0);
            serializer.RegisterSerializeCallback(1, BuiltinVersionListSerializer.UpdatableVersionListSerializeCallback_V1);
            serializer.RegisterSerializeCallback(2, BuiltinVersionListSerializer.UpdatableVersionListSerializeCallback_V2);
            string updatableVersionListPath = Utility.Path.GetRegularPath(Path.Combine(outputFullPath, RemoteVersionListFileName));
            using (FileStream fileStream = new FileStream(updatableVersionListPath, FileMode.Create, FileAccess.Write))
            {
                if (!serializer.Serialize(fileStream, versionList))
                {
                    throw new GameFrameworkException("Serialize updatable version list failure.");
                }
            }

            byte[] bytes = File.ReadAllBytes(updatableVersionListPath);
            int length = bytes.Length;
            int hashCode = Utility.Verifier.GetCrc32(bytes);
            bytes = Utility.Compression.Compress(bytes);
            int compressedLength = bytes.Length;
            File.WriteAllBytes(updatableVersionListPath, bytes);
            int compressedHashCode = Utility.Verifier.GetCrc32(bytes);
            int dotPosition = RemoteVersionListFileName.LastIndexOf('.');
            string versionListFullNameWithCrc32 = Utility.Text.Format("{0}.{2:x8}.{1}", RemoteVersionListFileName.Substring(0, dotPosition), RemoteVersionListFileName.Substring(dotPosition + 1), hashCode);
            string updatableVersionListPathWithCrc32 = Utility.Path.GetRegularPath(Path.Combine(outputFullPath, versionListFullNameWithCrc32));
            File.Move(updatableVersionListPath, updatableVersionListPathWithCrc32);

            return new VersionListData(updatableVersionListPathWithCrc32, length, hashCode, compressedLength, compressedHashCode);
        }

        private void ProcessReadOnlyVersionList(string outputPackedPath, Platform platform)
        {
            ResourceData[] packedResourceDatas = GetPackedResourceDatas();

            LocalVersionList.Resource[] resources = new LocalVersionList.Resource[packedResourceDatas.Length];
            for (int i = 0; i < resources.Length; i++)
            {
                ResourceData resourceData = packedResourceDatas[i];
                ResourceCode resourceCode = resourceData.GetCode(platform);
                resources[i] = new LocalVersionList.Resource(resourceData.Name, resourceData.Variant, GetExtension(resourceData), (byte)resourceData.LoadType, resourceCode.Length, resourceCode.HashCode);
            }

            string[] packedFileSystemNames = GetFileSystemNames(packedResourceDatas);
            LocalVersionList.FileSystem[] fileSystems = new LocalVersionList.FileSystem[packedFileSystemNames.Length];
            for (int i = 0; i < fileSystems.Length; i++)
            {
                fileSystems[i] = new LocalVersionList.FileSystem(packedFileSystemNames[i], GetResourceIndexesFromFileSystem(packedResourceDatas, packedFileSystemNames[i]));
            }

            LocalVersionList versionList = new LocalVersionList(resources, fileSystems);
            ReadOnlyVersionListSerializer serializer = new ReadOnlyVersionListSerializer();
            serializer.RegisterSerializeCallback(0, BuiltinVersionListSerializer.LocalVersionListSerializeCallback_V0);
            serializer.RegisterSerializeCallback(1, BuiltinVersionListSerializer.LocalVersionListSerializeCallback_V1);
            serializer.RegisterSerializeCallback(2, BuiltinVersionListSerializer.LocalVersionListSerializeCallback_V2);
            string readOnlyVersionListPath = Utility.Path.GetRegularPath(Path.Combine(outputPackedPath, LocalVersionListFileName));
            using (FileStream fileStream = new FileStream(readOnlyVersionListPath, FileMode.Create, FileAccess.Write))
            {
                if (!serializer.Serialize(fileStream, versionList))
                {
                    throw new GameFrameworkException("Serialize read-only version list failure.");
                }
            }
        }

        private int[] GetDependencyAssetIndexes(string assetName)
        {
            List<int> dependencyAssetIndexes = new List<int>();
            Asset[] assets = _ResourceCollection.GetAssets();
            DependencyData dependencyData = _ResourceAnalyzerController.GetDependencyData(assetName);
            foreach (Asset dependencyAsset in dependencyData.GetDependencyAssets())
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] == dependencyAsset)
                    {
                        dependencyAssetIndexes.Add(i);
                        break;
                    }
                }
            }

            dependencyAssetIndexes.Sort();
            return dependencyAssetIndexes.ToArray();
        }

        private int[] GetAssetIndexes(ResourceData resourceData)
        {
            Asset[] assets = _ResourceCollection.GetAssets();
            string[] assetGuids = resourceData.GetAssetGuids();
            int[] assetIndexes = new int[assetGuids.Length];
            for (int i = 0; i < assetGuids.Length; i++)
            {
                assetIndexes[i] = Array.BinarySearch(assets, _ResourceCollection.GetAsset(assetGuids[i]));
                if (assetIndexes[i] < 0)
                {
                    throw new GameFrameworkException("Asset is invalid.");
                }
            }

            return assetIndexes;
        }

        private ResourceData[] GetPackedResourceDatas()
        {
            List<ResourceData> packedResourceDatas = new List<ResourceData>();
            foreach (ResourceData resourceData in _ResourceDatas.Values)
            {
                if (!resourceData.Packed)
                {
                    continue;
                }

                packedResourceDatas.Add(resourceData);
            }

            return packedResourceDatas.ToArray();
        }

        private string[] GetFileSystemNames(IEnumerable<ResourceData> resourceDatas)
        {
            HashSet<string> fileSystemNames = new HashSet<string>();
            foreach (ResourceData resourceData in resourceDatas)
            {
                if (resourceData.FileSystem == null)
                {
                    continue;
                }

                fileSystemNames.Add(resourceData.FileSystem);
            }

            return fileSystemNames.OrderBy(x => x).ToArray();
        }

        private int[] GetResourceIndexesFromFileSystem(IEnumerable<ResourceData> resourceDatas, string fileSystemName)
        {
            int index = 0;
            List<int> resourceIndexes = new List<int>();
            foreach (ResourceData resourceData in resourceDatas)
            {
                if (resourceData.FileSystem == fileSystemName)
                {
                    resourceIndexes.Add(index);
                }

                index++;
            }

            resourceIndexes.Sort();
            return resourceIndexes.ToArray();
        }

        private string[] GetResourceGroupNames(IEnumerable<ResourceData> resourceDatas)
        {
            HashSet<string> resourceGroupNames = new HashSet<string>();
            foreach (ResourceData resourceData in resourceDatas)
            {
                foreach (string resourceGroup in resourceData.GetResourceGroups())
                {
                    resourceGroupNames.Add(resourceGroup);
                }
            }

            return resourceGroupNames.OrderBy(x => x).ToArray();
        }

        private int[] GetResourceIndexesFromResourceGroup(IEnumerable<ResourceData> resourceDatas, string resourceGroupName)
        {
            int index = 0;
            List<int> resourceIndexes = new List<int>();
            foreach (ResourceData resourceData in resourceDatas)
            {
                foreach (string resourceGroup in resourceData.GetResourceGroups())
                {
                    if (resourceGroup == resourceGroupName)
                    {
                        resourceIndexes.Add(index);
                        break;
                    }
                }

                index++;
            }

            resourceIndexes.Sort();
            return resourceIndexes.ToArray();
        }

        private void CreateFileSystems(IEnumerable<ResourceData> resourceDatas, string outputPath, Dictionary<string, IFileSystem> outputFileSystem)
        {
            outputFileSystem.Clear();
            string[] fileSystemNames = GetFileSystemNames(resourceDatas);
            if (fileSystemNames.Length > 0 && _FileSystemManager == null)
            {
                _FileSystemManager = GameFrameworkEntry.GetModule<IFileSystemManager>();
                _FileSystemManager.SetFileSystemHelper(new FileSystemHelper());
            }

            foreach (string fileSystemName in fileSystemNames)
            {
                int fileCount = GetResourceIndexesFromFileSystem(resourceDatas, fileSystemName).Length;
                string fullPath = Utility.Path.GetRegularPath(Path.Combine(outputPath, Utility.Text.Format("{0}.{1}", fileSystemName, DefaultExtension)));
                string directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                IFileSystem fileSystem = _FileSystemManager.CreateFileSystem(fullPath, FileSystemAccess.Write, fileCount, fileCount);
                outputFileSystem.Add(fileSystemName, fileSystem);
            }
        }

        private bool ProcessOutput(Platform platform, string outputPackagePath, string outputFullPath, string outputPackedPath, bool additionalCompressionSelected, string name, string variant, string fileSystem, ResourceData resourceData, byte[] bytes, int length, int hashCode, int compressedLength, int compressedHashCode)
        {
            string fullNameWithExtension = Utility.Text.Format("{0}.{1}", GetResourceFullName(name, variant), GetExtension(resourceData));

            if (OutputPackageSelected)
            {
                if (string.IsNullOrEmpty(fileSystem))
                {
                    string packagePath = Utility.Path.GetRegularPath(Path.Combine(outputPackagePath, fullNameWithExtension));
                    string packageDirectoryName = Path.GetDirectoryName(packagePath);
                    if (!Directory.Exists(packageDirectoryName))
                    {
                        Directory.CreateDirectory(packageDirectoryName);
                    }

                    File.WriteAllBytes(packagePath, bytes);
                }
                else
                {
                    if (!_OutputPackageFileSystems[fileSystem].WriteFile(fullNameWithExtension, bytes))
                    {
                        return false;
                    }
                }
            }

            if (OutputPackedSelected && resourceData.Packed)
            {
                if (string.IsNullOrEmpty(fileSystem))
                {
                    string packedPath = Utility.Path.GetRegularPath(Path.Combine(outputPackedPath, fullNameWithExtension));
                    string packedDirectoryName = Path.GetDirectoryName(packedPath);
                    if (!Directory.Exists(packedDirectoryName))
                    {
                        Directory.CreateDirectory(packedDirectoryName);
                    }

                    File.WriteAllBytes(packedPath, bytes);
                }
                else
                {
                    if (!_OutputPackedFileSystems[fileSystem].WriteFile(fullNameWithExtension, bytes))
                    {
                        return false;
                    }
                }
            }

            if (OutputFullSelected)
            {
                string fullNameWithCrc32AndExtension = variant != null ? Utility.Text.Format("{0}.{1}.{2:x8}.{3}", name, variant, hashCode, DefaultExtension) : Utility.Text.Format("{0}.{1:x8}.{2}", name, hashCode, DefaultExtension);
                string fullPath = Utility.Path.GetRegularPath(Path.Combine(outputFullPath, fullNameWithCrc32AndExtension));
                string fullDirectoryName = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(fullDirectoryName))
                {
                    Directory.CreateDirectory(fullDirectoryName);
                }

                if (additionalCompressionSelected)
                {
                    byte[] compressedBytes = Utility.Compression.Compress(bytes);
                    compressedLength = compressedBytes.Length;
                    compressedHashCode = Utility.Verifier.GetCrc32(compressedBytes);
                    File.WriteAllBytes(fullPath, compressedBytes);
                }
                else
                {
                    File.WriteAllBytes(fullPath, bytes);
                }
            }

            resourceData.AddCode(platform, length, hashCode, compressedLength, compressedHashCode);
            return true;
        }

        private BuildAssetBundleOptions GetBuildAssetBundleOptions()
        {
            BuildAssetBundleOptions buildOptions = BuildAssetBundleOptions.DeterministicAssetBundle;

            if (ForceRebuildAssetBundleSelected)
            {
                buildOptions |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
            }

            if (AssetBundleCompression == AssetBundleCompressionType.Uncompressed)
            {
                buildOptions |= BuildAssetBundleOptions.UncompressedAssetBundle;
            }
            else if (AssetBundleCompression == AssetBundleCompressionType.LZ4)
            {
                buildOptions |= BuildAssetBundleOptions.ChunkBasedCompression;
            }

            return buildOptions;
        }

        private bool PrepareBuildData(out AssetBundleBuild[] assetBundleBuildDatas, out ResourceData[] assetBundleResourceDatas, out ResourceData[] binaryResourceDatas)
        {
            assetBundleBuildDatas = null;
            assetBundleResourceDatas = null;
            binaryResourceDatas = null;
            _ResourceDatas.Clear();

            Resource[] resources = _ResourceCollection.GetResources();
            foreach (Resource resource in resources)
            {
                _ResourceDatas.Add(resource.FullName, new ResourceData(resource.Name, resource.Variant, resource.FileSystem, resource.LoadType, resource.Packed, resource.GetResourceGroups()));
            }

            Asset[] assets = _ResourceCollection.GetAssets();
            foreach (Asset asset in assets)
            {
                string assetName = asset.Name;
                if (string.IsNullOrEmpty(assetName))
                {
                    _BuildReport.LogError("Can not find asset by guid '{0}'.", asset.Guid);
                    return false;
                }

                string assetFileFullName = Application.dataPath.Substring(0, Application.dataPath.Length - AssetsStringLength) + assetName;
                if (!File.Exists(assetFileFullName))
                {
                    _BuildReport.LogError("Can not find asset '{0}'.", assetFileFullName);
                    return false;
                }

                byte[] assetBytes = File.ReadAllBytes(assetFileFullName);
                int assetHashCode = Utility.Verifier.GetCrc32(assetBytes);

                List<string> dependencyAssetNames = new List<string>();
                DependencyData dependencyData = _ResourceAnalyzerController.GetDependencyData(assetName);
                Asset[] dependencyAssets = dependencyData.GetDependencyAssets();
                foreach (Asset dependencyAsset in dependencyAssets)
                {
                    dependencyAssetNames.Add(dependencyAsset.Name);
                }

                dependencyAssetNames.Sort();

                _ResourceDatas[asset.Resource.FullName].AddAssetData(asset.Guid, assetName, assetBytes.Length, assetHashCode, dependencyAssetNames.ToArray());
            }

            List<AssetBundleBuild> assetBundleBuildDataList = new List<AssetBundleBuild>();
            List<ResourceData> assetBundleResourceDataList = new List<ResourceData>();
            List<ResourceData> binaryResourceDataList = new List<ResourceData>();
            foreach (ResourceData resourceData in _ResourceDatas.Values)
            {
                if (resourceData.AssetCount <= 0)
                {
                    _BuildReport.LogError("Resource '{0}' has no asset.", GetResourceFullName(resourceData.Name, resourceData.Variant));
                    return false;
                }

                if (resourceData.IsLoadFromBinary)
                {
                    binaryResourceDataList.Add(resourceData);
                }
                else
                {
                    assetBundleResourceDataList.Add(resourceData);

                    AssetBundleBuild build = new AssetBundleBuild();
                    build.assetBundleName = resourceData.Name;
                    build.assetBundleVariant = resourceData.Variant;
                    build.assetNames = resourceData.GetAssetNames();
                    assetBundleBuildDataList.Add(build);
                }
            }

            assetBundleBuildDatas = assetBundleBuildDataList.ToArray();
            assetBundleResourceDatas = assetBundleResourceDataList.ToArray();
            binaryResourceDatas = binaryResourceDataList.ToArray();
            return true;
        }

        private static string GetResourceFullName(string name, string variant)
        {
            return !string.IsNullOrEmpty(variant) ? Utility.Text.Format("{0}.{1}", name, variant) : name;
        }

        private static BuildTarget GetBuildTarget(Platform platform)
        {
            switch (platform)
            {
                case Platform.Windows:
                    return BuildTarget.StandaloneWindows;

                case Platform.Windows64:
                    return BuildTarget.StandaloneWindows64;

                case Platform.MacOS:
#if UNITY_2017_3_OR_NEWER
                    return BuildTarget.StandaloneOSX;
#else
                    return BuildTarget.StandaloneOSXUniversal;
#endif
                case Platform.Linux:
                    return BuildTarget.StandaloneLinux64;

                case Platform.IOS:
                    return BuildTarget.iOS;

                case Platform.Android:
                    return BuildTarget.Android;

                case Platform.WindowsStore:
                    return BuildTarget.WSAPlayer;

                case Platform.WebGL:
                    return BuildTarget.WebGL;

                default:
                    throw new GameFrameworkException("Platform is invalid.");
            }
        }

        private static string GetExtension(ResourceData data)
        {
            if (data.IsLoadFromBinary)
            {
                string assetName = data.GetAssetNames()[0];
                int position = assetName.LastIndexOf('.');
                if (position >= 0)
                {
                    return assetName.Substring(position + 1);
                }
            }

            return DefaultExtension;
        }
    }
}
