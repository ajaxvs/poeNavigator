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
			fillHotkeyVars();
			
			//start vars:
			this.txtPoeFolder.Text = config.read(CajConfig.poeFolderPath);			
			this.cbHotkey.Text = getUserAliasHotkey(config.read(CajConfig.switchHotkey));
			
			this.lblStatus.Text = "Game Part:\ncheck system tray menu."; 
			//rest text was moved to "About": it takes too much place, bad UX.
			
			this.lblLink.Text = "(c) " + CajApp.appAboutLink;
		}
		//================================================================================
		private void fillHotkeyVars() {
			//using Text. allow user to write any value.
			//make some examples from Enum.GetValues(typeof(Keys)):
			//var a = Enum.GetValues(typeof(Keys)); 
			//foreach (var k in a) CajApp.trace(k.ToString());
			
			cbHotkey.Items.Add("None");
			cbHotkey.Items.Add("Tilde"); //oemtilde.
			cbHotkey.Items.Add("Capital");
			cbHotkey.Items.Add("Space");
			cbHotkey.Items.Add("PageUp");
			cbHotkey.Items.Add("PageDown");
			cbHotkey.Items.Add("End");
			cbHotkey.Items.Add("Home");
			cbHotkey.Items.Add("Insert");
			cbHotkey.Items.Add("Delete");

			for (int i = 0; i <= 12; i++) {
				cbHotkey.Items.Add("F" + i.ToString());
			}
			for (int i = 0; i <= 9; i++) {
				cbHotkey.Items.Add("NumPad" + i.ToString());
			}
			for (char i = 'A'; i <= 'Z'; i++) {
				cbHotkey.Items.Add(i);
			}
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
			this.Show(); //enough atm.
		}
		//================================================================================
		public void onFirstLaunch() {
			string poeFolder = CajFuns.findPoeFolder();
			if (poeFolder != "") {
				txtPoeFolder.Text = poeFolder;
				CajApp.onOptionsChange(poeFolder, cbHotkey.Text);
			}
			txtPoeFolder.Focus(); //select it, so user can notice and correct it.
		}
		//================================================================================
		private string getUserAliasHotkey(string realHotkey) {
			string s = realHotkey;
			if (realHotkey == "Oemtilde") {
				s = "Tilde";
			}
			return s;
		}
		//================================================================================
		void ButSaveClick(object sender, EventArgs e) {			
			string sErr = "";
			string poeFolder = txtPoeFolder.Text.Trim();
			if (poeFolder == "" || !System.IO.Directory.Exists(poeFolder)) {
				sErr = "Can't find PoE folder.";
			}
			
			string sHotkey = cbHotkey.Text.Trim();
			if (sHotkey == "None") {
				sHotkey = "";
			}
			if (sHotkey == "Tilde") {
				sHotkey = "Oemtilde";
			}
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
