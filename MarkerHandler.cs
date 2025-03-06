using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace PONGAR;

public class MarkerHandler
{
    private List<Matrix<byte>> recognizableMarkers = new();

    public MarkerHandler()
    {
        AddMarker(new Matrix<byte>(new byte[,]
        {
            {0,0,0,0,0,0}, 
            {0,1,1,0,1,0}, 
            {0,1,1,1,0,0}, 
            {0,0,0,0,1,0}, 
            {0,0,0,0,1,0}, 
            {0,0,0,0,0,0}
        }));
        
        AddMarker(new Matrix<byte>(new byte[,]
        {
            {0,0,0,0,0,0}, 
            {0,0,1,1,1,0}, 
            {0,1,0,0,1,0}, 
            {0,1,1,0,0,0}, 
            {0,1,1,0,1,0}, 
            {0,0,0,0,0,0}
        }));
    }

    public void AddMarker(Matrix<byte> marker)
    {
        recognizableMarkers.Add(marker);
        
        for (int k = 0; k < 3; k++)
        {
            Matrix<byte> markerClone = recognizableMarkers[k].Clone();
            CvInvoke.Rotate(markerClone, markerClone, RotateFlags.Rotate90Clockwise);
            recognizableMarkers.Add(markerClone);
        }
    }

    public List<Matrix<byte>> GetRecognizableMarkers()
    {
        return recognizableMarkers;
    }
}