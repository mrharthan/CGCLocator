using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LRSLocator
{
  public enum UnitsOfLength { Miles,NauticalMiles,Kilometer,Feet,Yards}
  public enum CardinalPoints { N, E, W, S, NE, NW, SE, SW }
  public enum CardinalPointsFullName { North, East, West, South, NorthEast, NorthWest, SouthEast, SouthWest }

  public class Coordinate
  {
    private double latitude, longitude;

    /// <summary>
    /// Latitude in degrees. -90 to 90
    /// </summary>
    public Double Latitude
    {
      get { return latitude; }
      set
      {
        if (value > 90) throw new ArgumentOutOfRangeException("value", "Latitude value cannot be greater than 90.");
        if (value < -90) throw new ArgumentOutOfRangeException("value", "Latitude value cannot be less than -90.");
        latitude = value;
      }
    }

    /// <summary>
    /// Longitude in degree. -180 to 180
    /// </summary>
    public Double Longitude
    {
      get { return longitude; }
      set
      {
        if (value > 180) throw new ArgumentOutOfRangeException("value", "Longitude value cannot be greater than 180.");
        if (value < -180) throw new ArgumentOutOfRangeException("value", "Longitude value cannot be less than -180.");
        longitude = value;
      }
    }
  }

  public static class Helper
  {

    /// <summary>
    /// Class is used in a calculation to determin cardinal point enumeration values from degrees.
    /// </summary>
    public struct CardinalRanges
    {
      public CardinalPoints CardinalPoint;
      /// <summary>
      /// Low range value associated with the cardinal point.
      /// </summary>
      public Double LowRange;
      /// <summary>
      /// High range value associated with the cardinal point.
      /// </summary>
      public Double HighRange;
    }

    private const Double _MilesToKilometers = 1.609344;
    private const Double _MilesToNautical = 0.8684;

    /// <summary>
    /// Converts degrees to Radians.
    /// </summary>
    /// <returns>Returns a radian from degrees.</returns>
    public static Double ToRadian(this Double degree) { return (degree * Math.PI / 180.0); }
    /// <summary>
    /// To degress from a radian value.
    /// </summary>
    /// <returns>Returns degrees from radians.</returns>
    public static Double ToDegree(this Double radian) { return (radian / Math.PI * 180.0); }




    /// <summary>
    /// Calculates the distance between two points of latitude and longitude.
    /// Great Link - http://www.movable-type.co.uk/scripts/latlong.html
    /// </summary>
    /// <param name="coordinate1">First coordinate.</param>
    /// <param name="coordinate2">Second coordinate.</param>
    /// <param name="unitsOfLength">Sets the return value unit of length.</param>
    public static Double Distance(Coordinate coordinate1, Coordinate coordinate2, UnitsOfLength unitsOfLength)
    {

      var theta = coordinate1.Longitude - coordinate2.Longitude;
      var distance = Math.Sin(coordinate1.Latitude.ToRadian()) * Math.Sin(coordinate2.Latitude.ToRadian()) +
                     Math.Cos(coordinate1.Latitude.ToRadian()) * Math.Cos(coordinate2.Latitude.ToRadian()) *
                     Math.Cos(theta.ToRadian());

      distance = Math.Acos(distance);
      distance = distance.ToDegree();
      distance = distance * 60 * 1.1515;

      if (unitsOfLength == UnitsOfLength.Kilometer)
        distance = distance * _MilesToKilometers;
      else if (unitsOfLength == UnitsOfLength.NauticalMiles)
        distance = distance * _MilesToNautical;

      return (distance);

    }


    // The directional names are also routinely and very conveniently associated with 
    // the degrees of rotation in the unit circle, a necessary step for navigational 
    // calculations (derived from trigonometry) and/or for use with Global 
    // Positioning Satellite (GPS) Receivers. The four cardinal directions 
    // correspond to the following degrees of a compass:
    //
    // North (N): 0° = 360° 
    // East (E): 90° 
    // South (S): 180° 
    // West (W): 270° 
    // An ordinal, or intercardinal, direction is one of the four intermediate 
    // compass directions located halfway between the cardinal directions.
    //
    // Northeast (NE), 45°, halfway between north and east, is the opposite of southwest. 
    // Southeast (SE), 135°, halfway between south and east, is the opposite of northwest. 
    // Southwest (SW), 225°, halfway between south and west, is the opposite of northeast. 
    // Northwest (NW), 315°, halfway between north and west, is the opposite of southeast. 
    // These 8 words have been further compounded, resulting in a total of 32 named 
    // (and numbered) points evenly spaced around the compass. It is noteworthy that 
    // there are languages which do not use compound words to name the points, 
    // instead assigning unique words, colors, and/or associations with phenomena of the natural world.

    /// <summary>
    /// Method extension for Doubles. Converts a degree to a cardinal point enumeration.
    /// </summary>
    /// <returns>Returns a cardinal point enumeration representing a compass direction.</returns>
    public static CardinalPoints ToCardinalMark(this Double degree)
    {

      /*var CardinalRanges = new List<CardinalRanges>
                       {
                         new CardinalRanges {CardinalPoint = CardinalPoints.N, LowRange = 0, HighRange = 22.5},
                         new CardinalRanges {CardinalPoint = CardinalPoints.NE, LowRange = 22.5, HighRange = 67.5},
                         new CardinalRanges {CardinalPoint = CardinalPoints.E, LowRange = 67.5, HighRange = 112.5},
                         new CardinalRanges {CardinalPoint = CardinalPoints.SE, LowRange = 112.5, HighRange = 157.5},
                         new CardinalRanges {CardinalPoint = CardinalPoints.S, LowRange = 157.5, HighRange = 202.5},
                         new CardinalRanges {CardinalPoint = CardinalPoints.SW, LowRange = 202.5, HighRange = 247.5},
                         new CardinalRanges {CardinalPoint = CardinalPoints.W, LowRange = 247.5, HighRange = 292.5},
                         new CardinalRanges {CardinalPoint = CardinalPoints.NW, LowRange = 292.5, HighRange = 337.5},
                         new CardinalRanges {CardinalPoint = CardinalPoints.N, LowRange = 337.5, HighRange = 360.1}
                       };*/

      var CardinalRanges = new List<CardinalRanges>
                       {
                         new CardinalRanges {CardinalPoint = CardinalPoints.N, LowRange = 0, HighRange = 45},
                         new CardinalRanges {CardinalPoint = CardinalPoints.E, LowRange = 45, HighRange = 135},
                         new CardinalRanges {CardinalPoint = CardinalPoints.S, LowRange = 135, HighRange = 225},
                         new CardinalRanges {CardinalPoint = CardinalPoints.W, LowRange = 225, HighRange = 315},
                         new CardinalRanges {CardinalPoint = CardinalPoints.N, LowRange = 315, HighRange = 360.1}
                       };


      if (!(degree >= 0 && degree <= 360))
        throw new ArgumentOutOfRangeException("degree",
                                              "Degree value must be greater than or equal to 0 and less than or equal to 360.");


      return CardinalRanges.Find(p => (degree >= p.LowRange && degree < p.HighRange)).CardinalPoint;


    }

    /// <summary>
    /// Accepts two coordinates in degrees.
    /// </summary>
    /// <returns>A double value in degrees.  From 0 to 360.</returns>
    public static Double Bearing(Coordinate coordinate1, Coordinate coordinate2)
    {
      var latitude1 = coordinate1.Latitude.ToRadian();
      var latitude2 = coordinate2.Latitude.ToRadian();

      var longitudeDifference = (coordinate2.Longitude - coordinate1.Longitude).ToRadian();

      var y = Math.Sin(longitudeDifference) * Math.Cos(latitude2);
      var x = Math.Cos(latitude1) * Math.Sin(latitude2) -
              Math.Sin(latitude1) * Math.Cos(latitude2) * Math.Cos(longitudeDifference);

      return (Math.Atan2(y, x).ToDegree() + 360) % 360;
    }

    public static double[] Conversions = new double[]
    {
            1,                 //Miles
            0.8689741,         //Nautical Miles
            1.609344,          //Kilometre
            5280,              //Feet
            1760               //Yards
    };

    public static double Convert(double fromValue, UnitsOfLength fromUnit, UnitsOfLength toUnit)
    {
      double workingValue;

      if (fromUnit == UnitsOfLength.Miles)
        workingValue = fromValue;
      else
        workingValue = fromValue / Conversions[(int)fromUnit];

      if (toUnit != UnitsOfLength.Miles)
        workingValue = workingValue * Conversions[(int)toUnit];

      return workingValue;
    }
  }
}
