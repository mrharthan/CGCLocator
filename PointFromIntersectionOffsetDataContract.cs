using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace LRSLocator
{
  /// <summary>
  /// Data contract for the Closest Intersection Output
  /// </summary>
  [DataContract]
  class PointFromIntersectionOffsetDataContract : IComparable<PointFromIntersectionOffsetDataContract>
  {
    [DataMember(Order = 0, Name = "x")]
    public double x { get; set; }

    [DataMember(Order = 1, Name = "y")]
    public double y { get; set; }

    [DataMember(Order = 2, Name = "DFO")]
    public double DFO { get; set; }

    [DataMember(Order = 3, Name = "HighwayName")]
    public string HighwayName { get; set; }

    [DataMember(Order = 4, Name = "MarkerWithOffset")]
    public string MarkerWithOffset { get; set; }

    [DataMember(Order = 5, Name = "Bearing")]
    public string Bearing { get; set; }

    public PointFromIntersectionOffsetDataContract()
    {
      this.x = double.MaxValue;
      this.y = double.MaxValue;
      this.HighwayName = string.Empty;
      this.MarkerWithOffset = string.Empty;
      this.DFO = double.MaxValue;
      this.Bearing = string.Empty;
    }

    public int CompareTo(PointFromIntersectionOffsetDataContract other)
    {
      if (other == null) return -1;

      return DFO.CompareTo(other.DFO);
    }
  }
}
