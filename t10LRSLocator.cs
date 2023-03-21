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


//TODO: sign the project (project properties > signing tab > sign the assembly)
//      this is strongly suggested if the dll will be registered using regasm.exe <your>.dll /codebase


namespace LRSLocator
{
    [ComVisible(true)]
    [Guid("2cc38f21-b4bc-487e-8d03-7bcbd0a72845")]  //Alternate V2: {2701A8D8-4F7A-4688-AB09-1ADA81E098BA}  Original V1: {2cc38f21-b4bc-487e-8d03-7bcbd0a72845}
    [ClassInterface(ClassInterfaceType.None)]
    [ServerObjectExtension("MapServer",//use "MapServer" if SOE extends a Map service and "ImageServer" if it extends an Image service.
        AllCapabilities = "",
        DefaultCapabilities = "",
        Description = "TXDOT LRS Locator",
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


        private string _highwayFeatureClass_RIDFieldName = "LRS_RTE_ID";
        private string _intersectionFeatureClass_IDFieldName = "IntersectionID";
        private string _intersectionFeatureClass_AltIDFieldName = "hname";
        private string _intersectionFeatureClass_MValueField = "MValue";
        private string _markerTable_RIDFieldName = "lrs_rte_id";
        private string _markerTable_MarkerFieldName = "rmrkr_pnt_num";
        private string _markerTable_DFOFieldName = "rmrkr_pnt_dfo_ms";
        private double _searchTolerance = 0.001;

        private IFeatureClass _highwayFeatureClass = null;
        private IFeatureClass _intersectionFeatureClass = null;
        private IFeatureClass _lrsCtrlSectLayer = null;
        private IFeatureClass _lrsCtrlSectassetLayer = null;
        private IFeatureClass _lrsCtrlSectassetCompressedLayer = null;
        private IFeatureClass _lrsFrontageassetCompressedLayer = null;
        private ITable _markerTable = null;

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

            RestOperation getXYOperation = new RestOperation("Get Point",
                                                      new string[] { "LRS Route ID", "ReferenceMarker With Offset", "Output Spatial Reference" },
                                                      new string[] { "json" },
                                                      HandleOp_GetXY);

            RestOperation getPolylineOperation = new RestOperation("Get Polyline",
                                          new string[] { "LRS Route ID", "Start ReferenceMarker With Offset", "End ReferenceMarker With Offset", "Output Spatial Reference" },
                                          new string[] { "json" },
                                          HandleOp_GetPolyline);

            RestOperation getHighwayLocationOperation = new RestOperation("Get Highway Location",
                                          new string[] { "Longitude", "Latitude", "Spatial Reference", "Multiple Values" },
                                          new string[] { "json" },
                                          HandleOp_getHighwayLocation);

            RestOperation getPolyLineFromLatLongOperation = new RestOperation("Get Polyline From LatLong",
                              new string[] { "Begin_Longitude", "Begin_Latitude", "End_Longitude","End_Latitude"},
                              new string[] { "json" },
                              HandleOp_getPolyLineFromLatLongHandlerOperation);

            RestOperation getPolyLineFromCtrlSectAssetOperation = new RestOperation("Get Polyline From CtrlSect Asset",
                              new string[] { "Begin_Longitude", "Begin_Latitude", "End_Longitude", "End_Latitude", "Asset_Type" },
                              new string[] { "json" },
                              HandleOp_getPolyLineFromCtrlSectAssetHandlerOperation);

            RestOperation getPointFromCtrlSectAssetOperation = new RestOperation("Get Point From CtrlSect Asset",
                              new string[] { "Begin_Longitude", "Begin_Latitude", "Asset_Type" },
                              new string[] { "json" },
                              HandleOp_getPointFromCtrlSectAssetHandlerOperation);

            RestOperation getPolyLineFromCtrlSectMilePointOperation = new RestOperation("Get Polyline From CtrlSect MilePoint",
                              new string[] { "CtrlSect", "Begin_MilePoint", "End_MilePoint" },
                              new string[] { "json" },
                              HandleOp_getPolyLineFromCtrlSectMilePointOperation);

            RestOperation getPointFromCtrlSectMilePointOperation = new RestOperation("Get Point From CtrlSect MilePoint",
                              new string[] { "CtrlSect", "MilePoint" },
                              new string[] { "json" },
                              HandleOp_getPointFromCtrlSectMilePointOperation);

            RestOperation getXYFromIntersection = new RestOperation("Get Point from Intersection Offset",
                    new string[] { "Intersection Offset Location", "Spatial Reference" },
                    new string[] { "json" },
                    HandleOp_getPointFromIntersectionOffset);

            rootRes.operations.Add(getXYOperation);
            rootRes.operations.Add(getPolylineOperation);
            rootRes.operations.Add(getHighwayLocationOperation);
            rootRes.operations.Add(getPolyLineFromLatLongOperation);
            rootRes.operations.Add(getPolyLineFromCtrlSectAssetOperation);
            rootRes.operations.Add(getPointFromCtrlSectAssetOperation);
            rootRes.operations.Add(getPolyLineFromCtrlSectMilePointOperation);
            rootRes.operations.Add(getPointFromCtrlSectMilePointOperation);

            return rootRes;
        }

        private byte[] HandleOp_getPolyLineFromCtrlSectMilePointOperation(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return HandleOperation(new GetPolyLineFromCtrlSectMilePointHandler(), boundVariables, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        private byte[] HandleOp_getPointFromCtrlSectMilePointOperation(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return HandleOperation(new GetPointFromCtrlSectMilePointHandler(), boundVariables, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        private byte[] HandleOp_getPolyLineFromLatLongHandlerOperation(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return HandleOperation(new GetPolyLineFromLatLongHandler(), boundVariables, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        private byte[] HandleOp_getPolyLineFromCtrlSectAssetHandlerOperation(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return HandleOperation(new GetPolyLineFromCtrlSectAssetLayer(), boundVariables, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        private byte[] HandleOp_getPointFromCtrlSectAssetHandlerOperation(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return HandleOperation(new GetPointFromCtrlSectAssetLayer(), boundVariables, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        private byte[] RootResHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties, out string responseProperties)
        {
            responseProperties = null;

            JsonObject result = new JsonObject();
            return Encoding.UTF8.GetBytes(result.ToJson());
        }

        private byte[] HandleOp_GetXY(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            logger.LogMessage(ServerLogger.msgType.infoStandard, "GETXYHandler()", 8000, "Initiating GetXY");
            return HandleOperation(new GetXYHandler(), boundVariables, operationInput, outputFormat, requestProperties, out responseProperties);
        }


        private byte[] HandleOp_GetPolyline(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return HandleOperation(new GetPolylineHandler(), boundVariables, operationInput, outputFormat, requestProperties, out responseProperties);
        }


        private byte[] HandleOp_getHighwayLocation(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return HandleOperation(new GetHighwayLocationHandler(), boundVariables, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        private byte[] HandleOp_getIntersectionOffset(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return HandleOperation(new GetIntersectionOffsetHandler(), boundVariables, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        private byte[] HandleOp_getPointFromIntersectionOffset(NameValueCollection boundVariables, JsonObject operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return HandleOperation(new GetPointFromIntersectionOffsetHandler(), boundVariables, operationInput, outputFormat, requestProperties, out responseProperties);
        }

        /// <summary>
        /// A generic internal handler for all REST operations.
        /// The REST operation delegate methods should call this method to benefit 
        /// from uniform request processing, response formatting, and exception handling.
        /// </summary>
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


        /// <summary>
        /// A generic internal handler for all REST resources.
        /// The REST resource delegate methods should call this method to benefit 
        /// from uniform request processing, response formatting, and exception handling.
        /// </summary>
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

            context.HighwayFeatureClass = this._highwayFeatureClass;
            context.IntersectionFeatureClass = this._intersectionFeatureClass;
            context.LrsCtrlSectFeatureClass = this._lrsCtrlSectLayer;
            context.LrsCtrlSectAssetFeatureClass = this._lrsCtrlSectassetLayer;
            context.LrsCtrlSectAssetCompressedFeatureClass = this._lrsCtrlSectassetCompressedLayer;
            context.LrsFrontageCompressedFeatureClass = this._lrsFrontageassetCompressedLayer;
            context.ReferenceMarkerTable = this._markerTable;

            context.HighwayFeatureClass_RIDField = this._highwayFeatureClass_RIDFieldName;
            context.IntersectionFeatureClass_IDField = this._intersectionFeatureClass_IDFieldName;
            context.IntersectionFeatureClass_AltIDField = this._intersectionFeatureClass_AltIDFieldName;
            context.IntersectionFeatureClass_MValueField = this._intersectionFeatureClass_MValueField;
            context.MarkerTable_RIDField = this._markerTable_RIDFieldName;
            context.MarkerTable_MarkerNumberField = this._markerTable_MarkerFieldName;
            context.MarkerTable_DFOField = this._markerTable_DFOFieldName;
            context.SearchTolerance = _searchTolerance;
            return context;
        }

        /// <summary>
        /// A generic internal handler for all REST resources and operations.
        /// </summary>
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
                //string methodName = LogName + "." + (handler != null ? handler.GetType().Name : "HandleHelper");
                //_logger.LogMessage(ServerLogger.msgType.error, methodName, 9001, LogName + ": " + e.ToString());
                response = null; // JsonBuilder.BuildErrorObject(500, e.Message);
                responseProperties = null;
            }
            return JSONHelper.EncodeResponse(response);
        }



        private void initiateFeatures(IMapServer3 mapServer)
        {
            IMapServerDataAccess dataAccess = (IMapServerDataAccess)mapServer;
            IMapServerInfo msInfo = mapServer.GetServerInfo(mapServer.DefaultMapName);
            IMapLayerInfos layerInfos = msInfo.MapLayerInfos;

            int layerCount = layerInfos.Count;

            for (int j = 0; j < layerCount; j++)
            {
                IMapLayerInfo layerInfo = layerInfos.get_Element(j);
                if (layerInfo.IsFeatureLayer)
                {
                    IFeatureClass featureClass = (IFeatureClass)dataAccess.GetDataSource(mapServer.DefaultMapName, layerInfo.ID);
                    if (featureClass != null && featureClass.ShapeType == ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPolyline)
                    {
                        IGeometryDef geometryDef = featureClass.Fields.get_Field(featureClass.FindField(featureClass.ShapeFieldName)).GeometryDef;
                        if (geometryDef.HasM)
                        {
                            if (featureClass.AliasName == "gis_dw.sde.lrs_rdbd_gmtry_lrs_net")
                            {
                                // Use this as Route FeatureClass
                                this._highwayFeatureClass = featureClass;
                            }
                            else if (featureClass.AliasName == "gis_dw.sde.lrs_ctrl_sect_ln_lyr")
                            {
                                this._lrsCtrlSectLayer = featureClass;
                            }
                            else if (featureClass.AliasName == "gis_dw.sde.lrs_ctrl_sect_asset_lyr") 
                            {
                                this._lrsCtrlSectassetLayer = featureClass;
                            }
                            else if (featureClass.AliasName == "gis_dw.sde.lrs_ctrl_sect_asset_clyr")
                            {
                                this._lrsCtrlSectassetCompressedLayer = featureClass;
                            }
                            else if (featureClass.AliasName == "gis_dw.sde.lrs_ctrl_sect_asset_frntg_clyr")
                            {
                                this._lrsFrontageassetCompressedLayer = featureClass;
                            }
                            else
                            {
                                this._intersectionFeatureClass = featureClass;  //Currently the gis_dw.sde.lrs_ctrl_sect_rdbd_lyr
                            }
                            
                            //System.Diagnostics.Debugger.Break();
                        }
                    }
                    //else if (featureClass != null && featureClass.ShapeType == ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPoint)
                    //{
                    //    IGeometryDef geometryDef = featureClass.Fields.get_Field(featureClass.FindField(featureClass.ShapeFieldName)).GeometryDef;
                    //    if (geometryDef.HasM)
                    //    {
                    //        // Use this as intersection FeatureClass
                    //        this._intersectionFeatureClass = featureClass;
                    //    }
                    //}
                }
            }

            IMapServerInfo3 msInfo3 = (IMapServerInfo3)msInfo;
            IStandaloneTableInfos tableInfos = msInfo3.StandaloneTableInfos;
            if (tableInfos != null)
            {
                int tableCount = tableInfos.Count;
                for (int j = 0; j < tableCount; j++)
                {
                    IStandaloneTableInfo tableInfo = (IStandaloneTableInfo)tableInfos.get_Element(j);

                    ITable table = (ITable)dataAccess.GetDataSource(mapServer.DefaultMapName, tableInfo.ID);
                    if (table != null)
                        this._markerTable = table;
                }
            }
        }

    }
}
