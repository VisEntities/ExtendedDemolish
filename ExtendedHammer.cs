/*
 * Copyright (C) 2026 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Extended Hammer", "VisEntities", "2.0.0")]
    [Description("Extends the hammer's demolish and rotation timers for placed structures.")]
    public class ExtendedHammer : RustPlugin
    {
        #region Fields

        private static ExtendedHammer _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class ProfileSettings
        {
            [JsonProperty("Demolish Time (Seconds)")]
            public int DemolishTime { get; set; }

            [JsonProperty("Rotation Time (Seconds)")]
            public int RotationTime { get; set; }
        }

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Permission Profiles")]
            public Dictionary<string, ProfileSettings> PermissionProfiles { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            if (string.Compare(_config.Version, "2.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                PermissionProfiles = new Dictionary<string, ProfileSettings>
                {
                    ["default"] = new ProfileSettings
                    {
                        DemolishTime = 600,
                        RotationTime = 600
                    },
                    ["vip"] = new ProfileSettings
                    {
                        DemolishTime = 1200,
                        RotationTime = 1200
                    }
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions(_config.PermissionProfiles.Keys);
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            if (planner == null || gameObject == null)
                return;

            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null)
                return;

            BaseEntity entity = gameObject.ToBaseEntity();
            if (entity == null)
                return;

            StabilityEntity stabilityEntity = entity as StabilityEntity;
            if (stabilityEntity == null)
                return;

            ProfileSettings profile = GetProfileForPlayer(player);

            stabilityEntity.CancelInvoke(stabilityEntity.StopBeingDemolishable);
            stabilityEntity.Invoke(stabilityEntity.StopBeingDemolishable, profile.DemolishTime);

            BuildingBlock buildingBlock = stabilityEntity as BuildingBlock;
            if (buildingBlock != null)
            {
                buildingBlock.CancelInvoke(buildingBlock.StopBeingRotatable);
                buildingBlock.Invoke(buildingBlock.StopBeingRotatable, profile.RotationTime);
            }
        }

        #endregion Oxide Hooks

        #region Helper Functions

        private ProfileSettings GetProfileForPlayer(BasePlayer player)
        {
            foreach (var kvp in _config.PermissionProfiles)
            {
                if (PermissionUtil.HasPermission(player, kvp.Key))
                    return kvp.Value;
            }

            return new ProfileSettings
            {
                DemolishTime = (int)StabilityEntity.demolish_seconds,
                RotationTime = 600
            };
        }

        #endregion Helper Functions

        #region Permissions

        private static class PermissionUtil
        {
            public static void RegisterPermissions(IEnumerable<string> profileSuffixes)
            {
                foreach (string suffix in profileSuffixes)
                {
                    _plugin.permission.RegisterPermission(GetPermission(suffix), _plugin);
                }
            }

            public static bool HasPermission(BasePlayer player, string profileSuffix)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, GetPermission(profileSuffix));
            }

            private static string GetPermission(string suffix)
            {
                return nameof(ExtendedHammer).ToLower() + "." + suffix;
            }
        }

        #endregion Permissions
    }
}