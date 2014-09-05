using Photo.Net.Core;

namespace Photo.Net.Tool.Thumbnail
{
    public interface IThumbnailProvider
    {
        /// <summary>
        /// Renders a thumbnail for the underlying object.
        /// </summary>
        /// <param name="maxEdgeLength">The maximum edge length of the thumbnail.</param>
        Surface RenderThumbnail(int maxEdgeLength);
    }
}
