using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

namespace HelloWorld
{
    public partial class Parking : Form
    {
        VideoCapture _capture;
        int MIN_PERCENTAGE_COVERED = 5;
        // BGR
        MCvScalar RED = new MCvScalar(0, 0, 255);
        MCvScalar GREEN = new MCvScalar(0, 255, 0);
        MCvScalar BLUE = new MCvScalar(255, 0, 0);

        public Parking()
        {
            InitializeComponent();
            lblParkingStatus.Text = string.Empty;
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (_capture == null) _capture = new Emgu.CV.VideoCapture(1);

                _capture.ImageGrabbed += Capture_ImageGrabbed;
                _capture.Start();
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_capture != null) _capture.Stop();
            lblParkingStatus.Text = string.Empty;
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_capture != null) _capture.Pause();
            lblParkingStatus.Text = string.Empty;
        }

        #region [Process Frame & Update output]

        private void Capture_ImageGrabbed(object sender, EventArgs e)
        {
            try
            {
                Mat mat = new Mat();
                _capture.Retrieve(mat);

                ProcessImage(mat.ToImage<Bgr, Byte>());
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
        }

        private void ProcessImage(Image<Bgr, byte> input_image)
        {
            try
            {
                // trick is in gray to identify particular object :-
                Image<Gray, byte> gray_image = input_image.Convert<Gray, byte>().ThresholdBinary(new Gray(100), new Gray(250));

                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                Mat hier = new Mat();
                CvInvoke.FindContours(gray_image, contours, hier, RetrType.Tree, ChainApproxMethod.ChainApproxSimple);

                double total_area_covered = 0.0;
                double parking_slot_area = 0.0;
                int parking_contour_index = 2;
                Dictionary<int, double> listOfContourAreas = new Dictionary<int, double>();

                for (int i = 0; i < contours.Size; i++)
                {
                    var ctr = contours[i];
                    double contour_area = CvInvoke.ContourArea(ctr);

                    listOfContourAreas.Add(i, contour_area);
                }

                // sort reverse :-
                var sorted_dict = from pair in listOfContourAreas
                                  orderby pair.Value descending
                                  select pair;

                KeyValuePair<int, double> parking_slot_contour = sorted_dict.ElementAt(1); //second highest area :-

                parking_contour_index = parking_slot_contour.Key;
                parking_slot_area = parking_slot_contour.Value;

                foreach (KeyValuePair<int, double> ctr in listOfContourAreas)
                {
                    if (ctr.Value > 0 && ctr.Value < parking_slot_area)
                    {
                        total_area_covered += ctr.Value;
                        //CvInvoke.DrawContours(input_image, contours, ctr.Key, BLUE, 1, LineType.EightConnected);
                    }
                }

                // Calculate percentage of area covered by car :-
                double percentage_covered = (total_area_covered / parking_slot_area) * 100;

                // MCvScalar - uses (BGR) code - Blue, Green, Red :-
                if (percentage_covered > MIN_PERCENTAGE_COVERED)
                {
                    CvInvoke.DrawContours(input_image, contours, parking_contour_index, RED, 3, LineType.Filled);
                    UpdateOutputLabel("Parking FULL !!");
                }
                else
                {
                    CvInvoke.DrawContours(input_image, contours, parking_contour_index, GREEN, 3, LineType.Filled);
                    UpdateOutputLabel("Parking Vacant !!");
                }

                // Show the final image :-
                parkingLive.Image = input_image.Bitmap;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("Error: " + ex.Message + " - " + ex.StackTrace);
            }
        }

        private void UpdateOutputLabel(string parkingStatusMessage)
        {
            if (InvokeRequired)
            {
                this.Invoke(new MethodInvoker(delegate
                {
                    lblParkingStatus.Text = parkingStatusMessage;
                }));
            }
            else
            {
                lblParkingStatus.Text = parkingStatusMessage;
            }
        }

        #endregion

        // Error handling
        private void HandleError(Exception ex)
        {
            if (_capture != null)
            {
                _capture.Stop();
                _capture = null;
            }

            // MessageBox.Show("Error: " + ex.Message + " - " + ex.StackTrace);
        }
    }
}
