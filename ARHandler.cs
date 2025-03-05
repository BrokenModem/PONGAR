using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PONGAR;
public class ARHandler
{
    private Size patternSize = new Size(7, 4);
    private Matrix<float> intrinsics;
    private Matrix<float> distCoeffs;
    private List<Collider> collisionBoxes;
    private Collider[] tempCollisionBoxes;
    private Mat frame = new();
    
    public Mat GameFrame { get; set; }
    public VideoCapture VideoCapture { get; set; }
    public bool FrameGrabbed { get; set; } = false;
    public bool IsRunning { get; set; } = true;
    public Task readTask;

    //TIL ØVELSE 1 & 2
    Mat gray = new();
    Mat binary = new();
    Mat hierarchy = new();

    //TIL ØVELSE 3 & 4
    VectorOfVectorOfPoint contours = new();
    VectorOfVectorOfPoint correctedContours = new();

    //TIL ØVELSE 5 & 6
    Mat transformedGray = new();
    Mat transformedBinary = new();
    private MarkerHandler markerHandler = new();

    public ARHandler()
    {
        VideoCapture = new VideoCapture(1);
        UtilityAR.ReadIntrinsicsFromFile(out intrinsics, out distCoeffs);
    }

    public void StartTask()
    {
        readTask = Task.Run(() => 
        {
            while (IsRunning)
            {
                Mat tempFrame = Update();
                if (tempFrame != null)
                    GameFrame = tempFrame;

                Task.Delay(16).Wait();
            }
        });
    }

    public Mat Update()
    {
        collisionBoxes = new();
        Mat localFrame = new();
        FrameGrabbed = VideoCapture.Read(localFrame);
        if (!FrameGrabbed)
        {
            Console.Write("Failed to grab frame");
            return null;
        }
        lock (frame)
            frame = localFrame.Clone();

        CvInvoke.CvtColor(frame, gray, ColorConversion.Bgr2Gray);
        CvInvoke.Threshold(gray, binary, 0, 255, ThresholdType.Otsu);
        //CvInvoke.Imshow("ARHandler", frame);

        // --------- #2 -------------
        CvInvoke.FindContours(binary, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxSimple);
        //ShowWithContours(frame, contours);

        // --------- #3 -------------
        correctedContours = new();
        for (int i = 0; i < contours.Size; i++)
        {
            VectorOfPoint contour = contours[i];
            VectorOfPoint approxContour = new();
            CvInvoke.ApproxPolyDP(contour, approxContour, 4, true);

            if (approxContour.Size == 4)
                correctedContours.Push(approxContour);
        }
        
        // --------- #4 -------------
        for (int i = 0; i < correctedContours.Size; i++)
        {
            PointF[] correctedPoints = [
            new (0,0),
            new (300, 0),
            new (300, 300),
            new (0, 300)
            ];
            PointF[] contourArray = [.. correctedContours[i].ToArray().Select(p => new PointF(p.X, p.Y))];

            Mat homography = CvInvoke.FindHomography(contourArray, correctedPoints, RobustEstimationAlgorithm.Ransac);
            Mat transformed = new();

            if (homography == null)
                return null;

            CvInvoke.WarpPerspective(frame, transformed, homography, new Size(300, 300));
            //CvInvoke.Imshow("ARHandler", transformed);

        // --------- #5 -------------
            //GreyScale
            CvInvoke.CvtColor(transformed, transformedGray, ColorConversion.Bgr2Gray);
        
            //Binary Conversion
            CvInvoke.Threshold(transformedGray, transformedBinary, 0, 255, ThresholdType.Otsu);
            
            //Save pixel values
            int gridSize = 6;
            int cellSize = 300 / gridSize;
            Matrix<byte> centerPixels = new(gridSize, gridSize);
            Image<Gray, byte> markerImg = transformedBinary.ToImage<Gray, byte>();

            for (int r = 0; r < gridSize; r++)
            {
                for (int c = 0; c < gridSize; c++)
                {
                    int cx = c * cellSize + cellSize / 2;
                    int cy = r * cellSize + cellSize / 2;
                    byte pixelVal = markerImg.Data[cy, cx, 0];
                    centerPixels[r, c] = (byte)(pixelVal > 128 ? 1 : 0);
                }
            }

        // --------- #6 -------------

            //Prepare matrices
            Matrix<float> rotationVector = new(3, 1);
            Matrix<float> translationVector = new(3, 1);

            bool foundMarker = false;
            int detectedIndex = -1;

            for (int m = 0; m < 3; m++)
            {
                if (CompareByteMatrices(markerHandler.GetRecognizableMarkers()[m], centerPixels))
                {
                    foundMarker = true;
                    detectedIndex = m;
                    break;
                }
            }

            if (foundMarker)
            {
                MCvPoint3D32f[] objectPoints = new MCvPoint3D32f[4];

                switch (detectedIndex)
                {
                    case 0:
                        objectPoints =
                        [
                            new (0, 0, 0),
                            new (1, 0, 0),
                            new (1, 1, 0),
                            new (0, 1, 0)
                        ];
                        break;
                    
                    case 1:
                        objectPoints =
                        [
                            new (0, 1, 0),
                            new (0, 0, 0),
                            new (1, 0, 0),
                            new (1, 1, 0)
                        ];
                        break;
                    
                    case 2:
                        objectPoints =
                        [
                            new (1, 1, 0),
                            new (0, 1, 0),
                            new (0, 0, 0),
                            new (1, 0, 0)
                        ];
                        break;
                    
                    case 3:
                        objectPoints =
                        [
                            new (1, 0, 0),
                            new (1, 1, 0),
                            new (0, 1, 0),
                            new (0, 0, 0)
                        ];
                        break;
                }
                PointF[] screenPoints = contourArray;
                bool solvedPnP = false;

                if (objectPoints.Length >= 4 && contourArray.Length >= 4)
                    solvedPnP = CvInvoke.SolvePnP(objectPoints, contourArray, intrinsics, distCoeffs, rotationVector, translationVector);   

                if (solvedPnP)
                {
                    Matrix<float> rotationMatrix = new(3, 3);
                
                    CvInvoke.Rodrigues(rotationVector, rotationMatrix);
                
                    float[,] rValues = rotationMatrix.Data;
                    float[,] tValues = translationVector.Data;

                    Matrix<float> rtMatrix = new(new float[,] {
                        { rValues[0,0], rValues[0,1], rValues[0,2], tValues[0,0] },
                        { rValues[1,0], rValues[1,1], rValues[1,2], tValues[1,0] },
                        { rValues[2,0], rValues[2,1], rValues[2,2], tValues[2,0] }});
                
                    Matrix<float> worldToScreenMatrix = intrinsics * rtMatrix;
                
                    Rectangle collisionBox = UtilityAR.DrawCube(frame, worldToScreenMatrix, 1f, 2f);
                    CvInvoke.Rectangle(frame, collisionBox, new MCvScalar(255, 255, 0), 2);
                    collisionBoxes.Add(new Collider(collisionBox, "Player" + i));
                }
            }
        }
        
        tempCollisionBoxes = new Collider[collisionBoxes.Count];
        collisionBoxes.CopyTo(tempCollisionBoxes);
        
        return frame;
    }
    private static bool CompareByteMatrices(Matrix<byte> mat1, Matrix<byte> mat2)
    {
        if (mat1.Rows != mat2.Rows || mat1.Cols != mat2.Cols)
        {
            return false;
        }

        for (int o = 0; o < mat1.Rows; o++)
        {
            for (int p = 0; p < mat1.Cols; p++)
            {
                if (mat1[o, p] != mat2[o, p])
                {
                    return false;
                }
            }
        }
        return true;
    }

    public Collider[] GetArrayOfCollisionBoxes()
    {
        return tempCollisionBoxes;
    }
    
    private static void ShowWithContours(Mat frame, VectorOfVectorOfPoint listOfContours)
    {
        CvInvoke.DrawContours(frame, listOfContours, -1, new MCvScalar(255, 255, 0), 2);
        CvInvoke.Imshow("ARHandler", frame);
    }
}