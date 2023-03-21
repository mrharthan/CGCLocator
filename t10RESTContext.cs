using System.Collections.Specialized;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.SOESupport;

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

        // Layer Properties
        public IFeatureClass HighwayFeatureClass;
        public IFeatureClass IntersectionFeatureClass;
        public IFeatureClass LrsCtrlSectFeatureClass;
        public IFeatureClass LrsCtrlSectAssetFeatureClass;
        public IFeatureClass LrsCtrlSectAssetCompressedFeatureClass;
        public IFeatureClass LrsFrontageCompressedFeatureClass;
        public ITable ReferenceMarkerTable;

        public string HighwayFeatureClass_RIDField;
        public string IntersectionFeatureClass_IDField;
        public string IntersectionFeatureClass_AltIDField;
        public string IntersectionFeatureClass_MValueField;
        public string MarkerTable_RIDField;
        public string MarkerTable_DFOField;
        public string MarkerTable_MarkerNumberField;
        public double SearchTolerance;
        

        public IMapLayerInfos MapLayerInfos
        {
            get { return MapServer.GetServerInfo(MapServer.DefaultMapName).MapLayerInfos; }
        }
    }
}

