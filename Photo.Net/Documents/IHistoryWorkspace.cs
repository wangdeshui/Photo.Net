using Photo.Net.Documents;

namespace PaintDotNet
{
    internal interface IHistoryWorkspace
    {
        Document Document
        {
            get;
            set;
        }

        //        Selection Selection
        //        {
        //            get;
        //        }

        //        Layer ActiveLayer
        //        {
        //            get;
        //        }

        int ActiveLayerIndex
        {
            get;
        }
    }
}
