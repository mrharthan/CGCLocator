using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.SOESupport;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;

namespace LRSLocator
{
    /// Handles REST calls that take a X/Y as input and return the traversal information as output    
    public class GetAutoCostFromLatLongHandler : IRESTHandler
    {
        #region RESTHandler Members
        string first_rteid = "";
        double first_bdfo = 0.0;
        double first_edfo = 0.0;
        double first_avgspeed = 0.0;
        string last_rteid = "";
        double last_bdfo = 0.0;
        double last_edfo = 0.0;
        double last_avgspeed = 0.0;
        double autoSpeedAvg = 0.0;
        double autoIncomeAvg = 0.0;
        short parkpenaltyMin = 0;        

        private RESTContext _context;

        public object HandleRequest(RESTContext context)
        {
            this._context = context;            

            try
            {
                double? begin_latitude, begin_longitude;
                if (!context.OperationInput.TryGetAsDouble("Begin_Longitude", out begin_longitude) || !begin_longitude.HasValue)
                    return JSONHelper.BuildErrorObject(400, "Invalid Begin Longitude.");

                if (!context.OperationInput.TryGetAsDouble("Begin_Latitude", out begin_latitude) || !begin_latitude.HasValue)
                    return JSONHelper.BuildErrorObject(400, "Invalid Begin Latitude.");

                double? end_latitude, end_longitude;
                if (!context.OperationInput.TryGetAsDouble("End_Longitude", out end_longitude) || !end_longitude.HasValue)
                    return JSONHelper.BuildErrorObject(400, "Invalid End Longitude.");

                if (!context.OperationInput.TryGetAsDouble("End_Latitude", out end_latitude) || !end_latitude.HasValue)
                    return JSONHelper.BuildErrorObject(400, "Invalid End Latitude.");

                string inputTFrame;
                if (!context.OperationInput.TryGetString("Time_Of_Day", out inputTFrame) && (inputTFrame != null))
                    return JSONHelper.BuildErrorObject(400, "Invalid Time-Of-Day Entry.");

                long? wkid;
                ISpatialReference spatialReference = null;
            
                if (!context.OperationInput.TryGetAsLong("Spatial_Reference", out wkid) && !wkid.HasValue)
                {
                    spatialReference = (_context.HighwayFeatureClass as IGeoDataset).SpatialReference;
                }
                else
                {
                    IGeometryServer geometryServer = new GeometryServerClass();
                    spatialReference = geometryServer.FindSRByWKID("EPSG", (int)(wkid.Value), -1, true, true);
                }

                byte[] result;
                           
                result = idAutoFeatures(begin_longitude.Value, begin_latitude.Value, end_longitude.Value, end_latitude.Value, inputTFrame, wkid.Value);

                return result;
            }
            catch (Exception ex)
            {
                return JSONHelper.BuildErrorObject(400, "General Error:", new List<string>() { ex.Message });
            }            
        }

        #endregion

        private byte[] idAutoFeatures(double firstX, double firstY, double lastX, double lastY, string inputTFrame, long spatialRef)
        {
            using (ComReleaser comReleaser = new ComReleaser())
            {
                AutoDataContract resultLRMData = new AutoDataContract();
                JsonObject resultCollection = new JsonObject();
                
                List<IPoint> userPoints = new List<IPoint>();
                IPoint first_point = new PointClass();
                IPoint last_point = new PointClass();

                double beginMeasure = 0.0;
                double endMeasure = 0.0;
                double beginSegLen = 0.0;
                double endSegLen = 0.0;
                double beginSegTime = 0.0;
                double endSegTime = 0.0;
                int i = 1;

                first_point.PutCoords(firstX, firstY);
                first_point.Project((_context.HighwayFeatureClass as IGeoDataset).SpatialReference);
                userPoints.Add(first_point);

                last_point.PutCoords(lastX, lastY);
                last_point.Project((_context.HighwayFeatureClass as IGeoDataset).SpatialReference);
                userPoints.Add(last_point);

                IGeometryServer geometryServer = new GeometryServerClass();
                IGeometryCollection geometryCollection = new GeometryBag() as IGeometryCollection;

                // Find the closest Traveler Metrics feature at the beginning of the traversal -- Traveler Income
                List<IFeature> beginTravFeatures = findClosestFeatures(first_point, "Traveler Data");

                if (beginTravFeatures.Count == 0)
                {
                    //We have a problem. Return null
                    return JSONHelper.BuildErrorObjectAsBytes(400, "Could not find Traveler Income at begin point location.");
                }

                // Find the closest Parking Metrics feature at the end of the traversal -- Parking Penalty
                List<IFeature> endTrnsferFeatures = findClosestFeatures(last_point, "Parking Data");

                if (endTrnsferFeatures.Count == 0)
                {
                    //We have a problem. Return null
                    return JSONHelper.BuildErrorObjectAsBytes(400, "Could not find Parking Penalties at end point location.");
                }

                // Find the closest Trip Metrics features at each end of the traversal -- Trip Time
                List<IFeature> beginTripFeatures = findClosestFeatures(first_point, "Trip Data");
                List<IFeature> endTripFeatures = findClosestFeatures(last_point, "Trip Data");

                if (beginTripFeatures.Count == 0)
                {
                    //We have a problem. Return null
                    return JSONHelper.BuildErrorObjectAsBytes(400, "Could not find Trip Speed at begin point location.");
                }

                if (endTripFeatures.Count == 0)
                {
                    //We have a problem. Return null
                    return JSONHelper.BuildErrorObjectAsBytes(400, "Could not find Trip Speed at end point location.");
                }

                List<IFeature> firstMetrics = getMetricsLimitsFeatures(beginTravFeatures, inputTFrame, "Traveler Data", true);
                List<IFeature> lastMetrics = getMetricsLimitsFeatures(endTrnsferFeatures, inputTFrame, "Parking Data", false);
                // IF the return is two features, it is only because the User-defined point falls preceisely between two trip features.  In this case, the walking M-value from either feature should match.
                List<IFeature> firstLRMs = getMetricsLimitsFeatures(beginTripFeatures, inputTFrame, "Trip Data", true);
                first_avgspeed = autoSpeedAvg;
                List<IFeature> lastLRMs = getMetricsLimitsFeatures(endTripFeatures, inputTFrame, "Trip Data", false);
                last_avgspeed = autoSpeedAvg;

                IFeature firstGeometry = null;

                foreach (IFeature firstM in firstLRMs)
                {
                    beginMeasure = identifyMValue(first_point, firstM);  // User-Defined Begin M Value
                    beginSegLen = Math.Abs(first_edfo - beginMeasure);
                    firstGeometry = firstM;
                    if (beginSegLen > 0.0)
                        beginSegTime = (beginSegLen / first_avgspeed) * 60;                                                            
                }
                
                geometryCollection.AddGeometry((firstGeometry.Shape as IMSegmentation3).GetSubcurveBetweenMs(beginMeasure, first_edfo) as IGeometry);

                foreach (IFeature lastM in lastLRMs)
                {
                    endMeasure = identifyMValue(last_point, lastM);   // User-Defined End M Value
                    endSegLen = Math.Abs(endMeasure - last_bdfo);

                    if (endSegLen > 0.0)
                        endSegTime = (endSegLen / last_avgspeed) * 60;
                    
                }
                              
                // Two Points Entered: Define the Network Path
                RouteFromInputPoints traversal = new RouteFromInputPoints();
                GetPathControlSegments getPathControlSegments = new GetPathControlSegments();

                traversal.SimpleRouteSetupSolveAndSaveWorkflow(_context.NetworkCGCAuto, userPoints, _context);
                IFeature networkPath = traversal.network_path;
                
                // Get Metrics Features or Auto_TAZ Features contained within the 5-meter buffer
                getPathControlSegments.getCtrlFeatures(networkPath, _context, "Auto");
                // Filter getPathControlSegments results further for true segment traversal time
                List<IFeature> MetricsFeatures = getPathControlSegments.Control_FeaturesList;

                // DELIVERY                    
                bool bypass = false;
                string hwyID = "";
                double fromM = 0.0;
                double toM = 0.0;
                double nxtSegLen = 0.0;
                double nxtSegTime = 0.0;
                double totalSegLen = beginSegLen + endSegLen;
                double totalSegTime = beginSegTime + endSegTime;                                                    

                foreach (IFeature mFeatures in MetricsFeatures)
                {
                    bypass = false;
                    hwyID = mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_RIDField)).ToString();  // LINEARID
                    fromM = Math.Round(float.Parse(mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_FDFOField)).ToString()), 3);  // FROM_M
                    toM = Math.Round(float.Parse(mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_TDFOField)).ToString()), 3);  // TO_M
                    if (hwyID.Equals(first_rteid) && fromM <= beginMeasure && toM >= beginMeasure)
                        bypass = true;  // first segment attributes accounted for                                                 
                    else if (hwyID.Equals(last_rteid) && fromM <= endMeasure && toM >= endMeasure)
                    {
                        geometryCollection.AddGeometry((mFeatures.Shape as IMSegmentation3).GetSubcurveBetweenMs(fromM, endMeasure) as IGeometry);
                        bypass = true;  // last segment attributes accounted for
                    }                            
                    else if (inputTFrame.Equals("1:AllDay:Weekday"))
                        autoSpeedAvg = Math.Round(float.Parse(mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WDAllDayAASField)).ToString()), 0);
                    else if (inputTFrame.Equals("2:EarlyAM:Weekday"))
                        autoSpeedAvg = Math.Round(float.Parse(mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WDEarlyAMAASField)).ToString()), 0);
                    else if (inputTFrame.Equals("3:PeakAM:Weekday"))
                        autoSpeedAvg = Math.Round(float.Parse(mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WDPeakAMAASField)).ToString()), 0);
                    else if (inputTFrame.Equals("4:MidDay:Weekday"))
                        autoSpeedAvg = Math.Round(float.Parse(mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WDMidDayAASField)).ToString()), 0);
                    else if (inputTFrame.Equals("5:PeakPM:Weekday"))
                        autoSpeedAvg = Math.Round(float.Parse(mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WDPeakPMAASField)).ToString()), 0);
                    else if (inputTFrame.Equals("6:LatePM:Weekday"))
                        autoSpeedAvg = Math.Round(float.Parse(mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WDLatePMAASField)).ToString()), 0);
                    else if (inputTFrame.Equals("7:AllDay:Weekend"))
                        autoSpeedAvg = Math.Round(float.Parse(mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WEAllDayAASField)).ToString()), 0);
                    else if (inputTFrame.Equals("8:EarlyAM:Weekend"))
                        autoSpeedAvg = Math.Round(float.Parse(mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WEEarlyAMAASField)).ToString()), 0);
                    else if (inputTFrame.Equals("9:PeakAM:Weekend"))
                        autoSpeedAvg = Math.Round(float.Parse(mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WEPeakAMAASField)).ToString()), 0);
                    else if (inputTFrame.Equals("10:MidDay:Weekend"))
                        autoSpeedAvg = Math.Round(float.Parse(mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WEMidDayAASField)).ToString()), 0);
                    else if (inputTFrame.Equals("11:PeakPM:Weekend"))
                        autoSpeedAvg = Math.Round(float.Parse(mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WEPeakPMAASField)).ToString()), 0);
                    else if (inputTFrame.Equals("12:LatePM:Weekend"))
                        autoSpeedAvg = Math.Round(float.Parse(mFeatures.get_Value(mFeatures.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WELatePMAASField)).ToString()), 0);
                    else
                        autoSpeedAvg = 0.0;

                    if (bypass)
                        continue;
                    else
                    {
                        geometryCollection.AddGeometry((mFeatures.Shape as IMSegmentation3).GetSubcurveBetweenMs(fromM, toM) as IGeometry);

                        if (toM > fromM)
                        {
                            nxtSegLen = Math.Abs(toM - fromM);
                            if (nxtSegLen > 0.0)
                                nxtSegTime = (nxtSegLen / autoSpeedAvg) * 60;
                            else
                                nxtSegTime = 0.0;

                            totalSegLen = totalSegLen + nxtSegLen;
                            totalSegTime = totalSegTime + nxtSegTime;
                        }
                    }
                }               

                resultLRMData = calculateAutoResults(totalSegLen, totalSegTime, autoIncomeAvg, parkpenaltyMin, spatialRef);

                if (geometryCollection != null && geometryCollection.GeometryCount > 0)
                {
                    IPolyline6 polyline = geometryCollection as IPolyline6;
                    JsonObject jsonPolyline = new JsonObject();
                    jsonPolyline.AddObject("Total Miles", resultLRMData.TotalMiles);
                    jsonPolyline.AddObject("Total Minutes", resultLRMData.TotalMinutes);
                    jsonPolyline.AddObject("Total Dollars", resultLRMData.TotalDollars);
                    jsonPolyline.AddObject("Output Spatial Reference", resultLRMData.OutSpatialReference);
                    jsonPolyline.AddObject("Geometry", Conversion.ToJsonObject(polyline));
                    resultCollection.AddObject(i.ToString(), jsonPolyline);
                    i++;
                }

                return Encoding.UTF8.GetBytes(resultCollection.ToJson());
                               
            }
        }


        private List<IFeature> getMetricsLimitsFeatures(List<IFeature> limitsLRMFeatures, string dayTimeParam, string CtrlTable, bool firstFeature)
        {
            string road_id = "";
            double from_dfo = 0.0;
            double to_dfo = 0.0;
            short parkTimeMins = 0;
            List<IFeature> limitFeatures = new List<IFeature>();

            if (CtrlTable.Equals("Trip Data"))
            {
                foreach (IFeature lrmLimit in limitsLRMFeatures)
                {
                    road_id = lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_RIDField)).ToString();  // LINEARID
                    from_dfo = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_FDFOField)).ToString()), 3);  // FROM_M
                    to_dfo = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_TDFOField)).ToString()), 3);  // TO_M                    
                    if (dayTimeParam.Equals("1:AllDay:Weekday"))
                        autoSpeedAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WDAllDayAASField)).ToString()), 0);
                    else if (dayTimeParam.Equals("2:EarlyAM:Weekday"))
                        autoSpeedAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WDEarlyAMAASField)).ToString()), 0);
                    else if (dayTimeParam.Equals("3:PeakAM:Weekday"))
                        autoSpeedAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WDPeakAMAASField)).ToString()), 0);
                    else if (dayTimeParam.Equals("4:MidDay:Weekday"))
                        autoSpeedAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WDMidDayAASField)).ToString()), 0);
                    else if (dayTimeParam.Equals("5:PeakPM:Weekday"))
                        autoSpeedAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WDPeakPMAASField)).ToString()), 0);
                    else if (dayTimeParam.Equals("6:LatePM:Weekday"))
                        autoSpeedAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WDLatePMAASField)).ToString()), 0);
                    else if (dayTimeParam.Equals("7:AllDay:Weekend"))
                        autoSpeedAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WEAllDayAASField)).ToString()), 0);
                    else if (dayTimeParam.Equals("8:EarlyAM:Weekend"))
                        autoSpeedAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WEEarlyAMAASField)).ToString()), 0);
                    else if (dayTimeParam.Equals("9:PeakAM:Weekend"))
                        autoSpeedAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WEPeakAMAASField)).ToString()), 0);
                    else if (dayTimeParam.Equals("10:MidDay:Weekend"))
                        autoSpeedAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WEMidDayAASField)).ToString()), 0);
                    else if (dayTimeParam.Equals("11:PeakPM:Weekend"))
                        autoSpeedAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WEPeakPMAASField)).ToString()), 0);
                    else if (dayTimeParam.Equals("12:LatePM:Weekend"))
                        autoSpeedAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTripMetricFeatureClass_WELatePMAASField)).ToString()), 0);
                    else
                        autoSpeedAvg = 0.0;
                    if (to_dfo > from_dfo)
                    {
                        // Collect qualifying feature
                        limitFeatures.Add(lrmLimit);
                        if (firstFeature)
                        {
                            first_rteid = road_id;
                            first_bdfo = from_dfo;
                            first_edfo = to_dfo;
                        }
                        else
                        {
                            last_rteid = road_id;
                            last_bdfo = from_dfo;
                            last_edfo = to_dfo;
                        }

                        break;
                    }
                }
            }
            else if (CtrlTable.Equals("Traveler Data"))
            {
                foreach (IFeature lrmLimit in limitsLRMFeatures)
                {
                    road_id = lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_RIDField)).ToString();  // LINEARID
                    from_dfo = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_FDFOField)).ToString()), 3);  // FROM_M
                    to_dfo = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_TDFOField)).ToString()), 3);  // TO_M
                    if (dayTimeParam.Equals("1:AllDay:Weekday"))
                        autoIncomeAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_WDAllDayAAIField)).ToString()), 0);
                    else if (dayTimeParam.Equals("2:EarlyAM:Weekday"))
                        autoIncomeAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_WDEarlyAMAAIField)).ToString()), 0);
                    else if (dayTimeParam.Equals("3:PeakAM:Weekday"))
                        autoIncomeAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_WDPeakAMAAIField)).ToString()), 0);
                    else if (dayTimeParam.Equals("4:MidDay:Weekday"))
                        autoIncomeAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_WDMidDayAAIField)).ToString()), 0);
                    else if (dayTimeParam.Equals("5:PeakPM:Weekday"))
                        autoIncomeAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_WDPeakPMAAIField)).ToString()), 0);
                    else if (dayTimeParam.Equals("6:LatePM:Weekday"))
                        autoIncomeAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_WDLatePMAAIField)).ToString()), 0);
                    else if (dayTimeParam.Equals("7:AllDay:Weekend"))
                        autoIncomeAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_WEAllDayAAIField)).ToString()), 0);
                    else if (dayTimeParam.Equals("8:EarlyAM:Weekend"))
                        autoIncomeAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_WEEarlyAMAAIField)).ToString()), 0);
                    else if (dayTimeParam.Equals("9:PeakAM:Weekend"))
                        autoIncomeAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_WEPeakAMAAIField)).ToString()), 0);
                    else if (dayTimeParam.Equals("10:MidDay:Weekend"))
                        autoIncomeAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_WEMidDayAAIField)).ToString()), 0);
                    else if (dayTimeParam.Equals("11:PeakPM:Weekend"))
                        autoIncomeAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_WEPeakPMAAIField)).ToString()), 0);
                    else if (dayTimeParam.Equals("12:LatePM:Weekend"))
                        autoIncomeAvg = Math.Round(float.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsAutoTravMetricFeatureClass_WELatePMAAIField)).ToString()), 0);
                    else
                        autoIncomeAvg = 0.0;
                    if (to_dfo >= from_dfo)
                    {
                        // Collect qualifying feature
                        limitFeatures.Add(lrmLimit);
                        break;
                    }
                }
            }
            else // Parking Data
            {
                foreach (IFeature lrmLimit in limitsLRMFeatures)
                {
                    road_id = lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsTransferStreetsFeatureClass_RIDField)).ToString();  // LINEARID
                    parkTimeMins = short.Parse(lrmLimit.get_Value(lrmLimit.Fields.FindField(_context.LrsTransferStreetsFeatureClass_ParkPenaltyField)).ToString());
                    parkpenaltyMin = parkTimeMins;
                    limitFeatures.Add(lrmLimit);
                    break;
                }
            }

            return limitFeatures;
        }


        private AutoDataContract calculateAutoResults(double driveLength, double driveTime, double travIncome, short parkingTime, long srID)
        {
            double natlAvgFuelPerMile = 0.24;
            double estParkingFee = 1.25; // For work trips only
            double workTripValue = 0.0;
            double workTripTime = driveTime + parkingTime;

            // Authoritative constants are Minutes per Dollar -- NCRTPB 2020
            if (travIncome < 50001)
                workTripValue = workTripTime / 8.7;
            else if (travIncome < 100001)
                workTripValue = workTripTime / 2.9;
            else if (travIncome < 150001)
                workTripValue = workTripTime / 1.7;
            else
                workTripValue = workTripTime / 1.2;

            workTripValue = workTripValue + estParkingFee + (driveLength * natlAvgFuelPerMile);
            //Math.Round((double)(lrm_bmp + frm_dfo_diff), 3)
            AutoDataContract autoData = new AutoDataContract()
            {
                TotalMiles = Math.Round((double)driveLength, 2),
                TotalMinutes = Math.Round((double)driveTime, 2),
                TotalDollars = Math.Round((double)workTripValue, 2),
                OutSpatialReference = srID
            };

            return autoData;
        }


        private double identifyMValue(IPoint point, IFeature ctrlSectFeature)
        {
            using (ComReleaser comReleaser = new ComReleaser())
            {
                IPoint snapPoint = new PointClass();
                double dAlong = 0;
                double dFrom = 0;
                bool bRight = false;
                (ctrlSectFeature.Shape as IPolyline6).QueryPointAndDistance(esriSegmentExtension.esriNoExtension, point, true, snapPoint, ref dAlong, ref dFrom, ref bRight);
                double measureValue = (snapPoint.M * 0.000621);  // Convert meters to miles
                return measureValue;
            }
        }

        private List<IFeature> findClosestFeatures(IPoint point, string eventType)
        {
            List<IFeature> closestTFeatures = new List<IFeature>();
            List<IFeature> countFeatures = new List<IFeature>();
            using (ComReleaser comReleaser = new ComReleaser())
            {
                IEnvelope searchEnvelope = point.Envelope;
                searchEnvelope.Expand(_context.SearchTolerance, _context.SearchTolerance, false);

                ISpatialFilter spatialFilter = new SpatialFilter();
                spatialFilter.Geometry = searchEnvelope;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                IFeatureCursor featureCursor = null;
                comReleaser.ManageLifetime(spatialFilter);

                if (eventType.Equals("Trip Data"))
                    featureCursor = _context.LrsAutoTripMetricFeatureClass.Search(spatialFilter, false);
                else if (eventType.Equals("Traveler Data"))
                    featureCursor = _context.LrsAutoTravMetricFeatureClass.Search(spatialFilter, false);
                else  // "Parking Data"
                    featureCursor = _context.LrsTransferStreetsFeatureClass.Search(spatialFilter, false);                    
                
                comReleaser.ManageLifetime(featureCursor);

                IFeature feature;
                IFeature closestFeature = null;
                // double closestDistance = 0.00015;  //build88 map units are in decimal degrees, so 0.00015 is about 54.75 feet
                double closestDistance = double.MaxValue;

                while ((feature = featureCursor.NextFeature()) != null)
                {
                    if (feature.Shape == null || feature.Shape.IsEmpty)
                    {
                        continue;
                    }

                    countFeatures.Add(feature);
                }

                foreach (var hwyFeature in countFeatures)
                {
                    //Find the distance from input point to this feature
                    IProximityOperator proximityOperator = hwyFeature.Shape as IProximityOperator;
                    double distance = proximityOperator.ReturnDistance(point);  // ReturnNearestPoint(point) provides nearest point on the geometry, to the input point
                    if (distance < closestDistance)
                    {
                        if (countFeatures.Count < 2)
                        {
                            closestTFeatures.Add(hwyFeature);
                        }
                        else
                        {
                            closestDistance = distance;
                            closestFeature = hwyFeature;
                            closestTFeatures.Clear();
                            closestTFeatures.Add(closestFeature);
                        }
                    }
                }

                return closestTFeatures;

            }
        }
    }
}