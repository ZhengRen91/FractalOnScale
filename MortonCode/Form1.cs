using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using System.Windows.Forms.DataVisualization.Charting;
using System.Threading.Tasks; // multitask process to imporve the efficiency when loading the DEM data


namespace MortonCode
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public int row;
        public int col;
        public int bitsize;
        public int Morton;
        private string strMortonPath;
        private string strMortonFileName;
        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != null)
            {
                row = int.Parse(textBox1.Text.ToString());
            }
            else 
            {
                MessageBox.Show("Please input row");
            }
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox2.Text != null)
            {
                col= int.Parse(textBox2.Text.ToString());
            }
            else
            {
                MessageBox.Show("Please input column");
            }
        }

        public int GetMorton(int r, int c,int bs) 
        {
            string rbit =Convert.ToString(r,2);//十进制数字转二进制字符串            
            string cbit = Convert.ToString(c,2);                      
            //bs = 8;
            if (rbit.Length < bs) 
            {
                int bitlength = rbit.Length;// rbit will change in the loop
                for (int i = 0; i < bs - bitlength; i++)
                {
                    rbit = '0'+rbit;
                }                
            }
            if (cbit.Length < bs)//二进制补位
            {
                int bitlength = cbit.Length;
                for (int i = 0; i < bs - bitlength; i++)
                {
                    cbit = '0'+cbit;
                }
            }
            char[] rArray = rbit.ToCharArray();
            char[] cArray = cbit.ToCharArray();
            char[] mArray = new char[rArray.Length + cArray.Length];
            for (int i = 0; i < rArray.Length; i++)
            {
                mArray[i * 2] = rArray[i];
            }
            for (int i = 0; i < cArray.Length; i++)
            {
                mArray[i * 2 + 1] = cArray[i];
            }

            string m=mArray.ToString();
            string str = new string(mArray);
            //MessageBox.Show("BinaryString:["+str+"]");
            Morton = Convert.ToInt32(str, 2);
            return Morton;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            bitsize = int.Parse(listBox1.SelectedItem.ToString());
            textBox3.Text = GetMorton(row, col, bitsize).ToString();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                bitsize = int.Parse(listBox1.SelectedItem.ToString());
                folderBrowserDialog1.Description = "Specify the folder to save the MortonCode file.";
                DialogResult resultMorton = folderBrowserDialog1.ShowDialog();
                if (resultMorton == DialogResult.OK)
                {
                    strMortonPath = folderBrowserDialog1.SelectedPath;
                }
                this.strMortonFileName = Interaction.InputBox("Specify the textfile name", "FileName", "MortonCode.txt", 0, 0);
                MessageBox.Show(strMortonPath + "\\" + strMortonFileName);
                System.IO.StreamWriter sw = new System.IO.StreamWriter(strMortonPath + "\\" + strMortonFileName, true);
                sw.WriteLine("MortonCode:" + Math.Pow(2, bitsize) + "x" + Math.Pow(2, bitsize));
                //write morton code according to the chosen raster size 
                for (int i = 0; i < Math.Pow(2, bitsize); i++)
                {
                    for (int j = 0; j < Math.Pow(2, bitsize); j++)
                    {
                        sw.Write(GetMorton(i, j, bitsize) + " ");
                    }
                    sw.WriteLine("");
                }
                sw.Close();
                MessageBox.Show("Text file saved!");
            }
            else 
            { 
                MessageBox.Show("please choose raster size");              
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                bitsize = int.Parse(listBox1.SelectedItem.ToString());
                folderBrowserDialog1.Description = "Specify the folder to save the MortonCode file.";
                DialogResult resultMorton = folderBrowserDialog1.ShowDialog();
                if (resultMorton == DialogResult.OK)
                {
                    strMortonPath = folderBrowserDialog1.SelectedPath;
                }
                this.strMortonFileName = Interaction.InputBox("Specify the textfile name", "FileName", "MortonSeries.txt", 0, 0);
                MessageBox.Show(strMortonPath + "\\" + strMortonFileName);
                System.IO.StreamWriter sw = new System.IO.StreamWriter(strMortonPath + "\\" + strMortonFileName, true);
                int maxsize = bitsize;
                for (int k=0; bitsize > 0; bitsize--,k++) 
                {
                    sw.WriteLine("MortonCode:" + Math.Pow(2, bitsize) + "x" + Math.Pow(2, bitsize));
                    //write morton code according to the chosen raster size 
                    for (int i = 0 ; i < Math.Pow(2, maxsize); )
                    {
                        for (int j = 0; j < Math.Pow(2, maxsize); )
                        {
                            sw.Write(GetMorton(i, j, maxsize) + " ");
                            j = j + (int)(Math.Pow(2,k));
                        }
                        i = i + (int)(Math.Pow(2, k));
                    }
                    sw.WriteLine("");
                }
                sw.Close();
                MessageBox.Show("Text file saved!");
            }
            else
            {
                MessageBox.Show("please choose raster size");
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            xMap = 474693.1;
            yMap = 4474435.1;
            //Get the column and row by giving x,y coordinates in a map space.
            //col = raster.ToPixelColumn(xMap);
            //row = raster.ToPixelRow(yMap);
            IRaster2 ras=MapXY2RowCol(xMap,yMap,"slope8x8");
            MessageBox.Show("Row:"+row.ToString() + " Col:" + col.ToString());
            //Get the value at a given band.            
            double pixelValue = ras.GetPixelValue(0, col, row);
            MessageBox.Show("Row:" + row.ToString() + " Col:" + col.ToString() + " value: " + pixelValue.ToString());            
        }
        
        public IRaster2 MapXY2RowCol(double x, double y,string rasname) 
        {
            string filepath = @"D:\study\ao\MortonCode\slopedata";
            IRasterDataset rasds = OpenRaterDS(filepath,rasname);
            IRaster2 ras = rasds.CreateDefaultRaster() as IRaster2;
            IRasterLayer rasterlayer = new RasterLayerClass();
            rasterlayer.CreateFromDataset(rasds);
            bitsize = rasterlayer.RowCount;
            col = ras.ToPixelColumn(x);
            row = ras.ToPixelRow(y);
            return ras;
        }

        public IRasterDataset OpenRaterDS(string path, string rastername) 
        {
            IWorkspaceFactory pWSF = new RasterWorkspaceFactory();
            IRasterWorkspace pRWS = pWSF.OpenFromFile(path, 0) as IRasterWorkspace;
            if (pRWS == null)
            {
                MessageBox.Show("Could not open raster workspace");
                return null;
            }
            IRasterDataset rasterData = pRWS.OpenRasterDataset(rastername);
            if (rasterData == null)
            {
                MessageBox.Show("Could not open raster dataset");
                return null;
            }
            return rasterData;
        }

        public Dictionary<int, float> rasterdic1;
        public Dictionary<int, float> rasterdic2;
        public Dictionary<int, float> rasterdic3;
        public Dictionary<int, float> rasterdic4;
        public Dictionary<int, float> rasterdic5;
        public Dictionary<int, float> rasterdic6;
        public Dictionary<int, float> rasterdic7;
        public Dictionary<int, float> rasterdic8;
        public Dictionary<int, float> rasterdic9;
        public Dictionary<int, float> rasterdic10;
      
        public List<int> rasterkey;
        public List<float> rastervalue;
        public List<List<float>> resultseries = new List<List<float>>();
       

        double xMap;
        double yMap;

        public void LoadDEM(string rastername, ref Dictionary<int, float>resultdic) // 加ref防止null rasterdic
        {
            resultdic = new Dictionary<int, float>();
            rasterkey = new List<int>();
            rastervalue = new List<float>();
            IWorkspaceFactory pWSF = new RasterWorkspaceFactory();
            string fileName = @"D:\study\ao\MortonCode\slopedata";
            IRasterWorkspace pRWS = pWSF.OpenFromFile(fileName, 0) as IRasterWorkspace;
            if (pRWS == null)
            {
                MessageBox.Show("Could not open raster workspace");
                return;
            }
            IRasterDataset rasterData = pRWS.OpenRasterDataset(rastername);
            if (rasterData == null)
            {
                MessageBox.Show("Could not open raster dataset");
                return;
            }
            IRaster2 raster = rasterData.CreateDefaultRaster() as IRaster2;
            IRasterLayer rasterlayer = new RasterLayerClass();
            rasterlayer.CreateFromDataset(rasterData);
            int col;
            int row;
            float pixelValue;
            int maxrow = rasterlayer.RowCount;
            int maxcol = rasterlayer.ColumnCount;
            for (row = 0; row < maxrow; row++)
            {
                for (col = 0; col < maxcol; col++)
                {
                    rasterkey.Add(GetMorton(row, col, maxrow));
                    pixelValue = raster.GetPixelValue(0, col, row);
                    rastervalue.Add(pixelValue);
                }
            }
            for (int i = 0; i < maxcol * maxrow; i++)
            {
                resultdic.Add(rasterkey[i], rastervalue[i]);
            }
            //foreach (KeyValuePair<int, double> kvp in rasterdic1)
            //{
            //    MessageBox.Show("MD: " + kvp.Key + "  Slope: " + kvp.Value);
            //}
            MessageBox.Show("Number of pixels: " + resultdic.Count);
            
            //rasterdic1_sort = (from entry in rasterdic1
            //orderby entry.Key ascending
            //select entry).ToDictionary(pair => pair.Key, pair => pair.Value);
            //MessageBox.Show("Number of sorted pixel: " + rasterdic1_sort.Count);         
            //MessageBox.Show(rasterdic1_sort.Keys.ToString());
        }

        public int RasterNo=0;
        private void button7_Click(object sender, EventArgs e)
        {
           // LoadDEM("slope1024", ref rasterdic10); //processing time:15 min
            LoadDEM("slope512", ref rasterdic1); RasterNo++;
            LoadDEM("slope256", ref rasterdic2); RasterNo++;
            LoadDEM("slope128", ref rasterdic3); RasterNo++;
            LoadDEM("slope64", ref rasterdic4); RasterNo++;
            LoadDEM("slope32", ref rasterdic5); RasterNo++;
            LoadDEM("slope16", ref rasterdic6); RasterNo++;
            LoadDEM("slope8", ref rasterdic7); RasterNo++;
            LoadDEM("slope4", ref rasterdic8); RasterNo++;
            LoadDEM("slope2", ref rasterdic9); RasterNo++;
            MessageBox.Show("Load DEMs finished!");
        }


        private void button8_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("Make sure at least one pixel has been chosen","Reminder",MessageBoxButtons.YesNo);
            if (dr == DialogResult.No) 
            {
                return;
            }
            //int MD = int.Parse(Interaction.InputBox("Input MD code: "));           
            //int MD1 = GetMorton(row,col,bitsize);
            //MessageBox.Show("MD: " + MD1.ToString());
            //int MD2 = (int)Math.Floor(MD1 / Math.Pow(4, 1));
            //MessageBox.Show("MD2: "+MD2.ToString());
            //int MD3 = (int)Math.Floor(MD1 / Math.Pow(4, 2));
            //MessageBox.Show("MD3: " + MD3.ToString());
           
            if (RasterNo!=0)
            {
                int[] MD = new int[RasterNo];
                MD[0] = GetMorton(row, col, bitsize);
                for (int i = 1; i < RasterNo; i++)
                {
                    MD[i] = (int)Math.Floor(MD[0] / Math.Pow(4, i));
                }
                List<float> resultarray=new List<float>();
                resultarray.Add(rasterdic1[MD[0]]);
                resultarray.Add(rasterdic2[MD[1]]);
                resultarray.Add(rasterdic3[MD[2]]);
                resultarray.Add(rasterdic4[MD[3]]);
                resultarray.Add(rasterdic5[MD[4]]);
                resultarray.Add(rasterdic6[MD[5]]);
                resultarray.Add(rasterdic7[MD[6]]);
                resultarray.Add(rasterdic8[MD[7]]);
                resultarray.Add(rasterdic9[MD[8]]);
                //resultseries = new List<List<float>>();
                resultseries.Add(resultarray);

                string result = "";
                for (int j = 0; j < resultseries.Count; j++) 
                {
                    for (int k = 0;k < resultseries[j].Count-1;k++)
                    {
                        result = result + resultseries[j][k].ToString() + ", ";
                    }
                    result = result + resultseries[j][resultseries[j].Count-1].ToString()+"\n";
                }
                MessageBox.Show("Result series:" +"\n"+ result);
            }
            else 
            {
                MessageBox.Show("Load DEMs first");
                return;
            }
            
        }

        private void axMapControl1_OnMouseDown(object sender, ESRI.ArcGIS.Controls.IMapControlEvents2_OnMouseDownEvent e)
        {
            xMap = e.mapX;
            yMap = e.mapY;
            string selectedraster = "slope";
            if (listBox1.SelectedItem== null)
            {
                selectedraster = selectedraster + "512";//defaut finest raster
            }
            else 
            {
                selectedraster = selectedraster + Math.Pow(2, double.Parse(listBox1.SelectedItem.ToString())).ToString();
            }
            IRaster2 ras2 = MapXY2RowCol(xMap, yMap,selectedraster);//FInest Resolution Ratername
            if (ras2.GetPixelValue(0,col,row) == null) 
            {
                MessageBox.Show("Please open map");
                return;
            }
            float pixelvalue = ras2.GetPixelValue(0, col, row);
            if (e.button == 2)
            {
                MessageBox.Show("x:" + xMap + " y:" + yMap + "\n" + "row:" + row + " col:" + col + "value:" + pixelvalue);
            }            
        }

        private void button9_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = @"D:\study\ao\MortonCode\slopedata";
            saveFileDialog1.FileName = "SlopeMatrix.txt";
            saveFileDialog1.Filter = "SlopeResult（*.txt）|*.txt";
            saveFileDialog1.FilterIndex = 1;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK) 
            {
                string txtPath = saveFileDialog1.FileName.ToString();
                System.IO.StreamWriter sw = new System.IO.StreamWriter(txtPath, true);
                string result = "";
                sw.WriteLine("Result series:");
                for (int j = 0; j < resultseries.Count; j++)
                {
                    for (int k = 0; k < resultseries[j].Count - 1; k++)
                    {
                        result = result + resultseries[j][k].ToString() + ", ";
                    }
                    result = result + resultseries[j][resultseries[j].Count - 1].ToString();
                    sw.WriteLine(result);
                    result = "";
                }
                MessageBox.Show("Slope Matrix saved!");
                sw.Close();
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            foreach (var series in chart1.Series) 
            {
                series.Points.Clear();
            }
            for (int i = 0; i < resultseries.Count; i++) 
            {
                Series s1 = new Series();
                s1.ChartType = SeriesChartType.Spline;
                for (int j = 0; j < resultseries[i].Count; j++) 
                {
                    s1.Points.AddXY(j+1,resultseries[i][j]);
                }
                chart1.Series.Add(s1);
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) 
            {
                MessageBox.Show("Choose Coarse Raster size first");
            }            
           DialogResult dr = MessageBox.Show("Make sure at least one pixel has been chosen", "Reminder", MessageBoxButtons.YesNo);
            if (dr == DialogResult.No)
            {
                return;
            }
            if (RasterNo != 0)
            {
                int MDpresent = GetMorton(row,col,bitsize);
                int MDstart;
                int MDend;
                int FinestSize = 9;
                int PresentSize = (int)Math.Log(bitsize,2);
                MDstart = (int)(MDpresent * Math.Pow(4, FinestSize - PresentSize));
                MDend = (int)((MDpresent + 1) * Math.Pow(4, FinestSize -PresentSize)-1);
                while (MDstart <= MDend) 
                {
                    int[] MD = new int[RasterNo];
                    MD[0] = MDstart;
                    for (int j = 1; j < RasterNo; j++)
                    {
                        MD[j] = (int)Math.Floor(MD[0] / Math.Pow(4, j));
                    }
                    List<float> resultarray = new List<float>();
                    resultarray.Add(rasterdic1[MD[0]]);
                    resultarray.Add(rasterdic2[MD[1]]);
                    resultarray.Add(rasterdic3[MD[2]]);
                    resultarray.Add(rasterdic4[MD[3]]);
                    resultarray.Add(rasterdic5[MD[4]]);
                    resultarray.Add(rasterdic6[MD[5]]);
                    resultarray.Add(rasterdic7[MD[6]]);
                    resultarray.Add(rasterdic8[MD[7]]);
                    resultarray.Add(rasterdic9[MD[8]]);
                    //resultseries = new List<List<float>>();
                    resultseries.Add(resultarray);
                    MDstart++;
                }
                
                //string result = "";
                //for (int j = 0; j < resultseries.Count; j++)
                //{
                //    for (int k = 0; k < resultseries[j].Count - 1; k++)
                //    {
                //        result = result + resultseries[j][k].ToString() + ", ";
                //    }
                //    result = result + resultseries[j][resultseries[j].Count - 1].ToString() + "\n";
                //}
                MessageBox.Show("Number of result series: " + resultseries.Count);
            }
            else
            {
                MessageBox.Show("Load DEMs first");
                return;
            }


        }

        private void button12_Click(object sender, EventArgs e)
        {
            resultseries = new List<List<float>>();
        }



        
    }
}
