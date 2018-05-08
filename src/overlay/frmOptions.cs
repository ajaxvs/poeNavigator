/*
 * User: ajaxvs
 * Date: 08.05.2018
 * Time: 16:55
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using poeNavigator.app;

namespace poeNavigator
{
	public partial class frmOptions : Form {
		//================================================================================
		private string elementTitle;
		//================================================================================
		public frmOptions(CajConfig config) {
			InitializeComponent();	

			elementTitle = CajApp.appName + " :: Options";
			this.Text = elementTitle;
			this.lblStatus.Text = "";
			
			//start vars:
			this.txtPoeFolder.Text = config.read(CajConfig.poeFolderPath);			
			this.txtSwitchHotkey.Text = config.read(CajConfig.switchHotkey);	
			
			this.lblStatus.Text = "Don't forget:" +
				"\n# set \"Aero\" Windows theme." +
				"\n# Run PoE in \"Windowed Fullscreen\" mode." +
				"\n# change game part in notify icon menu." +
				"";
			
			this.lblLink.Text = "(c) " + CajApp.appAboutLink;
		}
		//================================================================================
		void AboutToolStripMenuItemClick(object sender, EventArgs e) {
			MessageBox.Show(CajApp.appAbout, CajApp.appName + " :: About", 
			                MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		//================================================================================
		void LblLinkLinkClicked(object sender, LinkClickedEventArgs e) {
			try {
				System.Diagnostics.Process.Start(e.LinkText);
			} catch (Exception ex) {
				//.
			}
		}
		//================================================================================
		void FrmOptionsFormClosing(object sender, FormClosingEventArgs e) {
			e.Cancel = true;
			this.Hide();
		}
		//================================================================================
		public void showSafe() {
			this.Show();
		}
		//================================================================================
		void ButSaveClick(object sender, EventArgs e) {			
			string sErr = "";
			string poeFolder = txtPoeFolder.Text.Trim();
			if (poeFolder == "" || !System.IO.Directory.Exists(poeFolder)) {
				sErr = "Can't find PoE folder.";
			}
			
			//TODO: hotkeys listbox or check keydown event or something else. atm = temp method.
			string sHotkey = txtSwitchHotkey.Text.Trim();
			if (sHotkey != "") {
				bool isOkHotkey = true;
				try {
					int h = (int)Enum.Parse(typeof(Keys), sHotkey);
					if (h == 0) {
						isOkHotkey = false;
					}
				} catch (Exception ex) {
					isOkHotkey = false;
				}
				if (!isOkHotkey) {
					sErr = "Incorrect hotkey." +
						"\n\nCorrect examples:\nCapital\nK\nF5" +
						"\n\nSet text field empty to disable hotkey";				
				}
			}
			
			if (sErr == "") {
				CajApp.onOptionsChange(poeFolder, sHotkey);
				this.Hide();
			} else {
				MessageBox.Show(sErr, elementTitle, 
                				MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}
		//================================================================================
		void ExitStripMenuItem1Click(object sender, EventArgs e) {
			CajApp.end();
		}
		//================================================================================
	}
}
