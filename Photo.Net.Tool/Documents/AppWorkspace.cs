/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Photo.Net.Tool.Documents
{
    public class AppWorkspace
        : UserControl
    {
        private DocumentWorkspace _initialWorkspace;

        private List<DocumentWorkspace> _documentWorkspaces = new List<DocumentWorkspace>();

        private readonly DockPanel _dockPanel = new DockPanel();

        public DocumentWorkspace ActiveDocumentWorkspace { get; private set; }

        public AppWorkspace(DocumentWorkspace initialWorkspace)
        {
            this._initialWorkspace = initialWorkspace;

            _dockPanel.Dock = DockStyle.Fill;
            _dockPanel.DocumentStyle = DocumentStyle.DockingWindow;
            this.Controls.Add(_dockPanel);
        }
    }
}
