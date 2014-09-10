using System;
using System.Drawing;
using System.Windows.Forms;
using Photo.Net.Resource;
using Photo.Net.Tool.Core.Enums;
using Photo.Net.Tool.Documents;

namespace Photo.Net.Tool.Tools
{
    /// <summary>
    /// A hand to move document
    /// </summary>
    public class PanTool : BaseTool
    {
        public PanTool(DocumentWorkspace documentWorkspace)
            : base(documentWorkspace, PdnResources.GetImageResource("Icons.PanToolIcon.png"), PdnResources.GetString("PanTool.Name"), PdnResources.GetString("PanTool.HelpText"), 'r', false, ToolBarConfigItems.All)
        {
        }

        private bool _tracking;
        private PointF _lastLocation;
        private Cursor _cursorMouseDown;
        private Cursor _cursorMouseUp;
        private Cursor _cursorMouseInvalid;
        private int _ignoreMouseMove;

        private bool CanPan()
        {
            if (DocumentWorkspace.VisibleDocumentRectangleF.Size == Document.Size)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            _tracking = true;
            _lastLocation = e.Location;
            Cursor = CanPan() ? _cursorMouseDown : _cursorMouseInvalid;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            _tracking = false;
            Cursor = CanPan() ? _cursorMouseUp : _cursorMouseInvalid;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Cursor = CanPan() ? _cursorMouseUp : _cursorMouseInvalid;

            if (_tracking && e.Button == MouseButtons.Left)
            {
                var location = e.Location;

                var distance = new PointF(location.X - _lastLocation.X, location.Y - _lastLocation.Y);
                PointF offset = DocumentWorkspace.DocumentScrollPositionF;


                if (!distance.IsEmpty)
                {
                    offset.X -= distance.X;
                    offset.Y -= distance.Y;
                    DocumentWorkspace.DocumentScrollPositionF = offset;

                    Update();
                }
            }

        }

        protected override void OnActivate()
        {
            // cursor-action assignments
            this._cursorMouseDown = new Cursor(PdnResources.GetResourceStream("Cursors.PanToolCursorMouseDown.cur"));
            this._cursorMouseUp = new Cursor(PdnResources.GetResourceStream("Cursors.PanToolCursor.cur"));
            this._cursorMouseInvalid = new Cursor(PdnResources.GetResourceStream("Cursors.PanToolCursorInvalid.cur"));
            this.Cursor = _cursorMouseUp;
            base.OnActivate();
        }

        protected override void OnDeactivate()
        {
            if (_cursorMouseDown != null)
            {
                _cursorMouseDown.Dispose();
                _cursorMouseDown = null;
            }

            if (_cursorMouseUp != null)
            {
                _cursorMouseUp.Dispose();
                _cursorMouseUp = null;
            }

            if (_cursorMouseInvalid != null)
            {
                _cursorMouseInvalid.Dispose();
                _cursorMouseInvalid = null;
            }

            base.OnDeactivate();
        }
    }
}
