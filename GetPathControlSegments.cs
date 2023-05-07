using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ESRI.ArcGIS.SOESupport;


namespace LRSLocator
{
    class GetPathControlSegments
    {
        [System.Runtime.Serialization.DataMember(Order = 0, Name = "Auto_FeaturesList")]
        public List<IFeature> Auto_FeaturesList { get; set; }
        //public List<IFeature> PedTrip_FeaturesList { get; set; }
        //public List<IFeature> BusTrip_FeaturesList { get; set; }
        //public List<IFeature> RailTrip_FeaturesList { get; set; }
        //public List<IFeature> BusStop_FeaturesList { get; set; }
        //public List<IFeature> RailStation_FeaturesList { get; set; }
        public IPolygon bufferPolygon { get; set; }

        private RESTContext _context;

        public void getCtrlFeatures(IFeature routeFt, RESTContext curr_context, string ctrlMode)
        // public void getCtrlSectFeatures(IFeature routeFetaure) //, IFeatureClass LrsCtrlSectFeatureClass, string HwyName, string ctrlSectNbr, RESTContext context)
        {
            this._context = curr_context;
            IFeature routeFeature = routeFt;
            // routeFetaure.Shape.Project((context.HighwayFeatureClass as IGeoDataset).SpatialReference);
            List<IFeature> ListOfSelectedCtrlFeats = new List<IFeature>();

            //Create a minimum 0.00005-meter buffer around the solved route path
            IPolygon bufferPolygon = createBufferPolygon(routeFeature);
            //bufferPolygon.Project((_context.HighwayFeatureClass as IGeoDataset).SpatialReference);
            bufferPolygon.Project(routeFeature.Shape.SpatialReference);

            if (ctrlMode.Equals("Auto"))    //Get the list of the Auto Trip features contained within the buffer                    
                this.Auto_FeaturesList = getCtrlPathMetricFeatures(bufferPolygon);

            if (ctrlMode.Equals("Transit"))    //Get the list of the Transit Trip features contained within the buffer
            {
                this.Auto_FeaturesList = getMultiCtrlPathMetricFeatures(bufferPolygon);
                // Get Pedestrian Trip/Traveller Lines
                // Get Bus Trip/Traveller Lines --> Also need Avg. Fares
                // Get Rail Trip/Traveller Lines --> Also need Avg. Fares
                // Get Rail Station Points --> transfer wait times
                // Get Bus Stop Points --> Analyze STOP_ID in Transfer Stations for a lookup of transfer wait times.
            }

            int nbrOfFeature = this.Auto_FeaturesList.Count;
        }

        // Create a polygon of x-meter buffer around the networkpath feature
        public IPolygon createBufferPolygon(IFeature routeFt)
        {
            IGeometryCollection originalGeometryBag = new GeometryBagClass();
            IGeometryCollection outBufferGeometryBag = new GeometryBagClass();

            IBufferConstruction bufferConstruct = new BufferConstructionClass();
            IBufferConstructionProperties bufferProper = bufferConstruct as IBufferConstructionProperties;

            IEnumGeometry originalGeometryEnum;
            IEnumGeometry resultGeometryEnum;

            IGeometry resultGeo = null;
            IPolygon resultPolygon5m = null;
            List<IGeometry> resultGeoList = new List<IGeometry>();

            //Field value IDoubleArray
            IDoubleArray daValue = new DoubleArray();

            IGeometry polyline = routeFt.ShapeCopy;
            double distance = 0.00005;   // previously 5.0
            daValue.Add(distance);

            esriGeometryType geomType;

            try
            {
                originalGeometryBag.AddGeometry(polyline);

                // Get our original geometry to buffer
                originalGeometryEnum = originalGeometryBag as IEnumGeometry;
                if (originalGeometryEnum != null)
                {
                    //Set buffer options
                    //Set flat head buffer
                    bufferProper.EndOption = esriBufferConstructionEndEnum.esriBufferFlat;
                    //Whether buffer overlap
                    bufferProper.UnionOverlappingBuffers = true;

                    //Construct the buffer geomtery
                    bufferConstruct.ConstructBuffersByDistances2(originalGeometryEnum, daValue, outBufferGeometryBag);

                    // Get the resulting buffered geometry
                    if (outBufferGeometryBag != null)
                    {
                        resultGeometryEnum = outBufferGeometryBag as IEnumGeometry;
                        if (resultGeometryEnum != null)
                        {
                            resultGeo = resultGeometryEnum.Next();
                            geomType = resultGeo.GeometryType;
                        }
                    }
                }
                resultPolygon5m = resultGeo as IPolygon;

            }

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }
            return resultPolygon5m;
        }

        //------------------Filter the Auto Trip Metrics features contained within the network path x-meter buffer
        private List<IFeature> getCtrlPathMetricFeatures(IPolygon routeBuffer)
        {
            List<IFeature> filteredFeatures = new List<IFeature>();
            List<IFeature> initiallySelectedFeatures = new List<IFeature>();
            using (ComReleaser comReleaser = new ComReleaser())
            {                
                System.String nameOfShapeField = _context.LrsAutoTripMetricFeatureClass.ShapeFieldName;
                ISpatialFilter spatialFilter = new SpatialFilter();
                spatialFilter.Geometry = routeBuffer;
                spatialFilter.GeometryField = nameOfShapeField;
                //spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;

                comReleaser.ManageLifetime(spatialFilter);

                IFeatureCursor featureCursor = _context.LrsAutoTripMetricFeatureClass.Search(spatialFilter, false);
                comReleaser.ManageLifetime(featureCursor);

                List<string> variablesNames = new List<string>();
                List<IFeature> ctrlFeatures = new List<IFeature>();
                IFeature pFeature = null;
                var currOID = 0;
                var prevOID = 0;                

                while ((pFeature = featureCursor.NextFeature()) != null)
                {
                    currOID = pFeature.OID;

                    if (pFeature.Shape != null && !pFeature.Shape.IsEmpty && currOID != prevOID)
                    {
                        ctrlFeatures.Add(pFeature);
                    }

                    prevOID = currOID;
                }

                return ctrlFeatures;
            }
        }

        //------------------Filter the Auto Trip Metrics features contained within the network path x-meter buffer
        private List<IFeature> getMultiCtrlPathMetricFeatures(IPolygon routeBuffer)
        {
            List<IFeature> ctrlMultiFeatures = new List<IFeature>();

            return ctrlMultiFeatures;
        }

        //--------   Get an intersection point between the input polygon feature and the buffer boundary polygon geometry and insert it in the polyline -------------  
        public List<IPoint> InsertPointAtIntersection(ref IPolyline pPolyline, IPolyline intersectingFt, IFeature fPolyline)
        {
            IGeometry pOther = intersectingFt;
            bool SplitHappened = false;
            int newPartIndex = 0;
            int newSegmentIndex = 0;
            int index = 0;
            List<int> indices;
            IPoint point = null;
            List<IPoint> interPoints = new List<IPoint>();

            try
            {

                IClone pClone = pPolyline.SpatialReference as IClone;
                if (pClone.IsEqual(pOther.SpatialReference as IClone) == false)
                {
                    pOther.Project(pPolyline.SpatialReference);
                }

                ITopologicalOperator pTopoOp = pOther as ITopologicalOperator;
                pTopoOp.Simplify();

                pTopoOp = pPolyline as ITopologicalOperator;
                IGeometry pGeomResult = pTopoOp.Intersect(pOther, esriGeometryDimension.esriGeometry0Dimension);

                indices = new List<int>();

                if ((pGeomResult is IPointCollection) && ((pGeomResult as IPointCollection).PointCount > 0))
                {

                    for (int i = 0; i < (pGeomResult as IPointCollection).PointCount; i++)  //HANDLES THE POINT OF INTERSECTION ALONG THE POLYLINE
                    {
                        (pPolyline as IPolycurve2).SplitAtPoint((pGeomResult as IPointCollection).get_Point(i), true, false, out SplitHappened, out newPartIndex, out newSegmentIndex);
                        index = 0;
                        for (int j = 0; j < newPartIndex; j++)
                        {
                            index += ((pPolyline as IGeometryCollection).get_Geometry(j) as IPointCollection).PointCount;
                        }
                        index += newSegmentIndex;

                        point = (pPolyline as IPointCollection).get_Point(index);

                        (pPolyline as IPointCollection).UpdatePoint(index, point);
                        interPoints.Add(point);  // NEED THIS M

                    }

                }

                (pPolyline as ITopologicalOperator2).IsKnownSimple_2 = false;
                (pPolyline as IPolyline4).SimplifyEx(true);
            }
            catch (Exception e)
            {
                string ex = e.ToString();
                Console.WriteLine(e.Message);
            }

            return interPoints;
        }
        //----------Get polygon sub-curves (between the two intersection points) of the selected input features, within x meters from the routh path geometry,  and return those as final result
        private IPolyline GetSubCurve(IFeature inFeature, double d1, double d2)  // IPolyline inpolyLine, IPoint pnt1, IPoint pnt2  --  FOR ENHANCEMENT TO HANDLE INTERSECT VIOLATORS
        {
            IGeometryCollection featureCollection = null;

            featureCollection = (inFeature.Shape as IMSegmentation3).GetSubcurveBetweenMs(d1, d2);

            IPolyline outPolyline = featureCollection as IPolyline;

            return outPolyline;

        }

        //Get a distance where a given point is located along a polygon
        private double GetDistAlong(IPolyline polyLine, IPoint pnt)
        {
            var outPnt = new PointClass() as IPoint;
            double distAlong = double.NaN;
            double distFrom = double.NaN;
            bool bRight = false;
            polyLine.QueryPointAndDistance(esriSegmentExtension.esriNoExtension, pnt, false, outPnt,
                ref distAlong, ref distFrom, ref bRight);
            return distAlong;
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
        //-------------------------------------------------------------------------------
        private IFeatureClass CreateNewOutputFC(IFeatureClass inFC, IFeatureWorkspace destination, String name)
        {
            IFieldsEdit outFields = new FieldsClass();
            ISpatialReference outGeomSR = null;
            for (int i = 0; i < inFC.Fields.FieldCount; i += 1)
            {
                IField field = inFC.Fields.get_Field(i);
                if (field.Type == esriFieldType.esriFieldTypeGeometry)
                {
                    outGeomSR = field.GeometryDef.SpatialReference;
                }
                else
                {
                    outFields.AddField(field);
                }
            }

            string ShapeFieldName = inFC.ShapeFieldName;

            IGeometryDefEdit geom = new GeometryDefClass();
            geom.GeometryType_2 = esriGeometryType.esriGeometryPolyline;  // 10/14/2020 was geom.GeometryType_2 = esriGeometryType.esriGeometryPolygon
            geom.SpatialReference_2 = outGeomSR;
            geom.HasM_2 = true;
            geom.HasZ_2 = false;

            IFieldEdit geomField = new FieldClass();
            geomField.Name_2 = ShapeFieldName;
            geomField.AliasName_2 = ShapeFieldName;
            geomField.Type_2 = esriFieldType.esriFieldTypeGeometry;
            geomField.GeometryDef_2 = geom;
            outFields.AddField(geomField);

            IFeatureClass outFC = destination.CreateFeatureClass(name, outFields, null, null, esriFeatureType.esriFTSimple, ShapeFieldName, "");

            return outFC;
        }

        private IFeature GetFeatureWithRecords(IFeature ftr, IFeatureClass OutFC, IPolyline ftrPolyline)
        {
            // IFeatureClass ftrFc = _context.LrsCtrlSectFeatureClass;
            IFeature newFtr = OutFC.CreateFeature();
            try
            {
                IFields flds = ftr.Fields;
                //IGeometry geo = ftrGeom;
                IPolyline polyline = ftrPolyline;

                for (int f = 0; f < flds.FieldCount; f++)
                {
                    IField fld = flds.get_Field(f);
                    esriFieldType fldType = fld.Type;
                    int newFldIndex = newFtr.Fields.FindField(fld.Name);
                    if (fld.Editable == true && (fldType != esriFieldType.esriFieldTypeGeometry))
                    {
                        newFtr.set_Value(newFldIndex, ftr.get_Value(f));

                    }
                }
                IGeometry geo = newFtr.ShapeCopy;
                geo = (IGeometry)ftrPolyline;
                newFtr.Shape = geo;
                newFtr.Store();
            }
            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
            return newFtr;

        }


    }
}