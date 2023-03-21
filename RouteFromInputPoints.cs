using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.SOESupport;
using ESRI.ArcGIS.SystemUI;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Server;
using ESRI.ArcGIS.Output;
using ESRI.ArcGIS.GISClient;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesOleDB;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.GeoDatabaseDistributed;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.NetworkAnalyst;

namespace LRSLocator
{
    class RouteFromInputPoints
    {
        [DataMember(Order = 0, Name = "network_path")]
        public IFeature network_path { get; set; }

        public void SimpleRouteSetupSolveAndSaveWorkflow(INetworkDataset netDataset, List<IPoint> inputPoints, RESTContext _context)
        {
            IFeatureWorkspace imFtWorkspace = (IFeatureWorkspace)CreateInMemoryWorkspace();
            IFeatureClass wayPointFC = CreateFeatureClassFromPoints(imFtWorkspace, inputPoints);

            //var networkDataset = _context.networkTxdot,  as the input parameter for the class method, getting the object from the RESTContext/Context Object Model-COM.
            var networkDataset = netDataset;

            var deNetworkDataset = ((IDatasetComponent)networkDataset).DataElement as IDENetworkDataset;

            // Set up your solver
            var routeSolver = new NARouteSolverClass() as INASolver;
            INASolverSettings naSolverSettings = routeSolver as INASolverSettings;

            // Set up your context by creating it, then binding it to a network dataset.
            var context = routeSolver.CreateContext(deNetworkDataset, "Path from points Context") as INAContext;
            var contextEdit = context as INAContextEdit;
            IGPMessages gpMessages = new GPMessagesClass();
            contextEdit.Bind(networkDataset, gpMessages);

            // Load new stops using the input feature class and a NAClassLoader.
            var inputStopsFClass = wayPointFC;
            var cursor = inputStopsFClass.Search(null, false) as ICursor;
            var classLoader = new NAClassLoaderClass() as INAClassLoader;
            classLoader.NAClass = context.NAClasses.get_ItemByName("Stops") as INAClass;
            classLoader.Locator = context.Locator;
            int rowsInCursor = 0;
            int rowsLocated = 0;
            classLoader.Load(cursor, null, ref rowsInCursor, ref rowsLocated);

            // Solve the route using current settings           
            // And check the GPMessages after a successful solve to see if there are any warning or informational messages.      

            try
            {
                bool IsPartialSolution = routeSolver.Solve(context, gpMessages, null);
            }
            catch (Exception e)
            {
                string ex = e.ToString();
                Console.WriteLine(e.ToString());
            }

            // Get the FeatureClass containing the route results.
            // Iterate over the route class features' attribute values to examine the results.
            var routesClass = context.NAClasses.get_ItemByName("Routes") as IFeatureClass;

            IFeature resultFC = null;

            IFeatureCursor pFeatureCursor = routesClass.Search(null, false);
            IFeature pFeature;
            int num1 = 0;
            while ((pFeature = pFeatureCursor.NextFeature()) != null)
            {
                resultFC = pFeature;
                num1++;
            }

            //.Project((_context.HighwayFeatureClass as IGeoDataset).SpatialReference);
            resultFC.Shape.SpatialReference = (_context.HighwayFeatureClass as IGeoDataset).SpatialReference;
            this.network_path = resultFC;

            //--SAVE THE NETWORK PATH TO LOCAL DRIVE FOR VISUAL INTERPRETATION OF THE RESULT----
            // string outputFilePath = @"\\txdot4awgisdwa.dot.state.tx.us\D$\mxd\dev\LRS\LRSTest\AutoRte.lyr";
            string outputFilePath = @"\\txdot4wvdgdtk01.dot.state.tx.us\D$\Projects\CGCLocator\Output\AutoRte.lyr";
            // string outputFilePath = @"\\L-1G5VPQ2.dot.state.tx.us\D$\Projects\CGCLocator\Output\AutoRte.lyr";            
            //----------------------------------------------------------------------------------
            try
            {
                INALayer3 naLayer = routeSolver.CreateLayer(context) as INALayer3;
                ILayerFile layerfile = new LayerFileClass();
                layerfile.New(outputFilePath);
                layerfile.ReplaceContents(naLayer as ILayer);
                layerfile.Save();
                layerfile.Close();
            }
            catch (Exception e)
            {
                string ex = e.ToString();
                Console.WriteLine(e.Message);
            }

        }

        private IWorkspace CreateInMemoryWorkspace()
        {
            try
            {
                // Create an in-memory workspace factory.
                IWorkspaceFactory workspaceFactory = new InMemoryWorkspaceFactory() as IWorkspaceFactory;

                // Create a new in-memory workspace. This returns a name object.
                IWorkspaceName workspaceName = workspaceFactory.Create(string.Empty, "MyWorkspace", null, 0);
                IName name = (IName)workspaceName;

                // Open the workspace through the name object.
                IWorkspace workspace = (IWorkspace)name.Open();

                return workspace;
            }

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }
        }
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static IFeatureClass CreateFeatureClassFromPoints(IFeatureWorkspace featWorkspace, List<IPoint> inputPts)
        {
            String fcName = "StopPoints";

            IFieldsEdit ptFieldsEdit = new FieldsClass();
            IFieldEdit ptField = new FieldClass();

            IPoint pt = inputPts[0];
            ISpatialReference ptSR = pt.SpatialReference;


            ptField = new FieldClass();
            ptField.Type_2 = esriFieldType.esriFieldTypeOID;
            ptField.Name_2 = "OBJECTID";
            ptField.AliasName_2 = "OBJECTID";
            ptFieldsEdit.AddField(ptField);

            IGeometryDefEdit ptGeomDef;
            ptGeomDef = new GeometryDefClass();
            ptGeomDef.GeometryType_2 = esriGeometryType.esriGeometryPoint;
            ptGeomDef.SpatialReference_2 = ptSR;
            ptGeomDef.HasZ_2 = false;

            ptField = new FieldClass();
            ptField.Name_2 = "SHAPE";
            ptField.AliasName_2 = "SHAPE";
            ptField.Type_2 = esriFieldType.esriFieldTypeGeometry;
            ptField.GeometryDef_2 = ptGeomDef;
            ptFieldsEdit.AddField(ptField);

            ptField = new FieldClass();
            ptField.Name_2 = "stopOrder";
            ptField.AliasName_2 = "stopOrder";
            ptField.Type_2 = esriFieldType.esriFieldTypeInteger;
            ptFieldsEdit.AddField(ptField);

            IFeatureClass ptFC = featWorkspace.CreateFeatureClass(fcName, ptFieldsEdit, null, null, esriFeatureType.esriFTSimple, "SHAPE", "");

            // Add data to Feature class setting the "stopOrder" field with position order of respecively to the position in the list
            int position = 0;
            int ptIdx;
            foreach (IPoint pnt in inputPts)
            {
                position++;
                IFeature stopFeature = ptFC.CreateFeature();
                stopFeature.Shape = pnt;
                ptIdx = stopFeature.Fields.FindField("stopOrder");
                stopFeature.set_Value(ptIdx, position);
                stopFeature.Store();
            }
            return ptFC;

        }


    }


}