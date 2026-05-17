/* Archive Cache Manager — fork additions
 * Copyright (C) 2026 Kyuss82
 *
 * Unified hub for the four bulk PKG / VPK operations. Until v2.62 these were
 * four separate Forms reached from four separate right-click menu items:
 *   • PS3 Update Builder       (Ps3BulkUpdateBuilderWindow, no context)
 *   • PSP Update Builder       (Ps3BulkUpdateBuilderWindow with SonyUpdateContext.ForPsp())
 *   • PS3 Raw PKG Export       (Ps3BulkPkgExportWindow)
 *   • PSV VPK Builder          (PsvBulkVpkBuilderWindow)
 * All four backends are kept verbatim — this hub just hosts each one as a
 * `TopLevel = false` embedded Form inside a TabPage, so the user can hop
 * between them without juggling four separate windows. Each child's [Close]
 * button still closes the hub (we listen to FormClosed on every embedded form
 * and dispose the hub when any of them closes).
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Unbroken.LaunchBox.Plugins;
using Unbroken.LaunchBox.Plugins.Data;

namespace ArchiveCacheManager
{
    public enum BulkOperationTab { Ps3Update, PspUpdate, Ps3RawExport, PsvVpk }

    public class BulkOperationsWindow : Form
    {
        private TabControl mTabs;
        private readonly Dictionary<BulkOperationTab, TabPage> mTabByOp = new Dictionary<BulkOperationTab, TabPage>();
        private readonly Dictionary<BulkOperationTab, Form> mChildByOp = new Dictionary<BulkOperationTab, Form>();

        public BulkOperationsWindow(BulkOperationTab initialTab = BulkOperationTab.Ps3Update, IGame[] preselected = null)
        {
            BuildUi(preselected);
            UserInterface.ApplyTheme(this);
            if (mTabByOp.TryGetValue(initialTab, out var page)) mTabs.SelectedTab = page;
        }

        private void BuildUi(IGame[] preselected)
        {
            Text = "Bulk Operations";
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(820, 640);
            ClientSize = new Size(960, 720);

            mTabs = new TabControl
            {
                Dock = DockStyle.Fill,
            };

            AddTab(BulkOperationTab.Ps3Update,    "PS3 Update Builder", new Ps3BulkUpdateBuilderWindow(preselected: preselected));
            AddTab(BulkOperationTab.PspUpdate,    "PSP Update Builder", new Ps3BulkUpdateBuilderWindow(SonyUpdateContext.ForPsp(), Config.MatchesPspPkgPlatform, preselected));
            AddTab(BulkOperationTab.Ps3RawExport, "PS3 Raw PKG Export", new Ps3BulkPkgExportWindow(preselected));
            AddTab(BulkOperationTab.PsvVpk,       "PSV VPK Builder",    new PsvBulkVpkBuilderWindow(preselected));

            Controls.Add(mTabs);
        }

        private void AddTab(BulkOperationTab op, string title, Form child)
        {
            var page = new TabPage(title);
            // Embed the child Form. TopLevel=false + Visible=true + Dock=Fill puts it inside
            // the TabPage, FormBorderStyle.None hides the (now-redundant) title bar, and the
            // [Close] button handler inside the child still fires Close() — we listen for
            // FormClosed and propagate it to the hub.
            child.TopLevel = false;
            child.FormBorderStyle = FormBorderStyle.None;
            child.Dock = DockStyle.Fill;
            child.FormClosed += (s, e) => { if (!IsDisposed) Close(); };
            page.Controls.Add(child);
            child.Show();
            mTabs.TabPages.Add(page);
            mTabByOp[op] = page;
            mChildByOp[op] = child;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Best-effort: dispose embedded children so their CancellationTokenSources
            // and any background tasks they own get released.
            foreach (var c in mChildByOp.Values)
            {
                try { if (!c.IsDisposed) c.Dispose(); } catch { }
            }
            base.OnFormClosing(e);
        }
    }
}
