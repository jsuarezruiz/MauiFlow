namespace MauiFlow.Controls;

public partial class Preview : ContentView
{
    const double TargetAspectRatio = 0.5; // Width/Height ratio for phone (1:2)
    const double DeviceMargin = 40; // Total margin (20 on each side)

    public Preview()
    {
        InitializeComponent();
    }

    void OnPreviewGridSizeChanged(object sender, EventArgs e)
    {
        if (sender is Grid grid && DeviceFrame != null)
        {
            UpdateDeviceFrameSize(grid.Width, grid.Height);
        }
    }

    void UpdateDeviceFrameSize(double availableWidth, double availableHeight)
    {
        if (availableWidth <= 0 || availableHeight <= 0)
            return;

        // Calculate available space minus margins
        var maxWidth = availableWidth - DeviceMargin;
        var maxHeight = availableHeight - DeviceMargin;

        // Calculate potential sizes based on aspect ratio
        var widthBasedOnHeight = maxHeight * TargetAspectRatio;
        var heightBasedOnWidth = maxWidth / TargetAspectRatio;

        double finalWidth, finalHeight;

        // Choose the constraining dimension
        if (widthBasedOnHeight <= maxWidth)
        {
            // Height is the limiting factor
            finalWidth = widthBasedOnHeight;
            finalHeight = maxHeight;
        }
        else
        {
            // Width is the limiting factor  
            finalWidth = maxWidth;
            finalHeight = heightBasedOnWidth;
        }

        // Apply the calculated size maintaining aspect ratio
        DeviceFrame.WidthRequest = finalWidth;
        DeviceFrame.HeightRequest = finalHeight;
    }
}