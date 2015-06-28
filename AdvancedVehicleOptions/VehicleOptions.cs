﻿using UnityEngine;
using ColossalFramework.Globalization;

using System;
using System.Text;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace AdvancedVehicleOptions
{
    public class VehicleOptions : IComparable
    {
        public enum Category
        {
            None = -1,
            Citizen,
            Forestry,
            Farming,
            Ore,
            Oil,
            IndustryGeneric,
            Police,
            FireSafety,
            Healthcare,
            Deathcare,
            Garbage,
            TransportBus,
            TransportMetro,
            CargoTrain,
            TransportTrain,
            CargoShip,
            TransportShip,
            TransportPlane
        }

        #region serialized
        [XmlAttribute("name")]
        public string name
        {
            get { return m_prefab.name; }
            set { SetPrefab(PrefabCollection<VehicleInfo>.FindLoaded(value)); }
        }
        // enabled
        public bool enabled
        {
            get { return m_prefab.m_placementStyle != ItemClass.Placement.Manual; }
            set
            {
                m_prefab.m_placementStyle = value ? m_placementStyle : ItemClass.Placement.Manual;
            }
        }
        // addBackEngine
        public bool addBackEngine
        {
            get
            {
                if (!hasTrailer) return false;
                return m_prefab.m_trailers[m_prefab.m_trailers.Length - 1].m_info == m_prefab;
            }
            set
            {
                if (!hasTrailer) return;

                VehicleInfo newTrailer = value ? m_prefab : m_prefab.m_trailers[0].m_info;
                int last = m_prefab.m_trailers.Length - 1;

                if (m_prefab.m_trailers[last].m_info == newTrailer) return;

                m_prefab.m_trailers[last].m_info = newTrailer;

                if (value)
                    m_prefab.m_trailers[last].m_invertProbability = m_prefab.m_trailers[last].m_probability;
                else
                    m_prefab.m_trailers[last].m_invertProbability = m_prefab.m_trailers[0].m_invertProbability;
            }
        }
        // maxSpeed
        public float maxSpeed
        {
            get { return m_prefab.m_maxSpeed; }
            set { m_prefab.m_maxSpeed = value; }
        }
        // acceleration
        public float acceleration
        {
            get { return m_prefab.m_acceleration; }
            set { m_prefab.m_acceleration = value; }
        }
        // colors
        public HexaColor color0
        {
            get { return m_prefab.m_color0; }
            set { m_prefab.m_color0 = value; }
        }
        public HexaColor color1
        {
            get { return m_prefab.m_color1; }
            set { m_prefab.m_color1 = value; }
        }
        public HexaColor color2
        {
            get { return m_prefab.m_color2; }
            set { m_prefab.m_color2 = value; }
        }
        public HexaColor color3
        {
            get { return m_prefab.m_color3; }
            set { m_prefab.m_color3 = value; }
        }
        // capacity
        [DefaultValue(-1)]
        public int capacity
        {
            get
            {
                VehicleAI ai;

                ai = m_prefab.m_vehicleAI as AmbulanceAI;
                if (ai != null) return ((AmbulanceAI)ai).m_patientCapacity;

                ai = m_prefab.m_vehicleAI as BusAI;
                if (ai != null) return ((BusAI)ai).m_passengerCapacity;

                ai = m_prefab.m_vehicleAI as CargoShipAI;
                if (ai != null) return ((CargoShipAI)ai).m_cargoCapacity;

                ai = m_prefab.m_vehicleAI as CargoTrainAI;
                if (ai != null) return ((CargoTrainAI)ai).m_cargoCapacity;

                ai = m_prefab.m_vehicleAI as CargoTruckAI;
                if (ai != null) return ((CargoTruckAI)ai).m_cargoCapacity;

                ai = m_prefab.m_vehicleAI as GarbageTruckAI;
                if (ai != null) return ((GarbageTruckAI)ai).m_cargoCapacity;

                ai = m_prefab.m_vehicleAI as FireTruckAI;
                if (ai != null) return ((FireTruckAI)ai).m_fireFightingRate;

                ai = m_prefab.m_vehicleAI as HearseAI;
                if (ai != null) return ((HearseAI)ai).m_corpseCapacity;

                ai = m_prefab.m_vehicleAI as PassengerPlaneAI;
                if (ai != null) return ((PassengerPlaneAI)ai).m_passengerCapacity;

                ai = m_prefab.m_vehicleAI as PassengerShipAI;
                if (ai != null) return ((PassengerShipAI)ai).m_passengerCapacity;

                ai = m_prefab.m_vehicleAI as PassengerTrainAI;
                if (ai != null) return ((PassengerTrainAI)ai).m_passengerCapacity;

                ai = m_prefab.m_vehicleAI as PoliceCarAI;
                if (ai != null) return ((PoliceCarAI)ai).m_crimeCapacity;

                return -1;
            }
            set
            {
                if (capacity == -1) return;

                VehicleAI ai;

                ai = m_prefab.m_vehicleAI as AmbulanceAI;
                if (ai != null) { ((AmbulanceAI)ai).m_patientCapacity = value; return; }

                ai = m_prefab.m_vehicleAI as BusAI;
                if (ai != null) { ((BusAI)ai).m_passengerCapacity = value; return; }

                ai = m_prefab.m_vehicleAI as CargoShipAI;
                if (ai != null) { ((CargoShipAI)ai).m_cargoCapacity = value; return; }

                ai = m_prefab.m_vehicleAI as CargoTrainAI;
                if (ai != null) { ((CargoTrainAI)ai).m_cargoCapacity = value; return; }

                ai = m_prefab.m_vehicleAI as CargoTruckAI;
                if (ai != null) { ((CargoTruckAI)ai).m_cargoCapacity = value; return; }

                ai = m_prefab.m_vehicleAI as GarbageTruckAI;
                if (ai != null) { ((GarbageTruckAI)ai).m_cargoCapacity = value; return; }

                ai = m_prefab.m_vehicleAI as FireTruckAI;
                if (ai != null) { ((FireTruckAI)ai).m_fireFightingRate = value; return; }

                ai = m_prefab.m_vehicleAI as HearseAI;
                if (ai != null) { ((HearseAI)ai).m_corpseCapacity = value; return; }

                ai = m_prefab.m_vehicleAI as PassengerPlaneAI;
                if (ai != null) { ((PassengerPlaneAI)ai).m_passengerCapacity = value; return; }

                ai = m_prefab.m_vehicleAI as PassengerShipAI;
                if (ai != null) { ((PassengerShipAI)ai).m_passengerCapacity = value; return; }

                ai = m_prefab.m_vehicleAI as PassengerTrainAI;
                if (ai != null) { ((PassengerTrainAI)ai).m_passengerCapacity = value; return; }

                ai = m_prefab.m_vehicleAI as PoliceCarAI;
                if (ai != null) { ((PoliceCarAI)ai).m_crimeCapacity = value; return; }
            }
        }
        #endregion

        private VehicleInfo m_prefab = null;
        private Category m_category = Category.None;
        private ItemClass.Placement m_placementStyle;
        private string m_localizedName;
        private bool m_isTrailer = false;
        private bool m_hasCapacity = false;

        public VehicleInfo prefab
        {
            get { return m_prefab; }
        }

        public ItemClass.Placement placementStyle
        {
            get { return m_placementStyle; }
        }

        public bool hasCapacity
        {
            get { return m_hasCapacity; }
        }

        public bool hasTrailer
        {
            get { return m_prefab.m_trailers != null && m_prefab.m_trailers.Length > 0; }
        }

        public bool isTrailer
        {
            get { return m_isTrailer; }
        }

        public string localizedName
        {
            get { return m_localizedName; }
        }

        public Category category
        {
            get
            {
                if (m_category == Category.None)
                    m_category = GetCategory(m_prefab);
                return m_category;
            }
        }

        public void SetPrefab(VehicleInfo prefab)
        {
            if (prefab == null) return;

            m_prefab = prefab;
            m_placementStyle = prefab.m_placementStyle;

            m_localizedName = Locale.GetUnchecked("VEHICLE_TITLE", prefab.name);
            if (m_localizedName.StartsWith("VEHICLE_TITLE"))
            {
                VehicleInfo engine = GetEngine();
                if (engine != null)
                {
                    m_localizedName = Locale.GetUnchecked("VEHICLE_TITLE", engine.name) + " (Trailer)";
                    m_isTrailer = true;
                    m_category = GetCategory(engine);
                }
                else
                {
                    m_localizedName = prefab.name;
                    // Removes the steam ID and trailing _Data from the name
                    m_localizedName = m_localizedName.Substring(m_localizedName.IndexOf('.') + 1).Replace("_Data", "");
                }
            }

            m_hasCapacity = capacity != -1;
        }

        public int CompareTo(object o)
        {
            if (o == null) return 1;

            VehicleOptions options = (VehicleOptions)o;

            int delta = category - options.category;
            if (delta == 0) return localizedName.CompareTo(options.localizedName);

            return delta;
        }

        private Category GetCategory(VehicleInfo prefab)
        {
            if (prefab == null) return Category.None;

            switch (prefab.m_class.m_service)
            {
                case ItemClass.Service.PoliceDepartment:
                    return Category.Police;
                case ItemClass.Service.FireDepartment:
                    return Category.FireSafety;
                case ItemClass.Service.HealthCare:
                    if (prefab.m_class.m_level == ItemClass.Level.Level1)
                        return Category.Healthcare;
                    else
                        return Category.Deathcare;
                case ItemClass.Service.Garbage:
                    return Category.Garbage;
            }

            switch (prefab.m_class.m_subService)
            {
                case ItemClass.SubService.PublicTransportBus:
                    return Category.TransportBus;
                case ItemClass.SubService.PublicTransportMetro:
                    return Category.TransportMetro;
                case ItemClass.SubService.PublicTransportTrain:
                    if (prefab.m_class.m_level == ItemClass.Level.Level1)
                        return Category.TransportTrain;
                    else
                        return Category.CargoTrain;
                case ItemClass.SubService.PublicTransportShip:
                    if (prefab.m_class.m_level == ItemClass.Level.Level1)
                        return Category.TransportShip;
                    else
                        return Category.CargoShip;
                case ItemClass.SubService.PublicTransportPlane:
                    return Category.TransportPlane;
                case ItemClass.SubService.IndustrialForestry:
                    return Category.Forestry;
                case ItemClass.SubService.IndustrialFarming:
                    return Category.Farming;
                case ItemClass.SubService.IndustrialOre:
                    return Category.Ore;
                case ItemClass.SubService.IndustrialOil:
                    return Category.Oil;
                case ItemClass.SubService.IndustrialGeneric:
                    return Category.IndustryGeneric;
            }

            return Category.Citizen;
        }

        private VehicleInfo GetEngine()
        {
            for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
            {
                VehicleInfo prefab = PrefabCollection<VehicleInfo>.GetPrefab(i);
                if (prefab == null) continue;

                try
                {
                    if (prefab.m_trailers != null && prefab.m_trailers.Length > 0 && prefab.m_trailers[0].m_info == m_prefab)
                        return prefab;
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            }

            return null;
        }
    }

    public struct HexaColor : IXmlSerializable
    {
        private float r, g, b;

        public string Value
        {
            get
            {
                return ToString();
            }

            set
            {
                value = value.Trim().Replace("#", "");

                if (value.Length != 6) return;

                try
                {
                    r = int.Parse(value.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
                    g = int.Parse(value.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
                    b = int.Parse(value.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
                }
                catch
                {
                    r = g = b = 0;
                }
            }
        }

        public HexaColor(string value)
        {
            try
            {
                r = int.Parse(value.Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
                g = int.Parse(value.Substring(2, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
                b = int.Parse(value.Substring(4, 2), System.Globalization.NumberStyles.HexNumber) / 255f;
            }
            catch
            {
                r = g = b = 0;
            }
        }
        
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            Value = reader.ReadString();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteString(Value);
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();

            s.Append(((int)(255 * r)).ToString("X2"));
            s.Append(((int)(255 * g)).ToString("X2"));
            s.Append(((int)(255 * b)).ToString("X2"));

            return s.ToString();
        }

        public static implicit operator HexaColor(Color c)
        {
            HexaColor temp = new HexaColor();

            temp.r = c.r;
            temp.g = c.g;
            temp.b = c.b;

            return temp;
        }

        public static implicit operator Color(HexaColor c)
        {
            return new Color(c.r, c.g, c.b, 1f);
        }
    }

    public class DefaultOptions
    {
        public class StoreDefault : MonoBehaviour
        {
            public void Awake()
            {
                DontDestroyOnLoad(this);
            }

            private void OnLevelWasLoaded(int level)
            {
                if(level == 6) StartCoroutine("Store");
            }

            private IEnumerator Store()
            {
                while (PrefabCollection<VehicleInfo>.GetPrefab(0) == null)
                    yield return null;

                for (uint i = 0; i < PrefabCollection<VehicleInfo>.PrefabCount(); i++)
                    DefaultOptions.Store(PrefabCollection<VehicleInfo>.GetPrefab(i));

                DebugUtils.Log("Default values stored");
                Destroy(gameObject);
            }
        }

        private static Dictionary<VehicleInfo, DefaultOptions> m_default = new Dictionary<VehicleInfo,DefaultOptions>();

        public static void Store(VehicleInfo prefab)
        {
            if (prefab != null && !m_default.ContainsKey(prefab))
            {
                m_default.Add(prefab, new DefaultOptions(prefab));
            }
        }

        public static void StoreAll()
        {
            new GameObject("AVO-StoreDefault").AddComponent<StoreDefault>();
        }

        public static void Restore(VehicleInfo prefab)
        {
            if (prefab == null) return;

            VehicleOptions options = new VehicleOptions();
            options.SetPrefab(prefab);

            DefaultOptions stored = m_default[prefab];
            if (stored == null) return;

            options.enabled = stored.m_enabled;
            options.addBackEngine = stored.m_addBackEngine;
            options.maxSpeed = stored.m_maxSpeed;
            options.acceleration = stored.m_acceleration;
            options.color0 = stored.m_color0;
            options.color1 = stored.m_color1;
            options.color2 = stored.m_color2;
            options.color3 = stored.m_color3;
            options.capacity = stored.m_capacity;
        }

        public static void RestoreAll()
        {
            foreach (VehicleInfo prefab in m_default.Keys)
            {
                Restore(prefab);
            }
        }

        public static void Clear()
        {
            m_default.Clear();
        }

        private DefaultOptions(VehicleInfo prefab)
        {
            VehicleOptions options = new VehicleOptions();
            options.SetPrefab(prefab);

            m_enabled = options.enabled;
            m_addBackEngine = options.addBackEngine;
            m_maxSpeed = options.maxSpeed;
            m_acceleration = options.acceleration;
            m_color0 = options.color0;
            m_color1 = options.color1;
            m_color2 = options.color2;
            m_color3 = options.color3;
            m_capacity = options.capacity;
        }

        private bool m_enabled;
        private bool m_addBackEngine;
        private float m_maxSpeed;
        private float m_acceleration;
        private HexaColor m_color0;
        private HexaColor m_color1;
        private HexaColor m_color2;
        private HexaColor m_color3;
        private int m_capacity;
    }
}
