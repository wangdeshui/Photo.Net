using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using System.Windows.Forms;
using Photo.Net.Base.Delegate;
using Photo.Net.Base.Exceptions;
using Photo.Net.Base.IO;
using Photo.Net.Core;
using Photo.Net.Core.Area;
using Photo.Net.Resource;
using Photo.Net.Tool.Core;
using Photo.Net.Tool.IO;
using Photo.Net.Tool.Thumbnail;
using Photo.Net.Tool.Tools;

namespace Photo.Net.Tool.Documents
{
    /// <summary>
    /// Builds on DocumentView by adding application-specific elements.
    /// </summary>
    public class DocumentWorkspace
        : DocumentView,
          IHistoryWorkspace,
          IThumbnailProvider
    {

        #region Propertys


        public int ActiveLayerIndex { get; set; }

        public AppWorkspace AppWorkspace { get; set; }

        public Selection Selection { get; set; }

        #endregion

        static DocumentWorkspace()
        {
            InitializeTools();
            InitializeToolInfos();
        }

        public DocumentWorkspace()
        {
            base.InitLayout();
        }

        public DocumentWorkspace(AppWorkspace appWorkspace)
            : this()
        {
            AppWorkspace = appWorkspace;
        }

        public Surface RenderThumbnail(int maxEdgeLength)
        {
            throw new NotImplementedException();
        }

        public DialogResult ChooseFiles(Control owner = null)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;

                dialog.FilterIndex = 0;

                DialogResult result = dialog.ShowDialog();

                return result;
            }
        }

        public static Document LoadDocument(Control owner, string fileName, out FileType fileTypeResult, ProgressEventHandler progressCallback)
        {
            fileTypeResult = null;

            try
            {
                var fileTypes = FileTypes.GetFileTypes();
                var extName = Path.GetExtension(fileName);
                if (extName == null)
                {
                    //Todo: Get file real type.
                    throw new Exception("Get file real type.");
                }
                extName = extName.Replace(".", "");
                fileTypeResult = fileTypes.First(x => x.Extensions.Any(ext => ext == extName));
            }

            catch (ArgumentException)
            {
                string format = PdnResources.GetString("LoadImage.Error.InvalidFileName.Format");
                string error = string.Format(format, fileName);
                Utility.ErrorBox(owner, error);
                return null;
            }

            Document document = null;

            using (new WaitCursorChanger(owner))
            {
                Utility.GCFullCollect();
                Stream stream = null;

                try
                {
                    try
                    {
                        stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                        long totalBytes = 0;

                        var siphonStream = new SiphonStream(stream);

                        IOEventHandler ioEventHandler = null;
                        ioEventHandler =
                            delegate(object sender, IOEventArgs e)
                            {
                                if (progressCallback != null)
                                {
                                    totalBytes += (long)e.Count;
                                    double percent = Utility.Clamp(100.0 * ((double)totalBytes / (double)siphonStream.Length), 0, 100);
                                    progressCallback(null, new ProgressEventArgs(percent));
                                }
                            };

                        siphonStream.IOFinished += ioEventHandler;

                        using (new WaitCursorChanger(owner))
                        {
                            document = fileTypeResult.Load(siphonStream);

                            if (progressCallback != null)
                            {
                                progressCallback(null, new ProgressEventArgs(100.0));
                            }
                        }

                        siphonStream.IOFinished -= ioEventHandler;
                        siphonStream.Close();
                    }

                    catch (WorkerThreadException ex)
                    {
                        Type innerExType = ex.InnerException.GetType();
                        ConstructorInfo ci = innerExType.GetConstructor(new Type[] { typeof(string), typeof(Exception) });

                        if (ci == null)
                        {
                            throw;
                        }
                        else
                        {
                            var ex2 = (Exception)ci.Invoke(new object[] { "Worker thread threw an exception of this type", ex.InnerException });
                            throw ex2;
                        }
                    }
                }

                catch (ArgumentException)
                {
                    if (fileName.Length == 0)
                    {
                        Utility.ErrorBox(owner, PdnResources.GetString("LoadImage.Error.BlankFileName"));
                    }
                    else
                    {
                        Utility.ErrorBox(owner, PdnResources.GetString("LoadImage.Error.ArgumentException"));
                    }
                }

                catch (UnauthorizedAccessException)
                {
                    Utility.ErrorBox(owner, PdnResources.GetString("LoadImage.Error.UnauthorizedAccessException"));
                }

                catch (SecurityException)
                {
                    Utility.ErrorBox(owner, PdnResources.GetString("LoadImage.Error.SecurityException"));
                }

                catch (FileNotFoundException)
                {
                    Utility.ErrorBox(owner, PdnResources.GetString("LoadImage.Error.FileNotFoundException"));
                }

                catch (DirectoryNotFoundException)
                {
                    Utility.ErrorBox(owner, PdnResources.GetString("LoadImage.Error.DirectoryNotFoundException"));
                }

                catch (PathTooLongException)
                {
                    Utility.ErrorBox(owner, PdnResources.GetString("LoadImage.Error.PathTooLongException"));
                }

                catch (IOException)
                {
                    Utility.ErrorBox(owner, PdnResources.GetString("LoadImage.Error.IOException"));
                }

                catch (SerializationException)
                {
                    Utility.ErrorBox(owner, PdnResources.GetString("LoadImage.Error.SerializationException"));
                }

                catch (OutOfMemoryException)
                {
                    Utility.ErrorBox(owner, PdnResources.GetString("LoadImage.Error.OutOfMemoryException"));
                }

                catch (Exception)
                {
                    Utility.ErrorBox(owner, PdnResources.GetString("LoadImage.Error.Exception"));
                }

                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                        stream = null;
                    }
                }
            }

            return document;
        }

        protected override void HandleMouseWheel(Control sender, MouseEventArgs e)
        {
            if (ModifierKeys == Keys.Control)
            {
                double mouseDelta = (double)e.Delta / 120.0f;
                Rectangle visibleDocBoundsStart = this.VisibleDocumentBounds;
                Point mouseDocPt = this.MouseToDocument(sender, new Point(e.X, e.Y));
                RectangleF visibleDocDocRect1 = this.VisibleDocumentRectangleF;

                PointF mouseNPt = new PointF(
                    (mouseDocPt.X - visibleDocDocRect1.X) / visibleDocDocRect1.Width,
                    (mouseDocPt.Y - visibleDocDocRect1.Y) / visibleDocDocRect1.Height);

                const double factor = 1.12;
                double mouseFactor = Math.Pow(factor, Math.Abs(mouseDelta));

                if (e.Delta > 0)
                {
                    this.ZoomIn(mouseFactor);
                }
                else if (e.Delta < 0)
                {
                    this.ZoomOut(mouseFactor);
                }

                RectangleF visibleDocDocRect2 = this.VisibleDocumentRectangleF;

                PointF scrollPt2 = new PointF(
                    mouseDocPt.X - visibleDocDocRect2.Width * mouseNPt.X,
                    mouseDocPt.Y - visibleDocDocRect2.Height * mouseNPt.Y);

                this.DocumentScrollPositionF = scrollPt2;

                Rectangle visibleDocBoundsEnd = this.VisibleDocumentBounds;

                if (visibleDocBoundsEnd != visibleDocBoundsStart)
                {
                    // Make sure the screen updates, otherwise it can get a little funky looking
                    this.Update();
                }
            }

            base.HandleMouseWheel(sender, e);
        }

        #region Tools

        public static Type[] ToolTypes { get; private set; }
        public static ToolInfo[] ToolInfos { get; private set; }
        public static BaseTool ActiveTool { get; private set; }

        private static void InitializeTools()
        {
            ToolTypes = new[]
            {
                typeof (PanTool),
                typeof(PencilTool)
            };
        }

        private static void InitializeToolInfos()
        {
            int i = 0;
            ToolInfos = new ToolInfo[ToolTypes.Length];

            foreach (Type toolType in ToolTypes)
            {
                using (BaseTool tool = CreateTool(toolType, null))
                {
                    ToolInfos[i] = tool.Info;
                    ++i;
                }
            }
        }

        private static BaseTool CreateTool(Type toolType, DocumentWorkspace document)
        {
            //Create tool by reflection and relate with DocumentWorkspace
            ConstructorInfo ci = toolType.GetConstructor(new[] { typeof(DocumentWorkspace) });
            if (ci == null) throw new TypeLoadException();

            var tool = (BaseTool)ci.Invoke(new object[] { document });
            return tool;
        }

        public void SetTool(Type toolType)
        {
            if (ActiveTool != null)
            {
                ActiveTool.PerformDeactivate();
            }

            ActiveTool = CreateTool(toolType, this);
            ActiveTool.PerformActivate();
            this.Cursor = ActiveTool.Cursor;
        }

        public void UpdateCursor()
        {
            if (ActiveTool != null)
                this.Cursor = ActiveTool.Cursor;
        }

        #endregion

        #region ScratchSurface

        public bool IsScratchSurfaceBorrowed;
        private Surface _scratchSurface;

        public Surface BorrowScratchSurface()
        {
            if (IsScratchSurfaceBorrowed)
            {
                throw new InvalidOperationException();
            }

            this.IsScratchSurfaceBorrowed = true;

            return this._scratchSurface;
        }

        protected override void OnDocumentChanged()
        {
            if (this._scratchSurface != null)
            {
                if (this.IsScratchSurfaceBorrowed)
                {
                    throw new InvalidOperationException();
                }

                if (Document == null || this._scratchSurface.Size != Document.Size)
                {
                    this._scratchSurface.Dispose();
                    this._scratchSurface = null;
                }
            }

            if (Document != null) this._scratchSurface = new Surface(Document.Size);

            base.OnDocumentChanged();
        }

        public void ReturnScratchSurface(Surface borrowedScratchSurface)
        {
            if (!this.IsScratchSurfaceBorrowed)
            {
                throw new InvalidOperationException("ScratchSurface wasn't borrowed");
            }

            if (this._scratchSurface != borrowedScratchSurface)
            {
                throw new InvalidOperationException("returned ScratchSurface doesn't match the real one");
            }

            this.IsScratchSurfaceBorrowed = false;
        }


        #endregion
    }
}