﻿#region CodeMaid is Copyright 2007-2015 Steve Cadwallader.

// CodeMaid is free software: you can redistribute it and/or modify it under the terms of the GNU
// Lesser General Public License version 3 as published by the Free Software Foundation.
//
// CodeMaid is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without
// even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details <http://www.gnu.org/licenses/>.

#endregion CodeMaid is Copyright 2007-2015 Steve Cadwallader.

using System;
using System.Configuration;
using System.IO;

namespace SteveCadwallader.CodeMaid.Helpers
{
    /// <summary>
    /// This class handles customizing the settings persistance.
    /// </summary>
    public class CodeMaidSettingsProvider : LocalFileSettingsProvider
    {
        #region Constants

        private const string ConfigFilename = "CodeMaid.config";

        #endregion Constants

        #region Overridden Members

        /// <summary>
        /// Returns the collection of setting property values for the specified application instance
        /// and settings property group.
        /// </summary>
        /// <param name="context">
        /// A <see cref="T:System.Configuration.SettingsContext"/> describing the current
        /// application usage.
        /// </param>
        /// <param name="properties">
        /// A <see cref="T:System.Configuration.SettingsPropertyCollection"/> containing the
        /// settings property group whose values are to be retrieved.
        /// </param>
        /// <returns>
        /// A <see cref="T:System.Configuration.SettingsPropertyValueCollection"/> containing the
        /// values for the specified settings property group.
        /// </returns>
        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection properties)
        {
            var values = new SettingsPropertyValueCollection();

            var userSettings = ReadUserSettingsFromFile(context);
            var solutionSettings = ReadSolutionSettingsFromFile(context);

            foreach (SettingsProperty property in properties)
            {
                var value = new SettingsPropertyValue(property);

                ApplySettingToValue(value, userSettings);
                ApplySettingToValue(value, solutionSettings);

                values.Add(value);
            }

            return base.GetPropertyValues(context, properties);
        }

        /// <summary>
        /// Sets the values of the specified group of property settings.
        /// </summary>
        /// <param name="context">
        /// A <see cref="T:System.Configuration.SettingsContext"/> describing the current
        /// application usage.
        /// </param>
        /// <param name="values">
        /// A <see cref="T:System.Configuration.SettingsPropertyValueCollection"/> representing the
        /// group of property settings to set.
        /// </param>
        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection values)
        {
            base.SetPropertyValues(context, values);
        }

        #endregion Overridden Members

        #region Private Methods

        /// <summary>
        /// Gets the path to the user configuration file.
        /// </summary>
        private static string GetUserConfigPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodeMaid", ConfigFilename);
        }

        /// <summary>
        /// Gets the path to the solution configuration file based on the specified <see cref="SettingsContext"/>.
        /// </summary>
        /// <param name="context">
        /// A <see cref="T:System.Configuration.SettingsContext"/> describing the current
        /// application usage.
        /// </param>
        /// <returns>The path to the solution configuration, otherwise null.</returns>
        private static string GetSolutionConfigPath(SettingsContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var solutionPath = context["SolutionPath"];

            return solutionPath != null ? Path.Combine(solutionPath.ToString(), ConfigFilename) : null;
        }

        /// <summary>
        /// Gets the name of the section where settings will be located based on the specified <see cref="SettingsContext"/>.
        /// </summary>
        /// <param name="context">
        /// A <see cref="T:System.Configuration.SettingsContext"/> describing the current
        /// application usage.
        /// </param>
        /// <returns>The section name, otherwise null.</returns>
        private static string GetSectionName(SettingsContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var groupName = context["GroupName"];

            return groupName != null ? groupName.ToString() : null;
        }

        /// <summary>
        /// Read users settings from the configuration file.
        /// </summary>
        /// <param name="context">
        /// A <see cref="T:System.Configuration.SettingsContext"/> describing the current
        /// application usage.
        /// </param>
        /// <returns>A collection representing the settings, otherwise an empty collection.</returns>
        private static SettingElementCollection ReadUserSettingsFromFile(SettingsContext context)
        {
            try
            {
                var path = GetUserConfigPath();
                var sectionName = GetSectionName(context);

                if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(sectionName))
                {
                    return ReadSettingsFromFile(path, sectionName);
                }
            }
            catch (Exception ex)
            {
                OutputWindowHelper.ExceptionWriteLine("Unable to read user settings.", ex);
            }

            return new SettingElementCollection();
        }

        /// <summary>
        /// Read solution settings from the configuration file.
        /// </summary>
        /// <param name="context">
        /// A <see cref="T:System.Configuration.SettingsContext"/> describing the current
        /// application usage.
        /// </param>
        /// <returns>A collection representing the settings, otherwise an empty collection.</returns>
        private static SettingElementCollection ReadSolutionSettingsFromFile(SettingsContext context)
        {
            try
            {
                var path = GetSolutionConfigPath(context);
                var sectionName = GetSectionName(context);

                if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(sectionName))
                {
                    return ReadSettingsFromFile(path, sectionName);
                }
            }
            catch (Exception ex)
            {
                OutputWindowHelper.ExceptionWriteLine("Unable to read solution settings.", ex);
            }

            return new SettingElementCollection();
        }

        /// <summary>
        /// Reads settings from a configuration file at the specified path.
        /// </summary>
        /// <param name="path">The configuration file path.</param>
        /// <param name="sectionName">The name of the settings section.</param>
        /// <returns>A collection representing the settings, otherwise an empty collection.</returns>
        private static SettingElementCollection ReadSettingsFromFile(string path, string sectionName)
        {
            var fileMap = new ExeConfigurationFileMap { ExeConfigFilename = path };
            var config = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

            var userSettings = config.GetSectionGroup("userSettings");
            if (userSettings != null)
            {
                var section = userSettings.Sections[sectionName] as ClientSettingsSection;
                if (section != null)
                {
                    return section.Settings;
                }
            }

            return new SettingElementCollection();
        }

        /// <summary>
        /// Checks the specified settings collection to see if it has a serialized value that should
        /// be applied to the specified <see cref="SettingsPropertyValue"/>.
        /// </summary>
        /// <param name="value">An individual settings property value.</param>
        /// <param name="settings">A collection representing the settings.</param>
        private static void ApplySettingToValue(SettingsPropertyValue value, SettingElementCollection settings)
        {
            var setting = settings.Get(value.Name);
            if (setting != null)
            {
                value.SerializedValue = setting.Value.ValueXml.InnerText;

                // Mark the value as not deserialized, which will trigger a deserialization of the SerializedValue into the PropertyValue.
                value.Deserialized = false;
            }
        }

        /// <summary>
        /// Writes settings to a configuration file within the specified path.
        /// </summary>
        private static void WriteSettingsToFile()
        {
        }

        #endregion Private Methods
    }
}