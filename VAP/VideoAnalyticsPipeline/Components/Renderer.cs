using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace VideoAnalyticsPipeline;
internal class Renderer(ChannelFactory channelFactory, ILogger<Renderer> logger) : IModule
{
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        var currentComponent = typeof(Renderer).FullName!;

        await foreach (var data in channelFactory.Reader(currentComponent).ReadAllAsync(cancellationToken))
        {
            var image = (Image)data;

            try
            {
                if (image == null) continue;

                var boundedBoxImg = await DrawBoundingBox(image.ImageStream!, data.Inference!.Timestamp,
                                            data.Inference!.Outputs!.Select(o => o.Location)!,
                                            cancellationToken);

                if (boundedBoxImg == null) continue;

                image.ImageStream = new MemoryStream();
                boundedBoxImg.Encode(image.ImageStream, SKEncodedImageFormat.Jpeg, 100);

                foreach (var writer in channelFactory.Writers(currentComponent))
                {
                    await writer.WriteAsync(image, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error rendering image for {CameSerial} at {Timestamp}", image.CameraSerial, data.Inference!.Timestamp);
            }
        }
    }

    internal async ValueTask<SKBitmap?> DrawBoundingBox(Stream image, long timestamp, IEnumerable<float[]> location,
        CancellationToken cancellationToken) =>

        await Task.Run(() =>
            {
                SKBitmap? outputImage = null;

                // Create a new image for drawing
                using var originalImage = SKBitmap.Decode(image);
                outputImage = new SKBitmap(originalImage.Width, originalImage.Height);

                // Draw the original image onto the canvas
                using var canvas = new SKCanvas(outputImage);
                canvas.DrawBitmap(originalImage, new SKPoint(0, 0));

                foreach (var l in location)
                {
                    logger.LogInformation("Drawing bounding box at {l} for violation detected at {timestamp}", l, timestamp);

                    // Convert the normalized coordinates to absolute pixel coordinates
                    // location values contain normalized coordinates of the bounding box. it follows [xmin, ymin, xmax, ymax] format

                    var absXmin = (int)(l[0] * originalImage.Width);
                    var absXmax = (int)(l[2] * originalImage.Width);
                    var absYmin = (int)(l[1] * originalImage.Height);
                    var absYmax = (int)(l[3] * originalImage.Height);

                    using var paint = new SKPaint();
                    paint.Style = SKPaintStyle.Stroke;
                    paint.Color = SKColors.Red;
                    paint.StrokeWidth = 2;

                    // Create the rectangle
                    var rect = new SKRect(absXmin, absYmin, absXmax, absYmax);

                    // Draw the rectangle on the canvas
                    canvas.DrawRect(rect, paint);

                }
                return outputImage;
            }, cancellationToken);
}


