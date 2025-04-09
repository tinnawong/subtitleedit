using Microsoft.Win32;
using System;
using System.IO;

namespace Nikse.SubtitleEdit.Core.Common
{
    /// <summary>
    /// Data extracted from URL parsing
    /// </summary>
    public class UrlData
    {
        /// <summary>
        /// Subtitle file URL
        /// </summary>
        public string SubtitleURL { get; set; }

        /// <summary>
        /// Audio/Video file URL
        /// </summary>
        public string AudioURL { get; set; }

        /// <summary>
        /// API token for authentication
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Whether to use streaming mode
        /// </summary>
        public bool UseStreaming { get; set; }

        // Constructor to set default values
        public UrlData()
        {
            SubtitleURL = string.Empty; // Default to empty string instead of null
            AudioURL = string.Empty;    // Default to empty string instead of null
            Token = string.Empty;       // Default to empty string instead of null
            UseStreaming = false;       // Default to false (though bool defaults to false anyway)
        }
    }

    public static class UrlProtocolHandler
    {
        public const string ProtocolName = "se";

        /// <summary>
        /// Register the URL Protocol in the system registry
        /// </summary>
        /// <returns>Operation result: true = success, false = failure</returns>
        public static bool RegisterUrlProtocol()
        {
            try
            {
                string applicationLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;

                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(ProtocolName))
                {
                    // Set protocol information
                    key.SetValue("", "URL:Subtitle Edit Protocol");
                    key.SetValue("URL Protocol", "");

                    using (RegistryKey defaultIcon = key.CreateSubKey("DefaultIcon"))
                    {
                        defaultIcon.SetValue("", applicationLocation + ",1");
                    }

                    using (RegistryKey commandKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        commandKey.SetValue("", "\"" + applicationLocation + "\" \"%1\"");
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the URL Protocol is already registered
        /// </summary>
        /// <returns>Check result: true = registered, false = not registered</returns>
        public static bool IsUrlProtocolRegistered()
        {
            try
            {
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(ProtocolName))
                {
                    return key != null;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static UrlData ParseUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {

                // Check if URL starts with the correct protocol
                string protocolPrefix = ProtocolName + "://";
                if (!url.StartsWith(protocolPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                // Remove protocol prefix
                string urlWithoutPrefix = url.Substring(protocolPrefix.Length);

                // Create URL data
                UrlData data = new UrlData();

                // Parse parameters - direct parsing for common cases
                string[] parts = urlWithoutPrefix.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                {
                    int equalIndex = part.IndexOf('=');
                    if (equalIndex > 0)
                    {
                        string key = part.Substring(0, equalIndex).Trim();
                        string value = part.Substring(equalIndex + 1).Trim();

                        // Remove quotes if present
                        if (value.StartsWith("\"") && value.EndsWith("\""))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }

                        // Store parameter values
                        switch (key.ToLowerInvariant())
                        {
                            case "subtitle":
                                data.SubtitleURL = value;
                                break;
                            case "video":
                                data.AudioURL = value;
                                break;
                            case "token":
                                data.Token = value;
                                break;
                            case "streaming":
                                data.UseStreaming = value.ToLowerInvariant() == "true" ||
                                                    value == "1" ||
                                                    value.ToLowerInvariant() == "yes";
                                break;
                        }
                    }
                }

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return null;
            }
        }
    }
}