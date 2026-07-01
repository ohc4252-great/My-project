using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;

namespace StarForge.EditorTools
{
    /// <summary>
    /// Stamps the generated iOS Info.plist with StarForge's required
    /// App Store and AdMob launch metadata on every build.
    ///
    /// StarForge implements no proprietary or non-standard cryptography and does
    /// not add encryption on top of iOS: it only relies on standard, exempt
    /// HTTPS/TLS (AdMob, networking). Declaring this here means App Store Connect
    /// skips the export-compliance questionnaire on each upload.
    ///
    /// If a future build adds non-exempt encryption (custom/proprietary crypto,
    /// E2E messaging, VPN, etc.), remove or revisit the export declaration.
    /// </summary>
    public static class StarForgeIosExportCompliance
    {
        private const string AdMobIosAppId =
            "ca-app-pub-3971219491693844~2351507763";
        private const string AdMobApplicationIdentifierKey =
            "GADApplicationIdentifier";

        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            string plistPath = Path.Combine(buildPath, "Info.plist");
            if (!File.Exists(plistPath))
            {
                return;
            }

            XmlDocument plist = new XmlDocument { PreserveWhitespace = true };
            plist.Load(plistPath);

            XmlElement rootDict = plist.SelectSingleNode("/plist/dict") as XmlElement;
            if (rootDict == null)
            {
                throw new BuildFailedException(
                    "Generated iOS Info.plist does not contain a root dict.");
            }

            StampAdMobApplicationIdentifier(rootDict);
            SetBoolean(rootDict, "ITSAppUsesNonExemptEncryption", false);
            plist.Save(plistPath);
        }

        private static void StampAdMobApplicationIdentifier(XmlElement rootDict)
        {
            if (string.IsNullOrWhiteSpace(AdMobIosAppId) ||
                !AdMobIosAppId.Contains("~") ||
                AdMobIosAppId.Contains("/"))
            {
                throw new BuildFailedException(
                    "Invalid AdMob iOS app ID. Use the ca-app-pub-...~... " +
                    "application ID, not a rewarded ad unit ID.");
            }

            SetString(
                rootDict,
                AdMobApplicationIdentifierKey,
                AdMobIosAppId);
        }

        private static void SetString(
            XmlElement dict,
            string key,
            string value)
        {
            XmlDocument document = dict.OwnerDocument;
            XmlElement valueElement = document.CreateElement("string");
            valueElement.InnerText = value;
            SetPlistValue(dict, key, valueElement);
        }

        private static void SetBoolean(
            XmlElement dict,
            string key,
            bool value)
        {
            XmlDocument document = dict.OwnerDocument;
            XmlElement valueElement =
                document.CreateElement(value ? "true" : "false");
            SetPlistValue(dict, key, valueElement);
        }

        private static void SetPlistValue(
            XmlElement dict,
            string key,
            XmlElement valueElement)
        {
            XmlElement keyElement = FindKeyElement(dict, key);
            if (keyElement == null)
            {
                XmlDocument document = dict.OwnerDocument;
                keyElement = document.CreateElement("key");
                keyElement.InnerText = key;
                dict.AppendChild(keyElement);
                dict.AppendChild(valueElement);
                return;
            }

            XmlElement existingValue = GetNextElementSibling(keyElement);
            if (existingValue == null)
            {
                dict.InsertAfter(valueElement, keyElement);
                return;
            }

            dict.ReplaceChild(valueElement, existingValue);
        }

        private static XmlElement FindKeyElement(XmlElement dict, string key)
        {
            foreach (XmlNode child in dict.ChildNodes)
            {
                XmlElement childElement = child as XmlElement;
                if (childElement != null &&
                    childElement.Name == "key" &&
                    childElement.InnerText == key)
                {
                    return childElement;
                }
            }

            return null;
        }

        private static XmlElement GetNextElementSibling(XmlNode node)
        {
            for (XmlNode current = node.NextSibling;
                 current != null;
                 current = current.NextSibling)
            {
                XmlElement element = current as XmlElement;
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }
    }
}
