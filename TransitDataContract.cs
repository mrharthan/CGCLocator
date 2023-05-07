using System;
using System.Runtime.Serialization;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.SOESupport;

namespace LRSLocator
{
    /// <summary>
    /// Data contract for Multi-Routing Polyline Tools
    /// </summary>
    [DataContract]
    public class TransitDataContract
    {
        [DataMember(Order = 1, Name = "Total Miles")]
        public double TotalMiles { get; set; }

        [DataMember(Order = 1, Name = "Total Minutes")]
        public double TotalMinutes { get; set; }

        [DataMember(Order = 1, Name = "Total Dollars")]
        public double TotalDollars { get; set; }

        [DataMember(Order = 1, Name = "Output Spatial Reference")]
        public long OutSpatialReference { get; set; }

        [DataMember(Order = 1, Name = "Geometry")]
        public IPolyline Geometry { get; set; }

        public TransitDataContract()
        {
            this.TotalMiles = double.NaN;
            this.TotalMinutes = double.NaN;
            this.TotalDollars = double.NaN;
            this.OutSpatialReference = 0;
            this.Geometry = null;
        }
    }
}