﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MortonCode
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ESRI.ArcGIS.RuntimeManager.BindLicense(ESRI.ArcGIS.ProductCode.Desktop);
            //ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            //Application.Run(new Form1());
            Application.Run(new FormCoastLine());
        }
    }
}
