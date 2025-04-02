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
        public string AdioURL { get; set; }

        /// <summary>
        /// API token for authentication
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Whether to use streaming mode
        /// </summary>
        public bool UseStreaming { get; set; }
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

        /// <summary>
        /// Parse URL Protocol into usable data
        /// </summary>
        /// <param name="url">URL to parse, e.g., subtitleedit://open?subtitle=file.srt</param>
        /// <returns>Parsed data or null if parsing failed</returns>
        public static UrlData ParseUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
                // Check if URL starts with the correct protocol
                string protocolPrefix = ProtocolName + "://";
                if (!url.StartsWith(protocolPrefix, StringComparison.OrdinalIgnoreCase))
                    return null;

                // Remove protocol prefix
                string urlWithoutPrefix = url.Substring(protocolPrefix.Length);

                // Create URL data
                UrlData data = new UrlData();
                
                // Default is not to use streaming
                data.UseStreaming = false;

                // Extract parameters
                string queryString = urlWithoutPrefix;
                int queryIndex = urlWithoutPrefix.IndexOf('?');
                if (queryIndex >= 0)
                {
                    queryString = urlWithoutPrefix.Substring(queryIndex + 1);
                }

                // Parse parameters
                if (!string.IsNullOrEmpty(queryString))
                {
                    string[] parts = queryString.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
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
                                    data.AdioURL = value;
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
                }

                return data;
            }
            catch (Exception)
            {
                // Return null if parsing fails for any reason
                return null;
            }
        }
    }
}