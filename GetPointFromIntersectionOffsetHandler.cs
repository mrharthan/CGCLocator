using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.SOESupport;

namespace LRSLocator
{
  class GetPointFromIntersectionOffsetHandler : IRESTHandler
  {
    #region RESTHandler Members
    // Taskes as input Highway and RefMarker + Offset

    private RESTContext _context;
    public object HandleRequest(RESTContext context)
    {
      this._context = context;

      string text;
      if (!context.OperationInput.TryGetString("Intersection Offset Location", out text))
        return JSONHelper.BuildErrorObject(400, "Invalid Input.");

      string[] elements = text.Split(' ');
      if (elements.Length != 7)
        return JSONHelper.BuildErrorObject(400, "Invalid Input.");

      double distance;
      try
      {
        distance = Convert.ToDouble(elements[0]);
      }
      catch (Exception ex)
      {
        return JSONHelper.BuildErrorObject(400, "Invalid Distance.");
      }

      UnitsOfLength unitoflength;
      try
      {
        unitoflength = (UnitsOfLength)Enum.Parse(typeof(UnitsOfLength), elements[1], true);
      }
      catch (Exception e)
      {
        return JSONHelper.BuildErrorObject(400, "Invalid Unit.");
      }

      CardinalPoints direction;
      try
      {
        string dir = elements[2].Substring(0, 1).ToUpper();
        direction = (CardinalPoints)Enum.Parse(typeof(CardinalPoints), dir, true);
      }
      catch (Exception e)
      {
        return JSONHelper.BuildErrorObject(400, "Invalid Direction.");
      }

      string crosshighway = elements[4];
      string onhighway = elements[6];

      long? wkid;
      ISpatialReference spatialReference = null;
      try
      {
        if (context.OperationInput.TryGetAsLong("Spatial Reference", out wkid) && wkid.HasValue)
        {
          IGeometryServer geometryServer = new GeometryServerClass();
          spatialReference = geometryServer.FindSRByWKID("", (int)(wkid.Value), -1, true, true);
        }
      }
      catch (Exception ex)
      {
        return JSONHelper.BuildErrorObject(400, "Invalid Spatial Reference.", new List<string>() { ex.Message });
      }

      byte[] result = getPointFromIntersectionOffset(distance, unitoflength, direction, crosshighway, onhighway, spatialReference);

      return result;
    }

    #endregion

    private int ixMarkerNumber;
    private int ixDFOField;

    private byte[] getPointFromIntersectionOffset(double distance, UnitsOfLength unit, CardinalPoints direction, 
      string crosshighway, string onhighway, ISpatialReference outSR)
    {
      using (ComReleaser comReleaser = new ComReleaser())
      {
        // JUST PLACEHOLDERS, HERE.  ALL OF THIS MUST BE REDEVELOPED
        ixMarkerNumber = _context.LrmRdbdEquivalencyFeatureClass.FindField(_context.LrmRdbdEquivalencyFeatureClass_FMarkerNumberField);
        ixDFOField = _context.LrmRdbdEquivalencyFeatureClass.FindField(_context.LrmRdbdEquivalencyFeatureClass_FDFOField);

        //get the onRoute to query
        // Get the highway feature with this name.
        IFeature routeFeature = findRouteByName(onhighway);
        if (routeFeature == null)
        {
          // We have a problem, return
          return JSONHelper.BuildErrorObjectAsBytes(400, "Invalid Highway");
        }

        //Unit could be mile,feet and yard
        double distanceInMile = Helper.Convert(distance, unit, UnitsOfLength.Miles);

        //find IntersectionID by crosshighway and onhighway
        //query intersection featureclass twice and compare the IntersectionID to find the identical one
        Dictionary<long, double> intersectionsMValues;
        Dictionary<long, IPoint> intersectionsShapes;
        List<long> intersectionsfromroutes = findIntersectionFromRoutes(crosshighway, onhighway, out intersectionsMValues, out intersectionsShapes);

        List<PointFromIntersectionOffsetDataContract> lstPoints = new List<PointFromIntersectionOffsetDataContract>();
        foreach (long oneintersection in intersectionsfromroutes)
        {
          //get the MValue
          double MValue = intersectionsMValues[oneintersection];
          //get the intersection shape
          IPoint pIntersection = intersectionsShapes[oneintersection];

          //queryPoint
          //compare MValue-distance and MValue+distance directions with input direction to determine which point
          double measurevalue;
          CardinalPoints outPointBearing;
          IPoint outPoint = compareDirection(pIntersection, MValue, distanceInMile, routeFeature, direction, 
            out outPointBearing, out measurevalue);
          if (outPoint != null)
          {
            if (outSR != null) outPoint.Project(outSR);
            string markerwithOffset = identifyHighwayLocation(onhighway, measurevalue);
            PointFromIntersectionOffsetDataContract locationData = new PointFromIntersectionOffsetDataContract()
            {
              x = outPoint.X,
              y = outPoint.Y,
              HighwayName = onhighway,
              MarkerWithOffset = markerwithOffset,
              DFO = Math.Round(measurevalue,3),
              Bearing = outPointBearing.ToString("G")
            };
            lstPoints.Add(locationData);
          }
        }

        lstPoints.Sort();
        JsonObject resultCollection = new JsonObject();
        int i = 1;
        foreach (var pointData in lstPoints)
        {
          JsonObject result = new JsonObject();
          result.AddObject(i.ToString(), pointData);
          resultCollection.AddObject(i.ToString(), pointData);
          i++;
        }
        return Encoding.UTF8.GetBytes(resultCollection.ToJson());
      }
    }

    private List<long> findIntersectionFromRoutes(string crosshighway, string onhighway, 
      out Dictionary<long, double> intersectionsMValues, out Dictionary<long, IPoint> intersectionsShapes)
    {
      using (ComReleaser comReleaser = new ComReleaser())
      {
        IQueryFilter queryFilter = new QueryFilterClass();
        queryFilter.WhereClause = this._context.HighwayFeatureClass_RIDField + " = '" + crosshighway + "'";
        comReleaser.ManageLifetime(queryFilter);

        IFeatureCursor featureCursor = _context.IntersectionFeatureClass.Search(queryFilter, false);
        comReleaser.ManageLifetime(featureCursor);

        int iIDFieldIndex = _context.IntersectionFeatureClass.Fields.FindField(_context.IntersectionFeatureClass_IDField); 
        int iMValueFieldIndex = _context.IntersectionFeatureClass.Fields.FindField(_context.IntersectionFeatureClass_MValueField);

        List<long> crossIntersections = new List<long>();
        IFeature crossintersectionfeature;
        while ((crossintersectionfeature = featureCursor.NextFeature()) != null)
        {
          if (crossintersectionfeature.Shape == null || crossintersectionfeature.Shape.IsEmpty)
            continue;

          long recID = long.Parse(crossintersectionfeature.get_Value(iIDFieldIndex).ToString());
          crossIntersections.Add(recID);
        }

        IQueryFilter queryFilter2 = new QueryFilterClass();
        queryFilter2.WhereClause = this._context.HighwayFeatureClass_RIDField + " = '" + onhighway + "'";
        comReleaser.ManageLifetime(queryFilter2);

        IFeatureCursor featureCursor2 = _context.IntersectionFeatureClass.Search(queryFilter2, false);
        comReleaser.ManageLifetime(featureCursor2);

        List<long> onIntersections = new List<long>();
        intersectionsMValues = new Dictionary<long, double>();
        intersectionsShapes = new Dictionary<long, IPoint>();

        IFeature intersectionfeature;
        while ((intersectionfeature = featureCursor2.NextFeature()) != null)
        {
          if (intersectionfeature.Shape == null || intersectionfeature.Shape.IsEmpty)
            continue;

          long recID = long.Parse(intersectionfeature.get_Value(iIDFieldIndex).ToString());
          onIntersections.Add(recID);

          double curremtIntersectionM = double.Parse(intersectionfeature.get_Value(iMValueFieldIndex).ToString());
          intersectionsMValues.Add(recID, curremtIntersectionM);

          intersectionsShapes.Add(recID, (IPoint)intersectionfeature.ShapeCopy);
        }

        List<long> sameintersections = crossIntersections.Intersect(onIntersections).ToList();
        return sameintersections;
      }
    }

    private IFeature findRouteByName(string highwayName)
    {
      IFeature routeFeature = null;
      using (ComReleaser comReleaser = new ComReleaser())
      {
        // Get the highway feature with this name.
        IQueryFilter queryFilter = new QueryFilterClass();
        queryFilter.WhereClause = this._context.HighwayFeatureClass_RIDField + " = '" + highwayName + "'";
        comReleaser.ManageLifetime(queryFilter);

        IFeatureCursor featureCursor = _context.HighwayFeatureClass.Search(queryFilter, false);
        comReleaser.ManageLifetime(featureCursor);
        routeFeature = featureCursor.NextFeature();
        return routeFeature;
      }
    }

    private IPoint compareDirection(IPoint intersectionPoint, double MValue, double offsetDistance, 
      IFeature routeFeature, CardinalPoints direction, out CardinalPoints outPointBearing, out double measurevalue)
    {
      IPoint outPoint = new PointClass();
      bool selectPlusPoint = false;
      bool selectMinusPoint = false;
      CardinalPoints plusDir = CardinalPoints.E;
      CardinalPoints minusDir = CardinalPoints.E;

      // Use IMSegmentation to find the point on the highway with this measure value
      IMSegmentation3 segmentation = routeFeature.Shape as IMSegmentation3;
      double plusBearing = double.MaxValue;
      IPoint plusPoint = findPointOnHighwaybyMeasurevalue(segmentation, MValue + offsetDistance, intersectionPoint, out plusBearing);
      if (plusBearing < 360.1) plusDir = Helper.ToCardinalMark(plusBearing);

      double minusBearing = double.MaxValue;
      IPoint minusPoint = findPointOnHighwaybyMeasurevalue(segmentation, MValue - offsetDistance, intersectionPoint, out minusBearing);
      if (minusBearing < 360.1) minusDir = Helper.ToCardinalMark(minusBearing);

      if (plusPoint != null && minusPoint != null)
      {
        if (plusDir.CompareTo(direction) == 0) selectPlusPoint = true;
        else if (minusDir.CompareTo(direction) == 0) selectMinusPoint = true;
        else
        {
          double compareBearing = findclosetbearing(plusBearing, minusBearing, direction);
          if (Math.Abs(compareBearing - plusBearing) <= 0.001) selectPlusPoint = true;
          else selectMinusPoint = true;
        }
      }
      else if (plusPoint != null && minusPoint == null)
      {
        selectPlusPoint = true;
      }
      else if (plusPoint == null && minusPoint != null)
      {
        selectMinusPoint = true;
      }

      if (selectPlusPoint)
      {
        outPoint = plusPoint;
        outPointBearing = plusDir;
        measurevalue = MValue + offsetDistance;
      }
      else if (selectMinusPoint)
      {
        outPoint = minusPoint;
        outPointBearing = minusDir;
        measurevalue = MValue - offsetDistance;
      }
      else
      {
        outPoint = null;
        outPointBearing = CardinalPoints.E;
        measurevalue = double.MaxValue;
      }

      return outPoint;
    }

    private double findclosetbearing(double plusdegree, double minusdegree, CardinalPoints dir)
    {
      var CardinalRanges = new List<Helper.CardinalRanges>
                       {
                         new Helper.CardinalRanges {CardinalPoint = CardinalPoints.E, LowRange = 45, HighRange = 135},
                         new Helper.CardinalRanges {CardinalPoint = CardinalPoints.S, LowRange = 135, HighRange = 225},
                         new Helper.CardinalRanges {CardinalPoint = CardinalPoints.W, LowRange = 225, HighRange = 315},
                         new Helper.CardinalRanges {CardinalPoint = CardinalPoints.N, LowRange = 315, HighRange = 405}
                       };

      double lowrange = CardinalRanges.Find(p => (dir == p.CardinalPoint)).LowRange;
      double highrange = CardinalRanges.Find(p => (dir == p.CardinalPoint)).HighRange;
      double plusdegreecloseto;
      double minusedegreecloseto;
      double select;

      if (dir != CardinalPoints.N)
      {
        plusdegreecloseto = (Math.Abs(plusdegree - lowrange) > Math.Abs(plusdegree - highrange)) ? highrange : lowrange;
        minusedegreecloseto = (Math.Abs(minusdegree - lowrange) > Math.Abs(minusdegree - highrange)) ? highrange : lowrange;

        select = (Math.Abs(plusdegree - plusdegreecloseto) > Math.Abs(minusdegree - minusedegreecloseto)) ? minusdegree : plusdegree;
      }
      else
      {
        if (plusdegree < 180)
        {
          plusdegree += 360;
          plusdegreecloseto = highrange;
        }
        else
          plusdegreecloseto = lowrange;

        if (minusdegree < 180)
        {
          minusdegree += 360;
          minusedegreecloseto = highrange;
        }
        else
          minusedegreecloseto = lowrange;

        select = (Math.Abs(plusdegree - plusdegreecloseto) > Math.Abs(minusdegree - minusedegreecloseto)) ? minusdegree : plusdegree;
        if (select > 360) select -= 360;
      }

      return select;
    }

    private IPoint findPointOnHighwaybyMeasurevalue(IMSegmentation3 segmentation, double measureValue, IPoint intersectionPoint,
      out double calculatedBearing)
    {
      IPoint calculatedPoint = new PointClass();

      //get the spatial reference
      IGeometryServer geometryServer = new GeometryServerClass();
      //define a geographic coordinate system
      ISpatialReference spatialReference = geometryServer.FindSRByWKID("", 4269, -1, true, true);
      intersectionPoint.Project(spatialReference);

      // Use IMSegmentation to find the point on the highway with this measure value
      IGeometryCollection pointCollection = segmentation.GetPointsAtM(measureValue, 0);
      if (pointCollection != null && pointCollection.GeometryCount > 0)
      {
        calculatedPoint = pointCollection.get_Geometry(0) as IPoint;
        calculatedPoint.Project(spatialReference);

        Coordinate point1 = new Coordinate() { Latitude = calculatedPoint.Y, Longitude = calculatedPoint.X };
        Coordinate point2 = new Coordinate() { Latitude = intersectionPoint.Y, Longitude = intersectionPoint.X };
        calculatedBearing = Helper.Bearing(point2, point1);
      }
      else
      {
        calculatedPoint = null;
        calculatedBearing = double.MaxValue;
      }

      return calculatedPoint;
    }

    private string identifyHighwayLocation(string highwayName, double measureValue)
    {
      using (ComReleaser comReleaser = new ComReleaser())
      {
        // Query Marker table and find the closest Marker to this measure
        // JUST PLACEHOLDERS, HERE.  ALL OF THIS MUST BE REDEVELOPED
        IQueryFilter queryFilter = new QueryFilterClass();
        queryFilter.WhereClause = this._context.LrmRdbdEquivalencyFeatureClass_RIDField + " = '" + highwayName + "'";
        comReleaser.ManageLifetime(queryFilter);

        IFeatureCursor markerTableCursor = _context.LrmRdbdEquivalencyFeatureClass.Search(queryFilter, true);
        comReleaser.ManageLifetime(markerTableCursor);

        IFeature markerRow;

        string closestMarker = "";
        double closestDFO = 0;
        double closestDistance = double.MaxValue;

        while ((markerRow = markerTableCursor.NextFeature()) != null)
        {
          string marker = markerRow.get_Value(ixMarkerNumber).ToString();
          object objDFODistance = markerRow.get_Value(ixDFOField);
          if (objDFODistance == null && objDFODistance == DBNull.Value)
            continue;

          double dfoDistance;
          if (double.TryParse(markerRow.get_Value(ixDFOField).ToString(), out dfoDistance) == false)
            continue;

          if (Math.Abs(measureValue - dfoDistance) < closestDistance)
          {
            closestDistance = Math.Abs(measureValue - dfoDistance);
            closestMarker = marker;
            closestDFO = dfoDistance;
          }
        }

        // Calculate Offset
        double offset = measureValue - closestDFO;

        offset = Math.Round(offset, 3);

        if (offset >= 0)
          closestMarker += " +" + offset;
        else
          closestMarker += offset;

        // Return
        return closestMarker;
      }
    }
  }
}
