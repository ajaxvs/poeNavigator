/*
 * User: ajaxvs
 * Date: 06.05.2018
 * Time: 18:24
 */
using System;
using System.Diagnostics;
using System.Text;
using System.Timers;
using System.Windows.Forms;

namespace poeNavigator.app
{
	public class CajApp {
		//================================================================================
		public const string appName = "poeNavigator";
		public const string appAbout = "This app shows last entered location layouts & tips." +
			"\nUse it for navigation & faster leveling." + //atm.
			"\n(White: entrance. Green: exit and main path. Blue: waypoint. Grey: optional area)" +
			"\n\nRequires:" +
			"\n# Aero Windows theme." +
			"\n# English PoE in \"Windowed Fullscreen\" mode." +
			"\n\nAuthors:" +
			"\n# original guide: Engineering Eternity" +
			"\n# notes: _treB" +
			"\n# software development: AjaxVS" + 
			"\n\nv.180510";
		public const string appAboutLink = "https://ajaxvs.github.io/poenavigator.html";
		//================================================================================
		private const string dataFolderName = "data/";
		private const string locationsFolderName = "locations/";		
		private const string configFileName = "config.ini";
		private const int updateTime = 500; //ms
		private const bool hideOnPoeBackground = true;
		private const string poeWindowTitle = "Path of Exile";
		//================================================================================
		static private CajConfig config;
		
		static private MainForm mainForm;
		static private frmOptions optionsForm;

		static private CajClient client;
		static private CajLocations locations;
		
		static private System.Timers.Timer timer;
		static private string dataDir;
		static private bool hiddenByForegroundCheck = false;
		static private bool hiddenByNotifyOption = false;
		//================================================================================
		public CajApp() {} //nn singleton
		//================================================================================
		static public MainForm start() {
			//data:
			validateDirs();
			loadConfig();
			
			//view:
			CajApp.mainForm = new MainForm(config);
			CajApp.optionsForm = new frmOptions(config);

			//logic:			
			string poeFolder = config.read(CajConfig.poeFolderPath);
			locations = new CajLocations(dataDir + locationsFolderName,
			                             mainForm.setLocationData, trace);
			locations.setPart(config.readInt(CajConfig.gamePart, 1));
			client = new CajClient(poeFolder, onLocationChange, trace);
			
			//first launch:
			if (poeFolder == "") {
				showOptions();
				optionsForm.onFirstLaunch();
			}
			
			//launch main timer:
			timer = new System.Timers.Timer(updateTime);
			timer.Elapsed += new ElapsedEventHandler(OnTimerEvent);
			timer.Enabled = true;
			
			tests();
			
			return mainForm;
		}
		//================================================================================
		static private void tests() {
			//.
		}
		//================================================================================
		static private void validateDirs() {
			//dataDir:
			string appPath = CajFuns.getAppPath();
			dataDir = appPath + dataFolderName;
			if (!System.IO.Directory.Exists(dataDir)) {
				dataDir = appPath + "..\\..\\" + dataFolderName; //dev path.
				if (!System.IO.Directory.Exists(dataDir)) {	
					appError("Can't find data folder. Did you unzip the archive?" + 
						"\n\nUnzip all and launch again.");
				}
			}
		}
		//================================================================================
		static private void loadConfig() {
			config = new CajConfig(dataDir + configFileName, trace);
		}
		//================================================================================
		static public void trace(string msg) {
			//TODO: log to file.
			Debug.WriteLine(msg);
		}
		//================================================================================
		static private void appError(string msg) {
			MessageBox.Show(msg,
					appName + " :: Error", 
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			end();
		}
		//================================================================================
		static public void end() {
			//need improvement:
			if (optionsForm != null) optionsForm.Close();
			if (mainForm != null) mainForm.Close();
			if (System.Windows.Forms.Application.MessageLoop) {
				System.Windows.Forms.Application.Exit();
			} else {
				System.Environment.Exit(1);
			}
		}
		//================================================================================
		static private void onLocationChange(string loc) {
			trace("new loc: " + loc);
			if (locations != null) {
				locations.onLocationChange(loc);
			}
		}
		//================================================================================
		static public void setGamePart(int part) {
			if (locations != null) {
				locations.setPart(part);
			}
			config.write(CajConfig.gamePart, part.ToString(), true);
		}
		//================================================================================
		static public void showByNotifyOption(bool show) {
			hiddenByNotifyOption = !show;
		}
		//================================================================================
		static private void checkWindows() {
			if (!hideOnPoeBackground) return;
			
			//hide overlay if PoE window isn't on front:
			string s = CajFuns.getForegroundWindowTitle();
			if (s == poeWindowTitle || s == appName) {
				if (hiddenByForegroundCheck) {
					trace("foreground show");
					hiddenByForegroundCheck = false;
					mainForm.setVisibilitySafe(true);
				}
			} else {
				if (!hiddenByForegroundCheck) {
					trace("foreground hide");
					hiddenByForegroundCheck = true;
					mainForm.setVisibilitySafe(false);
				}
			}
		}
		//================================================================================
		static public void showOptions() {
			optionsForm.showSafe();
		}
		//================================================================================
		static public void onOptionsChange(string poeFolderPath, string switchHotkey) {
			client.setFolder(poeFolderPath);
			mainForm.changeHotkey(switchHotkey);
			
			config.write(CajConfig.poeFolderPath, poeFolderPath, false);
			config.write(CajConfig.switchHotkey, switchHotkey, false);
			config.flush();
		}
		//================================================================================
		static private void OnTimerEvent(object source, ElapsedEventArgs e) {
			//main tick:			
			if (hiddenByNotifyOption) {
				return;
			}
			
			checkWindows();
			if (hiddenByForegroundCheck) {
				return;
			}
			
			client.update();
 		}
		//================================================================================
	}
}
