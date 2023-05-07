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


namespace LRSLocator
{
    /// LRS_SOE_CGC Relationship Control Class
    /// Control INGRESS/EGRESS approaches based on the RelationshipClass names from the imnplementing class
    public class RelationshipClassController
    {
        [DataMember(Order = 0, Name = "bus_pnt_ln_feats")]  /// StopsToBusRoute Features
        private List<IFeature> bus_pnt_ln_feats { get; set; }

        [DataMember(Order = 0, Name = "rail_pnt_ln_feats")]  /// StationsToRailLine Features
        private List<IFeature> rail_pnt_ln_feats { get; set; }
                
        public List<IFeature> getStopsToBusRoute(List<IFeature> entryBusFeatures, string relClass)   //1
        {                         
            List<IFeature> relatedBusPntToLnFtrs = new List<IFeature>();
            List<IObject> bfeatureObjects = new List<IObject>();

            try
            {
                List<IObject> resultBFtrs = null;

                foreach (IObject nodeObj in entryBusFeatures)
                {
                    bfeatureObjects.Add(nodeObj);
                }

                resultBFtrs = Identify_RelationshipClasseObjs(bfeatureObjects, relClass);

                foreach (IFeature ftr in resultBFtrs)
                {
                    relatedBusPntToLnFtrs.Add(ftr);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }

            this.bus_pnt_ln_feats = relatedBusPntToLnFtrs;
            return relatedBusPntToLnFtrs;

        }        

        public List<IFeature> getStationsToRailLine(List<IFeature> entryRailFeatures, string relClass)  //2
        {             
            List<IFeature> relatedRailPntToLnFtrs = new List<IFeature>();
            List<IObject> rfeatureObjects = new List<IObject>();

            try
            {
                List<IObject> resultRFtrs = null;

                foreach (IObject ctrlSectObj in entryRailFeatures)
                {
                    rfeatureObjects.Add(ctrlSectObj);
                }

                resultRFtrs = Identify_RelationshipClasseObjs(rfeatureObjects, relClass);

                foreach (IFeature ftr in resultRFtrs)
                {
                    relatedRailPntToLnFtrs.Add(ftr);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }

            this.rail_pnt_ln_feats = relatedRailPntToLnFtrs;
            return relatedRailPntToLnFtrs;
        
        }               

        //-----Configure Relationships between Classes / Objects-----------------------------------------------------------------------------------------------------------------------
        private List<IObject> Identify_RelationshipClasseObjs(List<IObject> features, string relationshipClassName)
        {
            string originObjClassName = "";
            string destinationObjClassName = "";
            string OriginPk = "";
            string OriginFk = "";
            // SANDBOX_STATIC.GIS  (SQL Server)
            switch (relationshipClassName)
            {
                case "bus_stop_to_route_ln":              //1
                    {
                        originObjClassName = "SANDBOX_STATIC.GIS.BusTripMetrics";
                        destinationObjClassName = "SANDBOX_STATIC.GIS.Bus_TAZ_Stops";
                        OriginPk = "MetricKey";
                        OriginFk = "MetricKey";
                    }
                    break;
                case "rail_stop_to_route_ln":            //2
                    {
                        originObjClassName = "SANDBOX_STATIC.GIS.RailTripMetrics";
                        destinationObjClassName = "SANDBOX_STATIC.GIS.Rail_TAZ_Stations";
                        OriginPk = "MetricKey";
                        OriginFk = "MetricKey";
                    }
                    break;
                default:
                    originObjClassName = "";
                    destinationObjClassName = "";
                    OriginPk = "";
                    OriginFk = "";
                    break;
            }

            string originObjClassNameTest = originObjClassName;
            string destinationObjClassNameTest = destinationObjClassName;
            string OriginPkTest = OriginPk;
            List<IObject> relatedFeatures = new List<IObject>();

            //Get all RelationshipClasses where this feature participates as Origin or Destination
            List<IRelationshipClass> relClasses = new List<IRelationshipClass>();
            IEnumRelationshipClass enumRelClass = features[0].Class.get_RelationshipClasses(esriRelRole.esriRelRoleAny);
            List<string> pathDirections = new List<string>();
            List<string> relatedClassNames = new List<string>();
            IRelationshipClass relationshipClass = null;
            while ((relationshipClass = enumRelClass.Next()) != null)
            {

                relClasses.Add(relationshipClass);
                //pathDirections.Add("[" + relationshipClass.ForwardPathLabel + "," + relationshipClass.BackwardPathLabel + "],");
                //relatedClassNames.Add("[ Origin: " + relationshipClass.OriginClass.AliasName + ", Destination: " + relationshipClass.DestinationClass.AliasName + "],");


            }
            List<string> dirNames = pathDirections;
            List<string> relNames = relatedClassNames;

            //If the feature with no Relationships established has been selected, exit
            if (relClasses == null)
            {
                return null;
            }

            IObject relFtr = null;
            List<IObject> relFtrList = new List<IObject>();
            ISet inputFeatureSet = new SetClass();
            foreach (IObject obj in features)
            {
                inputFeatureSet.Add(obj);
            }

            try
            {
                int c = 0;
                foreach (IRelationshipClass relClass in relClasses)
                {
                    if (string.Equals(relClass.OriginClass.AliasName, originObjClassName) && string.Equals(relClass.DestinationClass.AliasName, destinationObjClassName) && string.Equals(relClass.OriginPrimaryKey, OriginPk) && string.Equals(relClass.OriginForeignKey, OriginFk))
                    {
                        ISet relFtrSet = relClass.GetObjectsRelatedToObjectSet(inputFeatureSet);
                        relFtrSet.Reset();
                        // 
                        while ((relFtr = (IObject)relFtrSet.Next()) != null)
                        {
                            relatedFeatures.Add(relFtr);
                        }
                    }
                }
                c = relatedFeatures.Count;
            }

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }
            return relatedFeatures;

        }

    }

}
