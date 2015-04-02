using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App12
{
    class SettingsManager
    {

        // Local Settings
        public void SetValue(string settingName, int value)
        {
            settingName = settingName.ToLower();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(settingName))
            {
                localSettings.Values[settingName] = value;
            }
            else
            {
                localSettings.Values.Add(settingName, value);
            };
        }

        public void SetValue(string settingName, Guid value)
        {
            settingName = settingName.ToLower();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(settingName))
            {
                localSettings.Values[settingName] = value;
            }
            else
            {
                localSettings.Values.Add(settingName, value);
            };
        }

        public void SetValue(string settingName, long value)
        {
            settingName = settingName.ToLower();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(settingName))
            {
                localSettings.Values[settingName] = value;
            }
            else
            {
                localSettings.Values.Add(settingName, value);
            };
        }

        public int GetValue(string settingName, int defaultValue)
        {
            settingName = settingName.ToLower();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(settingName))
            {
                return (int)localSettings.Values[settingName];
            }
            else
            {
                return defaultValue;
            }
        }

        public Guid GetValue(string settingName, Guid defaultValue)
        {
            settingName = settingName.ToLower();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(settingName))
            {
                return (Guid)localSettings.Values[settingName];
            }
            else
            {
                return defaultValue;
            }
        }

        public long GetValue(string settingName, long defaultValue)
        {
            settingName = settingName.ToLower();
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(settingName))
            {
                return (long)localSettings.Values[settingName];
            }
            else
            {
                return defaultValue;
            }
        }

        // Roaming Settings
        public void SetRoamingValue(string settingName, int value)
        {
            settingName = settingName.ToLower();
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values.ContainsKey(settingName))
            {
                roamingSettings.Values[settingName] = value;
            }
            else
            {
                roamingSettings.Values.Add(settingName, value);
            }
        }

        public void SetRoamingValue(string settingName, long value)
        {
            settingName = settingName.ToLower();
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values.ContainsKey(settingName))
            {
                roamingSettings.Values[settingName] = value;
            }
            else
            {
                roamingSettings.Values.Add(settingName, value);
            }
        }

        public int GetRoamingValue(string settingName, int defaultValue)
        {
            settingName = settingName.ToLower();
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values.ContainsKey(settingName))
            {
                return (int)roamingSettings.Values[settingName];
            }
            else
            {
                return defaultValue;
            }
        }

        public long GetRoamingValue(string settingName, long defaultValue)
        {
            settingName = settingName.ToLower();
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            if (roamingSettings.Values.ContainsKey(settingName))
            {
                return (long)roamingSettings.Values[settingName];
            }
            else
            {
                return defaultValue;
            }
        }

    }
}
