using AVFoundation;
using CoreMedia;
using CoreVideo;
using Foundation;
using ObjCRuntime;

namespace Xamarin.Macios.Generator.Sample;

// This code will not compile until you build the project with the Source Generators
public partial class AVAsynchronousVideoCompositionRequest
{
    public static partial string Video { get; }
}

public partial class AVAsynchronousVideoCompositionRequest
{
   public static partial string Video
   {
       get => "test";
   }

   public static nfloat GetNFloat (nfloat value)
   {
	   return value;
   }

}

/*
//[NoWatch]
//[MacCatalyst (13, 1)]
[BindingType(Name = "Test")]
public partial class AVAsynchronousVideoCompositionRequest : NSObject, INSCopying {

    public partial static NSString Video { get; }

    /*
    [Export ("sourceFrameByTrackID:")]
    public partial CVPixelBuffer? SourceFrameByTrackID (int /* CMPersistentTrackID = int32_t  trackID);

    [Export ("renderContext", ArgumentSemantic.Copy)]
    public partial AVVideoCompositionRenderContext RenderContext { get; }

    [Export ("compositionTime", ArgumentSemantic.Copy)]
    public partial CMTime CompositionTime { get; }

    [Export ("sourceTrackIDs")]
    public partial NSNumber [] SourceTrackIDs { get; }

    [Export ("videoCompositionInstruction", ArgumentSemantic.Copy)]
    public partial AVVideoCompositionInstruction VideoCompositionInstruction { get; }

    [Export ("sourceFrameByTrackID:")]
    public partial CVPixelBuffer? SourceFrameByTrackID (int /* CMPersistentTrackID = int32_t   trackID);

    [Export ("finishWithComposedVideoFrame:")]
    public partial void FinishWithComposedVideoFrame (CVPixelBuffer composedVideoFrame);

    [Export ("finishWithError:")]
    public partial void FinishWithError (NSError error);

    [Export ("finishCancelledRequest")]
    public partial void FinishCancelledRequest ();

    //[TV (15, 0), NoWatch, Mac (12, 0), iOS (15, 0), MacCatalyst (15, 0)]
    [Export ("sourceSampleBufferByTrackID:")]
    public partial CMSampleBuffer? GetSourceSampleBuffer (int trackId);

    //[TV (15, 0), NoWatch, Mac (12, 0), iOS (15, 0), MacCatalyst (15, 0)]
    [Export ("sourceTimedMetadataByTrackID:")]
    public partial AVTimedMetadataGroup? GetSourceTimedMetadata (int trackId);

    //[TV (15, 0), NoWatch, Mac (12, 0), iOS (15, 0), MacCatalyst (15, 0)]
    [Export ("sourceSampleDataTrackIDs")]
    [BindAs (typeof (int []))]
    public partial NSNumber [] SourceSampleDataTrackIds { get; }
}
*/
