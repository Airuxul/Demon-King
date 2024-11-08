//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;

namespace UnityGameFramework.Editor.ResourceTools
{
    public sealed partial class ResourceBuilderController
    {
        private sealed class BuildReport
        {
            private const string BuildReportName = "BuildReport.xml";
            private const string BuildLogName = "BuildLog.txt";

            private string _BuildReportName = null;
            private string _BuildLogName = null;
            private string _ProductName = null;
            private string _CompanyName = null;
            private string _GameIdentifier = null;
            private string _GameFrameworkVersion = null;
            private string _UnityVersion = null;
            private string _ApplicableGameVersion = null;
            private int _InternalResourceVersion = 0;
            private Platform _Platforms = Platform.Undefined;
            private AssetBundleCompressionType _AssetBundleCompression;
            private string _CompressionHelperTypeName;
            private bool _AdditionalCompressionSelected = false;
            private bool _ForceRebuildAssetBundleSelected = false;
            private string _BuildEventHandlerTypeName;
            private string _OutputDirectory;
            private BuildAssetBundleOptions _BuildAssetBundleOptions = BuildAssetBundleOptions.None;
            private StringBuilder _LogBuilder = null;
            private SortedDictionary<string, ResourceData> _ResourceDatas = null;

            public void Initialize(string buildReportPath, string productName, string companyName, string gameIdentifier, string gameFrameworkVersion, string unityVersion, string applicableGameVersion, int internalResourceVersion,
                Platform platforms, AssetBundleCompressionType assetBundleCompression, string compressionHelperTypeName, bool additionalCompressionSelected, bool forceRebuildAssetBundleSelected, string buildEventHandlerTypeName, string outputDirectory, BuildAssetBundleOptions buildAssetBundleOptions, SortedDictionary<string, ResourceData> resourceDatas)
            {
                if (string.IsNullOrEmpty(buildReportPath))
                {
                    throw new GameFrameworkException("Build report path is invalid.");
                }

                _BuildReportName = Utility.Path.GetRegularPath(Path.Combine(buildReportPath, BuildReportName));
                _BuildLogName = Utility.Path.GetRegularPath(Path.Combine(buildReportPath, BuildLogName));
                _ProductName = productName;
                _CompanyName = companyName;
                _GameIdentifier = gameIdentifier;
                _GameFrameworkVersion = gameFrameworkVersion;
                _UnityVersion = unityVersion;
                _ApplicableGameVersion = applicableGameVersion;
                _InternalResourceVersion = internalResourceVersion;
                _Platforms = platforms;
                _AssetBundleCompression = assetBundleCompression;
                _CompressionHelperTypeName = compressionHelperTypeName;
                _AdditionalCompressionSelected = additionalCompressionSelected;
                _ForceRebuildAssetBundleSelected = forceRebuildAssetBundleSelected;
                _BuildEventHandlerTypeName = buildEventHandlerTypeName;
                _OutputDirectory = outputDirectory;
                _BuildAssetBundleOptions = buildAssetBundleOptions;
                _LogBuilder = new StringBuilder();
                _ResourceDatas = resourceDatas;
            }

            public void LogInfo(string format, params object[] args)
            {
                LogInternal("INFO", format, args);
            }

            public void LogWarning(string format, params object[] args)
            {
                LogInternal("WARNING", format, args);
            }

            public void LogError(string format, params object[] args)
            {
                LogInternal("ERROR", format, args);
            }

            public void LogFatal(string format, params object[] args)
            {
                LogInternal("FATAL", format, args);
            }

            public void SaveReport()
            {
                XmlElement xmlElement = null;
                XmlAttribute xmlAttribute = null;

                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null));

                XmlElement xmlRoot = xmlDocument.CreateElement("UnityGameFramework");
                xmlDocument.AppendChild(xmlRoot);

                XmlElement xmlBuildReport = xmlDocument.CreateElement("BuildReport");
                xmlRoot.AppendChild(xmlBuildReport);

                XmlElement xmlSummary = xmlDocument.CreateElement("Summary");
                xmlBuildReport.AppendChild(xmlSummary);

                xmlElement = xmlDocument.CreateElement("ProductName");
                xmlElement.InnerText = _ProductName;
                xmlSummary.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("CompanyName");
                xmlElement.InnerText = _CompanyName;
                xmlSummary.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("GameIdentifier");
                xmlElement.InnerText = _GameIdentifier;
                xmlSummary.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("GameFrameworkVersion");
                xmlElement.InnerText = _GameFrameworkVersion;
                xmlSummary.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("UnityVersion");
                xmlElement.InnerText = _UnityVersion;
                xmlSummary.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("ApplicableGameVersion");
                xmlElement.InnerText = _ApplicableGameVersion;
                xmlSummary.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("InternalResourceVersion");
                xmlElement.InnerText = _InternalResourceVersion.ToString();
                xmlSummary.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("Platforms");
                xmlElement.InnerText = _Platforms.ToString();
                xmlSummary.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("AssetBundleCompression");
                xmlElement.InnerText = _AssetBundleCompression.ToString();
                xmlSummary.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("CompressionHelperTypeName");
                xmlElement.InnerText = _CompressionHelperTypeName;
                xmlSummary.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("AdditionalCompressionSelected");
                xmlElement.InnerText = _AdditionalCompressionSelected.ToString();
                xmlSummary.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("ForceRebuildAssetBundleSelected");
                xmlElement.InnerText = _ForceRebuildAssetBundleSelected.ToString();
                xmlSummary.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("BuildEventHandlerTypeName");
                xmlElement.InnerText = _BuildEventHandlerTypeName;
                xmlSummary.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("OutputDirectory");
                xmlElement.InnerText = _OutputDirectory;
                xmlSummary.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("BuildAssetBundleOptions");
                xmlElement.InnerText = _BuildAssetBundleOptions.ToString();
                xmlSummary.AppendChild(xmlElement);

                XmlElement xmlResources = xmlDocument.CreateElement("Resources");
                xmlAttribute = xmlDocument.CreateAttribute("Count");
                xmlAttribute.Value = _ResourceDatas.Count.ToString();
                xmlResources.Attributes.SetNamedItem(xmlAttribute);
                xmlBuildReport.AppendChild(xmlResources);
                foreach (ResourceData resourceData in _ResourceDatas.Values)
                {
                    XmlElement xmlResource = xmlDocument.CreateElement("Resource");
                    xmlAttribute = xmlDocument.CreateAttribute("Name");
                    xmlAttribute.Value = resourceData.Name;
                    xmlResource.Attributes.SetNamedItem(xmlAttribute);
                    if (resourceData.Variant != null)
                    {
                        xmlAttribute = xmlDocument.CreateAttribute("Variant");
                        xmlAttribute.Value = resourceData.Variant;
                        xmlResource.Attributes.SetNamedItem(xmlAttribute);
                    }

                    xmlAttribute = xmlDocument.CreateAttribute("Extension");
                    xmlAttribute.Value = GetExtension(resourceData);
                    xmlResource.Attributes.SetNamedItem(xmlAttribute);

                    if (resourceData.FileSystem != null)
                    {
                        xmlAttribute = xmlDocument.CreateAttribute("FileSystem");
                        xmlAttribute.Value = resourceData.FileSystem;
                        xmlResource.Attributes.SetNamedItem(xmlAttribute);
                    }

                    xmlAttribute = xmlDocument.CreateAttribute("LoadType");
                    xmlAttribute.Value = ((byte)resourceData.LoadType).ToString();
                    xmlResource.Attributes.SetNamedItem(xmlAttribute);
                    xmlAttribute = xmlDocument.CreateAttribute("Packed");
                    xmlAttribute.Value = resourceData.Packed.ToString();
                    xmlResource.Attributes.SetNamedItem(xmlAttribute);
                    string[] resourceGroups = resourceData.GetResourceGroups();
                    if (resourceGroups.Length > 0)
                    {
                        xmlAttribute = xmlDocument.CreateAttribute("ResourceGroups");
                        xmlAttribute.Value = string.Join(",", resourceGroups);
                        xmlResource.Attributes.SetNamedItem(xmlAttribute);
                    }

                    xmlResources.AppendChild(xmlResource);

                    AssetData[] assetDatas = resourceData.GetAssetDatas();
                    XmlElement xmlAssets = xmlDocument.CreateElement("Assets");
                    xmlAttribute = xmlDocument.CreateAttribute("Count");
                    xmlAttribute.Value = assetDatas.Length.ToString();
                    xmlAssets.Attributes.SetNamedItem(xmlAttribute);
                    xmlResource.AppendChild(xmlAssets);
                    foreach (AssetData assetData in assetDatas)
                    {
                        XmlElement xmlAsset = xmlDocument.CreateElement("Asset");
                        xmlAttribute = xmlDocument.CreateAttribute("Guid");
                        xmlAttribute.Value = assetData.Guid;
                        xmlAsset.Attributes.SetNamedItem(xmlAttribute);
                        xmlAttribute = xmlDocument.CreateAttribute("Name");
                        xmlAttribute.Value = assetData.Name;
                        xmlAsset.Attributes.SetNamedItem(xmlAttribute);
                        xmlAttribute = xmlDocument.CreateAttribute("Length");
                        xmlAttribute.Value = assetData.Length.ToString();
                        xmlAsset.Attributes.SetNamedItem(xmlAttribute);
                        xmlAttribute = xmlDocument.CreateAttribute("HashCode");
                        xmlAttribute.Value = assetData.HashCode.ToString();
                        xmlAsset.Attributes.SetNamedItem(xmlAttribute);
                        xmlAssets.AppendChild(xmlAsset);
                        string[] dependencyAssetNames = assetData.GetDependencyAssetNames();
                        if (dependencyAssetNames.Length > 0)
                        {
                            XmlElement xmlDependencyAssets = xmlDocument.CreateElement("DependencyAssets");
                            xmlAttribute = xmlDocument.CreateAttribute("Count");
                            xmlAttribute.Value = dependencyAssetNames.Length.ToString();
                            xmlDependencyAssets.Attributes.SetNamedItem(xmlAttribute);
                            xmlAsset.AppendChild(xmlDependencyAssets);
                            foreach (string dependencyAssetName in dependencyAssetNames)
                            {
                                XmlElement xmlDependencyAsset = xmlDocument.CreateElement("DependencyAsset");
                                xmlAttribute = xmlDocument.CreateAttribute("Name");
                                xmlAttribute.Value = dependencyAssetName;
                                xmlDependencyAsset.Attributes.SetNamedItem(xmlAttribute);
                                xmlDependencyAssets.AppendChild(xmlDependencyAsset);
                            }
                        }
                    }

                    XmlElement xmlCodes = xmlDocument.CreateElement("Codes");
                    xmlResource.AppendChild(xmlCodes);
                    foreach (ResourceCode resourceCode in resourceData.GetCodes())
                    {
                        XmlElement xmlCode = xmlDocument.CreateElement(resourceCode.Platform.ToString());
                        xmlAttribute = xmlDocument.CreateAttribute("Length");
                        xmlAttribute.Value = resourceCode.Length.ToString();
                        xmlCode.Attributes.SetNamedItem(xmlAttribute);
                        xmlAttribute = xmlDocument.CreateAttribute("HashCode");
                        xmlAttribute.Value = resourceCode.HashCode.ToString();
                        xmlCode.Attributes.SetNamedItem(xmlAttribute);
                        xmlAttribute = xmlDocument.CreateAttribute("CompressedLength");
                        xmlAttribute.Value = resourceCode.CompressedLength.ToString();
                        xmlCode.Attributes.SetNamedItem(xmlAttribute);
                        xmlAttribute = xmlDocument.CreateAttribute("CompressedHashCode");
                        xmlAttribute.Value = resourceCode.CompressedHashCode.ToString();
                        xmlCode.Attributes.SetNamedItem(xmlAttribute);
                        xmlCodes.AppendChild(xmlCode);
                    }
                }

                xmlDocument.Save(_BuildReportName);
                File.WriteAllText(_BuildLogName, _LogBuilder.ToString());
            }

            private void LogInternal(string type, string format, object[] args)
            {
                _LogBuilder.AppendFormat("[{0:HH:mm:ss.fff}][{1}] ", DateTime.UtcNow.ToLocalTime(), type);
                _LogBuilder.AppendFormat(format, args);
                _LogBuilder.AppendLine();
            }
        }
    }
}
