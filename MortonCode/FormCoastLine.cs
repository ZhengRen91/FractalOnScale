using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.Geometry;

namespace MortonCode
{
    public partial class FormCoastLine : Form
    {
        public FormCoastLine()
        {
            InitializeComponent();
        }

        private string maxLength;
        private string minLength;
        private string meanLength;
        private string yardStickLength;
        private string strFilePath;
        private string strFileName;
        private string layerName;
        private ISpatialReference pSpatialRef;

        private IPolyline Coastline;
        private double radius;
      
        private IGeometryCollection pGeometryCollection = new GeometryBagClass();
        private IPoint startCenterPoint;
        private IPoint secondCenterPoint;

        private IPoint tempCenterPoint;
        private IPoint lastCenterPoint;
        private IPoint presentCenterPoint;
        private ICircularArc presentCircle; 
        private IPolygon startCirclePolygon;

        private List<IPoint> initialCenterPoints;
        private List<IPoint> resultCenterPoints;
        
        public class VerticePoint
        {
            public IPoint pt;
            public int id;
        }

        private IWorkspace OpenShapfileWorkspace(string ShapeFilePath)
        {
            IWorkspace ws = null;
            IWorkspaceFactory wsf = new ShapefileWorkspaceFactoryClass(); //using DataSourcesFile
            if (ShapeFilePath != null)
            {
                ws = wsf.OpenFromFile(ShapeFilePath, 0);
            }           
            return ws;
        }

        private IFields CreateFieldsCollection(ISpatialReference spatialReference, esriGeometryType geometryType)
        {
            IFeatureClassDescription fcDesc = new FeatureClassDescriptionClass();
            IObjectClassDescription ocDesc = fcDesc as IObjectClassDescription;

            IFields fields = ocDesc.RequiredFields;
            IFieldsEdit fieldsEdit = fields as IFieldsEdit;

            int shapeFieldIndex = fields.FindField(fcDesc.ShapeFieldName);
            IField shapeField = fields.get_Field(shapeFieldIndex);

            IGeometryDef geometryDef = shapeField.GeometryDef;
            IGeometryDefEdit geometryDefEdit = geometryDef as IGeometryDefEdit;

            geometryDefEdit.GeometryType_2 = geometryType;
            geometryDefEdit.GridCount_2 = 1;
            geometryDefEdit.set_GridSize(0, 0);
            geometryDefEdit.SpatialReference_2 = spatialReference;

            return fields;
        }

        private IFeatureClass CreateNewFeatureClass(IWorkspace pWS, String featureClassName, IFields pFields, esriFeatureType pEsriFeatureType) 
        {
            IFeatureClassDescription fcDesc = new FeatureClassDescriptionClass();
            IObjectClassDescription ocDesc = fcDesc as IObjectClassDescription;
            IFieldChecker pFieldChecker = new FieldCheckerClass();
            IEnumFieldError pEnumFieldError = null;
            IFields validatedFields = null;
            IFeatureWorkspace pFeatureWorkspace = pWS as IFeatureWorkspace;
            pFieldChecker.ValidateWorkspace = pWS;
            pFieldChecker.Validate(pFields, out pEnumFieldError, out validatedFields);

            IFeatureClass pFeatureClass = pFeatureWorkspace.CreateFeatureClass(featureClassName, validatedFields, ocDesc.InstanceCLSID, ocDesc.ClassExtensionCLSID, pEsriFeatureType, fcDesc.ShapeFieldName, "");
            return pFeatureClass;
        }

        private void AddGeometryColToFeatureClass(IGeometryCollection pGeometryCollection, IFeatureClass pFeatureClass)
        {
            IFeatureCursor pFeatureCursor;
            IFeatureBuffer pFeatureBuffer;

            pFeatureCursor = pFeatureClass.Insert(true);
            pFeatureBuffer = pFeatureClass.CreateFeatureBuffer();

            IFields pFields;
            IField pField;

            pFields = pFeatureClass.Fields;


            for (int i = 0; i < pGeometryCollection.GeometryCount; i++)
            {
                IGeometry pCurrentGeometry = pGeometryCollection.get_Geometry(i) as IGeometry;

                for (int n = 1; n <= pFields.FieldCount - 1; n++)
                {
                    pField = pFields.get_Field(n);

                    if (pField.Type == esriFieldType.esriFieldTypeGeometry)
                    {

                        pFeatureBuffer.set_Value(n, pCurrentGeometry);


                    }

                }
                pFeatureCursor.InsertFeature(pFeatureBuffer);
            }
            pFeatureCursor.Flush();

        }


        //Geometry Type must be point polyline polygon...
        public void CreateShpfile(String featureClassName, IGeometryCollection pGeometryCollection) 
        {
            GetWorkPath();
            IWorkspace pWS = OpenShapfileWorkspace(this.strFilePath);
            GetSpatialReference();
            ISpatialReference pSpatialReference = this.pSpatialRef;
            IGeometry pGeometry = pGeometryCollection.get_Geometry(0);
            esriGeometryType GeometryType = pGeometry.GeometryType;
            IFields pFields = CreateFieldsCollection(pSpatialReference, GeometryType);
            IFeatureClass pFeatureClass = CreateNewFeatureClass(pWS, featureClassName, pFields, esriFeatureType.esriFTSimple);
            AddGeometryColToFeatureClass(pGeometryCollection, pFeatureClass);
            MessageBox.Show("The shapefile: "+featureClassName+" has been saved in the same directory");
        }

        

        private void GetWorkPath() //get the path of the first layer in the TOC table
        {
            IMap pMap = axTOCControl1.ActiveView.FocusMap;
            try
            {
                ILayer pLayer = pMap.Layer[0];
                // get first layer of the Map           
                IFeatureLayer pFLayer = pLayer as IFeatureLayer;
                IFeatureClass pFC = pFLayer.FeatureClass;
                /// get dataset of the standalone feature class
                IDataset pDataset = pFC as IDataset;
                IWorkspace pWS = pDataset.Workspace;
                this.strFilePath = pWS.PathName;
                // get selected layers
                object legendGroup = new object();
                object index = new object();
                IBasicMap map = new MapClass();
                ILayer layer = new FeatureLayerClass();
                esriTOCControlItem item = new esriTOCControlItem();
                axTOCControl1.GetSelectedItem(ref item, ref map, ref layer, ref legendGroup, ref index);
                if (item == esriTOCControlItem.esriTOCControlItemLayer) 
                {
                    MessageBox.Show("Selected layer name: "+ layer.Name);
                } 
            }
            catch
            {
                MessageBox.Show("Add at least one layer to the map");              
            }
        }

        private void GetSpatialReference() 
        {
            try
            {
                IMap pMap = axTOCControl1.ActiveView.FocusMap;
                ILayer pLayer = pMap.Layer[0];
                // get first layer of the Map           
                IFeatureLayer pFLayer = pLayer as IFeatureLayer;
                this.pSpatialRef = pMap.SpatialReference;
            }
            catch
            {
                MessageBox.Show("Add at least one layer to the map");
            }
        }

        private void removeData() 
        {
            IMap map = axTOCControl1.ActiveView.FocusMap;
            ILayer layer = null;
            for (int i = 0; i < map.LayerCount; i++) 
            {
                if (map.get_Layer(i).Name == layerName) 
                {
                    layer = map.get_Layer(i);
                }
            }
               
            axTOCControl1.ActiveView.FocusMap.DeleteLayer(layer);
            //axTOCControl1.Update();
            axTOCControl1.ActiveView.Refresh();
            axMapControl1.ActiveView.Refresh();
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            GetWorkPath();
            IWorkspace ws = OpenShapfileWorkspace(this.strFilePath);
            IFeatureWorkspace pFWS = ws as IFeatureWorkspace;
            
            if (strFileName == null || strFileName != "UK_CoastlineSplit") 
            {
                MessageBox.Show("Please select the CoastlineSplit in TOC");
                return;
            }
            IFeatureClass fcCoastLineSplit = pFWS.OpenFeatureClass(strFileName);
            //IQueryFilter queryFilter = new QueryFilterClass();
            ICursor cursor = (ICursor)fcCoastLineSplit.Search(null, false);
            //Get statistic of length field and get min and max values
            IDataStatistics dataStatistic = new DataStatisticsClass();
            int fieldindex = fcCoastLineSplit.Fields.FindField("length");
            if (fieldindex == -1)
            {
                MessageBox.Show("Please add 'length' field!");
                return;
            }
            dataStatistic.Field = "length";
            dataStatistic.Cursor = cursor;
            ESRI.ArcGIS.esriSystem.IStatisticsResults staResult = dataStatistic.Statistics;
            // round the result in 4 decimals
            maxLength = Math.Round(staResult.Maximum, 4).ToString();
            minLength = Math.Round(staResult.Minimum, 4).ToString();
            meanLength = Math.Round(staResult.Mean, 4).ToString();
            textBox2.Text = minLength;
            textBox3.Text = maxLength;
            textBox4.Text = meanLength;
            // If minimum length is smaller than 1 meter, set the minimum to the minimum value greater than 1 meter
            if (staResult.Minimum <= 1) 
            {
                if (MessageBox.Show("The minimum length is shorter than 1 meter, change the minimum value greater than 1?", "Note", MessageBoxButtons.YesNo) == DialogResult.Yes) 
                {
                    IQueryFilter queryFilter = new QueryFilterClass();
                    queryFilter.WhereClause = "length > 1";
                    ISelectionSet pSelectionSet = fcCoastLineSplit.Select(queryFilter, esriSelectionType.esriSelectionTypeHybrid, esriSelectionOption.esriSelectionOptionNormal, null);
                    IFeatureCursor pFCursor;
                    ICursor pCursor;
                    pSelectionSet.Search(null, true, out pCursor);
                    pFCursor = pCursor as IFeatureCursor;
                    dataStatistic = new DataStatisticsClass();
                    dataStatistic.Field = "length";
                    dataStatistic.Cursor = pCursor;
                    staResult = dataStatistic.Statistics;
                    minLength = Math.Round(staResult.Minimum, 4).ToString();
                    textBox2.Text = minLength;
                }
            }
        }


        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            removeData();
        }

        private void axTOCControl1_OnMouseDown(object sender, ITOCControlEvents_OnMouseDownEvent e)
        {
            axTOCControl1.ContextMenuStrip = contextMenuStrip1;
            //judge the type of item selected in the TOC 
            IBasicMap map = new MapClass();
            ILayer layer = null; 
            System.Object other = null;
            System.Object index = null;
            esriTOCControlItem item = esriTOCControlItem.esriTOCControlItemNone;
            axTOCControl1.HitTest(e.x, e.y, ref item, ref map, ref layer, ref other, ref index);
            if (e.button == 2 && item == esriTOCControlItem.esriTOCControlItemLayer) 
            {
                //transfer the screen point location to form location to popup the contextmenu
                System.Drawing.Point p = new System.Drawing.Point();
                p.X = e.x;
                p.Y = e.y;
                p = this.axTOCControl1.PointToScreen(p);
                this.contextMenuStrip1.Show(p);
                layerName = layer.Name;
            }
            if (e.button == 1 && item == esriTOCControlItem.esriTOCControlItemLayer) 
            {
                strFileName = layer.Name;
            }
        }
        private void initialCoastLine() 
        {
            GetWorkPath();
            if (strFilePath == null)
            {
                return;
            }
            IWorkspace ws2 = OpenShapfileWorkspace(strFilePath);
            IFeatureWorkspace pFWS2 = ws2 as IFeatureWorkspace;

            //Only one feature in the featureclass
            IFeatureClass fcCoastLine = pFWS2.OpenFeatureClass("UK_Coastline");
            IFeature fCoastLine = fcCoastLine.GetFeature(0);
            //NOTE!!! Use SHAPECOPY NOT SHAPE!!!
            //Initialize coastline;
            Coastline = fCoastLine.ShapeCopy as IPolyline;
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "") 
            {
                MessageBox.Show("Please input the yardstick length");
                return;
            }
            
            //Start main functions...
            GetWorkPath();
            if (strFilePath == null)
            {
                return;
            }
            IWorkspace ws = OpenShapfileWorkspace(strFilePath);
            IFeatureWorkspace pFWS = ws as IFeatureWorkspace;

            //Only one feature in the featureclass
            IFeatureClass fcCoastLine= pFWS.OpenFeatureClass("UK_Coastline");
            IFeature fCoastLine = fcCoastLine.GetFeature(0);
            IPolyline polylineCoast = new PolylineClass();
            polylineCoast=fCoastLine.ShapeCopy as IPolyline;
            //NOTE!!! Use SHAPECOPY NOT SHAPE!!!
            //Initialize coastline;
            Coastline = fCoastLine.ShapeCopy as IPolyline;

            IFeatureClass fcVertices = pFWS.OpenFeatureClass("UK_CoastlineVertices");

            ICircularArc startCircle;
            startCenterPoint = fcVertices.GetFeature(0).Shape as IPoint;
            MessageBox.Show("x: "+startCenterPoint.X.ToString()+" y: "+startCenterPoint.Y.ToString());

            yardStickLength = textBox1.Text;
            radius = double.Parse(yardStickLength);

            startCircle = CreateCircleArc(startCenterPoint, radius, false);
            //Initialize startcirlcePolygon
            startCirclePolygon = Arc2Polygon(startCircle);
            //Get first Intersct points
            IPointCollection firstPointColl = IntersectPointColl(startCircle,polylineCoast);
            MessageBox.Show("first intersect point: "+firstPointColl.PointCount.ToString());

            //ICircularArc movingCircle = CreateCircleArc(firstPointColl.get_Point(0), radius, false);
            //IPointCollection newPointColl = IntersectPointColl(movingCircle, polylineCoast);
            //pGeometryCollection = newPointColl as IGeometryCollection;
            //CreateShpfile("2ndPoint2", pGeometryCollection);
            //判断交点个数
            if (firstPointColl.PointCount != 2)
            {
                initialCenterPoints = GetCorrectPointInitial(SplitPolyline(polylineCoast, firstPointColl, startCenterPoint), startCenterPoint);
            }
            else 
            {
                IPoint pt1 = firstPointColl.get_Point(0) as IPoint;
                IPoint pt2 = firstPointColl.get_Point(1) as IPoint;
                List<IPoint> ptlist = new List<IPoint>();
                ptlist.Add(pt1);
                ptlist.Add(pt2);
                initialCenterPoints = ptlist;
               
            }
            //确定第二个圆心位置
            if (initialCenterPoints.Count == 2)
            {
                if (initialCenterPoints[0].X > initialCenterPoints[1].X)
                {
                    secondCenterPoint = initialCenterPoints[0];
                }
                else 
                {
                    secondCenterPoint = initialCenterPoints[1];
                }
                MessageBox.Show("The second circle center is X "+secondCenterPoint.X+" Y "+secondCenterPoint.Y);
            }
            else 
            {
                MessageBox.Show("Initialize failed");
            }
        }

        private List<IPoint> SplitPolyline(IPolyline polyline, IPointCollection intersectpointsColl, IPoint presentCP) 
        {
            IEnumVertex pEnumVertex = intersectpointsColl.EnumVertices;
            //IPolycurve2 has SplitAtPoints

            IPolycurve2 pPolyCurve = polyline as IPolycurve2;           
            pPolyCurve.SplitAtPoints(pEnumVertex,false,true,-1);
            IGeometryCollection geoColl = pPolyCurve as IGeometryCollection;
            //MessageBox.Show(geoColl.GeometryCount.ToString());
            List<IPoint> ptlist = new List<IPoint>();
            // The results are pathclass
            IPath resultPath;
            for (int i = 0; i < geoColl.GeometryCount; i++) 
            {
                object obj = Type.Missing;
                resultPath = new PathClass();
                resultPath = (IPath)geoColl.get_Geometry(i);
                IGeometryCollection lineColl = new PolylineClass();
                lineColl.AddGeometry(resultPath, ref obj, ref obj);
                IPolyline line = (IPolyline)lineColl;                
                IRelationalOperator pRelOperator = (IRelationalOperator)line;
                if (pRelOperator.Touches(presentCP)||pRelOperator.Contains(presentCP))
                {
                    IPoint temPT1 = resultPath.FromPoint;
                    IPoint temPT2 = resultPath.ToPoint;
                    //pGeometryCollection.AddGeometry(temPT1);
                    //pGeometryCollection.AddGeometry(temPT2);
                    ptlist.Add(temPT1);
                    ptlist.Add(temPT2);                    
                }
            }
            return ptlist;
        }

        private List<IPoint> GetCorrectPointInitial(List<IPoint> splitresult, IPoint lastCP) 
        {
            List<IPoint> correctPT = new List<IPoint>();
            IRelationalOperator rel=lastCP as IRelationalOperator;
            foreach (IPoint pt in splitresult) 
            {
                if (!rel.Equals(pt))
                {
                    correctPT.Add(pt);
                }
            }
            return correctPT;
        }

        private List<IPoint> GetCorrectPoint(List<IPoint> splitresult, IPoint lastCP)
        {
            List<IPoint> correctPT = new List<IPoint>();
            IRelationalOperator rel = lastCP as IRelationalOperator;
            foreach (IPoint pt in splitresult)
            {
                if (!rel.Equals(pt))
                {
                    correctPT.Add(pt);
                }
            }
            if (correctPT.Count == 2) 
            {
                IProximityOperator prox = lastCP as IProximityOperator;
                double Dis1 = prox.ReturnDistance(correctPT[0]);
                double Dis2 = prox.ReturnDistance(correctPT[1]);
                if (Dis1 > Dis2)
                {
                    correctPT.Remove(correctPT[1]);
                }
                else 
                {
                    correctPT.Remove(correctPT[0]);
                }
            }
            return correctPT;
        }

        private bool IsNewPoint(IPoint Point, IPolygon lastcircle) 
        {
            IRelationalOperator pRelOperator = (IRelationalOperator)lastcircle;
            if (pRelOperator.Contains(Point))
            {
                return false;
            }
            else 
            {
                return true;
            }
        }

        private IPointCollection AddVerticePointToCollection(IPolyline coastline) 
        {
            //IPointCollection4
            IPointCollection4 verticePointColl = (IPointCollection4)coastline;         
            return verticePointColl;
        }

        private ICircularArc CreateCircleArc(IPoint point, double radius, bool isCounterCW) 
        {
            ICircularArc circularArc = new CircularArcClass();
            IConstructCircularArc2 constructCircularArc = circularArc as IConstructCircularArc2;
            constructCircularArc.ConstructCircle(point,radius,isCounterCW);
            return circularArc;
        }

        private IPointCollection IntersectPointColl(ICircularArc circularArc, IPolyline coastline) 
        {
            IPolyline circlePolyline = Arc2Polyline(circularArc);
            ITopologicalOperator topOperator = (ITopologicalOperator)circlePolyline;
            IGeometry interGeometry = (IGeometry)topOperator.Intersect(coastline,esriGeometryDimension.esriGeometry0Dimension);
            IPointCollection interPointColl = (IPointCollection)interGeometry;
            return interPointColl;
        }

        private IPolyline Arc2Polyline(ICircularArc Arc) 
        {
            object obj = Type.Missing;
            ISegmentCollection segC = new PolylineClass();
            segC.AddSegment((ISegment)Arc, ref obj, ref obj);
            IPolyline circlePolyline = (IPolyline)segC;
            // mirror spatial referrence
            ISpatialReference spr = Arc.SpatialReference;
            circlePolyline.SpatialReference = spr;
            return circlePolyline;
        }

        private IPolyline Seg2Polyline(ISegment Seg) 
        {
            object obj = Type.Missing;
            ISegmentCollection segC = new PolylineClass();
            segC.AddSegment((ISegment)Seg, ref obj, ref obj);
            IPolyline SegPolyline = (IPolyline)segC;
            // mirror spatial referrence
            ISpatialReference spr = Seg.SpatialReference;
            SegPolyline.SpatialReference = spr;
            return SegPolyline;
        }

        private IPolygon Arc2Polygon(ICircularArc Arc) 
        {
            ISegmentCollection pSegCol = new RingClass();
            object obj = Type.Missing;
            pSegCol.AddSegment((ISegment)Arc,ref obj, ref obj);
            //enclose ring make it valid
            IRing pRing;
            pRing = pSegCol as IRing;
            pRing.Close();
            IGeometryCollection pPolygonCol = new PolygonClass();
            pPolygonCol.AddGeometry(pRing,ref obj,ref obj);
            IPolygon circlePolygon=(IPolygon)pPolygonCol;
            // mirror spatial referrence
            ISpatialReference spr = Arc.SpatialReference;
            circlePolygon.SpatialReference = spr;
            return circlePolygon;
        }


        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (secondCenterPoint == null) 
            {
                MessageBox.Show("Initialize first!");
                return;
            }
            //add startvertice
            pGeometryCollection.AddGeometry(startCenterPoint);
            //add 2nd
            pGeometryCollection.AddGeometry(secondCenterPoint);
            //3rd
            tempCenterPoint = CreateNewCenterPoint(secondCenterPoint,startCenterPoint);
            pGeometryCollection.AddGeometry(tempCenterPoint);
            //4th
            lastCenterPoint = secondCenterPoint;
            presentCenterPoint = tempCenterPoint;
            tempCenterPoint = CreateNewCenterPoint(presentCenterPoint, lastCenterPoint);
            pGeometryCollection.AddGeometry(tempCenterPoint);
            ////5th
            //lastCenterPoint = presentCenterPoint;
            //presentCenterPoint = tempCenterPoint;
            //tempCenterPoint = CreateNewCenterPoint(presentCenterPoint, lastCenterPoint);

            if (startCirclePolygon == null)
            {
                MessageBox.Show("Initialize the startCirclePolygon");
                return;
            }
            IRelationalOperator relCircle = startCirclePolygon as IRelationalOperator;
            while (!relCircle.Contains(tempCenterPoint))
            {
                lastCenterPoint = presentCenterPoint;
                presentCenterPoint = tempCenterPoint;
                tempCenterPoint = CreateNewCenterPoint(presentCenterPoint, lastCenterPoint);
                pGeometryCollection.AddGeometry(tempCenterPoint);
            }
            if (MessageBox.Show("Create vertices shapefile?", "Note", MessageBoxButtons.YesNo) == DialogResult.Yes) 
            {
                CreateShpfile("YardstickVertices", pGeometryCollection);
            }         

            /*...Test Paths...
            presentCircle = CreateCircleArc(tempCenterPoint, radius, false);
            IPointCollection interColl = IntersectPointColl(presentCircle, this.Coastline);
            List<IPoint> interList = new List<IPoint>();
            for (int i = 0; i < interColl.PointCount; i++)
            {
                interList.Add(interColl.get_Point(i));
            }
            initialCoastLine();
            IEnumVertex pEnumVertex = interColl.EnumVertices;
            IPolycurve2 pPolyCurve = this.Coastline as IPolycurve2;
            pPolyCurve.SplitAtPoints(pEnumVertex, false, true, -1);
            IGeometryCollection geoColl = pPolyCurve as IGeometryCollection;
            MessageBox.Show(geoColl.GeometryCount.ToString());
            List<IPoint> ptlist = new List<IPoint>();
            // The results are pathclass
            IPath resultPath;
            for (int i = 0; i < geoColl.GeometryCount; i++)
            {
                object obj = Type.Missing;
                resultPath = new PathClass();
                resultPath = (IPath)geoColl.get_Geometry(i);
                IGeometryCollection lineColl = new PolylineClass();
                lineColl.AddGeometry(resultPath, ref obj, ref obj);
                IPolyline line = (IPolyline)lineColl;
                pGeometryCollection.AddGeometry(line);
                IRelationalOperator pRelOperator = (IRelationalOperator)line;
                if (pRelOperator.Touches(tempCenterPoint))
                {
                    IPoint temPT1 = resultPath.FromPoint;
                    IPoint temPT2 = resultPath.ToPoint;
                    //pGeometryCollection.AddGeometry(temPT1);
                    //pGeometryCollection.AddGeometry(temPT2);
                    ptlist.Add(temPT1);
                    ptlist.Add(temPT2);
                }
            }
            CreateShpfile("Path9", pGeometryCollection);
            */

            //...Create yardstick using the generated vertices...
            if (MessageBox.Show("Finish generating vertices, create yardsticks?", "Note", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                pGeometryCollection = GenerateYardSticks(pGeometryCollection);
                CreateShpfile("Yardsticks", pGeometryCollection);
            }
            else 
            {
                return;
            }
            
        }

        private IGeometryCollection GenerateYardSticks(IGeometryCollection verticesColl) 
        {
            IPoint startPT=new PointClass();
            IPoint endPT = new PointClass();
            IGeometryCollection yardstickColl=new GeometryBagClass();
            ISegmentCollection yardsegColl = new PolylineClass();            
            object obj = Type.Missing;
            for (int i = 0; i < verticesColl.GeometryCount; i++) 
            {
                if (i < verticesColl.GeometryCount - 1)
                {
                    ILine pLine = new LineClass();
                    
                    startPT = verticesColl.get_Geometry(i) as IPoint;
                    endPT = verticesColl.get_Geometry(i + 1) as IPoint;
                    pLine.PutCoords(startPT, endPT);
                    yardsegColl.AddSegment(pLine as ISegment, ref obj, ref obj);                                   
                }
                else 
                {
                    MessageBox.Show(i+" yardsticks generated");
                }
            }
            IPolyline pPolyline = (IPolyline)yardsegColl; 
            yardstickColl.AddGeometry(pPolyline, ref obj, ref obj);
            return yardstickColl;
        }

        private IPoint CreateNewCenterPoint(IPoint presentCP, IPoint lastCP) 
        {
            initialCoastLine();
            IPoint newCP = new PointClass();
            IPolyline line=new PolylineClass();
            line = Coastline;
            ICircularArc presentCA = CreateCircleArc(presentCP,radius,false);
            IPointCollection intersectPtColl = IntersectPointColl(presentCA, line);
            List<IPoint> intersectPtlist = new List<IPoint>();
            List<IPoint> splitResult = new List<IPoint>();
            List<IPoint> correctPtlist = new List<IPoint>();
            for (int i = 0; i < intersectPtColl.PointCount; i++) 
            {
                intersectPtlist.Add(intersectPtColl.get_Point(i));
            }
            if (intersectPtlist.Count == 2)
            {
                correctPtlist = GetCorrectPoint(intersectPtlist, lastCP);
            }
            else 
            {
                splitResult = SplitPolyline(line,intersectPtColl,presentCP);
                //MessageBox.Show(splitResult.Count.ToString());
                correctPtlist = GetCorrectPoint(splitResult, lastCP);
            }

            if (correctPtlist.Count == 1)
            {
                newCP = correctPtlist[0];
            }
            else 
            {
                MessageBox.Show("Wrong result");
            }
            return newCP;
        }

        

        
        



      

       
        
        


       
        
    }
}
