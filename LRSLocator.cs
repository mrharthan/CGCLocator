using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections.Specialized;
using System.Runtime.InteropServices;

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Server;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.SOESupport;
using ESRI.ArcGIS.DataSourcesGDB;


// Auto and Transit - Commute GeoCalculator

namespace LRSLocator
{
    [ComVisible(true)]
    [Guid("3F3E38DE-3351-4807-B1DB-9897B42DEA90")]  //Alternate V1: [Guid("3F3E38DE-3351-4807-B1DB-9897B42DEA90")]  Original DW: [Guid("2cc38f21-b4bc-487e-8d03-7bcbd0a72845")]
    [ClassInterface(ClassInterfaceType.None)]
    [ServerObjectExtension("MapServer",//use "MapServer" if SOE extends a Map service and "ImageServer" if it extends an Image service.
        AllCapabilities = "",
        DefaultCapabilities = "",
        Description = "CGC LRS Locator",
        DisplayName = "LRSLocator",
        Properties = "",
        SupportsREST = true,
        SupportsSOAP = false)]
    public class LRSLocator : IServerObjectExtension, IObjectConstruct, IRESTRequestHandler
    {
        private string soe_name;

        private IPropertySet configProps;
        private IServerObjectHelper serverObjectHelper;
        private ServerLogger logger;
        private IRESTRequestHandler reqHandler;
        // Auto Field Names ------------------------------------------------------------
        private string _highwayFeatureClass_RIDFieldName = "LINEARID";
        private string _highwayFeatureClass_FDFOFieldName = "FROM_M";
        private string _highwayFeatureClass_TDFOFieldName = "TO_M";
        private string _lrsTransferStreets_RIDFieldName = "LINEARID";
        private string _lrsTransferStreets_ParkPenaltyFieldName = "Parking_Penalty_Mins";
        private string _lrsAutoTripMetrics_RIDFieldName = "LINEARID";
        private string _lrsAutoTripMetrics_FDFOFieldName = "FROM_M";
        private string _lrsAutoTripMetrics_TDFOFieldName = "TO_M";
        // private string _lrsAutoTripMetrics_ZoneName = "ZONE_NAME";
        private string _lrsAutoTripMetrics_AllDayAvgSpeed_WkDay = "WkDay_AllDay_AAS";
        private string _lrsAutoTripMetrics_EarlyAMAvgSpeed_WkDay = "WkDay_EarlyAM_AAS";
        private string _lrsAutoTripMetrics_PeakAMAvgSpeed_WkDay = "WkDay_PeakAM_AAS";
        private string _lrsAutoTripMetrics_MidDayAvgSpeed_WkDay = "WkDay_MidDay_AAS";
        private string _lrsAutoTripMetrics_PeakPMAvgSpeed_WkDay = "WkDay_PeakPM_AAS";
        private string _lrsAutoTripMetrics_LatePMAvgSpeed_WkDay = "WkDay_LatePM_AAS";
        private string _lrsAutoTripMetrics_AllDayAvgSpeed_WkEnd = "WkEnd_AllDay_AAS";
        private string _lrsAutoTripMetrics_EarlyAMAvgSpeed_WkEnd = "WkEnd_EarlyAM_AAS";
        private string _lrsAutoTripMetrics_PeakAMAvgSpeed_WkEnd = "WkEnd_PeakAM_AAS";
        private string _lrsAutoTripMetrics_MidDayAvgSpeed_WkEnd = "WkEnd_MidDay_AAS";
        private string _lrsAutoTripMetrics_PeakPMAvgSpeed_WkEnd = "WkEnd_PeakPM_AAS";
        private string _lrsAutoTripMetrics_LatePMAvgSpeed_WkEnd = "WkEnd_LatePM_AAS";
        private string _lrsAutoTravMetrics_RIDFieldName = "LINEARID";
        private string _lrsAutoTravMetrics_FDFOFieldName = "FROM_M";
        private string _lrsAutoTravMetrics_TDFOFieldName = "TO_M";
        // private string _lrsAutoTravMetrics_ZoneName = "ZONE_NAME";
        private string _lrsAutoTravMetrics_AllDayAvgInc_WkDay = "WkDay_AllDay_AAI";
        private string _lrsAutoTravMetrics_EarlyAMAvgInc_WkDay = "WkDay_EarlyAM_AAI";
        private string _lrsAutoTravMetrics_PeakAMAvgInc_WkDay = "WkDay_PeakAM_AAI";
        private string _lrsAutoTravMetrics_MidDayAvgInc_WkDay = "WkDay_MidDay_AAI";
        private string _lrsAutoTravMetrics_PeakPMAvgInc_WkDay = "WkDay_PeakPM_AAI";
        private string _lrsAutoTravMetrics_LatePMAvgInc_WkDay = "WkDay_LatePM_AAI";
        private string _lrsAutoTravMetrics_AllDayAvgInc_WkEnd = "WkEnd_AllDay_AAI";
        private string _lrsAutoTravMetrics_EarlyAMAvgInc_WkEnd = "WkEnd_EarlyAM_AAI";
        private string _lrsAutoTravMetrics_PeakAMAvgInc_WkEnd = "WkEnd_PeakAM_AAI";
        private string _lrsAutoTravMetrics_MidDayAvgInc_WkEnd = "WkEnd_MidDay_AAI";
        private string _lrsAutoTravMetrics_PeakPMAvgInc_WkEnd = "WkEnd_PeakPM_AAI";
        private string _lrsAutoTravMetrics_LatePMAvgInc_WkEnd = "WkEnd_LatePM_AAI";
        // --------------------------------------------------------------------------------
        // Transit Field Names ------------------------------------------------------------
        private string _railwayFeatureClass_RIDFieldName = "LINEARID";
        private string _railwayFeatureClass_FDFOFieldName = "FROM_M";
        private string _railwayFeatureClass_TDFOFieldName = "TO_M";
        private string _railstationFeatureClass_GeoID = "GeoID";
        private string _railstationFeatureClass_RIDFieldName = "Station_Name";
        private string _railstationFeatureClass_AllDayWait_WkDay = "Avg_AllDay_WaitTime_WD";
        private string _railstationFeatureClass_EarlyAMWait_WkDay = "Avg_EarlyAM_WaitTime_WD";
        private string _railstationFeatureClass_PeakAMWait_WkDay = "Avg_PeakAM_WaitTime_WD";
        private string _railstationFeatureClass_MidDayWait_WkDay = "Avg_MidDay_WaitTime_WD";
        private string _railstationFeatureClass_PeakPMWait_WkDay = "Avg_PeakPM_WaitTime_WD";
        private string _railstationFeatureClass_LatePMWait_WkDay = "Avg_LatePM_WaitTime_WD";
        private string _railstationFeatureClass_AllDayWait_WkEnd = "Avg_AllDay_WaitTime_WE";
        private string _railstationFeatureClass_EarlyAMWait_WkEnd = "Avg_EarlyAM_WaitTime_WE";
        private string _railstationFeatureClass_PeakAMWait_WkEnd = "Avg_PeakAM_WaitTime_WE";
        private string _railstationFeatureClass_MidDayWait_WkEnd = "Avg_MidDay_WaitTime_WE";
        private string _railstationFeatureClass_PeakPMWait_WkEnd = "Avg_PeakPM_WaitTime_WE";
        private string _railstationFeatureClass_LatePMWait_WkEnd = "Avg_LatePM_WaitTime_WE";
        private string _lrsRailTripMetrics_MetricKey = "MetricKey";
        private string _lrsRailTripMetrics_RIDFieldName = "LINEARID";
        private string _lrsRailTripMetrics_FDFOFieldName = "FROM_M";
        private string _lrsRailTripMetrics_TDFOFieldName = "TO_M";
        private string _lrsRailTripMetrics_AllDayAvgSpeed_WkDay = "WkDay_AllDay_AAS";
        private string _lrsRailTripMetrics_EarlyAMAvgSpeed_WkDay = "WkDay_EarlyAM_AAS";
        private string _lrsRailTripMetrics_PeakAMAvgSpeed_WkDay = "WkDay_PeakAM_AAS";
        private string _lrsRailTripMetrics_MidDayAvgSpeed_WkDay = "WkDay_MidDay_AAS";
        private string _lrsRailTripMetrics_PeakPMAvgSpeed_WkDay = "WkDay_PeakPM_AAS";
        private string _lrsRailTripMetrics_LatePMAvgSpeed_WkDay = "WkDay_LatePM_AAS";
        private string _lrsRailTripMetrics_AllDayAvgSpeed_WkEnd = "WkEnd_AllDay_AAS";
        private string _lrsRailTripMetrics_EarlyAMAvgSpeed_WkEnd = "WkEnd_EarlyAM_AAS";
        private string _lrsRailTripMetrics_PeakAMAvgSpeed_WkEnd = "WkEnd_PeakAM_AAS";
        private string _lrsRailTripMetrics_MidDayAvgSpeed_WkEnd = "WkEnd_MidDay_AAS";
        private string _lrsRailTripMetrics_PeakPMAvgSpeed_WkEnd = "WkEnd_PeakPM_AAS";
        private string _lrsRailTripMetrics_LatePMAvgSpeed_WkEnd = "WkEnd_LatePM_AAS";
        private string _lrsRailTravMetrics_RIDFieldName = "LINEARID";
        private string _lrsRailTravMetrics_FDFOFieldName = "FROM_M";
        private string _lrsRailTravMetrics_TDFOFieldName = "TO_M";
        private string _lrsRailTravMetrics_AllDayAvgInc_WkDay = "WkDay_AllDay_AAI";
        private string _lrsRailTravMetrics_EarlyAMAvgInc_WkDay = "WkDay_EarlyAM_AAI";
        private string _lrsRailTravMetrics_PeakAMAvgInc_WkDay = "WkDay_PeakAM_AAI";
        private string _lrsRailTravMetrics_MidDayAvgInc_WkDay = "WkDay_MidDay_AAI";
        private string _lrsRailTravMetrics_PeakPMAvgInc_WkDay = "WkDay_PeakPM_AAI";
        private string _lrsRailTravMetrics_LatePMAvgInc_WkDay = "WkDay_LatePM_AAI";
        private string _lrsRailTravMetrics_AllDayAvgInc_WkEnd = "WkEnd_AllDay_AAI";
        private string _lrsRailTravMetrics_EarlyAMAvgInc_WkEnd = "WkEnd_EarlyAM_AAI";
        private string _lrsRailTravMetrics_PeakAMAvgInc_WkEnd = "WkEnd_PeakAM_AAI";
        private string _lrsRailTravMetrics_MidDayAvgInc_WkEnd = "WkEnd_MidDay_AAI";
        private string _lrsRailTravMetrics_PeakPMAvgInc_WkEnd = "WkEnd_PeakPM_AAI";
        private string _lrsRailTravMetrics_LatePMAvgInc_WkEnd = "WkEnd_LatePM_AAI";
        private string _buslineFeatureClass_RIDFieldName = "BusRouteID1";
        private string _buslineFeatureClass_FDFOFieldName = "FROM_M";
        private string _buslineFeatureClass_TDFOFieldName = "TO_M";
        private string _buslineFeatureClass_AllDayWait_WkDay = "Avg_AllDay_WaitTime_WD";
        private string _buslineFeatureClass_EarlyAMWait_WkDay = "Avg_EarlyAM_WaitTime_WD";
        private string _buslineFeatureClass_PeakAMWait_WkDay = "Avg_PeakAM_WaitTime_WD";
        private string _buslineFeatureClass_MidDayWait_WkDay = "Avg_MidDay_WaitTime_WD";
        private string _buslineFeatureClass_PeakPMWait_WkDay = "Avg_PeakPM_WaitTime_WD";
        private string _buslineFeatureClass_LatePMWait_WkDay = "Avg_LatePM_WaitTime_WD";
        private string _buslineFeatureClass_AllDayWait_WkEnd = "Avg_AllDay_WaitTime_WE";
        private string _buslineFeatureClass_EarlyAMWait_WkEnd = "Avg_EarlyAM_WaitTime_WE";
        private string _buslineFeatureClass_PeakAMWait_WkEnd = "Avg_PeakAM_WaitTime_WE";
        private string _buslineFeatureClass_MidDayWait_WkEnd = "Avg_MidDay_WaitTime_WE";
        private string _buslineFeatureClass_PeakPMWait_WkEnd = "Avg_PeakPM_WaitTime_WE";
        private string _buslineFeatureClass_LatePMWait_WkEnd = "Avg_LatePM_WaitTime_WE";
        private string _busstopFeatureClass_GeoID = "GeoID";
        private string _busstopFeatureClass_RIDFieldName = "BusRouteID1";
        private string _transferstationFeatureClass_RIDFieldName = "BusRouteID1";
        private string _transferstationFeatureClass_AllDayWait_WkDay = "Avg_AllDay_WaitTime_WD";
        private string _transferstationFeatureClass_EarlyAMWait_WkDay = "Avg_EarlyAM_WaitTime_WD";
        private string _transferstationFeatureClass_PeakAMWait_WkDay = "Avg_PeakAM_WaitTime_WD";
        private string _transferstationFeatureClass_MidDayWait_WkDay = "Avg_MidDay_WaitTime_WD";
        private string _transferstationFeatureClass_PeakPMWait_WkDay = "Avg_PeakPM_WaitTime_WD";
        private string _transferstationFeatureClass_LatePMWait_WkDay = "Avg_LatePM_WaitTime_WD";
        private string _transferstationFeatureClass_AllDayWait_WkEnd = "Avg_AllDay_WaitTime_WE";
        private string _transferstationFeatureClass_EarlyAMWait_WkEnd = "Avg_EarlyAM_WaitTime_WE";
        private string _transferstationFeatureClass_PeakAMWait_WkEnd = "Avg_PeakAM_WaitTime_WE";
        private string _transferstationFeatureClass_MidDayWait_WkEnd = "Avg_MidDay_WaitTime_WE";
        private string _transferstationFeatureClass_PeakPMWait_WkEnd = "Avg_PeakPM_WaitTime_WE";
        private string _transferstationFeatureClass_LatePMWait_WkEnd = "Avg_LatePM_WaitTime_WE";
        private string _lrsBusTripMetrics_MetricKey = "MetricKey";
        private string _lrsBusTripMetrics_RIDFieldName = "BusRouteID1";
        private string _lrsBusTripMetrics_FDFOFieldName = "FROM_M";
        private string _lrsBusTripMetrics_TDFOFieldName = "TO_M";
        private string _lrsBusTripMetrics_AllDayAvgSpeed_WkDay = "WkDay_AllDay_AAS";
        private string _lrsBusTripMetrics_EarlyAMAvgSpeed_WkDay = "WkDay_EarlyAM_AAS";
        private string _lrsBusTripMetrics_PeakAMAvgSpeed_WkDay = "WkDay_PeakAM_AAS";
        private string _lrsBusTripMetrics_MidDayAvgSpeed_WkDay = "WkDay_MidDay_AAS";
        private string _lrsBusTripMetrics_PeakPMAvgSpeed_WkDay = "WkDay_PeakPM_AAS";
        private string _lrsBusTripMetrics_LatePMAvgSpeed_WkDay = "WkDay_LatePM_AAS";
        private string _lrsBusTripMetrics_AllDayAvgSpeed_WkEnd = "WkEnd_AllDay_AAS";
        private string _lrsBusTripMetrics_EarlyAMAvgSpeed_WkEnd = "WkEnd_EarlyAM_AAS";
        private string _lrsBusTripMetrics_PeakAMAvgSpeed_WkEnd = "WkEnd_PeakAM_AAS";
        private string _lrsBusTripMetrics_MidDayAvgSpeed_WkEnd = "WkEnd_MidDay_AAS";
        private string _lrsBusTripMetrics_PeakPMAvgSpeed_WkEnd = "WkEnd_PeakPM_AAS";
        private string _lrsBusTripMetrics_LatePMAvgSpeed_WkEnd = "WkEnd_LatePM_AAS";
        private string _lrsBusTravMetrics_RIDFieldName = "BusRouteID1";
        private string _lrsBusTravMetrics_FDFOFieldName = "FROM_M";
        private string _lrsBusTravMetrics_TDFOFieldName = "TO_M";
        private string _lrsBusTravMetrics_AllDayAvgInc_WkDay = "WkDay_AllDay_AAI";
        private string _lrsBusTravMetrics_EarlyAMAvgInc_WkDay = "WkDay_EarlyAM_AAI";
        private string _lrsBusTravMetrics_PeakAMAvgInc_WkDay = "WkDay_PeakAM_AAI";
        private string _lrsBusTravMetrics_MidDayAvgInc_WkDay = "WkDay_MidDay_AAI";
        private string _lrsBusTravMetrics_PeakPMAvgInc_WkDay = "WkDay_PeakPM_AAI";
        private string _lrsBusTravMetrics_LatePMAvgInc_WkDay = "WkDay_LatePM_AAI";
        private string _lrsBusTravMetrics_AllDayAvgInc_WkEnd = "WkEnd_AllDay_AAI";
        private string _lrsBusTravMetrics_EarlyAMAvgInc_WkEnd = "WkEnd_EarlyAM_AAI";
        private string _lrsBusTravMetrics_PeakAMAvgInc_WkEnd = "WkEnd_PeakAM_AAI";
        private string _lrsBusTravMetrics_MidDayAvgInc_WkEnd = "WkEnd_MidDay_AAI";
        private string _lrsBusTravMetrics_PeakPMAvgInc_WkEnd = "WkEnd_PeakPM_AAI";
        private string _lrsBusTravMetrics_LatePMAvgInc_WkEnd = "WkEnd_LatePM_AAI";
        private string _walkwayFeatureClass_RIDFieldName = "RouteID";
        private string _walkwayFeatureClass_FDFOFieldName = "FROM_M";
        private string _walkwayFeatureClass_TDFOFieldName = "TO_M";
        private string _lrsWalkTripMetrics_RIDFieldName = "RouteID";
        private string _lrsWalkTripMetrics_FDFOFieldName = "FROM_M";
        private string _lrsWalkTripMetrics_TDFOFieldName = "TO_M";
        private string _lrsWalkTripMetrics_AllDayAvgSpeed_WkDay = "WkDay_AllDay_AAS";
        private string _lrsWalkTripMetrics_EarlyAMAvgSpeed_WkDay = "WkDay_EarlyAM_AAS";
        private string _lrsWalkTripMetrics_PeakAMAvgSpeed_WkDay = "WkDay_PeakAM_AAS";
        private string _lrsWalkTripMetrics_MidDayAvgSpeed_WkDay = "WkDay_MidDay_AAS";
        private string _lrsWalkTripMetrics_PeakPMAvgSpeed_WkDay = "WkDay_PeakPM_AAS";
        private string _lrsWalkTripMetrics_LatePMAvgSpeed_WkDay = "WkDay_LatePM_AAS";
        private string _lrsWalkTripMetrics_AllDayAvgSpeed_WkEnd = "WkEnd_AllDay_AAS";
        private string _lrsWalkTripMetrics_EarlyAMAvgSpeed_WkEnd = "WkEnd_EarlyAM_AAS";
        private string _lrsWalkTripMetrics_PeakAMAvgSpeed_WkEnd = "WkEnd_PeakAM_AAS";
        private string _lrsWalkTripMetrics_MidDayAvgSpeed_WkEnd = "WkEnd_MidDay_AAS";
        private string _lrsWalkTripMetrics_PeakPMAvgSpeed_WkEnd = "WkEnd_PeakPM_AAS";
        private string _lrsWalkTripMetrics_LatePMAvgSpeed_WkEnd = "WkEnd_LatePM_AAS";
        private string _lrsWalkTravMetrics_RIDFieldName = "RouteID";
        private string _lrsWalkTravMetrics_FDFOFieldName = "FROM_M";
        private string _lrsWalkTravMetrics_TDFOFieldName = "TO_M";
        private string _lrsWalkTravMetrics_AllDayAvgInc_WkDay = "WkDay_AllDay_AAI";
        private string _lrsWalkTravMetrics_EarlyAMAvgInc_WkDay = "WkDay_EarlyAM_AAI";
        private string _lrsWalkTravMetrics_PeakAMAvgInc_WkDay = "WkDay_PeakAM_AAI";
        private string _lrsWalkTravMetrics_MidDayAvgInc_WkDay = "WkDay_MidDay_AAI";
        private string _lrsWalkTravMetrics_PeakPMAvgInc_WkDay = "WkDay_PeakPM_AAI";
        private string _lrsWalkTravMetrics_LatePMAvgInc_WkDay = "WkDay_LatePM_AAI";
        private string _lrsWalkTravMetrics_AllDayAvgInc_WkEnd = "WkEnd_AllDay_AAI";
        private string _lrsWalkTravMetrics_EarlyAMAvgInc_WkEnd = "WkEnd_EarlyAM_AAI";
        private string _lrsWalkTravMetrics_PeakAMAvgInc_WkEnd = "WkEnd_PeakAM_AAI";
        private string _lrsWalkTravMetrics_MidDayAvgInc_WkEnd = "WkEnd_MidDay_AAI";
        private string _lrsWalkTravMetrics_PeakPMAvgInc_WkEnd = "WkEnd_PeakPM_AAI";
        private string _lrsWalkTravMetrics_LatePMAvgInc_WkEnd = "WkEnd_LatePM_AAI";
        //-----------------------------------------------------------------------------
        //---- Relationship Class Field Names -----------------------------------------
        private string _busStopToTripLnRelationshipClass_GeoID = "GeoID";
        private string _busStopToTripLnRelationshipClass_MetricKey = "MetricKey";
        private string _railStopToTripLnRelationshipClass_GeoID = "GeoID";
        private string _railStopToTripLnRelationshipClass_MetricKey = "MetricKey";
        // --- Network Dataset Names ---------------------------------------------------
        private string _cgcAutoND_NetworkName = "AutoNetwork_ND";
        private string _cgcTransitND_NetworkName = "TransitNetwork_ND";
        private double _searchTolerance = 0.00015;  // version 2.0, this is approx. 55 feet        

        private IFeatureClass _naRoutes = null;

        //-- auto data model feature classes -----------------------------
        private IFeatureClass _highwayFeatureClass = null;
        private IFeatureClass _lrsAutoTripMetrics = null;
        private IFeatureClass _lrsAutoTravMetrics = null;
        private IFeatureClass _lrsTransferStreets = null;
        private IFeatureClass _lrsAutoNetworkPath = null;
        private IFeatureClass _cgcAutoNetJunctions = null;

        //-- transit data model feature classes -----------------------------
        private IFeatureClass _railwayFeatureClass = null;
        private IFeatureClass _railstationFeatureClass = null;
        private IFeatureClass _lrsRailTripMetrics = null;
        private IFeatureClass _lrsRailTravMetrics = null;
        private IFeatureClass _buslineFeatureClass = null;
        private IFeatureClass _busstopFeatureClass = null;
        private IFeatureClass _transferstationFeatureClass = null;
        private IFeatureClass _lrsBusTripMetrics = null;
        private IFeatureClass _lrsBusTravMetrics = null;
        private IFeatureClass _walkwayFeatureClass = null;
        private IFeatureClass _lrsWalkTripMetrics = null;
        private IFeatureClass _lrsWalkTravMetrics = null;
        private IFeatureClass _lrsTransitNetworkPath = null;
        private IFeatureClass _cgcTransitNetJunctions = null;

        //-- map server objects ------------------------------------------
        private IMapServer3 _mapserver = null;
        private IMapServerDataAccess _dataAccess;
        private IFeatureWorkspace _gdWorkspace = null;
        private INetworkDataset _cgcAutoND = null;
        private INetworkDataset _cgcTransitND = null;
        private IRelationshipClass _busStopToTripLnRelationshipClass = null;
        private IRelationshipClass _railStopToTripLnRelationshipClass = null;
        //----------------------------------------------------------------

        private List<string> _ftrsNames = null;
        private int _nbrFtrs = 0;
        private int _nbrOfnonMfts = 0;
        private int _allLayerCount = 0;
        private int _hasMcount = 0;
        private List<string> _listHasMFtrs = null;

        private int _saTblCount = 0;
        private List<string> _saTblNames = null;
        private IStandaloneTableInfos _tableInfos = null;
        private List<ITable> _saTables = null;
        private List<IRelationshipClass2> _relCls = null;
        //------------------------------------------------------------------

        public LRSLocator()
        {
            soe_name = this.GetType().Name;
            logger = new ServerLogger();
            reqHandler = new SoeRestImpl(soe_name, CreateRestSchema()) as IRESTRequestHandler;
        }

        #region IServerObjectExtension Members

        public void Init(IServerObjectHelper pSOH)
        {
            serverObjectHelper = pSOH;

            IMapServer3 mapServer = serverObjectHelper.ServerObject as IMapServer3;

            IMapServerObjects3 mapServerObjects = mapServer as IMapServerObjects3;

            IMapServerDataAccess dataAccess = (IMapServerDataAccess)mapServer;
            this._mapserver = mapServer;
            this._dataAccess = dataAccess;
            initiateFeatures(mapServer);
        }

        public void Shutdown()
        {
            soe_name = null;
            serverObjectHelper = null;
            logger = null;
        }

        #endregion

        #region IObjectConstruct Members

        public void Construct(IPropertySet props)
        {
            configProps = props;
        }

        #endregion

        #region IRESTRequestHandler Members

        public string GetSchema()
        {
            return reqHandler.GetSchema();
        }

        public byte[] HandleRESTRequest(string Capabilities, string resourceName, string operationName, string operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return reqHandler.HandleRESTRequest(Capabilities, resourceName, operationName, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        #endregion

        private RestResource CreateRestSchema()
        {
            RestResource rootRes = new RestResource(soe_name, false, RootResHandler);            

            RestOperation getAutoCostFromLatLongOperation = new RestOperation("Driving Cost From LatLong",
                              new string[] { "Begin_Longitude", "Begin_Latitude", "End_Longitude", "End_Latitude", "Time_Of_Day", "Spatial_Reference" },
                              new string[] { "json" },
                              HandleOp_getAutoCostFromLatLongHandlerOperation);

            RestOperation getTransitCostFromLatLongOperation = new RestOperation("Transit Cost From LatLong",
                              new string[] { "Begin_Longitude", "Begin_Latitude", "End_Longitude", "End_Latitude", "Time_Of_Day", "Spatial_Reference" },
                              new string[] { "json" },
                              HandleOp_getTransitCostFromLatLongHandlerOperation);

            rootRes.operations.Add(getAutoCostFromLatLongOperation);
            rootRes.operations.Add(getTransitCostFromLatLongOperation);

            return rootRes;
        }

        private byte[] HandleOp_getAutoCostFromLatLongHandlerOperation(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return HandleOperation(new GetAutoCostFromLatLongHandler(), boundVariables, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        private byte[] HandleOp_getTransitCostFromLatLongHandlerOperation(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return HandleOperation(new GetTransitCostFromLatLongHandler(), boundVariables, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private byte[] RootResHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = null;

            JsonObject result = new JsonObject();
            return Encoding.UTF8.GetBytes(result.ToJson());
        }        

        /// A generic internal handler for all REST operations.
        /// The REST operation delegate methods should call this method to benefit 
        /// from uniform request processing, response formatting, and exception handling.

        private byte[] HandleOperation(IRESTHandler handler,
                                       NameValueCollection boundVariables,
                                       JsonObject operationInput,
                                       string outputFormat,
                                       string requestProperties,
                                       out string responseProperties)
        {
            RESTContext context = CreateContext(boundVariables, operationInput, outputFormat, requestProperties);
            return HandleHelper(handler, context, out responseProperties);
        }

        /// A generic internal handler for all REST resources.
        /// The REST resource delegate methods should call this method to benefit 
        /// from uniform request processing, response formatting, and exception handling.

        private byte[] HandleResource(IRESTHandler handler,
                                      NameValueCollection boundVariables,
                                      string outputFormat,
                                      string requestProperties,
                                      out string responseProperties)
        {
            RESTContext context = CreateContext(boundVariables, null, outputFormat, requestProperties);
            return HandleHelper(handler, context, out responseProperties);
        }

        private RESTContext CreateContext(NameValueCollection boundVariables,
                                     JsonObject operationInput,
                                     string outputFormat,
                                     string requestProperties)
        {
            RESTContext context = new RESTContext();
            context.BoundVariables = boundVariables;
            context.OperationInput = operationInput;
            context.OutputFormat = outputFormat;
            context.RequestProperties = requestProperties;
            context.NARoutes = this._naRoutes;

            context.SearchTolerance = _searchTolerance;

            context.LrsFeatureWorkspace = this._gdWorkspace;

            //-------- Auto network analysis layer ---------------------------------------------------------------------
            context.NetworkCGCAuto = this._cgcAutoND;
            //-------- Auto network dataset (edges) --------------------------------------------------------------------
            context.NetworkCGC_AutoNDName = this._cgcAutoND_NetworkName;
            //---------- Auto network junctions ------------------------------------------------------------------------            
            context.NetworkCGCAutoJunctions = this._cgcAutoNetJunctions;
            //--------- Auto solved network path -----------------------------------------------------------------------
            context.lrsAutoNetworkPath = this._lrsAutoNetworkPath;
            //----------------------------------------------------------------------------------------------------------

            //-------- Transit network analysis layer -----------------------------------------------------------------
            context.NetworkCGCTransit = this._cgcTransitND;
            //-------- Transit network dataset (edges) ----------------------------------------------------------------
            context.NetworkCGC_TransitNDName = this._cgcTransitND_NetworkName;
            //---------- Transit network junctions --------------------------------------------------------------------            
            context.NetworkCGCTransitJunctions = this._cgcTransitNetJunctions;
            //--------- Transit solved network path --------------------------------------------------------------------
            context.lrsTransitNetworkPath = this._lrsTransitNetworkPath;
            //----------------------------------------------------------------------------------------------------------

            //-- Relationship Classes ----------------------------------------------------------------------------------
            context.BusStopToTripLnRelationshipClass = this._busStopToTripLnRelationshipClass;
            context.RailStopToTripLnRelationshipClass = this._railStopToTripLnRelationshipClass;

            // -- Auto Network Feature Classes
            context.HighwayFeatureClass = this._highwayFeatureClass;
            context.LrsAutoTripMetricFeatureClass = this._lrsAutoTripMetrics;
            context.LrsAutoTravMetricFeatureClass = this._lrsAutoTravMetrics;
            context.LrsTransferStreetsFeatureClass = this._lrsTransferStreets;            
            // -- Auto Network layer fields
            context.HighwayFeatureClass_RIDField = this._highwayFeatureClass_RIDFieldName;
            context.HighwayFeatureClass_FDFOField = this._highwayFeatureClass_FDFOFieldName;
            context.HighwayFeatureClass_TDFOField = this._highwayFeatureClass_TDFOFieldName;
            // -- Transfer Streets Parking Penalty fields
            context.LrsTransferStreetsFeatureClass_RIDField = this._lrsTransferStreets_RIDFieldName;
            context.LrsTransferStreetsFeatureClass_ParkPenaltyField = this._lrsTransferStreets_ParkPenaltyFieldName;
            // -- Auto Trip Speed fields
            context.LrsAutoTripMetricFeatureClass_RIDField = this._lrsAutoTripMetrics_RIDFieldName;
            context.LrsAutoTripMetricFeatureClass_FDFOField = this._lrsAutoTripMetrics_FDFOFieldName;
            context.LrsAutoTripMetricFeatureClass_TDFOField = this._lrsAutoTripMetrics_TDFOFieldName;
            context.LrsAutoTripMetricFeatureClass_WDAllDayAASField = this._lrsAutoTripMetrics_AllDayAvgSpeed_WkDay;
            context.LrsAutoTripMetricFeatureClass_WDEarlyAMAASField = this._lrsAutoTripMetrics_EarlyAMAvgSpeed_WkDay;
            context.LrsAutoTripMetricFeatureClass_WDPeakAMAASField = this._lrsAutoTripMetrics_PeakAMAvgSpeed_WkDay;
            context.LrsAutoTripMetricFeatureClass_WDMidDayAASField = this._lrsAutoTripMetrics_MidDayAvgSpeed_WkDay;
            context.LrsAutoTripMetricFeatureClass_WDPeakPMAASField = this._lrsAutoTripMetrics_PeakPMAvgSpeed_WkDay;
            context.LrsAutoTripMetricFeatureClass_WDLatePMAASField = this._lrsAutoTripMetrics_LatePMAvgSpeed_WkDay;
            context.LrsAutoTripMetricFeatureClass_WEAllDayAASField = this._lrsAutoTripMetrics_AllDayAvgSpeed_WkEnd;
            context.LrsAutoTripMetricFeatureClass_WEEarlyAMAASField = this._lrsAutoTripMetrics_EarlyAMAvgSpeed_WkEnd;
            context.LrsAutoTripMetricFeatureClass_WEPeakAMAASField = this._lrsAutoTripMetrics_PeakAMAvgSpeed_WkEnd;
            context.LrsAutoTripMetricFeatureClass_WEMidDayAASField = this._lrsAutoTripMetrics_MidDayAvgSpeed_WkEnd;
            context.LrsAutoTripMetricFeatureClass_WEPeakPMAASField = this._lrsAutoTripMetrics_PeakPMAvgSpeed_WkEnd;
            context.LrsAutoTripMetricFeatureClass_WELatePMAASField = this._lrsAutoTripMetrics_LatePMAvgSpeed_WkEnd;
            // -- Auto Traveller Income fields
            context.LrsAutoTravMetricFeatureClass_RIDField = this._lrsAutoTravMetrics_RIDFieldName;
            context.LrsAutoTravMetricFeatureClass_FDFOField = this._lrsAutoTravMetrics_FDFOFieldName;
            context.LrsAutoTravMetricFeatureClass_TDFOField = this._lrsAutoTravMetrics_TDFOFieldName;
            context.LrsAutoTravMetricFeatureClass_WDAllDayAAIField = this._lrsAutoTravMetrics_AllDayAvgInc_WkDay;
            context.LrsAutoTravMetricFeatureClass_WDEarlyAMAAIField = this._lrsAutoTravMetrics_EarlyAMAvgInc_WkDay;
            context.LrsAutoTravMetricFeatureClass_WDPeakAMAAIField = this._lrsAutoTravMetrics_PeakAMAvgInc_WkDay;
            context.LrsAutoTravMetricFeatureClass_WDMidDayAAIField = this._lrsAutoTravMetrics_MidDayAvgInc_WkDay;
            context.LrsAutoTravMetricFeatureClass_WDPeakPMAAIField = this._lrsAutoTravMetrics_PeakPMAvgInc_WkDay;
            context.LrsAutoTravMetricFeatureClass_WDLatePMAAIField = this._lrsAutoTravMetrics_LatePMAvgInc_WkDay;
            context.LrsAutoTravMetricFeatureClass_WEAllDayAAIField = this._lrsAutoTravMetrics_AllDayAvgInc_WkEnd;
            context.LrsAutoTravMetricFeatureClass_WEEarlyAMAAIField = this._lrsAutoTravMetrics_EarlyAMAvgInc_WkEnd;
            context.LrsAutoTravMetricFeatureClass_WEPeakAMAAIField = this._lrsAutoTravMetrics_PeakAMAvgInc_WkEnd;
            context.LrsAutoTravMetricFeatureClass_WEMidDayAAIField = this._lrsAutoTravMetrics_MidDayAvgInc_WkEnd;
            context.LrsAutoTravMetricFeatureClass_WEPeakPMAAIField = this._lrsAutoTravMetrics_PeakPMAvgInc_WkEnd;
            context.LrsAutoTravMetricFeatureClass_WELatePMAAIField = this._lrsAutoTravMetrics_LatePMAvgInc_WkEnd;
            // -- Transit Network Feature Classes
            context.RailwayFeatureClass = this._railwayFeatureClass;
            context.RailStationFeatureClass = this._railstationFeatureClass;
            context.LrsRailTripMetricFeatureClass = this._lrsRailTripMetrics;
            context.LrsRailTravMetricFeatureClass = this._lrsRailTravMetrics;
            context.BuslineFeatureClass = this._buslineFeatureClass;
            context.BusStopFeatureClass = this._busstopFeatureClass;
            context.TransferStationFeatureClass = this._transferstationFeatureClass;
            context.LrsBusTripMetricFeatureClass = this._lrsBusTripMetrics;
            context.LrsBusTravMetricFeatureClass = this._lrsBusTravMetrics;            
            context.WalkwayFeatureClass = this._walkwayFeatureClass;
            context.LrsWalkTripMetricFeatureClass = this._lrsWalkTripMetrics;
            context.LrsWalkTravMetricFeatureClass = this._lrsWalkTravMetrics;
            // -- Transit Network layer fields
            context.RailwayFeatureClass_RIDField = this._railwayFeatureClass_RIDFieldName;
            context.RailwayFeatureClass_FDFOField = this._railwayFeatureClass_FDFOFieldName;
            context.RailwayFeatureClass_TDFOField = this._railwayFeatureClass_TDFOFieldName;
            context.BuslineFeatureClass_RIDField = this._buslineFeatureClass_RIDFieldName;
            context.BuslineFeatureClass_FDFOField = this._buslineFeatureClass_FDFOFieldName;
            context.BuslineFeatureClass_TDFOField = this._buslineFeatureClass_TDFOFieldName;
            context.WalkwayFeatureClass_RIDField = this._walkwayFeatureClass_RIDFieldName;
            context.WalkwayFeatureClass_FDFOField = this._walkwayFeatureClass_FDFOFieldName;
            context.WalkwayFeatureClass_TDFOField = this._walkwayFeatureClass_TDFOFieldName;
            // -- Transit Transfer Wait Time fields            
            context.RailStationFeatureClass_WDAllDayWaitField = this._railstationFeatureClass_AllDayWait_WkDay;
            context.RailStationFeatureClass_WDEarlyAMWaitField = this._railstationFeatureClass_EarlyAMWait_WkDay;
            context.RailStationFeatureClass_WDPeakAMWaitField = this._railstationFeatureClass_PeakAMWait_WkDay;
            context.RailStationFeatureClass_WDMidDayWaitField = this._railstationFeatureClass_MidDayWait_WkDay;
            context.RailStationFeatureClass_WDPeakPMWaitField = this._railstationFeatureClass_PeakPMWait_WkDay;
            context.RailStationFeatureClass_WDLatePMWaitField = this._railstationFeatureClass_LatePMWait_WkDay;
            context.RailStationFeatureClass_WEAllDayWaitField = this._railstationFeatureClass_AllDayWait_WkEnd;
            context.RailStationFeatureClass_WEEarlyAMWaitField = this._railstationFeatureClass_EarlyAMWait_WkEnd;
            context.RailStationFeatureClass_WEPeakAMWaitField = this._railstationFeatureClass_PeakAMWait_WkEnd;
            context.RailStationFeatureClass_WEMidDayWaitField = this._railstationFeatureClass_MidDayWait_WkEnd;
            context.RailStationFeatureClass_WEPeakPMWaitField = this._railstationFeatureClass_PeakPMWait_WkEnd;
            context.RailStationFeatureClass_WELatePMWaitField = this._railstationFeatureClass_LatePMWait_WkEnd;
            context.BuslineFeatureClass_WDAllDayWaitField = this._buslineFeatureClass_AllDayWait_WkDay;
            context.BuslineFeatureClass_WDEarlyAMWaitField = this._buslineFeatureClass_EarlyAMWait_WkDay;
            context.BuslineFeatureClass_WDPeakAMWaitField = this._buslineFeatureClass_PeakAMWait_WkDay;
            context.BuslineFeatureClass_WDMidDayWaitField = this._buslineFeatureClass_MidDayWait_WkDay;
            context.BuslineFeatureClass_WDPeakPMWaitField = this._buslineFeatureClass_PeakPMWait_WkDay;
            context.BuslineFeatureClass_WDLatePMWaitField = this._buslineFeatureClass_LatePMWait_WkDay;
            context.BuslineFeatureClass_WEAllDayWaitField = this._buslineFeatureClass_AllDayWait_WkEnd;
            context.BuslineFeatureClass_WEEarlyAMWaitField = this._buslineFeatureClass_EarlyAMWait_WkEnd;
            context.BuslineFeatureClass_WEPeakAMWaitField = this._buslineFeatureClass_PeakAMWait_WkEnd;
            context.BuslineFeatureClass_WEMidDayWaitField = this._buslineFeatureClass_MidDayWait_WkEnd;
            context.BuslineFeatureClass_WEPeakPMWaitField = this._buslineFeatureClass_PeakPMWait_WkEnd;
            context.BuslineFeatureClass_WELatePMWaitField = this._buslineFeatureClass_LatePMWait_WkEnd;
            context.TransferStationFeatureClass_RIDField = this._transferstationFeatureClass_RIDFieldName;
            context.TransferStationFeatureClass_WDAllDayWaitField = this._transferstationFeatureClass_AllDayWait_WkDay;
            context.TransferStationFeatureClass_WDEarlyAMWaitField = this._transferstationFeatureClass_EarlyAMWait_WkDay;
            context.TransferStationFeatureClass_WDPeakAMWaitField = this._transferstationFeatureClass_PeakAMWait_WkDay;
            context.TransferStationFeatureClass_WDMidDayWaitField = this._transferstationFeatureClass_MidDayWait_WkDay;
            context.TransferStationFeatureClass_WDPeakPMWaitField = this._transferstationFeatureClass_PeakPMWait_WkDay;
            context.TransferStationFeatureClass_WDLatePMWaitField = this._transferstationFeatureClass_LatePMWait_WkDay;
            context.TransferStationFeatureClass_WEAllDayWaitField = this._transferstationFeatureClass_AllDayWait_WkEnd;
            context.TransferStationFeatureClass_WEEarlyAMWaitField = this._transferstationFeatureClass_EarlyAMWait_WkEnd;
            context.TransferStationFeatureClass_WEPeakAMWaitField = this._transferstationFeatureClass_PeakAMWait_WkEnd;
            context.TransferStationFeatureClass_WEMidDayWaitField = this._transferstationFeatureClass_MidDayWait_WkEnd;
            context.TransferStationFeatureClass_WEPeakPMWaitField = this._transferstationFeatureClass_PeakPMWait_WkEnd;
            context.TransferStationFeatureClass_WELatePMWaitField = this._transferstationFeatureClass_LatePMWait_WkEnd;
            // == Transit Trip Speed fields
            context.RailStationFeatureClass_GeoID = this._railstationFeatureClass_GeoID;
            context.RailStationFeatureClass_RIDField = this._railstationFeatureClass_RIDFieldName;
            context.LrsRailTripMetricFeatureClass_MetricKey = this._lrsRailTripMetrics_MetricKey;
            context.LrsRailTripMetricFeatureClass_RIDField = this._lrsRailTripMetrics_RIDFieldName;
            context.LrsRailTripMetricFeatureClass_FDFOField = this._lrsRailTripMetrics_FDFOFieldName;
            context.LrsRailTripMetricFeatureClass_TDFOField = this._lrsRailTripMetrics_TDFOFieldName;
            context.LrsRailTripMetricFeatureClass_WDAllDayAASField = this._lrsRailTripMetrics_AllDayAvgSpeed_WkDay;
            context.LrsRailTripMetricFeatureClass_WDEarlyAMAASField = this._lrsRailTripMetrics_EarlyAMAvgSpeed_WkDay;
            context.LrsRailTripMetricFeatureClass_WDPeakAMAASField = this._lrsRailTripMetrics_PeakAMAvgSpeed_WkDay;
            context.LrsRailTripMetricFeatureClass_WDMidDayAASField = this._lrsRailTripMetrics_MidDayAvgSpeed_WkDay;
            context.LrsRailTripMetricFeatureClass_WDPeakPMAASField = this._lrsRailTripMetrics_PeakPMAvgSpeed_WkDay;
            context.LrsRailTripMetricFeatureClass_WDLatePMAASField = this._lrsRailTripMetrics_LatePMAvgSpeed_WkDay;
            context.LrsRailTripMetricFeatureClass_WEAllDayAASField = this._lrsRailTripMetrics_AllDayAvgSpeed_WkEnd;
            context.LrsRailTripMetricFeatureClass_WEEarlyAMAASField = this._lrsRailTripMetrics_EarlyAMAvgSpeed_WkEnd;
            context.LrsRailTripMetricFeatureClass_WEPeakAMAASField = this._lrsRailTripMetrics_PeakAMAvgSpeed_WkEnd;
            context.LrsRailTripMetricFeatureClass_WEMidDayAASField = this._lrsRailTripMetrics_MidDayAvgSpeed_WkEnd;
            context.LrsRailTripMetricFeatureClass_WEPeakPMAASField = this._lrsRailTripMetrics_PeakPMAvgSpeed_WkEnd;
            context.LrsRailTripMetricFeatureClass_WELatePMAASField = this._lrsRailTripMetrics_LatePMAvgSpeed_WkEnd;
            context.BusStopFeatureClass_GeoID = this._busstopFeatureClass_GeoID;
            context.BusStopFeatureClass_RIDField = this._busstopFeatureClass_RIDFieldName;
            context.LrsBusTripMetricFeatureClass_MetricKey = this._lrsBusTripMetrics_MetricKey;
            context.LrsBusTripMetricFeatureClass_RIDField = this._lrsBusTripMetrics_RIDFieldName;
            context.LrsBusTripMetricFeatureClass_FDFOField = this._lrsBusTripMetrics_FDFOFieldName;
            context.LrsBusTripMetricFeatureClass_TDFOField = this._lrsBusTripMetrics_TDFOFieldName;
            context.LrsBusTripMetricFeatureClass_WDAllDayAASField = this._lrsBusTripMetrics_AllDayAvgSpeed_WkDay;
            context.LrsBusTripMetricFeatureClass_WDEarlyAMAASField = this._lrsBusTripMetrics_EarlyAMAvgSpeed_WkDay;
            context.LrsBusTripMetricFeatureClass_WDPeakAMAASField = this._lrsBusTripMetrics_PeakAMAvgSpeed_WkDay;
            context.LrsBusTripMetricFeatureClass_WDMidDayAASField = this._lrsBusTripMetrics_MidDayAvgSpeed_WkDay;
            context.LrsBusTripMetricFeatureClass_WDPeakPMAASField = this._lrsBusTripMetrics_PeakPMAvgSpeed_WkDay;
            context.LrsBusTripMetricFeatureClass_WDLatePMAASField = this._lrsBusTripMetrics_LatePMAvgSpeed_WkDay;
            context.LrsBusTripMetricFeatureClass_WEAllDayAASField = this._lrsBusTripMetrics_AllDayAvgSpeed_WkEnd;
            context.LrsBusTripMetricFeatureClass_WEEarlyAMAASField = this._lrsBusTripMetrics_EarlyAMAvgSpeed_WkEnd;
            context.LrsBusTripMetricFeatureClass_WEPeakAMAASField = this._lrsBusTripMetrics_PeakAMAvgSpeed_WkEnd;
            context.LrsBusTripMetricFeatureClass_WEMidDayAASField = this._lrsBusTripMetrics_MidDayAvgSpeed_WkEnd;
            context.LrsBusTripMetricFeatureClass_WEPeakPMAASField = this._lrsBusTripMetrics_PeakPMAvgSpeed_WkEnd;
            context.LrsBusTripMetricFeatureClass_WELatePMAASField = this._lrsBusTripMetrics_LatePMAvgSpeed_WkEnd;
            context.LrsWalkTripMetricFeatureClass_RIDField = this._lrsWalkTripMetrics_RIDFieldName;
            context.LrsWalkTripMetricFeatureClass_FDFOField = this._lrsWalkTripMetrics_FDFOFieldName;
            context.LrsWalkTripMetricFeatureClass_TDFOField = this._lrsWalkTripMetrics_TDFOFieldName;
            context.LrsWalkTripMetricFeatureClass_WDAllDayAASField = this._lrsWalkTripMetrics_AllDayAvgSpeed_WkDay;
            context.LrsWalkTripMetricFeatureClass_WDEarlyAMAASField = this._lrsWalkTripMetrics_EarlyAMAvgSpeed_WkDay;
            context.LrsWalkTripMetricFeatureClass_WDPeakAMAASField = this._lrsWalkTripMetrics_PeakAMAvgSpeed_WkDay;
            context.LrsWalkTripMetricFeatureClass_WDMidDayAASField = this._lrsWalkTripMetrics_MidDayAvgSpeed_WkDay;
            context.LrsWalkTripMetricFeatureClass_WDPeakPMAASField = this._lrsWalkTripMetrics_PeakPMAvgSpeed_WkDay;
            context.LrsWalkTripMetricFeatureClass_WDLatePMAASField = this._lrsWalkTripMetrics_LatePMAvgSpeed_WkDay;
            context.LrsWalkTripMetricFeatureClass_WEAllDayAASField = this._lrsWalkTripMetrics_AllDayAvgSpeed_WkEnd;
            context.LrsWalkTripMetricFeatureClass_WEEarlyAMAASField = this._lrsWalkTripMetrics_EarlyAMAvgSpeed_WkEnd;
            context.LrsWalkTripMetricFeatureClass_WEPeakAMAASField = this._lrsWalkTripMetrics_PeakAMAvgSpeed_WkEnd;
            context.LrsWalkTripMetricFeatureClass_WEMidDayAASField = this._lrsWalkTripMetrics_MidDayAvgSpeed_WkEnd;
            context.LrsWalkTripMetricFeatureClass_WEPeakPMAASField = this._lrsWalkTripMetrics_PeakPMAvgSpeed_WkEnd;
            context.LrsWalkTripMetricFeatureClass_WELatePMAASField = this._lrsWalkTripMetrics_LatePMAvgSpeed_WkEnd;
            // -- Trnasit Traveler Income fields
            context.LrsRailTravMetricFeatureClass_RIDField = this._lrsRailTravMetrics_RIDFieldName;
            context.LrsRailTravMetricFeatureClass_FDFOField = this._lrsRailTravMetrics_FDFOFieldName;
            context.LrsRailTravMetricFeatureClass_TDFOField = this._lrsRailTravMetrics_TDFOFieldName;
            context.LrsRailTravMetricFeatureClass_WDAllDayAAIField = this._lrsRailTravMetrics_AllDayAvgInc_WkDay;
            context.LrsRailTravMetricFeatureClass_WDEarlyAMAAIField = this._lrsRailTravMetrics_EarlyAMAvgInc_WkDay;
            context.LrsRailTravMetricFeatureClass_WDPeakAMAAIField = this._lrsRailTravMetrics_PeakAMAvgInc_WkDay;
            context.LrsRailTravMetricFeatureClass_WDMidDayAAIField = this._lrsRailTravMetrics_MidDayAvgInc_WkDay;
            context.LrsRailTravMetricFeatureClass_WDPeakPMAAIField = this._lrsRailTravMetrics_PeakPMAvgInc_WkDay;
            context.LrsRailTravMetricFeatureClass_WDLatePMAAIField = this._lrsRailTravMetrics_LatePMAvgInc_WkDay;
            context.LrsRailTravMetricFeatureClass_WEAllDayAAIField = this._lrsRailTravMetrics_AllDayAvgInc_WkEnd;
            context.LrsRailTravMetricFeatureClass_WEEarlyAMAAIField = this._lrsRailTravMetrics_EarlyAMAvgInc_WkEnd;
            context.LrsRailTravMetricFeatureClass_WEPeakAMAAIField = this._lrsRailTravMetrics_PeakAMAvgInc_WkEnd;
            context.LrsRailTravMetricFeatureClass_WEMidDayAAIField = this._lrsRailTravMetrics_MidDayAvgInc_WkEnd;
            context.LrsRailTravMetricFeatureClass_WEPeakPMAAIField = this._lrsRailTravMetrics_PeakPMAvgInc_WkEnd;
            context.LrsRailTravMetricFeatureClass_WELatePMAAIField = this._lrsRailTravMetrics_LatePMAvgInc_WkEnd;
            context.LrsBusTravMetricFeatureClass_RIDField = this._lrsBusTravMetrics_RIDFieldName;
            context.LrsBusTravMetricFeatureClass_FDFOField = this._lrsBusTravMetrics_FDFOFieldName;
            context.LrsBusTravMetricFeatureClass_TDFOField = this._lrsBusTravMetrics_TDFOFieldName;
            context.LrsBusTravMetricFeatureClass_WDAllDayAAIField = this._lrsBusTravMetrics_AllDayAvgInc_WkDay;
            context.LrsBusTravMetricFeatureClass_WDEarlyAMAAIField = this._lrsBusTravMetrics_EarlyAMAvgInc_WkDay;
            context.LrsBusTravMetricFeatureClass_WDPeakAMAAIField = this._lrsBusTravMetrics_PeakAMAvgInc_WkDay;
            context.LrsBusTravMetricFeatureClass_WDMidDayAAIField = this._lrsBusTravMetrics_MidDayAvgInc_WkDay;
            context.LrsBusTravMetricFeatureClass_WDPeakPMAAIField = this._lrsBusTravMetrics_PeakPMAvgInc_WkDay;
            context.LrsBusTravMetricFeatureClass_WDLatePMAAIField = this._lrsBusTravMetrics_LatePMAvgInc_WkDay;
            context.LrsBusTravMetricFeatureClass_WEAllDayAAIField = this._lrsBusTravMetrics_AllDayAvgInc_WkEnd;
            context.LrsBusTravMetricFeatureClass_WEEarlyAMAAIField = this._lrsBusTravMetrics_EarlyAMAvgInc_WkEnd;
            context.LrsBusTravMetricFeatureClass_WEPeakAMAAIField = this._lrsBusTravMetrics_PeakAMAvgInc_WkEnd;
            context.LrsBusTravMetricFeatureClass_WEMidDayAAIField = this._lrsBusTravMetrics_MidDayAvgInc_WkEnd;
            context.LrsBusTravMetricFeatureClass_WEPeakPMAAIField = this._lrsBusTravMetrics_PeakPMAvgInc_WkEnd;
            context.LrsBusTravMetricFeatureClass_WELatePMAAIField = this._lrsBusTravMetrics_LatePMAvgInc_WkEnd;
            context.LrsWalkTravMetricFeatureClass_RIDField = this._lrsWalkTravMetrics_RIDFieldName;
            context.LrsWalkTravMetricFeatureClass_FDFOField = this._lrsWalkTravMetrics_FDFOFieldName;
            context.LrsWalkTravMetricFeatureClass_TDFOField = this._lrsWalkTravMetrics_TDFOFieldName;
            context.LrsWalkTravMetricFeatureClass_WDAllDayAAIField = this._lrsWalkTravMetrics_AllDayAvgInc_WkDay;
            context.LrsWalkTravMetricFeatureClass_WDEarlyAMAAIField = this._lrsWalkTravMetrics_EarlyAMAvgInc_WkDay;
            context.LrsWalkTravMetricFeatureClass_WDPeakAMAAIField = this._lrsWalkTravMetrics_PeakAMAvgInc_WkDay;
            context.LrsWalkTravMetricFeatureClass_WDMidDayAAIField = this._lrsWalkTravMetrics_MidDayAvgInc_WkDay;
            context.LrsWalkTravMetricFeatureClass_WDPeakPMAAIField = this._lrsWalkTravMetrics_PeakPMAvgInc_WkDay;
            context.LrsWalkTravMetricFeatureClass_WDLatePMAAIField = this._lrsWalkTravMetrics_LatePMAvgInc_WkDay;
            context.LrsWalkTravMetricFeatureClass_WEAllDayAAIField = this._lrsWalkTravMetrics_AllDayAvgInc_WkEnd;
            context.LrsWalkTravMetricFeatureClass_WEEarlyAMAAIField = this._lrsWalkTravMetrics_EarlyAMAvgInc_WkEnd;
            context.LrsWalkTravMetricFeatureClass_WEPeakAMAAIField = this._lrsWalkTravMetrics_PeakAMAvgInc_WkEnd;
            context.LrsWalkTravMetricFeatureClass_WEMidDayAAIField = this._lrsWalkTravMetrics_MidDayAvgInc_WkEnd;
            context.LrsWalkTravMetricFeatureClass_WEPeakPMAAIField = this._lrsWalkTravMetrics_PeakPMAvgInc_WkEnd;
            context.LrsWalkTravMetricFeatureClass_WELatePMAAIField = this._lrsWalkTravMetrics_LatePMAvgInc_WkEnd;
            // -- Relationship Class Fields ---------------------------------------------------------------------
            context.RailStopToTripLnRelationshipClass_GeoID = this._railStopToTripLnRelationshipClass_GeoID;
            context.RailStopToTripLnRelationshipClass_MetricKey = this._railStopToTripLnRelationshipClass_MetricKey;
            context.BusStopToTripLnRelationshipClass_GeoID = this._busStopToTripLnRelationshipClass_GeoID;
            context.BusStopToTripLnRelationshipClass_MetricKey = this._busStopToTripLnRelationshipClass_MetricKey;            
            //---------------------------------------------------------------------------------------------------
            context.NbrOfnonMfts = this._nbrOfnonMfts;
            context.NbrFtrs = this._nbrFtrs;
            context.FtrsNames = this._ftrsNames;
            context.AllLayerCount = this._allLayerCount;
            context.HasMcount = this._hasMcount;
            context.ListHasMFtrs = this._listHasMFtrs;
            context.SaTables = this._saTables;
            context.RelClasses = this._relCls;

            context.MapServer = this._mapserver;
            //-----------------------------------------------------------------------------------------------------------
            return context;
        }

        /// A generic internal handler for all REST resources and operations.       
        private byte[] HandleHelper(IRESTHandler handler, RESTContext context, out string responseProperties)
        {
            object response = null;
            try
            {
                response = handler.HandleRequest(context);
                responseProperties = context.ResponseProperties;
            }
            catch (Exception e)
            {
                response = null; // JsonBuilder.BuildErrorObject(500, e.Message);
                responseProperties = null;
                Console.WriteLine(e.Message);
                // throw e;
            }
            return JSONHelper.EncodeResponse(response);
        }

        private void initiateFeatures(IMapServer3 mapServer)
        {
            IMapServerDataAccess dataAccess = (IMapServerDataAccess)mapServer;
            IMapServerInfo msInfo = mapServer.GetServerInfo(mapServer.DefaultMapName);

            IMapLayerInfos layerInfos = msInfo.MapLayerInfos;
            this._mapserver = mapServer;
            this._dataAccess = dataAccess;

            //--Test -------------------------------------------------

            int layerCount = layerInfos.Count;

            int featureLyrCount = 0;
            List<string> fcNames = new List<string>();
            int nonMFtrsCount = 0;
            int hasMCount = 0;
            int allLayersCount = layerCount;
            List<string> saTblNames = new List<string>();
            List<string> listOfMftrs = new List<string>();
            //----------------------------------------------------------------

            for (int j = 0; j < layerCount; j++)
            {
                IMapLayerInfo layerInfo = layerInfos.get_Element(j);

                if (layerInfo.IsFeatureLayer)
                {
                    featureLyrCount++;

                    IFeatureClass featureClass = (IFeatureClass)dataAccess.GetDataSource(mapServer.DefaultMapName, layerInfo.ID);
                    IGeometryDef geometryDef = featureClass.Fields.get_Field(featureClass.FindField(featureClass.ShapeFieldName)).GeometryDef;

                    fcNames.Add(featureClass.AliasName);

                    // if (featureClass != null && featureClass.ShapeType == ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolyline)
                    if (featureClass != null)
                    {
                        //IGeometryDef geometryDef = featureClass.Fields.get_Field(featureClass.FindField(featureClass.ShapeFieldName)).GeometryDef;
                        if (geometryDef.HasM)
                        {
                            hasMCount++;
                            listOfMftrs.Add(featureClass.AliasName);

                            if (featureClass.AliasName == "SANDBOX_STATIC.GIS.Auto_TAZ_Network")  //1
                            {
                                // Use this as Route FeatureClass
                                this._highwayFeatureClass = featureClass;
                            }
                            else if (featureClass.AliasName == "SANDBOX_STATIC.GIS.AutoTripMetrics") //2
                            {
                                this._lrsAutoTripMetrics = featureClass;
                            }
                            else if (featureClass.AliasName == "SANDBOX_STATIC.GIS.AutoTravMetrics") //3
                            {
                                this._lrsAutoTravMetrics = featureClass;
                            }
                            else if (featureClass.AliasName == "SANDBOX_STATIC.GIS.Rail_TAZ_Network") //4
                            {
                                this._railwayFeatureClass = featureClass;
                            }
                            else if (featureClass.AliasName == "SANDBOX_STATIC.GIS.RailTripMetrics") //5
                            {
                                this._lrsRailTripMetrics = featureClass;
                            }
                            else if (featureClass.AliasName == "SANDBOX_STATIC.GIS.RailTravellerMetrics") //6
                            {
                                this._lrsRailTravMetrics = featureClass;
                            }
                            else if (featureClass.AliasName == "SANDBOX_STATIC.GIS.Bus_TAZ_Network") //7
                            {
                                this._buslineFeatureClass = featureClass;
                            }
                            else if (featureClass.AliasName == "SANDBOX_STATIC.GIS.BusTripMetrics") //8
                            {
                                this._lrsBusTripMetrics = featureClass;
                            }
                            else if (featureClass.AliasName == "SANDBOX_STATIC.GIS.BusTravellerMetrics") //9
                            {
                                this._lrsBusTravMetrics = featureClass;
                            }
                            else if (featureClass.AliasName == "SANDBOX_STATIC.GIS.Ped_TAZ_Network") //10
                            {
                                this._walkwayFeatureClass = featureClass;
                            }
                            else if (featureClass.AliasName == "SANDBOX_STATIC.GIS.PedTripMetrics") //11
                            {
                                this._lrsWalkTripMetrics = featureClass;
                            }
                            else if (featureClass.AliasName == "SANDBOX_STATIC.GIS.PedTravellerMetrics") //12
                            {
                                this._lrsWalkTravMetrics = featureClass;
                            }
                            else
                            {
                                this._naRoutes = featureClass;

                            }
                            //System.Diagnostics.Debugger.Break();
                        }
                        else
                        {
                            if ((featureClass.AliasName != "Stops") || (featureClass.AliasName != "Barriers") || (featureClass.AliasName != "PolylineBarriers") || (featureClass.AliasName != "PolygonBarriers"))
                            {
                                if (featureClass.AliasName == "SANDBOX_STATIC.GIS.Transfer_Street_Stations") //13
                                {
                                    nonMFtrsCount++;
                                    this._lrsTransferStreets = featureClass;
                                }
                                if (featureClass.AliasName == "SANDBOX_STATIC.GIS.Rail_TAZ_Stations") //14
                                {
                                    nonMFtrsCount++;
                                    this._railstationFeatureClass = featureClass;
                                }
                                if (featureClass.AliasName == "SANDBOX_STATIC.GIS.Bus_TAZ_Stops") //15
                                {
                                    nonMFtrsCount++;
                                    this._busstopFeatureClass = featureClass;
                                }
                                if (featureClass.AliasName == "SANDBOX_STATIC.GIS.Transit_Transfer_Stations") //16
                                {
                                    nonMFtrsCount++;
                                    this._transferstationFeatureClass = featureClass;
                                }
                                else if (featureClass.AliasName == "SANDBOX_STATIC.GIS.AutoNetwork_ND_Junctions") //17
                                {
                                    nonMFtrsCount++;
                                    this._cgcAutoNetJunctions = featureClass;
                                }
                                else if (featureClass.AliasName == "SANDBOX_STATIC.GIS.TransitNetwork_ND_Junctions") //18
                                {
                                    nonMFtrsCount++;
                                    this._cgcTransitNetJunctions = featureClass;
                                }
                                else
                                {
                                    this._naRoutes = featureClass;
                                }

                            }

                        }


                    }// end if Feature


                }// 
                if (layerInfo.Name == "RouteAuto")
                {
                    INetworkDataset nwkAutoDataset = (INetworkDataset)dataAccess.GetDataSource(mapServer.DefaultMapName, layerInfo.ID);
                    this._cgcAutoND = nwkAutoDataset;
                }
                else if (layerInfo.Name == "RouteTransit")
                {
                    INetworkDataset nwkTransitDataset = (INetworkDataset)dataAccess.GetDataSource(mapServer.DefaultMapName, layerInfo.ID);
                    this._cgcTransitND = nwkTransitDataset;
                }

                this._nbrFtrs = featureLyrCount;
                this._ftrsNames = fcNames;
                this._nbrOfnonMfts = nonMFtrsCount;
                this._hasMcount = hasMCount;
                this._allLayerCount = allLayersCount;
                this._listHasMFtrs = listOfMftrs;

            }

            IMapServerObjects3 msObj = (IMapServerObjects3)mapServer;
            //get map server info
            IMapServerInfo3 msInfo3 = (IMapServerInfo3)msInfo;
            IStandaloneTableInfos tableInfos = msInfo3.StandaloneTableInfos;
            this._tableInfos = tableInfos;

            //get any standalone table info collection             
            List<ITable> saTables = new List<ITable>();
            List<IRelationshipClass2> relClasses = new List<IRelationshipClass2>();
            ITable table = null;
            IRelationshipClass2 relCl = null;
            if (tableInfos != null)
            {
                int tableCount = tableInfos.Count;
                int? tableID = null;
                for (int j = 0; j < tableCount; j++)
                {
                    IStandaloneTableInfo tableInfo = tableInfos.get_Element(j);
                    //tableInfo.Name = "";
                    saTblNames.Add(tableInfo.Name);
                    tableID = tableInfo.ID;

                    //table = (ITable)dataAccess.GetDataSource(mapServer.DefaultMapName, tableInfo.ID);
                    if (tableID != null)
                    {
                        table = msObj.get_StandaloneTable(mapServer.DefaultMapName, Convert.ToInt16(tableID));
                        saTables.Add(table);
                        relCl = (IRelationshipClass2)table;
                        relClasses.Add(relCl);
                    }

                }
            }
            this._saTables = saTables;
            int standAloneTablesCount = tableInfos.Count;
            this._saTblCount = tableInfos.Count; ;
            this._saTblNames = saTblNames;
            this._relCls = relClasses;

        }

    }
}