using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.SOESupport;
using ESRI.ArcGIS.esriSystem;

namespace LRSLocator
{
  class GetIntersectionOffsetHandler : IRESTHandler
  {
    #region RESTHandler Members
    // Taskes as input Highway and RefMarker + Offset

    private RESTContext _context;
    public object HandleRequest(RESTContext context)
    {
      this._context = context;

      double? latitude, longitude;
      if (!context.OperationInput.TryGetAsDouble("Longitude", out longitude) || !longitude.HasValue)
        return JSONHelper.BuildErrorObject(400, "Invalid Longitude.");

      if (!context.OperationInput.TryGetAsDouble("Latitude", out latitude) || !latitude.HasValue)
        return JSONHelper.BuildErrorObject(400, "Invalid Latitude.");

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

      string highwayName;
      try
      {
        if (!context.OperationInput.TryGetString("Highway", out highwayName) || highwayName == null)
          highwayName = ""; // highwayName is optional
      }
      catch(Exception ex)
      {
        highwayName = ""; // highwayName is optional
      }

      bool? returnMultipleValues;
      if (!context.OperationInput.TryGetAsBoolean("Multiple Values", out returnMultipleValues) || !returnMultipleValues.HasValue)
        returnMultipleValues = false; // Default is false.

      byte[] result = findIntersectionOffset(longitude.Value, latitude.Value, spatialReference, highwayName, returnMultipleValues.Value);

      return result;
    }

    #endregion

    private int ixHighwayNameField;
    private int ixMarkerNumber;
    private int ixDFOField;
    private int iIDFieldIndex;
    private int iMValueFieldIndex;

    private byte[] findIntersectionOffset(double x, double y, ISpatialReference spatialReference, string highwayName, bool returnMultipleValues)
    {
      using (ComReleaser comReleaser = new ComReleaser())
      {
        IPoint point = new PointClass();
        point.PutCoords(x, y);
        if (spatialReference != null)
          point.SpatialReference = spatialReference;

        ISpatialReference highwayFCSR = (_context.HighwayFeatureClass as IGeoDataset).SpatialReference;

        point.Project(highwayFCSR);
        // JUST PLACEHOLDERS, HERE.  ALL OF THIS MUST BE REDEVELOPED
        ixHighwayNameField = _context.HighwayFeatureClass.Fields.FindField(_context.HighwayFeatureClass_RIDField);
        ixMarkerNumber = _context.LrmRdbdEquivalencyFeatureClass.FindField(_context.LrmRdbdEquivalencyFeatureClass_FMarkerNumberField);
        ixDFOField = _context.LrmRdbdEquivalencyFeatureClass.FindField(_context.LrmRdbdEquivalencyFeatureClass_FDFOField);
        iIDFieldIndex = _context.IntersectionFeatureClass.Fields.FindField(_context.IntersectionFeatureClass_IDField);
        iMValueFieldIndex = _context.IntersectionFeatureClass.Fields.FindField(_context.IntersectionFeatureClass_MValueField);

        List<IFeature> highwayFeatures = new List<IFeature>();
        if (highwayName != "")
        {
          // Get the highway feature with this name.
          IFeature routeFeature = findRouteByName(highwayName);
          if (routeFeature == null)
          {
            // We have a problem, return
            return JSONHelper.BuildErrorObjectAsBytes(400, "Invalid Highway");
          }
          highwayFeatures.Add(routeFeature);
        }
        else
        {
          // Find the closest Highway
          highwayFeatures = findClosestFeatures(point, returnMultipleValues);
          if (highwayFeatures == null || highwayFeatures.Count == 0)
          {
            // We have a problem. Return null
            return JSONHelper.BuildErrorObjectAsBytes(400, "Could not find location.");
          }
        }

        List<IntersectionOffsetDataContract> lstintersectionOffset = new List<IntersectionOffsetDataContract>();
        foreach (var highwayFeature in highwayFeatures)
        {
          // Get the Measure value
          IPoint snapPoint = new PointClass();
          double dAlong = 0;
          double dFrom = 0;
          bool bRight = false;
          (highwayFeature.Shape as IPolyline6).QueryPointAndDistance(esriSegmentExtension.esriNoExtension, point, true, snapPoint, ref dAlong, ref dFrom, ref bRight);
          double measureValue = snapPoint.M;

          //get input point distance from route
          double distancefromRoute = double.MaxValue; 
          IUnitConverter unitConverter = new UnitConverterClass();
          if (highwayFCSR is IGeographicCoordinateSystem)
          {
            //Geographic, unit is in decimal degrees
            distancefromRoute = unitConverter.ConvertUnits(dFrom, esriUnits.esriDecimalDegrees, esriUnits.esriFeet);
          }
          else if (highwayFCSR is IProjectedCoordinateSystem)
          {
            // Projected, unit is in meters
            distancefromRoute = unitConverter.ConvertUnits(dFrom, esriUnits.esriMeters, esriUnits.esriFeet);
          }
          else
          {
            // Unknown coordinate system
          }
          distancefromRoute = Math.Round(distancefromRoute, 3);

          highwayName = highwayFeature.get_Value(ixHighwayNameField).ToString();
          string markerwithOffset = identifyHighwayLocation(highwayName, measureValue);
          //query intersection table to get the closet intersection on this route
          double closest;
          UnitsOfLength unit = UnitsOfLength.Miles;
          List<FeatureRec> lstIntersectionPoint = findClosestFeatures(highwayName, measureValue, returnMultipleValues, out closest);
          closest = Math.Round(closest, 3);
          if (Convert.ToInt32(closest) == 0)
          {
            closest = Helper.Convert(closest, UnitsOfLength.Miles, UnitsOfLength.Feet);
            unit = UnitsOfLength.Feet;
          }

          //get the interesected routename by query interesectionID and RouteName
          List<IntersectionOffsetDataContract> lstrouteNM = findIntersectedRoute(snapPoint, spatialReference, highwayName,
            returnMultipleValues, lstIntersectionPoint, closest, unit, markerwithOffset, distancefromRoute);
          foreach (var oneoffsetitem in lstrouteNM)
            lstintersectionOffset.Add(oneoffsetitem);
        }

        //Sort the output by Highway Name
        lstintersectionOffset.Sort();

        JsonObject resultCollection = new JsonObject();
        int i = 1;
        foreach (var locationData in lstintersectionOffset)
        {
          JsonObject result = new JsonObject();
          result.AddObject(i.ToString(), locationData);

          if (returnMultipleValues == false)
            return Encoding.UTF8.GetBytes(result.ToJson());
          else
            resultCollection.AddObject(i.ToString(), locationData);

          i++;
        }
        return Encoding.UTF8.GetBytes(resultCollection.ToJson());
      }
    }

    private List<FeatureRec> findClosestFeatures(string highwayName, double measureValue, bool returnMultipleValues, out double closest)
    {
      List<FeatureRec> closestFeatureRecs = new List<FeatureRec>();
      using (ComReleaser comReleaser = new ComReleaser())
      {
        IQueryFilter queryFilter2 = new QueryFilterClass();
        queryFilter2.WhereClause = this._context.HighwayFeatureClass_RIDField + " = '" + highwayName + "'";
        comReleaser.ManageLifetime(queryFilter2);

        IFeatureCursor featureCursor2 = _context.IntersectionFeatureClass.Search(queryFilter2, false);
        comReleaser.ManageLifetime(featureCursor2);

        //get the closet intersection(s)
        //note: not the minimum one is the closet one, should be minimum of the absoulte value
        closest = double.MaxValue;
        double difference = Math.Abs(0.0001); // Define the tolerance for variation in their values

        IFeature intersectionfeature;
        while ((intersectionfeature = featureCursor2.NextFeature()) != null)
        {
          if (intersectionfeature.Shape == null || intersectionfeature.Shape.IsEmpty)
            continue;

          double curremtIntersectionM = double.Parse(intersectionfeature.get_Value(iMValueFieldIndex).ToString());
          double dvalue = Math.Abs(curremtIntersectionM - measureValue);
          if (Math.Abs(dvalue - closest) <= difference) //equal case
          {
            if (returnMultipleValues)
              closestFeatureRecs.Add(new FeatureRec() 
              { 
                recShape = (IPoint)intersectionfeature.ShapeCopy, 
                recID = long.Parse(intersectionfeature.get_Value(iIDFieldIndex).ToString())
              });
          }
          else if (dvalue < closest)
          {
            closest = dvalue;
            closestFeatureRecs.Clear();
            closestFeatureRecs.Add(new FeatureRec() 
            { 
              recShape = (IPoint)intersectionfeature.ShapeCopy, 
              recID = long.Parse(intersectionfeature.get_Value(iIDFieldIndex).ToString())
            });
          }
        }
      }
      return closestFeatureRecs;
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

    private List<IntersectionOffsetDataContract> findIntersectedRoute(IPoint snapPoint, ISpatialReference spatialReference, 
      string highwayName, bool returnMultipleValues, List<FeatureRec> lstIntersectionPoint, 
      double closest, UnitsOfLength unit, string markerwithOffset, double distancefromRoute)
    {
      List<IntersectionOffsetDataContract> lstrouteNM = new List<IntersectionOffsetDataContract>();
      //distance along the route value is closest value,
      //bearing can be calculated by 2 points
      snapPoint.Project(spatialReference);
      Coordinate point1 = new Coordinate() { Latitude = snapPoint.Y, Longitude = snapPoint.X };
      foreach (FeatureRec intersectionP in lstIntersectionPoint)
      {
        IPoint pClosestIntersection = intersectionP.recShape;
        pClosestIntersection.Project(spatialReference);
        long lIntersectionID = intersectionP.recID;
        Coordinate point2 = new Coordinate() { Latitude = pClosestIntersection.Y, Longitude = pClosestIntersection.X };
        CardinalPoints bearing = Helper.ToCardinalMark(Helper.Bearing(point2, point1));
        CardinalPointsFullName bearingfullname = (CardinalPointsFullName) Enum.Parse(typeof(CardinalPointsFullName), bearing.ToString("d"));

        using (ComReleaser comReleaser = new ComReleaser())
        {
          //find the corresponding highways and directions
          IQueryFilter queryFilter3 = new QueryFilterClass();
          queryFilter3.WhereClause = this._context.IntersectionFeatureClass_IDField + " = " + lIntersectionID;
          comReleaser.ManageLifetime(queryFilter3);

          IFeatureCursor featureCursor3 = _context.IntersectionFeatureClass.Search(queryFilter3, false);
          comReleaser.ManageLifetime(featureCursor3);

          int iRouteNameFieldIndex = _context.IntersectionFeatureClass.Fields.FindField(_context.HighwayFeatureClass_RIDField);

          IFeature routeNMfeature = null;
          while ((routeNMfeature = featureCursor3.NextFeature()) != null)
          {
            string routeName = (string)routeNMfeature.get_Value(iRouteNameFieldIndex);
            if (!routeName.Equals(highwayName))
            {
              IntersectionOffsetDataContract locationData = new IntersectionOffsetDataContract()
              {
                IntersectionOffset = closest.ToString() + " " + unit.ToString("G") + " " + bearingfullname.ToString("G") + " of " + routeName + " on " + highwayName,
                SnapDistance = distancefromRoute,
                HighwayName = highwayName,
                MarkerWithOffset = markerwithOffset
              };
              lstrouteNM.Add(locationData);

              if (!returnMultipleValues) break;
            }
          }
        }
        if (lstrouteNM.Count > 0 && !returnMultipleValues) break;
      }
      return lstrouteNM;
    }

    private List<IFeature> findClosestFeatures(IPoint point, bool returnMultipleValues)
    {
      List<IFeature> closestFeatures = new List<IFeature>();
      using (ComReleaser comReleaser = new ComReleaser())
      {
        IEnvelope searchEnvelope = point.Envelope;
        searchEnvelope.Expand(_context.SearchTolerance, _context.SearchTolerance, false);

        ISpatialFilter spatialFilter = new SpatialFilter();
        spatialFilter.Geometry = searchEnvelope;
        spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

        comReleaser.ManageLifetime(spatialFilter);

        IFeatureCursor featureCursor = _context.HighwayFeatureClass.Search(spatialFilter, false);
        comReleaser.ManageLifetime(featureCursor);
        IFeature feature;

        IFeature closestFeature = null;
        double closestDistance = double.MaxValue;

        while ((feature = featureCursor.NextFeature()) != null)
        {
          if (feature.Shape == null || feature.Shape.IsEmpty)
            continue;

          if (returnMultipleValues)
            closestFeatures.Add(feature);

          else
          {
            //Find the distance from input point to this feature
            IProximityOperator proximityOperator = feature.Shape as IProximityOperator;
            double distance = proximityOperator.ReturnDistance(point);
            if (distance < closestDistance)
            {
              closestDistance = distance;
              closestFeature = feature;
              closestFeatures.Clear();
              closestFeatures.Add(closestFeature);
            }
          }
        }
        return closestFeatures;
      }
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

  public class FeatureRec
  {
    public IPoint recShape { get; set; }
    public long recID { get; set; }
  }
}
