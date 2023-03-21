using System.Collections.Specialized;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.SOESupport;
using System.Collections.Generic;

namespace LRSLocator
{
    public class RESTContext
    {
        // REST request
        public NameValueCollection BoundVariables;
        public JsonObject OperationInput; // operations only
        public string OutputFormat;
        public string RequestProperties; // in JSON format

        // REST response
        public string ResponseProperties;

        // SOE properties
        public ServerLogger Logger;
        public IMapServer3 MapServer;

        // Auto Layer Properties
        public IFeatureClass HighwayFeatureClass;
        public IFeatureClass LrsTransferStreetsFeatureClass;
        public IFeatureClass LrsAutoTripMetricFeatureClass;
        public IFeatureClass LrsAutoTravMetricFeatureClass;
        // Transit Layer Properties
        public IFeatureClass RailwayFeatureClass;
        public IFeatureClass RailStationFeatureClass;
        public IFeatureClass LrsRailTripMetricFeatureClass;
        public IFeatureClass LrsRailTravMetricFeatureClass;
        public IFeatureClass BuslineFeatureClass;
        public IFeatureClass BusStopFeatureClass;
        public IFeatureClass TransferStationFeatureClass;
        public IFeatureClass LrsBusTripMetricFeatureClass;
        public IFeatureClass LrsBusTravMetricFeatureClass;
        public IFeatureClass WalkwayFeatureClass;
        public IFeatureClass LrsWalkTripMetricFeatureClass;
        public IFeatureClass LrsWalkTravMetricFeatureClass;
        // Auto Field Properties
        public string HighwayFeatureClass_RIDField;
        public string HighwayFeatureClass_FDFOField;
        public string HighwayFeatureClass_TDFOField;
        public string LrsTransferStreetsFeatureClass_RIDField;
        public string LrsTransferStreetsFeatureClass_ParkPenaltyField;
        public string LrsAutoTripMetricFeatureClass_RIDField;
        public string LrsAutoTripMetricFeatureClass_FDFOField;
        public string LrsAutoTripMetricFeatureClass_TDFOField;
        public string LrsAutoTripMetricFeatureClass_WDAllDayAASField;
        public string LrsAutoTripMetricFeatureClass_WDEarlyAMAASField;
        public string LrsAutoTripMetricFeatureClass_WDPeakAMAASField;
        public string LrsAutoTripMetricFeatureClass_WDMidDayAASField;
        public string LrsAutoTripMetricFeatureClass_WDPeakPMAASField;
        public string LrsAutoTripMetricFeatureClass_WDLatePMAASField;
        public string LrsAutoTripMetricFeatureClass_WEAllDayAASField;
        public string LrsAutoTripMetricFeatureClass_WEEarlyAMAASField;
        public string LrsAutoTripMetricFeatureClass_WEPeakAMAASField;
        public string LrsAutoTripMetricFeatureClass_WEMidDayAASField;
        public string LrsAutoTripMetricFeatureClass_WEPeakPMAASField;
        public string LrsAutoTripMetricFeatureClass_WELatePMAASField;
        public string LrsAutoTravMetricFeatureClass_RIDField;
        public string LrsAutoTravMetricFeatureClass_FDFOField;
        public string LrsAutoTravMetricFeatureClass_TDFOField;
        public string LrsAutoTravMetricFeatureClass_WDAllDayAAIField;
        public string LrsAutoTravMetricFeatureClass_WDEarlyAMAAIField;
        public string LrsAutoTravMetricFeatureClass_WDPeakAMAAIField;
        public string LrsAutoTravMetricFeatureClass_WDMidDayAAIField;
        public string LrsAutoTravMetricFeatureClass_WDPeakPMAAIField;
        public string LrsAutoTravMetricFeatureClass_WDLatePMAAIField;
        public string LrsAutoTravMetricFeatureClass_WEAllDayAAIField;
        public string LrsAutoTravMetricFeatureClass_WEEarlyAMAAIField;
        public string LrsAutoTravMetricFeatureClass_WEPeakAMAAIField;
        public string LrsAutoTravMetricFeatureClass_WEMidDayAAIField;
        public string LrsAutoTravMetricFeatureClass_WEPeakPMAAIField;
        public string LrsAutoTravMetricFeatureClass_WELatePMAAIField;
        // Transit Field Properties
        public string RailwayFeatureClass_RIDField;
        public string RailwayFeatureClass_FDFOField;
        public string RailwayFeatureClass_TDFOField;
        public string RailStationFeatureClass_GeoID;
        public string RailStationFeatureClass_RIDField;
        public string RailStationFeatureClass_WDAllDayWaitField;
        public string RailStationFeatureClass_WDEarlyAMWaitField;
        public string RailStationFeatureClass_WDPeakAMWaitField;
        public string RailStationFeatureClass_WDMidDayWaitField;
        public string RailStationFeatureClass_WDPeakPMWaitField;
        public string RailStationFeatureClass_WDLatePMWaitField;
        public string RailStationFeatureClass_WEAllDayWaitField;
        public string RailStationFeatureClass_WEEarlyAMWaitField;
        public string RailStationFeatureClass_WEPeakAMWaitField;
        public string RailStationFeatureClass_WEMidDayWaitField;
        public string RailStationFeatureClass_WEPeakPMWaitField;
        public string RailStationFeatureClass_WELatePMWaitField;
        public string LrsRailTripMetricFeatureClass_MetricKey;
        public string LrsRailTripMetricFeatureClass_RIDField;
        public string LrsRailTripMetricFeatureClass_FDFOField;
        public string LrsRailTripMetricFeatureClass_TDFOField;
        public string LrsRailTripMetricFeatureClass_WDAllDayAASField;
        public string LrsRailTripMetricFeatureClass_WDEarlyAMAASField;
        public string LrsRailTripMetricFeatureClass_WDPeakAMAASField;
        public string LrsRailTripMetricFeatureClass_WDMidDayAASField;
        public string LrsRailTripMetricFeatureClass_WDPeakPMAASField;
        public string LrsRailTripMetricFeatureClass_WDLatePMAASField;
        public string LrsRailTripMetricFeatureClass_WEAllDayAASField;
        public string LrsRailTripMetricFeatureClass_WEEarlyAMAASField;
        public string LrsRailTripMetricFeatureClass_WEPeakAMAASField;
        public string LrsRailTripMetricFeatureClass_WEMidDayAASField;
        public string LrsRailTripMetricFeatureClass_WEPeakPMAASField;
        public string LrsRailTripMetricFeatureClass_WELatePMAASField;
        public string LrsRailTravMetricFeatureClass_RIDField;
        public string LrsRailTravMetricFeatureClass_FDFOField;
        public string LrsRailTravMetricFeatureClass_TDFOField;
        public string LrsRailTravMetricFeatureClass_WDAllDayAAIField;
        public string LrsRailTravMetricFeatureClass_WDEarlyAMAAIField;
        public string LrsRailTravMetricFeatureClass_WDPeakAMAAIField;
        public string LrsRailTravMetricFeatureClass_WDMidDayAAIField;
        public string LrsRailTravMetricFeatureClass_WDPeakPMAAIField;
        public string LrsRailTravMetricFeatureClass_WDLatePMAAIField;
        public string LrsRailTravMetricFeatureClass_WEAllDayAAIField;
        public string LrsRailTravMetricFeatureClass_WEEarlyAMAAIField;
        public string LrsRailTravMetricFeatureClass_WEPeakAMAAIField;
        public string LrsRailTravMetricFeatureClass_WEMidDayAAIField;
        public string LrsRailTravMetricFeatureClass_WEPeakPMAAIField;
        public string LrsRailTravMetricFeatureClass_WELatePMAAIField;
        public string BuslineFeatureClass_RIDField;
        public string BuslineFeatureClass_FDFOField;
        public string BuslineFeatureClass_TDFOField;
        public string BuslineFeatureClass_WDAllDayWaitField;
        public string BuslineFeatureClass_WDEarlyAMWaitField;
        public string BuslineFeatureClass_WDPeakAMWaitField;
        public string BuslineFeatureClass_WDMidDayWaitField;
        public string BuslineFeatureClass_WDPeakPMWaitField;
        public string BuslineFeatureClass_WDLatePMWaitField;
        public string BuslineFeatureClass_WEAllDayWaitField;
        public string BuslineFeatureClass_WEEarlyAMWaitField;
        public string BuslineFeatureClass_WEPeakAMWaitField;
        public string BuslineFeatureClass_WEMidDayWaitField;
        public string BuslineFeatureClass_WEPeakPMWaitField;
        public string BuslineFeatureClass_WELatePMWaitField;
        public string BusStopFeatureClass_GeoID;
        public string BusStopFeatureClass_RIDField;
        public string TransferStationFeatureClass_RIDField;
        public string TransferStationFeatureClass_WDAllDayWaitField;
        public string TransferStationFeatureClass_WDEarlyAMWaitField;
        public string TransferStationFeatureClass_WDPeakAMWaitField;
        public string TransferStationFeatureClass_WDMidDayWaitField;
        public string TransferStationFeatureClass_WDPeakPMWaitField;
        public string TransferStationFeatureClass_WDLatePMWaitField;
        public string TransferStationFeatureClass_WEAllDayWaitField;
        public string TransferStationFeatureClass_WEEarlyAMWaitField;
        public string TransferStationFeatureClass_WEPeakAMWaitField;
        public string TransferStationFeatureClass_WEMidDayWaitField;
        public string TransferStationFeatureClass_WEPeakPMWaitField;
        public string TransferStationFeatureClass_WELatePMWaitField;
        public string LrsBusTripMetricFeatureClass_MetricKey;
        public string LrsBusTripMetricFeatureClass_RIDField;
        public string LrsBusTripMetricFeatureClass_FDFOField;
        public string LrsBusTripMetricFeatureClass_TDFOField;
        public string LrsBusTripMetricFeatureClass_WDAllDayAASField;
        public string LrsBusTripMetricFeatureClass_WDEarlyAMAASField;
        public string LrsBusTripMetricFeatureClass_WDPeakAMAASField;
        public string LrsBusTripMetricFeatureClass_WDMidDayAASField;
        public string LrsBusTripMetricFeatureClass_WDPeakPMAASField;
        public string LrsBusTripMetricFeatureClass_WDLatePMAASField;
        public string LrsBusTripMetricFeatureClass_WEAllDayAASField;
        public string LrsBusTripMetricFeatureClass_WEEarlyAMAASField;
        public string LrsBusTripMetricFeatureClass_WEPeakAMAASField;
        public string LrsBusTripMetricFeatureClass_WEMidDayAASField;
        public string LrsBusTripMetricFeatureClass_WEPeakPMAASField;
        public string LrsBusTripMetricFeatureClass_WELatePMAASField;
        public string LrsBusTravMetricFeatureClass_RIDField;
        public string LrsBusTravMetricFeatureClass_FDFOField;
        public string LrsBusTravMetricFeatureClass_TDFOField;
        public string LrsBusTravMetricFeatureClass_WDAllDayAAIField;
        public string LrsBusTravMetricFeatureClass_WDEarlyAMAAIField;
        public string LrsBusTravMetricFeatureClass_WDPeakAMAAIField;
        public string LrsBusTravMetricFeatureClass_WDMidDayAAIField;
        public string LrsBusTravMetricFeatureClass_WDPeakPMAAIField;
        public string LrsBusTravMetricFeatureClass_WDLatePMAAIField;
        public string LrsBusTravMetricFeatureClass_WEAllDayAAIField;
        public string LrsBusTravMetricFeatureClass_WEEarlyAMAAIField;
        public string LrsBusTravMetricFeatureClass_WEPeakAMAAIField;
        public string LrsBusTravMetricFeatureClass_WEMidDayAAIField;
        public string LrsBusTravMetricFeatureClass_WEPeakPMAAIField;
        public string LrsBusTravMetricFeatureClass_WELatePMAAIField;
        public string WalkwayFeatureClass_RIDField;
        public string WalkwayFeatureClass_FDFOField;
        public string WalkwayFeatureClass_TDFOField;
        public string LrsWalkTripMetricFeatureClass_RIDField;
        public string LrsWalkTripMetricFeatureClass_FDFOField;
        public string LrsWalkTripMetricFeatureClass_TDFOField;
        public string LrsWalkTripMetricFeatureClass_WDAllDayAASField;
        public string LrsWalkTripMetricFeatureClass_WDEarlyAMAASField;
        public string LrsWalkTripMetricFeatureClass_WDPeakAMAASField;
        public string LrsWalkTripMetricFeatureClass_WDMidDayAASField;
        public string LrsWalkTripMetricFeatureClass_WDPeakPMAASField;
        public string LrsWalkTripMetricFeatureClass_WDLatePMAASField;
        public string LrsWalkTripMetricFeatureClass_WEAllDayAASField;
        public string LrsWalkTripMetricFeatureClass_WEEarlyAMAASField;
        public string LrsWalkTripMetricFeatureClass_WEPeakAMAASField;
        public string LrsWalkTripMetricFeatureClass_WEMidDayAASField;
        public string LrsWalkTripMetricFeatureClass_WEPeakPMAASField;
        public string LrsWalkTripMetricFeatureClass_WELatePMAASField;
        public string LrsWalkTravMetricFeatureClass_RIDField;
        public string LrsWalkTravMetricFeatureClass_FDFOField;
        public string LrsWalkTravMetricFeatureClass_TDFOField;
        public string LrsWalkTravMetricFeatureClass_WDAllDayAAIField;
        public string LrsWalkTravMetricFeatureClass_WDEarlyAMAAIField;
        public string LrsWalkTravMetricFeatureClass_WDPeakAMAAIField;
        public string LrsWalkTravMetricFeatureClass_WDMidDayAAIField;
        public string LrsWalkTravMetricFeatureClass_WDPeakPMAAIField;
        public string LrsWalkTravMetricFeatureClass_WDLatePMAAIField;
        public string LrsWalkTravMetricFeatureClass_WEAllDayAAIField;
        public string LrsWalkTravMetricFeatureClass_WEEarlyAMAAIField;
        public string LrsWalkTravMetricFeatureClass_WEPeakAMAAIField;
        public string LrsWalkTravMetricFeatureClass_WEMidDayAAIField;
        public string LrsWalkTravMetricFeatureClass_WEPeakPMAAIField;
        public string LrsWalkTravMetricFeatureClass_WELatePMAAIField;
        // Relationship Class Fields
        public string RailStopToTripLnRelationshipClass_GeoID;
        public string RailStopToTripLnRelationshipClass_MetricKey;
        public string BusStopToTripLnRelationshipClass_GeoID;
        public string BusStopToTripLnRelationshipClass_MetricKey;
        
        public double SearchTolerance;
        public IFeatureClass NARoutes;

        //--- Auto Network Dataset Elements ---------------
        public IFeatureClass lrsAutoNetworkPath;
        public IFeatureClass NetworkCGCAutoJunctions;        
        public INetworkDataset NetworkCGCAuto;
        public string NetworkCGC_AutoNDName;
        // public System.Collections.Generic.List<ESRI.ArcGIS.Geometry.IPoint> stopAutoPNT;

        //--- Transit Network Dataset Elements -------------
        public IFeatureClass lrsTransitNetworkPath;
        public IFeatureClass NetworkCGCTransitJunctions;        
        public INetworkDataset NetworkCGCTransit;
        public string NetworkCGC_TransitNDName;

        //-- Relationship Classes --
        public IRelationshipClass RailStopToTripLnRelationshipClass;
        public IRelationshipClass BusStopToTripLnRelationshipClass;        
        
        //-- map server elements ------------------------
        public IFeatureWorkspace LrsFeatureWorkspace;
        public List<string> FtrsNames;
        public int NbrOfnonMfts;
        public int NbrFtrs;
        public int AllLayerCount;
        public int HasMcount;
        public List<string> ListHasMFtrs;
        public List<ITable> SaTables;
        public List<IRelationshipClass2> RelClasses;
        //-----------------------------------------------        

        public IMapLayerInfos MapLayerInfos
        {
            get { return MapServer.GetServerInfo(MapServer.DefaultMapName).MapLayerInfos; }
        }
    }
}
