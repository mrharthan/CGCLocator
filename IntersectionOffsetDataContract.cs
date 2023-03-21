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
  class IntersectionOffsetDataContract : IComparable<IntersectionOffsetDataContract>
  {
    [DataMember(Order = 0, Name = "IntersectionOffset")]
    public string IntersectionOffset { get; set; }

    [DataMember(Order = 1, Name = "SnapDistance")]
    public double SnapDistance { get; set; }

    [DataMember(Order = 2, Name = "HighwayName")]
    public string HighwayName { get; set; }

    [DataMember(Order = 3, Name = "AltHighwayName")]
    public string AltHighwayName { get; set; }

    [DataMember(Order = 4, Name = "MarkerWithOffset")]
    public string MarkerWithOffset { get; set; }    

    public IntersectionOffsetDataContract()
    {
      this.IntersectionOffset = string.Empty;
      this.SnapDistance = Double.MaxValue;
      this.HighwayName = string.Empty;
      this.MarkerWithOffset = string.Empty;
    }

    public int CompareTo(IntersectionOffsetDataContract other)
    {
      if (other == null) return -1;

      HighwayHierarchy thisHierarchy;
      HighwayHierarchy otherHierarchy;

      try
      {
        thisHierarchy = (HighwayHierarchy)Enum.Parse(typeof(HighwayHierarchy), HighwayName.Substring(0, 2), true);
      }
      catch (Exception ex)
      {
        return -1;
      }

      try
      {
        otherHierarchy = (HighwayHierarchy)Enum.Parse(typeof(HighwayHierarchy), other.HighwayName.Substring(0, 2), true);
      }
      catch (Exception ex)
      {
        return -1;
      }

      if (thisHierarchy > otherHierarchy)
        return 1;
      else if (thisHierarchy < otherHierarchy)
        return -1;
      else
      {
        //compare number
        int thisNum;
        int otherNum;

        try
        {
          thisNum = int.Parse(HighwayName.Substring(3));
        }
        catch (Exception ex)
        {
          thisNum = 0; // thisNum can't convert to number
        }

        try
        {
          otherNum = int.Parse(other.HighwayName.Substring(3));
        }
        catch (Exception ex)
        {
          otherNum = 0; // otherNum can't convert to number
        }

        if (thisNum > otherNum)
          return 1;
        else if (thisNum < otherNum)
          return -1;
        else 
          return 0;
      }
    }

    public enum HighwayHierarchy { IH, US, UA, UP, SH, SA, SL, SS, BI, BS, BU, BF, FM, RM, RR, PR, RE, RP, FS, RS, RU, PA }
  }
}
