﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Xml;
using System.Diagnostics;
using ANT.Resources;

namespace ANT._04_SVM
{
    public class _04_SupportVectorRegression : GH_Component
    {
        string VersionDate = "9-6-2017"+"\n";
        string Version = "0.1alpha" + "\n";
        string CompName = "SupportVectorRegression" + "\n"; 

        // this handles exiting pyton file event. 
        private bool processhasexit = false;

        // This Process starts runnign python file. 
        System.Diagnostics.Process process2 = new System.Diagnostics.Process();

        // XML result file initiating 
        XmlDocument doc = new XmlDocument();


        //Input variables and documentations
        Object[,] Invars;
        string[] Indocs;

        // Output variables and documentation
        string[] Outvars;
        string[] OutDocs;
        

        /// <summary>
        /// Initializes a new instance of the _04_SupportVectorRegression class.
        /// </summary>
        public _04_SupportVectorRegression()
            : base("_04_SupportVectorRegression", "SVR",
                "",
                "ANT", "4|SVM")
        {
            this.Message = CompName + "V "+Version + VersionDate;
            this.Description = "";

            //initiating the process which runs the Python file. 
            process2.StartInfo.FileName = "python.exe";
            process2.StartInfo.Arguments = "doAllWork.py";
            process2.EnableRaisingEvents = true;
            process2.StartInfo.CreateNoWindow = true;
            process2.StartInfo.UseShellExecute = true;
            process2.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            process2.Exited += process_Exited;

        }

        public override Grasshopper.Kernel.GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {


            Indocs = new string[]{
                "C : float, optional (default=1.0)\nPenalty parameter C of the error term."
                ,"epsilon : float, optional (default=0.1)\nEpsilon in the epsilon-SVR model. It specifies the epsilon-tube within which no penalty is associated in the training loss function with points predicted within a distance epsilon from the actual value"
                ,"kernel : string, optional (default=’rbf’)\nSpecifies the kernel type to be used in the algorithm. It must be one of ‘linear’, ‘poly’, ‘rbf’, ‘sigmoid’, ‘precomputed’ or a callable. If none is given, ‘rbf’ will be used. If a callable is given it is used to precompute the kernel matrix."
                ,"degree : int, optional (default=3)\nDegree of kernel function is significant only in poly, rbf, sigmoid."
                ,"gamma : float, optional (default=0.0)\nKernel coefficient for rbf and poly, if gamma is 0.0 then 1/n_features will be taken."
                ,"coef0 : float, optional (default=0.0)\nindependent term in kernel function. It is only significant in poly/sigmoid."
                ,"shrinking: boolean, optional (default=True) :\nWhether to use the shrinking heuristic."
                ,"tol : float, optional (default=1e-3)\nTolerance for stopping criterion."
                ,"cache_size : float, optional\nSpecify the size of the kernel cache (in MB)."
                ,"verbose : bool, default: False\nEnable verbose output. Note that this setting takes advantage of a per-process runtime setting in libsvm that, if enabled, may not work properly in a multithreaded context."
                ,"max_iter : int, optional (default=-1)\nHard limit on iterations within solver, or -1 for no limit."
            };
            
            // Input Variables : {"variableName", "VaribaleType 's', 'b','f', 'i'", defaultValue}
            Invars = new Object[,] {
                {"C", "f",1.0 },
                {"epsilon", "f" ,1.0 },
                {"kernel", "s" ,"rbf"},
                {"degree","i", 3 },
                {"gamma", "f", 0.0},
                {"coef0", "f", 0.0},
                {"shrinking", "b" , true },
                {"tol", "f", 1e-3},
                {"cache_size","f", 200},
                {"verbose","b", false},
                {"max_iter", "i", -1}};

            initInputParams(Indocs, Invars, pManager);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            Outvars = new string[] { "support_", "support_vectors_", "dual_coef_", "coef_", "intercept_" };
            OutDocs = new string[] {
                "support_ : array-like, shape = [n_SV]\nIndices of support vectors."
                ,"support_vectors_ : array-like, shape = [nSV, n_features]\nSupport vectors."
                ,"dual_coef_ : array, shape = [1, n_SV]\nCoefficients of the support vector in the decision function."
                ,"coef_ : array, shape = [1, n_features]\nWeights assigned to the features (coefficients in the primal problem). This is only available in the case of linear kernel.\ncoef_ is readonly property derived from dual_coef_ and support_vectors_."
                ,"intercept_ : array, shape = [1]\nConstants in decision function."
            };
            initOutputParams(OutDocs, Outvars, pManager);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string defaultDir = "C:\\\\ant\\\\_03_LinearModel\\\\_04_SVM\\\\";

            string dataFile = null;
            string targetFile = null;
            string resultFile = "result.txt";
            string workingDir = null;
            bool fit = false;
            string Logs = "";

            //Optional Data:

            double C = 1.0;
            double epsilon=1.0;
            string kernel = "rbf";
            int degree= 3 ;
            double gamma= 0.0;
            double coef0= 0.0;
            bool shrinking=true ;
            double tol= 1e-3;
            double cache_size= 200;
            bool verbose= false;
            int max_iter = -1;

            List<double> predicTData = new List<double>();


            if (!DA.GetData(0, ref dataFile)) { return; }
            if (dataFile == null) { return; }
            if (!DA.GetData(1, ref targetFile)) { return; }
            if (targetFile == null) { return; }
            if (!DA.GetData(2, ref workingDir)) { return; }
            if (workingDir == null) { return; }
            if (!DA.GetDataList(3, predicTData)) { return; }
            if (predicTData == null) { return; }
            if (!DA.GetData(4, ref fit)) { return; }
            if (fit == false) { return; }

            long ticks0 = DateTime.Now.Ticks;
            try
            {
                DA.GetData(5, ref C);                  
                DA.GetData(6, ref epsilon);               
                DA.GetData(7, ref kernel);
                DA.GetData(8, ref degree);            
                DA.GetData(9, ref gamma);              
                DA.GetData(10, ref coef0);
                DA.GetData(11, ref shrinking);      
                DA.GetData(12, ref tol);     
                DA.GetData(13, ref cache_size); 
                DA.GetData(14, ref verbose);     
                DA.GetData(15, ref max_iter);           
            }
            catch (Exception ex)
            {
                var st = new StackTrace(ex, true);
                // Get the top stack frame
                var frame = st.GetFrame(0);
                // Get the line number from the stack frame
                var line = frame.GetFileLineNumber();
                Logs += line.ToString() + "\n";
            }

            // 01 Specify the working directory at which pythonFile exists, note that this Python file should run separately using Proccess2 (see 5). 
            process2.StartInfo.WorkingDirectory = workingDir;
            try
            {
                // 02 Initiate helperFunctions
                HelperFunction helpFunctions = new HelperFunction();

                // Put inputData in one list of strings. 
                List<string> dataInput = new List<string>(new string[] {
                C.ToString(),
                epsilon.ToString(),
                @"'"+kernel+@"'",
                degree.ToString(),
                gamma.ToString(),
                coef0.ToString(),
                shrinking==true?"True":"False",
                tol.ToString(),
                cache_size.ToString(),
                verbose==true?"True":"False",
                max_iter.ToString(),
            });

                // 03 Convert data from grasshopper syntax to python NumPy like array. 
                string newString = helpFunctions.dataInput2Python(workingDir, predicTData);

                // 04 Write the Python file in the working directory 
                helpFunctions.PythonFile(defaultDir, dataFile, targetFile, workingDir, resultFile, newString, "True", workingDir + "logFile.txt", allResources._04_SupportVectorRegression, dataInput);


            }
            catch (Exception e)
            {
                this.Message = e.ToString();
            }

            // 05 Start running Python file. and wait until it is closed i.e. raising the process_Exited event. 
            process2.Start();

            while (!processhasexit)
            {

            }
            try
            {
                doc.Load(workingDir + "res.xml");

                //TODO : add all the output variables here
                //AllData = {"prediction":result, "score":sroce, "support":support, "support_vectors":support_vectors, "n_support": n_support, "dual_coef": dual_coef, "coef": coeff, "intercept":intercept}

                XmlNode res_node = doc.DocumentElement.SelectSingleNode("/result/prediction");
                XmlNode score_node = doc.DocumentElement.SelectSingleNode("/result/score");
                XmlNode support_node = doc.DocumentElement.SelectSingleNode("/result/support");
                XmlNode support_vectors_node = doc.DocumentElement.SelectSingleNode("/result/support_vectors");
                XmlNode dual_coef_node = doc.DocumentElement.SelectSingleNode("/result/dual_coef");
                XmlNode coeff_node = doc.DocumentElement.SelectSingleNode("/result/coef");
                XmlNode intercept_node = doc.DocumentElement.SelectSingleNode("/result/intercept");


                string res = res_node.InnerText;
                string score = score_node.InnerText;
                string support = support_node.InnerText;
                string support_vectors = support_vectors_node.InnerText;
                string dual_coef = dual_coef_node.InnerText;
                string coeff = coeff_node.InnerText;
                string intercept = intercept_node.InnerText;



                //string res = System.IO.File.ReadAllText(workingDir + "result.txt");
                res = res.Replace("[", "").Replace("]", "");
                DA.SetData(1, res);
                DA.SetData(2, score);
                DA.SetData(3, support);
                DA.SetData(4, support_vectors);
                DA.SetData(5, dual_coef);
                DA.SetData(6, coeff);
                DA.SetData(7, intercept);

                long ticks1 = DateTime.Now.Ticks;
                double timeElaspsed = ((double)ticks1 - (double)ticks0) / 10000000;
                Logs += "Success !! in : " + timeElaspsed + " Seconds\n ";


            }
            catch (Exception e)
            {
                var st = new StackTrace(e, true);
                // Get the top stack frame
                var frame = st.GetFrame(0);
                // Get the line number from the stack frame
                var line = frame.GetFileLineNumber();
                Logs += e.Message + line.ToString() + "\n";
            }
            DA.SetData(0, Logs);
            processhasexit = false;

        }

        private void process_Exited(object sender, EventArgs e)
        {
            processhasexit = true;
        }


        /// <summary>
        /// This function initiates input parametrs of the component. 
        /// </summary>
        /// <param name="docs"></param>
        /// <param name="vars"></param>
        /// <param name="pManager"></param>
        private void initInputParams(string[] docs, Object[,] vars, GH_Component.GH_InputParamManager pManager)
        {

            pManager.AddTextParameter("_Data \t    | ", "_D", "Input instances for training ..\nndarray or scipy.sparse matrix, (n_samples, n_features) Data", GH_ParamAccess.item);                                      //0
            pManager.AddTextParameter("_Target \t    | ", "_T", "Input Targets for Fitting .. ", GH_ParamAccess.item);                                  //1
            pManager.AddTextParameter("_workingFolder   | ", "Fldr", "Please specify the folder that you are working on .. ", GH_ParamAccess.item);     //2
            pManager.AddGenericParameter("_Predict \t    | ", "_P", "Insert the new features of the current test  .. ", GH_ParamAccess.list);           //3
            pManager.AddBooleanParameter("_FIT? \t    | ", "_F", "Start fitting data ? ", GH_ParamAccess.item, false);                                  //4


            for (int i = 0; i < docs.Length; i++)
            {
                if ((string)vars[i, 1] == "f")
                {
                    pManager.AddNumberParameter((string)vars[i, 0] + " \t    | ", (string)vars[i, 0], docs[i], GH_ParamAccess.item, Convert.ToDouble(vars[i, 2]));
                }
                else if ((string)vars[i, 1] == "s")
                {
                    pManager.AddTextParameter((string)vars[i, 0] + " \t    | ", (string)vars[i, 0], docs[i], GH_ParamAccess.item, vars[i, 2].ToString());
                }
                else if ((string)vars[i, 1] == "b")
                {
                    pManager.AddBooleanParameter((string)vars[i, 0] + " \t    | ", (string)vars[i, 0], docs[i], GH_ParamAccess.item, Convert.ToBoolean(vars[i, 2]));
                }
                else if ((string)vars[i, 1] == "i")
                {
                    pManager.AddIntegerParameter((string)vars[i, 0] + " \t    | ", (string)vars[i, 0], docs[i], GH_ParamAccess.item, Convert.ToInt32(vars[i, 2]));

                }
            }
        }


        private void initOutputParams(string[] docs, string[] vars, GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter(" |    Log", " |    Log", "data log", GH_ParamAccess.item);
            pManager.AddTextParameter(" |    Result", "  |    Result", "Fitting result", GH_ParamAccess.item);

            pManager.AddTextParameter("score", "score", "result score", GH_ParamAccess.item);

            for (int i = 0; i < vars.Length; i++)
            {
                pManager.AddTextParameter(" |    " + (string)vars[i], (string)vars[i], (string)docs[i], GH_ParamAccess.item);
            }
        }


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return allResources._04_SVR;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{0a60f955-c8b5-4963-9457-33981ca45128}"); }
        }
    }
}