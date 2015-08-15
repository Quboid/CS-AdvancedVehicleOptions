﻿using ICities;
using UnityEngine;

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

using ColossalFramework;
using ColossalFramework.Threading;
using ColossalFramework.UI;

namespace AdvancedVehicleOptions
{
    public class ModInfo : IUserMod
    {
        public string Name
        {
            get { return "Advanced Vehicle Options " + version; }
        }

        public string Description
        {
            get { return "Customize your vehicles"; }
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            try
            {
                AdvancedVehicleOptions.LoadConfig();
                Detour.RandomSpeed.enabled = AdvancedVehicleOptions.config.randomSpeed;
                Detour.RandomSpeed.highwaySpeed = AdvancedVehicleOptions.config.highwaySpeed;

                UICheckBox highway = null;

                UIHelperBase group = helper.AddGroup(Name);
                group.AddCheckbox("Slightly randomize the speed of vehicles", Detour.RandomSpeed.enabled, (b) =>
                {
                    Detour.RandomSpeed.enabled = b;
                    highway.enabled = b;
                    AdvancedVehicleOptions.SaveConfig();
                });

                highway = (UICheckBox)group.AddCheckbox("Realistic highway speeds", Detour.RandomSpeed.highwaySpeed, (b) =>
                {
                    Detour.RandomSpeed.highwaySpeed = b;
                    AdvancedVehicleOptions.SaveConfig();
                });

                highway.enabled = Detour.RandomSpeed.enabled;
            }
            catch (Exception e)
            {
                DebugUtils.Log("OnSettingsUI failed");
                Debug.LogException(e);
            }
        }

        public const string version = "1.3.8";
    }
    
    public class AdvancedVehicleOptions : LoadingExtensionBase
    {
        private static GameObject m_gameObject;
        private static GUI.UIMainPanel m_mainPanel;

        private static List<VehicleInfo> m_removeList = new List<VehicleInfo>();
        private static bool m_removeAll = false;
        private static bool m_removeThreadRunning = false;

        private static List<VehicleInfo> m_removeParkedList = new List<VehicleInfo>();
        private static bool m_removeParkedThreadRunning = false;
        private static bool m_removeParkedAll = false;

        private static FieldInfo m_transferVehiclesDirty = typeof(VehicleManager).GetField("m_transferVehiclesDirty", BindingFlags.Instance | BindingFlags.NonPublic);

        private const string m_fileName = "AdvancedVehicleOptions.xml";

        public static bool isGameLoaded = false;
        public static Configuration config = new Configuration();
                
        #region LoadingExtensionBase overrides
        public override void OnCreated(ILoading loading)
        {
            try
            {
                // Storing default values ASAP (before any mods have the time to change values)
                DefaultOptions.StoreAll();

                // Creating a backup
                if (File.Exists(m_fileName))
                {
                    File.Copy(m_fileName, m_fileName + ".bak", true);
                    DebugUtils.Log("Backup configuration file created");
                }
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }
        /// <summary>
        /// Called when the level (game, map editor, asset editor) is loaded
        /// </summary>
        public override void OnLevelLoaded(LoadMode mode)
        {
            try
            {
                // Is it an actual game ?
                if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame)
                {
                    DefaultOptions.Clear();
                    return;
                }

                isGameLoaded = true;

                // Creating GUI
                UIView view = UIView.GetAView();
                m_gameObject = new GameObject("AdvancedVehicleOptions");
                m_gameObject.transform.SetParent(view.transform);

                try
                {
                    VehicleOptions.Clear();
                    m_mainPanel = m_gameObject.AddComponent<GUI.UIMainPanel>();
                    DebugUtils.Log("UIMainPanel created");
                }
                catch
                {
                    DebugUtils.Warning("A new version of the mod has been installed." + Environment.NewLine +
                        "The game must be exited completely for changes to take effect." + Environment.NewLine +
                        "Until then the mod is disabled.");
                    return;
                }

                new EnumerableActionThread(BrokenAssetsFix);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Called when the level is unloaded
        /// </summary>
        public override void OnLevelUnloading()
        {
            try
            {
                SaveConfig();
                Detour.RandomSpeed.Restore();

                GUI.UIUtils.DestroyDeeply(m_mainPanel);
                GameObject.Destroy(m_gameObject);

                isGameLoaded = false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public override void OnReleased()
        {
            try
            {
                DebugUtils.Log("Restoring default values");
                DefaultOptions.RestoreAll();
                DefaultOptions.Clear();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        #endregion

        /// <summary>
        /// Load and apply the configuration file
        /// </summary>
        public static void LoadConfig()
        {
            if(!isGameLoaded)
            {
                if (File.Exists(m_fileName)) config.Deserialize(m_fileName);
                return;
            }

            // Store modded values
            DefaultOptions.StoreAllModded();

            if (!File.Exists(m_fileName))
            {
                DebugUtils.Log("Configuration file not found. Creating new configuration file.");

                CreateConfig();

                // Update GUI list
                m_mainPanel.optionList = config.options;
                return;
            }

            config.Deserialize(m_fileName);

            if (config.options == null)
            {
                DebugUtils.Log("Configuration empty. Default values will be used.");
            }
            else
            {
                // Remove unneeded options
                List<VehicleOptions> optionsList = new List<VehicleOptions>();

                for (uint i = 0; i < config.options.Length; i++)
                {
                    if (config.options[i] != null && config.options[i].prefab != null) optionsList.Add(config.options[i]);
                }

                config.options = optionsList.ToArray();
            }

            // Checking for new vehicles
            CompileVehiclesList();

            // Checking for conflicts
            DefaultOptions.CheckForConflicts();

            // Update existing vehicles
            new EnumerableActionThread(VehicleOptions.UpdateCapacityUnits);
            new EnumerableActionThread(VehicleOptions.UpdateBackEngines);

            // Update GUI list
            m_mainPanel.optionList = config.options;

            DebugUtils.Log("Configuration loaded");
            LogVehicleListSteamID();
        }

        /// <summary>
        /// Save the configuration file
        /// </summary>
        public static void SaveConfig()
        {
            config.version = ModInfo.version;
            config.randomSpeed = Detour.RandomSpeed.enabled;
            config.highwaySpeed = Detour.RandomSpeed.highwaySpeed;
            config.Serialize(m_fileName);
        }

        public static void ClearVehicles(VehicleOptions options, bool parked)
        {
            if (parked)
            {
                if(options == null)
                {
                    m_removeParkedAll = true;
                    m_removeParkedList.Clear();
                    if (!m_removeParkedThreadRunning) new EnumerableActionThread(ActionRemoveParkedAll);
                    return;
                }
                if (!m_removeParkedList.Contains(options.prefab))
                {
                    m_removeParkedList.Add(options.prefab);
                    if (!m_removeParkedThreadRunning) new EnumerableActionThread(ActionRemoveParked);
                }
            }
            else
            {
                if (options == null)
                {
                    m_removeAll = true;
                    m_removeList.Clear();
                    if (!m_removeThreadRunning) new EnumerableActionThread(ActionRemoveExistingAll);
                    return;
                }
                if (!m_removeList.Contains(options.prefab))
                {
                    m_removeList.Add(options.prefab);
                    if (!m_removeThreadRunning) new EnumerableActionThread(ActionRemoveExisting);
                }
            }
        }

        public static IEnumerator ActionRemoveExisting(ThreadBase t)
        {
            m_removeThreadRunning = true;

            VehicleManager vehicleManager =  Singleton<VehicleManager>.instance;
            
            while(m_removeList.Count != 0)
            {
                VehicleInfo[] prefabs = m_removeList.ToArray();

                for (ushort i = 0; i < vehicleManager.m_vehicles.m_size; i++)
                {
                    if (m_removeAll) break;
                    if (vehicleManager.m_vehicles.m_buffer[i].Info != null)
                    {
                        if (prefabs.Contains(vehicleManager.m_vehicles.m_buffer[i].Info))
                            vehicleManager.ReleaseVehicle(i);
                    }

                    if (i % 256 == 255) yield return i;
                }

                if(m_removeAll)
                {
                    new EnumerableActionThread(ActionRemoveExistingAll);
                    break;
                }

                m_removeList.RemoveRange(0, prefabs.Count());
            }

            m_removeThreadRunning = false;
        }

        public static IEnumerator ActionRemoveParked(ThreadBase t)
        {
            m_removeParkedThreadRunning = true;

            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;

            while (m_removeParkedList.Count != 0)
            {
                VehicleInfo[] prefabs = m_removeParkedList.ToArray();

                for (ushort i = 0; i < vehicleManager.m_parkedVehicles.m_size; i++)
                {
                    if (m_removeParkedAll) break;
                    if (vehicleManager.m_parkedVehicles.m_buffer[i].Info != null)
                    {
                        if (prefabs.Contains(vehicleManager.m_parkedVehicles.m_buffer[i].Info))
                            vehicleManager.ReleaseParkedVehicle(i);
                    }

                    if (i % 256 == 255) yield return i;
                }

                if (m_removeParkedAll)
                {
                    new EnumerableActionThread(ActionRemoveParkedAll);
                    break;
                }
                m_removeParkedList.RemoveRange(0, prefabs.Count());
            }

            m_removeParkedThreadRunning = false;
        }

        public static IEnumerator ActionRemoveExistingAll(ThreadBase t)
        {
            m_removeThreadRunning = true;

            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;

            for (ushort i = 0; i < vehicleManager.m_vehicles.m_size; i++)
            {
                if (vehicleManager.m_vehicles.m_buffer[i].Info != null)
                    vehicleManager.ReleaseVehicle(i);

                if (i % 256 == 255) yield return i;
            }

            m_removeThreadRunning = false;
        }

        public static IEnumerator ActionRemoveParkedAll(ThreadBase t)
        {
            m_removeParkedThreadRunning = true;

            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;

            for (ushort i = 0; i < vehicleManager.m_parkedVehicles.m_size; i++)
            {
                if (vehicleManager.m_parkedVehicles.m_buffer[i].Info != null)
                    vehicleManager.ReleaseParkedVehicle(i);

                if (i % 256 == 255) yield return i;
            }

            m_removeParkedThreadRunning = false;
        }

        private static void CreateConfig()
        {
            CompileVehiclesList();

            // Loading old mods saves
            List<OldMods.Vehicle> removerList = OldMods.VehicleRemoverMod.LoadConfig();
            List<OldMods.VehicleColorInfo> colorList = OldMods.VehicleColorChangerMod.LoadConfig();

            if (removerList != null || colorList != null)
            {
                for (int i = 0; i < config.options.Length; i++)
                {
                    if (removerList != null)
                    {
                        OldMods.Vehicle vehicle = removerList.Find((v) => { return v.name == config.options[i].name; });
                        if (vehicle.name == config.options[i].name) config.options[i].enabled = vehicle.enabled;
                    }

                    if (colorList != null)
                    {
                        OldMods.VehicleColorInfo vehicle = colorList.Find((v) => { return v.name == config.options[i].name; });
                        if (vehicle != null && vehicle.name == config.options[i].name)
                        {
                            config.options[i].color0 = vehicle.color0;
                            config.options[i].color1 = vehicle.color1;
                            config.options[i].color2 = vehicle.color2;
                            config.options[i].color3 = vehicle.color3;
                        }
                    }
                }
            }

            SaveConfig();
        }

        /// <summary>
        /// Check if there are new vehicles and add them to the options list
        /// </summary>
        private static void CompileVehiclesList()
        {
            List<VehicleOptions> optionsList = new List<VehicleOptions>();
            if (config.options != null) optionsList.AddRange(config.options);

            for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab(i);

                if (prefab == null || ContainsPrefab(prefab)) continue;

                // New vehicle
                VehicleOptions options = new VehicleOptions();
                options.SetPrefab(prefab);

                optionsList.Add(options);
            }

            if (config.options != null)
                DebugUtils.Log("Found " + (optionsList.Count - config.options.Length) + " new vehicle(s)");
            else
                DebugUtils.Log("Found " + optionsList.Count + " new vehicle(s)");

            config.options = optionsList.ToArray();

        }

        private static bool ContainsPrefab(VehicleInfo prefab)
        {
            if (config.options == null) return false;
            for (int i = 0; i < config.options.Length; i++)
            {
                if (config.options[i].prefab == prefab) return true;
            }
            return false;
        }

        private static void LogVehicleListSteamID()
        {
            StringBuilder steamIDs = new StringBuilder("Vehicle Steam IDs : ");

            for (int i = 0; i < config.options.Length; i++)
            {
                if (config.options[i] != null && config.options[i].name.Contains("."))
                {
                    steamIDs.Append(config.options[i].name.Substring(0, config.options[i].name.IndexOf(".")));
                    steamIDs.Append(",");
                }
            }
            steamIDs.Length--;

            DebugUtils.Log(steamIDs.ToString());
        }

        private static bool IsAICustom(VehicleAI ai)
        {
            Type type = ai.GetType();
            return (type != typeof(AmbulanceAI) ||
                type != typeof(BusAI) ||
                type != typeof(CargoTruckAI) ||
                type != typeof(FireTruckAI) ||
                type != typeof(GarbageTruckAI) ||
                type != typeof(HearseAI) ||
                type != typeof(PassengerCarAI) ||
                type != typeof(PoliceCarAI));
        }

        private IEnumerator BrokenAssetsFix(ThreadBase t)
        {
            uint count = 0;

            // Fix broken vehicles
            Array16<Vehicle> vehicles = Singleton<VehicleManager>.instance.m_vehicles;
            for (int i = 0; i < vehicles.m_size; i++)
            {
                if (vehicles.m_buffer[i].m_flags != Vehicle.Flags.None)
                {
                    bool exists = (vehicles.m_buffer[i].m_flags & Vehicle.Flags.Spawned) != Vehicle.Flags.None;

                    bool isSingleTrailer = false;
                    if (exists && vehicles.m_buffer[i].Info != null)
                    {
                        VehicleOptions options = new VehicleOptions();
                        options.SetPrefab(vehicles.m_buffer[i].Info);
                        isSingleTrailer = options.isTrailer && vehicles.m_buffer[i].m_leadingVehicle == 0 && vehicles.m_buffer[i].m_trailingVehicle == 0;
                    }

                    if (vehicles.m_buffer[i].Info == null || (exists && isSingleTrailer))
                    {
                        try
                        {
                            Singleton<VehicleManager>.instance.ReleaseVehicle((ushort)i);
                            count++;
                        }
                        catch { }
                    }
                }
                if (i % 256 == 255) yield return i;
            }

            Array16<VehicleParked> vehiclesParked = Singleton<VehicleManager>.instance.m_parkedVehicles;
            for (int i = 0; i < vehiclesParked.m_size; i++)
            {
                if (vehiclesParked.m_buffer[i].Info == null)
                {
                    try
                    {
                        Singleton<VehicleManager>.instance.ReleaseParkedVehicle((ushort)i);
                        count++;
                    }
                    catch { }
                }
                if (i % 256 == 255) yield return i;
            }

            if (count > 0) DebugUtils.Log("Removed " + count + " broken vehicle instances.");
            count = 0;

            // Fix broken buildings
            Array16<Building> buildings = Singleton<BuildingManager>.instance.m_buildings;
            for (int i = 0; i < buildings.m_size; i++)
            {
                if (buildings.m_buffer[i].Info == null)
                {
                    try
                    {
                        Singleton<BuildingManager>.instance.ReleaseBuilding((ushort)i);
                        count++;
                    }
                    catch { }
                }
                if (i % 256 == 255) yield return i;
            }

            if (count > 0) DebugUtils.Log("Removed " + count + " broken building instances.");
            count = 0;

            // Fix broken offers
            TransferManager.TransferOffer[] incomingOffers = typeof(TransferManager).GetField("m_incomingOffers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as TransferManager.TransferOffer[];
            TransferManager.TransferOffer[] outgoingOffers = typeof(TransferManager).GetField("m_outgoingOffers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as TransferManager.TransferOffer[];

            ushort[] incomingCount = typeof(TransferManager).GetField("m_incomingCount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as ushort[];
            ushort[] outgoingCount = typeof(TransferManager).GetField("m_outgoingCount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as ushort[];

            int[] incomingAmount = typeof(TransferManager).GetField("m_incomingAmount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as int[];
            int[] outgoingAmount = typeof(TransferManager).GetField("m_outgoingAmount", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(TransferManager.instance) as int[];

            // Based on TransferManager.RemoveAllOffers
            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int num = i * 8 + j;
                    int num2 = (int)incomingCount[num];
                    for (int k = num2 - 1; k >= 0; k--)
                    {
                        int num3 = num * 256 + k;
                        if (IsInfoNull(incomingOffers[num3]))
                        {
                            incomingAmount[i] -= incomingOffers[num3].Amount;
                            incomingOffers[num3] = incomingOffers[--num2];
                            count++;
                        }
                    }
                    incomingCount[num] = (ushort)num2;
                    int num4 = (int)outgoingCount[num];
                    for (int l = num4 - 1; l >= 0; l--)
                    {
                        int num5 = num * 256 + l;
                        if (IsInfoNull(outgoingOffers[num5]))
                        {
                            outgoingAmount[i] -= outgoingOffers[num5].Amount;
                            outgoingOffers[num5] = outgoingOffers[--num4];
                            count++;
                        }
                    }
                    outgoingCount[num] = (ushort)num4;
                }
            }

            if (count > 0) DebugUtils.Log("Removed " + count + " broken transfer offers.");
        }

        private static bool IsInfoNull(TransferManager.TransferOffer offer)
        {
            if (!offer.Active) return false;

            if (offer.Vehicle != 0)
                return VehicleManager.instance.m_vehicles.m_buffer[offer.Vehicle].Info == null;
            
            if (offer.Building != 0)
                return BuildingManager.instance.m_buildings.m_buffer[offer.Building].Info == null;

            return false;
        }
    }
}
