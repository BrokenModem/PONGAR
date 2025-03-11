using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Rectangle = System.Drawing.Rectangle;

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
    public Vector2 GameCenterPosition { get; set; } = Vector2.Zero;
    public Ball GameBall { get; set; }

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
        int activeCameraIndex = FindActiveCamera();
        VideoCapture = new VideoCapture(activeCameraIndex);
        UtilityAR.ReadIntrinsicsFromFile(out intrinsics, out distCoeffs);
    }
    public static int FindActiveCamera()
    {
        for (int i = 0; i < 10; i++) // Check first 10 indices
        {
            try
            {
                using VideoCapture capture = new(i, VideoCapture.API.Any);
                if (capture.IsOpened)
                    return i; // Return the first active camera index
            }
            catch (Exception)
            {

            }
        }
        System.Console.WriteLine(" NO CAMERA MAN!");
        return -1; // No active camera
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

            for (int m = 0; m < markerHandler.GetRecognizableMarkers().Count; m++)
            {
                if (CompareByteMatrices(markerHandler.GetRecognizableMarkers()[m], centerPixels))
                {
                    foundMarker = true;
                    detectedIndex = m;
                    break;
                }
            }

            int recognizedMarkerIndex = detectedIndex / 4;

            if (foundMarker)
            {
                MCvPoint3D32f[] objectPoints = new MCvPoint3D32f[4];
                
                detectedIndex %= 4;
                
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
                    
                    //Add graphics

                    switch (recognizedMarkerIndex)
                    {
                        case 0:
                            Rectangle playerCollisionBox = UtilityAR.DrawCube(frame, worldToScreenMatrix, 0.25f, 1f);
                            //CvInvoke.Rectangle(frame, playerCollisionBox, new MCvScalar(255, 255, 0), 2);
                            collisionBoxes.Add(new Collider(playerCollisionBox, "Player1"));
                            break;

                        case 1:
                            Matrix<float> modifiedMatrixLeft = worldToScreenMatrix.Clone();
                            Matrix<float> modifiedMatrixRight = worldToScreenMatrix.Clone();

                            // Define offset in marker's local space
                            Vector3 localOffsetLeft = new(-2, -2f, 0);  // Left side of marker
                            Vector3 localOffsetRight = new(3f, -2f, 0);   // Right side of marker

                            // Extract rotation matrix from worldToScreenMatrix (assumes it's 3x4 or 4x4)
                            Matrix<float> defferedRotationMatrix = new(3, 3);
                            defferedRotationMatrix[0, 0] = worldToScreenMatrix[0, 0];
                            defferedRotationMatrix[0, 1] = worldToScreenMatrix[0, 1];
                            defferedRotationMatrix[0, 2] = worldToScreenMatrix[0, 2];
                            defferedRotationMatrix[1, 0] = worldToScreenMatrix[1, 0];
                            defferedRotationMatrix[1, 1] = worldToScreenMatrix[1, 1];
                            defferedRotationMatrix[1, 2] = worldToScreenMatrix[1, 2];
                            defferedRotationMatrix[2, 0] = worldToScreenMatrix[2, 0];
                            defferedRotationMatrix[2, 1] = worldToScreenMatrix[2, 1];
                            defferedRotationMatrix[2, 2] = worldToScreenMatrix[2, 2];

                            // Rotate the offsets using the extracted rotation matrix
                            Vector3 rotatedOffsetLeft = TransformOffset(defferedRotationMatrix, localOffsetLeft);
                            Vector3 rotatedOffsetRight = TransformOffset(defferedRotationMatrix, localOffsetRight);

                            // Apply the rotated offset to the translation part of the transformation matrix
                            modifiedMatrixLeft[0, 3] += rotatedOffsetLeft.X;
                            modifiedMatrixLeft[1, 3] += rotatedOffsetLeft.Y;
                            modifiedMatrixLeft[2, 3] += rotatedOffsetLeft.Z;

                            modifiedMatrixRight[0, 3] += rotatedOffsetRight.X;
                            modifiedMatrixRight[1, 3] += rotatedOffsetRight.Y;
                            modifiedMatrixRight[2, 3] += rotatedOffsetRight.Z;
                            
                            float z = worldToScreenMatrix[2, 3];

                            if (z > 0)
                            {
                                GameCenterPosition = new Vector2(worldToScreenMatrix[0, 3], worldToScreenMatrix[1, 3]);
                                GameCenterPosition = GameCenterPosition / z;
                            }
                            
                            Rectangle wallLeftCollisionBox = UtilityAR.DrawCube(frame, modifiedMatrixLeft, 0.25f, 5f);
                            //CvInvoke.Rectangle(frame, wallLeftCollisionBox, new MCvScalar(255, 255, 0), 2);
                            collisionBoxes.Add(new Collider(wallLeftCollisionBox, "WallLeft"));

                            Rectangle wallRightCollisionBox = UtilityAR.DrawCube(frame, modifiedMatrixRight, 0.25f, 5f);
                            //CvInvoke.Rectangle(frame, wallRightCollisionBox, new MCvScalar(255, 255, 0), 2);
                            collisionBoxes.Add(new Collider(wallRightCollisionBox, "WallRight"));
                            break;

                        case 2:
                            Rectangle player2CollisionBox = UtilityAR.DrawCube(frame, worldToScreenMatrix, 0.25f, 1f);
                            //CvInvoke.Rectangle(frame, player2CollisionBox, new MCvScalar(255, 255, 0), 2);
                            collisionBoxes.Add(new Collider(player2CollisionBox, "Player2"));
                            break;
                    }
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
    private Vector3 TransformOffset(Matrix<float> rotationMatrix, Vector3 localOffset)
    {
        return new Vector3(
            rotationMatrix[0, 0] * localOffset.X + rotationMatrix[0, 1] * localOffset.Y + rotationMatrix[0, 2] * localOffset.Z,
            rotationMatrix[1, 0] * localOffset.X + rotationMatrix[1, 1] * localOffset.Y + rotationMatrix[1, 2] * localOffset.Z,
            rotationMatrix[2, 0] * localOffset.X + rotationMatrix[2, 1] * localOffset.Y + rotationMatrix[2, 2] * localOffset.Z
        );
    }
}