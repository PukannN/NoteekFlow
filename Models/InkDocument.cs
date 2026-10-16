using System;
using System.Collections.ObjectModel;

namespace NoteekFlow.Models;

public sealed class InkDocument
{
    public int Version { get; set; } = 1;
    public bool ShowGrid { get; set; }
    public string BackgroundColor { get; set; } = "#FBFAF7";
    public double StrokeWidth { get; set; } = 3;
    public ObservableCollection<InkStroke> Elements { get; set; } = new();
}

public sealed class InkStroke
{
    public string Kind { get; set; } = "Pen";
    public string Color { get; set; } = "#20242B";
    public string? Text { get; set; }
    public double Width { get; set; } = 3;
    public ObservableCollection<InkPoint> Points { get; set; } = new();
}

public sealed class InkPoint
{
    public double X { get; set; }
    public double Y { get; set; }
    public float Pressure { get; set; } = 0.5f;

    public InkPoint() { }

    public InkPoint(double x, double y, float pressure = 0.5f)
    {
        X = x;
        Y = y;
        Pressure = Math.Clamp(pressure, 0.1f, 1f);
    }
}
